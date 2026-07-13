namespace AIUsage.Core.Domain;

public sealed record ProviderId
{
    public ProviderId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record ProviderAccountId
{
    public ProviderAccountId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
