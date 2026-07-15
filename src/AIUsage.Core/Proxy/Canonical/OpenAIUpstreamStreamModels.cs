namespace AIUsage.Core.Proxy.Canonical;

public abstract record OpenAIUpstreamStreamEvent;

public sealed record OpenAIUpstreamTextDelta(string Text) : OpenAIUpstreamStreamEvent;

public sealed record OpenAIUpstreamReasoningSummaryDelta(string Text) : OpenAIUpstreamStreamEvent;

public sealed record OpenAIUpstreamToolCallStarted(
    long UpstreamIndex,
    string Id,
    string Name) : OpenAIUpstreamStreamEvent;

public sealed record OpenAIUpstreamToolCallArgumentsDelta(
    long UpstreamIndex,
    string ArgumentsDelta) : OpenAIUpstreamStreamEvent;

public sealed record OpenAIUpstreamCompleted(
    string? FinishReason,
    OpenAIUpstreamUsage? Usage) : OpenAIUpstreamStreamEvent;

public sealed record OpenAIUpstreamUsage(
    long PromptTokens,
    long CompletionTokens,
    long TotalTokens,
    long? PromptCacheHitTokens,
    long? PromptCacheMissTokens,
    long? CachedTokens)
{
    public long? EffectiveCachedTokens => OpenAITokenUsageNormalizer.SelectCachedTokens(
        PromptCacheHitTokens,
        CachedTokens);

    public long EffectiveInputTokens => OpenAITokenUsageNormalizer.CalculateEffectiveInputTokens(
        PromptTokens,
        PromptCacheMissTokens,
        EffectiveCachedTokens);
}
