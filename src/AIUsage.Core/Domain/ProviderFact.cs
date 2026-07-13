using System.Collections.Immutable;

namespace AIUsage.Core.Domain;

public abstract record ProviderFact
{
    private ProviderFact()
    {
    }

    public static NullValue Null { get; } = new();

    public static BooleanValue FromBoolean(bool value) => new(value);

    public static IntegerValue FromInteger(long value) => new(value);

    public static NumberValue FromNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Provider fact numbers must be finite.");
        }

        return new NumberValue(value);
    }

    public static StringValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new StringValue(value);
    }

    public static ArrayValue FromArray(IEnumerable<ProviderFact> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var copy = items.ToImmutableArray();
        if (copy.Any(item => item is null))
        {
            throw new ArgumentException("Provider fact arrays cannot contain null references.", nameof(items));
        }

        return new ArrayValue(copy);
    }

    public static ObjectValue FromObject(IEnumerable<KeyValuePair<string, ProviderFact>> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var copy = ImmutableDictionary.CreateBuilder<string, ProviderFact>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (property.Key is null || property.Value is null)
            {
                throw new ArgumentException(
                    "Provider fact objects cannot contain null keys or values.",
                    nameof(properties));
            }

            copy.Add(property.Key, property.Value);
        }

        return new ObjectValue(copy.ToImmutable());
    }

    public sealed record NullValue : ProviderFact
    {
        internal NullValue()
        {
        }
    }

    public sealed record BooleanValue : ProviderFact
    {
        internal BooleanValue(bool value) => Value = value;

        public bool Value { get; }
    }

    public sealed record IntegerValue : ProviderFact
    {
        internal IntegerValue(long value) => Value = value;

        public long Value { get; }
    }

    public sealed record NumberValue : ProviderFact
    {
        internal NumberValue(double value) => Value = value;

        public double Value { get; }
    }

    public sealed record StringValue : ProviderFact
    {
        internal StringValue(string value) => Value = value;

        public string Value { get; }
    }

    public sealed record ArrayValue : ProviderFact
    {
        internal ArrayValue(ImmutableArray<ProviderFact> items) => Items = items;

        public ImmutableArray<ProviderFact> Items { get; }

        public bool Equals(ArrayValue? other) =>
            other is not null && Items.SequenceEqual(other.Items);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var item in Items)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }
    }

    public sealed record ObjectValue : ProviderFact
    {
        internal ObjectValue(ImmutableDictionary<string, ProviderFact> properties) =>
            Properties = properties;

        public ImmutableDictionary<string, ProviderFact> Properties { get; }

        public bool Equals(ObjectValue? other) =>
            other is not null
            && Properties.Count == other.Properties.Count
            && Properties.All(property =>
                other.Properties.TryGetValue(property.Key, out var value) && property.Value == value);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var property in Properties.OrderBy(property => property.Key, StringComparer.Ordinal))
            {
                hash.Add(property.Key, StringComparer.Ordinal);
                hash.Add(property.Value);
            }

            return hash.ToHashCode();
        }
    }
}
