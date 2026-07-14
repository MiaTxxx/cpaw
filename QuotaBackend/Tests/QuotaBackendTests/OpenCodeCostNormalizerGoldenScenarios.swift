import Foundation
@testable import QuotaBackend

enum OpenCodeCostNormalizerGoldenScenarios {
    typealias ScenarioInput = ClaudeCostNormalizerGoldenScenarios.ScenarioInput
    typealias Projection = ClaudeCostNormalizerGoldenScenarios.Projection

    // Synthetic negative inputs prove that OpenCode never projects accountEmail
    // or unpricedModels even if an upstream caller supplies them.
    static let periodsAccountRange = ScenarioInput(
        accountEmail: "  opencode@example.test \n",
        extra: [
            "today.estimatedCostUsd": AnyCodable(1),
            "today.totalTokens": AnyCodable(4_294_967_296),
            "today.key": AnyCodable("2030-01-02"),
            "currentWeek.estimatedCostUsd": AnyCodable(2.5),
            "currentWeek.totalTokens": AnyCodable(42.9),
            "currentWeek.key": AnyCodable("2029-12-31..2030-01-06"),
            "currentMonth.estimatedCostUsd": AnyCodable(3),
            "currentMonth.totalTokens": AnyCodable(300),
            "currentMonth.key": AnyCodable("2030-01"),
            "overall.estimatedCostUsd": AnyCodable(4.125),
            "overall.totalTokens": AnyCodable(4_294_967_300),
            "overall.rangeLabel": AnyCodable("2029-12-31..2030-01-02"),
            "overall.unpricedModels": array([
                AnyCodable("ignored-opencode-model"),
            ]),
        ]
    )

    static let modelBreakdowns = ScenarioInput(
        accountEmail: nil,
        extra: [
            "currentMonth.models": array([
                object([
                    "model": AnyCodable("opencode-beta"),
                    "totalTokens": AnyCodable(8.9),
                    "outputTokens": AnyCodable(5),
                    "cacheReadTokens": AnyCodable(2.9),
                    "cacheCreateTokens": AnyCodable(1),
                    "estimatedCostUsd": AnyCodable(0.5),
                    "percentage": AnyCodable(20),
                ]),
                object([
                    "model": AnyCodable("opencode-alpha"),
                    "totalTokens": AnyCodable(4_294_967_296),
                    "inputTokens": AnyCodable(3_000_000_000),
                    "outputTokens": AnyCodable(1_294_967_296),
                    "cacheReadTokens": AnyCodable("invalid"),
                    "estimatedCostUsd": AnyCodable(2),
                    "percentage": AnyCodable(80.5),
                ]),
                object([
                    "totalTokens": AnyCodable(999),
                    "estimatedCostUsd": AnyCodable(9.99),
                ]),
            ]),
            "today.models": array([
                object(["model": AnyCodable("opencode-today")]),
            ]),
            "currentWeek.models": array([]),
            "overall.models": array([
                object([
                    "model": AnyCodable("opencode-overall"),
                    "totalTokens": AnyCodable(4_294_967_300),
                    "inputTokens": AnyCodable(3_000_000_000),
                    "outputTokens": AnyCodable(1_000_000_000),
                    "cacheReadTokens": AnyCodable(200_000_000),
                    "cacheCreateTokens": AnyCodable(94_967_300),
                    "estimatedCostUsd": AnyCodable(4.125),
                    "percentage": AnyCodable(100),
                ]),
            ]),
        ]
    )

    static let timelinesDefaults = ScenarioInput(
        accountEmail: "opencode@example.test",
        extra: [
            "today.estimatedCostUsd": AnyCodable("invalid"),
            "today.totalTokens": AnyCodable("invalid"),
            "today.key": AnyCodable(7),
            "currentWeek.estimatedCostUsd": AnyCodable("invalid"),
            "currentWeek.totalTokens": AnyCodable("invalid"),
            "currentWeek.key": AnyCodable(false),
            "currentMonth.estimatedCostUsd": AnyCodable("invalid"),
            "currentMonth.totalTokens": AnyCodable("invalid"),
            "currentMonth.key": AnyCodable(9),
            "overall.estimatedCostUsd": AnyCodable("invalid"),
            "overall.totalTokens": AnyCodable("invalid"),
            "overall.rangeLabel": AnyCodable(20300102),
            "timeline.hourly": array([
                object([
                    "bucket": AnyCodable("2030-01-02T03:00:00Z"),
                    "label": AnyCodable("03:00"),
                    "usd": AnyCodable(2),
                    "tokens": AnyCodable(4_294_967_296),
                    "inputTokens": AnyCodable(1.9),
                    "outputTokens": AnyCodable(2),
                    "cacheCreateTokens": AnyCodable("invalid"),
                ]),
                object([
                    "bucket": AnyCodable("2030-01-02T04:00:00Z"),
                    "tokens": AnyCodable(99),
                ]),
            ]),
            "timeline.daily": array([
                object([
                    "bucket": AnyCodable("2030-01-02"),
                    "label": AnyCodable("Jan 2"),
                ]),
                object([
                    "bucket": AnyCodable(20300103),
                    "label": AnyCodable("Jan 3"),
                ]),
            ]),
            "timeline.byModel": array([
                object([
                    "model": AnyCodable("opencode-alpha"),
                    "hourly": array([]),
                    "daily": array([
                        object([
                            "bucket": AnyCodable("2030-01-02"),
                            "label": AnyCodable("Jan 2"),
                            "usd": AnyCodable(1.25),
                            "tokens": AnyCodable(125),
                        ]),
                    ]),
                ]),
                object([
                    "model": AnyCodable("opencode-beta"),
                    "hourly": array([
                        object([
                            "bucket": AnyCodable("2030-01-02T03:00:00Z"),
                            "label": AnyCodable("03:00"),
                            "usd": AnyCodable(0.75),
                            "tokens": AnyCodable(75),
                        ]),
                    ]),
                    "daily": array([]),
                ]),
                object([
                    "model": AnyCodable("empty-model"),
                    "hourly": array([
                        object(["bucket": AnyCodable("missing-label")]),
                    ]),
                    "daily": array([]),
                ]),
                object([
                    "hourly": array([
                        object([
                            "bucket": AnyCodable("2030-01-02T05:00:00Z"),
                            "label": AnyCodable("05:00"),
                        ]),
                    ]),
                ]),
            ]),
        ]
    )

    static func normalize(_ input: ScenarioInput) -> Projection {
        var usage = ProviderUsage(
            provider: "opencode",
            label: "OpenCode",
            accountId: "fixture-account",
            extra: input.extra
        )
        usage.fetchedAt = "2030-01-02T03:04:05Z"
        usage.accountEmail = input.accountEmail

        let summary = UsageNormalizer.normalize(provider: FixtureOpenCodeProvider(), usage: usage)
        return Projection(
            accountLabel: summary.accountLabel,
            category: summary.category,
            costSummary: summary.costSummary,
            unpricedModels: summary.unpricedModels
        )
    }

    private static func array(_ values: [AnyCodable]) -> AnyCodable {
        AnyCodable(values)
    }

    private static func object(_ values: [String: AnyCodable]) -> AnyCodable {
        AnyCodable(values)
    }

    private struct FixtureOpenCodeProvider: ProviderFetcher {
        let id = "opencode"
        let displayName = "OpenCode"
        let description = "OpenCode cost normalization fixture"

        func fetchUsage() async throws -> ProviderUsage {
            throw ProviderError("fixture_only", "Fixture provider does not fetch usage.")
        }
    }
}
