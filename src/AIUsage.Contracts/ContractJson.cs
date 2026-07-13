using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace AIUsage.Contracts;

public static class ContractJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new();
    private static readonly NullabilityInfoContext Nullability = new();

    public static T Deserialize<T>(string json)
    {
        var value = JsonSerializer.Deserialize<T>(json, SerializerOptions)
            ?? throw new JsonException($"{typeof(T).Name} decoded to null.");
        Validate(value);
        return value;
    }

    public static object Deserialize(string json, Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        var value = JsonSerializer.Deserialize(json, contractType, SerializerOptions)
            ?? throw new JsonException($"{contractType.Name} decoded to null.");
        Validate(value);
        return value;
    }

    public static string Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Validate(value);
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static string Serialize(object value, Type contractType)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(contractType);
        Validate(value);
        return JsonSerializer.Serialize(value, contractType, SerializerOptions);
    }

    private static void Validate(object value)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        ValidateObject(value, "$", visited);
    }

    private static void ValidateObject(object value, string path, HashSet<object> visited)
    {
        var type = value.GetType();
        if (type.IsValueType || value is string or JsonElement or SafeJsonMetadata)
        {
            return;
        }

        if (!visited.Add(value))
        {
            return;
        }

        if (value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    ValidateObject(item, $"{path}[{index}]", visited);
                }

                index++;
            }

            return;
        }

        if (type.Namespace != typeof(ContractJson).Namespace)
        {
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var propertyValue = property.GetValue(value);
            var nullability = Nullability.Create(property);
            if (propertyValue is null)
            {
                if (nullability.ReadState == NullabilityState.NotNull)
                {
                    throw new JsonException($"Required contract value is null at {path}.{property.Name}.");
                }

                continue;
            }

            ValidateObject(propertyValue, $"{path}.{property.Name}", visited);
        }
    }
}
