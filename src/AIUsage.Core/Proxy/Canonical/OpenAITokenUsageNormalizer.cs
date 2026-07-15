namespace AIUsage.Core.Proxy.Canonical;

internal static class OpenAITokenUsageNormalizer
{
    public static long? SelectCachedTokens(long? flatCachedTokens, long? nestedCachedTokens) =>
        flatCachedTokens ?? nestedCachedTokens;

    public static long CalculateEffectiveInputTokens(
        long promptTokens,
        long? explicitMissTokens,
        long? cachedTokens) =>
        explicitMissTokens ?? Math.Max(promptTokens - (cachedTokens ?? 0), 0);
}
