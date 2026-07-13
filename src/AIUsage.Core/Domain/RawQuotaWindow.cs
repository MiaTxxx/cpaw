namespace AIUsage.Core.Domain;

public sealed record RawQuotaWindow
{
    public RawQuotaWindow(RawQuotaWindowSlot slot)
    {
        if (!Enum.IsDefined(slot))
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }

        Slot = slot;
    }

    public RawQuotaWindowSlot Slot { get; }

    public double? UsedPercent { get; init; }

    public double? RemainingPercent { get; init; }

    public ProviderTimestamp? ResetAt { get; init; }

    public string? ResetDescription { get; init; }

    public long? Entitlement { get; init; }

    public long? Remaining { get; init; }

    public bool? Unlimited { get; init; }

    public string? Label { get; init; }
}

public enum RawQuotaWindowSlot
{
    Primary,
    Secondary,
    Tertiary,
}
