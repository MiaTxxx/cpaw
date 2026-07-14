using AIUsage.Core.Domain;
using Xunit;

namespace AIUsage.Core.Tests;

public sealed class ProviderUsageSummaryTests
{
    [Fact]
    public void Summary_combines_snapshot_identity_with_normalized_quota()
    {
        var providerId = new ProviderId("codex");
        var account = ProviderAccountReference.Create(
            new ProviderAccountId("fixture-account"),
            "alice@example.test",
            "Fixture User",
            "alice",
            "Pro");
        var observedAt = ProviderTimestamp.Parse("2030-01-02T03:04:05Z");
        var source = UsageSource.Create("local", "auth-file", roots: ["/fixture/root"]);
        var rawWindow = new RawQuotaWindow(RawQuotaWindowSlot.Primary)
        {
            RemainingPercent = 72,
        };
        var raw = RawUsageSnapshot.Create(
            providerId,
            "Codex Fixture",
            account,
            observedAt,
            source,
            [rawWindow],
            []);
        var quota = QuotaWindowNormalizer.Normalize(
        [
            new QuotaWindowNormalizationInput(
                "5h Window",
                rawWindow,
                QuotaWindowInterpretation.Percent),
        ]);

        var summary = ProviderUsageSummary.Create(raw, quota);

        Assert.Equal(providerId, summary.ProviderId);
        Assert.Equal(account, summary.Account);
        Assert.Equal(observedAt, summary.ObservedAt);
        Assert.Equal(source, summary.Source);
        Assert.Equal(72, summary.Quota.RemainingPercent);
        Assert.Equal(QuotaWindowState.Healthy, summary.Quota.State);
        Assert.Equal("5h Window", Assert.Single(summary.Quota.Windows).Label);
    }

    [Fact]
    public void Summary_requires_both_raw_and_normalized_inputs()
    {
        var raw = RawUsageSnapshot.Create(
            new ProviderId("fixture"),
            "Fixture",
            null,
            ProviderTimestamp.Parse("2030-01-02T03:04:05Z"),
            null,
            [],
            []);
        var quota = QuotaWindowNormalizer.Normalize([]);

        Assert.Throws<ArgumentNullException>(() => ProviderUsageSummary.Create(null!, quota));
        Assert.Throws<ArgumentNullException>(() => ProviderUsageSummary.Create(raw, null!));
    }

    [Fact]
    public void Summary_rejects_quota_derived_from_a_different_snapshot()
    {
        var foreignWindow = new RawQuotaWindow(RawQuotaWindowSlot.Primary)
        {
            RemainingPercent = 42,
        };
        var equalWindowFromTargetSnapshot = new RawQuotaWindow(RawQuotaWindowSlot.Primary)
        {
            RemainingPercent = 42,
        };
        var raw = RawUsageSnapshot.Create(
            new ProviderId("fixture"),
            "Fixture",
            null,
            ProviderTimestamp.Parse("2030-01-02T03:04:05Z"),
            null,
            [equalWindowFromTargetSnapshot],
            []);
        var quota = QuotaWindowNormalizer.Normalize(
        [
            new QuotaWindowNormalizationInput(
                "Foreign Window",
                foreignWindow,
                QuotaWindowInterpretation.Percent),
        ]);

        Assert.Throws<ArgumentException>(() => ProviderUsageSummary.Create(raw, quota));
    }
}
