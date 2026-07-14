using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsage.Core.Proxy.Protocols;

public static class WireJson
{
    private const string WireNamespacePrefix = "AIUsage.Core.Proxy.Protocols";
    private static readonly Assembly WireAssembly = typeof(WireJson).Assembly;
    private static readonly JsonSerializerOptions SerializerOptions = new();
    private static readonly NullabilityInfoContext Nullability = new();
    private static readonly object NullabilityGate = new();

    public static T Deserialize<T>(string json) =>
        (T)Deserialize(json, typeof(T));

    public static T Deserialize<T>(ReadOnlySpan<byte> utf8Json) =>
        (T)Deserialize(utf8Json, typeof(T));

    public static T Deserialize<T>(JsonElement json) =>
        (T)Deserialize(json, typeof(T));

    internal static T Deserialize<T>(JsonElement json, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return (T)ExecuteDeserialize(typeof(T), () => json.Deserialize<T>(options));
    }

    public static object Deserialize(string json, Type contractType)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(contractType);
        EnsureSupportedRootType(contractType);
        return ExecuteDeserialize(
            contractType,
            () => JsonSerializer.Deserialize(json, contractType, SerializerOptions));
    }

    public static object Deserialize(ReadOnlySpan<byte> utf8Json, Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        EnsureSupportedRootType(contractType);
        return ExecuteDeserialize(utf8Json, contractType);
    }

    public static object Deserialize(JsonElement json, Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        EnsureSupportedRootType(contractType);
        return ExecuteDeserialize(
            contractType,
            () => json.Deserialize(contractType, SerializerOptions));
    }

    private static object ExecuteDeserialize(Type contractType, Func<object?> deserialize)
    {
        try
        {
            return FinishDeserialize(deserialize(), contractType);
        }
        catch (WireJsonException)
        {
            throw;
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            throw DecodeFailure(contractType);
        }
    }

    private static object ExecuteDeserialize(ReadOnlySpan<byte> utf8Json, Type contractType)
    {
        try
        {
            return FinishDeserialize(
                JsonSerializer.Deserialize(utf8Json, contractType, SerializerOptions),
                contractType);
        }
        catch (WireJsonException)
        {
            throw;
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            throw DecodeFailure(contractType);
        }
    }

    private static object FinishDeserialize(object? value, Type contractType)
    {
        if (value is null)
        {
            throw NullValue(contractType, "$", contractType);
        }

        ValidateObjectGraph(value, contractType);
        return value;
    }

    private static void ValidateObjectGraph(object value, Type contractType)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        ValidateValue(value, contractType, nullability: null, "$", contractType, visited, required: true);
    }

    private static void ValidateValue(
        object? value,
        Type declaredType,
        NullabilityInfo? nullability,
        string path,
        Type contractType,
        HashSet<object> visited,
        bool required)
    {
        if (value is null)
        {
            if (required || IsNonNullable(declaredType, nullability))
            {
                throw NullValue(contractType, path, declaredType);
            }

            return;
        }

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                && (required || IsNonNullable(declaredType, nullability)))
            {
                throw NullValue(contractType, path, declaredType);
            }

            return;
        }

        var runtimeType = value.GetType();
        if (runtimeType.IsValueType || value is string)
        {
            return;
        }

        if (!visited.Add(value))
        {
            return;
        }

        if (value is IEnumerable enumerable)
        {
            if (!IsFrameworkCollectionType(declaredType))
            {
                return;
            }

            var elementType = GetEnumerableElementType(declaredType) ?? typeof(object);
            var elementNullability = GetEnumerableElementNullability(nullability);
            var index = 0;
            foreach (var item in enumerable)
            {
                ValidateValue(
                    item,
                    elementType,
                    elementNullability,
                    $"{path}[{index}]",
                    contractType,
                    visited,
                    required: true);
                index++;
            }

            return;
        }

        if (!IsWireType(runtimeType))
        {
            return;
        }

        foreach (var property in runtimeType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod is null
                || property.GetIndexParameters().Length != 0
                || property.IsDefined(typeof(JsonExtensionDataAttribute), inherit: true))
            {
                continue;
            }

            var propertyNullability = GetNullability(property);
            ValidateValue(
                property.GetValue(value),
                property.PropertyType,
                propertyNullability,
                $"{path}.{property.Name}",
                contractType,
                visited,
                required: false);
        }
    }

    private static bool IsNonNullable(Type type, NullabilityInfo? nullability)
    {
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) is null;
        }

        return nullability?.ReadState == NullabilityState.NotNull;
    }

    private static NullabilityInfo GetNullability(PropertyInfo property)
    {
        lock (NullabilityGate)
        {
            return Nullability.Create(property);
        }
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumerableType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces()
                .FirstOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerableType?.GetGenericArguments()[0];
    }

    private static NullabilityInfo? GetEnumerableElementNullability(NullabilityInfo? nullability) =>
        nullability?.ElementType ?? nullability?.GenericTypeArguments.FirstOrDefault();

    private static void EnsureSupportedRootType(Type contractType)
    {
        if (!IsSupportedRootType(contractType))
        {
            throw new ArgumentException(
                "Type must be a closed Core proxy wire contract.",
                nameof(contractType));
        }
    }

    private static bool IsSupportedRootType(Type type) =>
        IsWireType(type)
        && !type.ContainsGenericParameters
        && type.GetGenericArguments().All(IsSupportedRootType);

    private static bool IsWireType(Type type)
    {
        if (type.Assembly != WireAssembly)
        {
            return false;
        }

        var typeNamespace = type.Namespace;
        return string.Equals(typeNamespace, WireNamespacePrefix, StringComparison.Ordinal)
            || typeNamespace?.StartsWith($"{WireNamespacePrefix}.", StringComparison.Ordinal) == true;
    }

    private static bool IsFrameworkCollectionType(Type type) =>
        type.IsArray
        || (type.Assembly == typeof(IEnumerable).Assembly
            && GetEnumerableElementType(type) is not null);

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or StackOverflowException or AccessViolationException;

    private static WireJsonException DecodeFailure(Type contractType) =>
        new(contractType, $"Wire JSON could not be decoded as {contractType.Name}.");

    private static WireJsonException NullValue(Type contractType, string path, Type valueType) =>
        new(contractType, $"Wire JSON contains null for non-null {valueType.Name}.", path);
}
