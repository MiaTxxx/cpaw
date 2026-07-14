import Foundation
@testable import QuotaBackend

enum CodexUsageNormalizerGoldenScenarios {
    struct ScenarioInput: Codable {
        let primary: RawQuotaWindow?
        let secondary: RawQuotaWindow?
        let tertiary: RawQuotaWindow?
    }

    struct WindowProjection: Codable {
        let label: String
        let remainingPercent: Double?
        let usedPercent: Double?
        let resetAt: String?
    }

    struct Projection: Codable {
        let windows: [WindowProjection]
        let remainingPercent: Double?
        let state: String
        let nextResetAt: String?
    }

    struct NamedScenario: Codable {
        let id: String
        let input: ScenarioInput
    }

    struct MatrixInput: Codable {
        let cases: [NamedScenario]
    }

    struct NamedProjection: Codable {
        let id: String
        let summary: Projection
    }

    struct MatrixExpected: Codable {
        let cases: [NamedProjection]
    }

    static let allWindowsOrder = ScenarioInput(
        primary: window(
            remainingPercent: 72,
            resetAt: "2030-01-03T03:04:05Z",
            resetDescription: "Primary reset"
        ),
        secondary: window(
            usedPercent: 56,
            remainingPercent: 44,
            resetAt: "2030-01-09T03:04:05Z",
            resetDescription: "Secondary reset"
        ),
        tertiary: window(
            usedPercent: 91,
            remainingPercent: 9,
            resetAt: "2030-01-04T03:04:05Z",
            resetDescription: "Code review reset"
        )
    )

    static let secondaryResetFallback = ScenarioInput(
        primary: window(
            usedPercent: 20,
            remainingPercent: 80,
            resetDescription: "Primary reset unavailable"
        ),
        secondary: window(
            usedPercent: 40,
            remainingPercent: 60,
            resetAt: "2030-01-09T03:04:05Z",
            resetDescription: "Secondary reset"
        ),
        tertiary: window(
            usedPercent: 80,
            remainingPercent: 20,
            resetAt: "2030-01-04T03:04:05Z",
            resetDescription: "Earlier code review reset"
        )
    )

    static let missingSlotMatrix = MatrixInput(
        cases: [
            NamedScenario(
                id: "none",
                input: ScenarioInput(primary: nil, secondary: nil, tertiary: nil)
            ),
            NamedScenario(
                id: "primary-only",
                input: ScenarioInput(
                    primary: window(
                        remainingPercent: 80,
                        resetAt: "2030-01-03T03:04:05Z",
                        resetDescription: "Primary reset"
                    ),
                    secondary: nil,
                    tertiary: nil
                )
            ),
            NamedScenario(
                id: "secondary-only",
                input: ScenarioInput(
                    primary: nil,
                    secondary: window(
                        remainingPercent: 48,
                        resetAt: "2030-01-09T03:04:05Z",
                        resetDescription: "Secondary reset"
                    ),
                    tertiary: nil
                )
            ),
            NamedScenario(
                id: "tertiary-only",
                input: ScenarioInput(
                    primary: nil,
                    secondary: nil,
                    tertiary: window(
                        remainingPercent: 15,
                        resetAt: "2030-01-04T03:04:05Z",
                        resetDescription: "Code review reset"
                    )
                )
            ),
            NamedScenario(
                id: "primary-and-tertiary",
                input: ScenarioInput(
                    primary: window(
                        remainingPercent: 70,
                        resetDescription: "Primary reset unavailable"
                    ),
                    secondary: nil,
                    tertiary: window(
                        remainingPercent: 8,
                        resetAt: "2030-01-04T03:04:05Z",
                        resetDescription: "Code review reset"
                    )
                )
            ),
        ]
    )

    static func normalize(_ input: ScenarioInput) -> Projection {
        var usage = ProviderUsage(provider: "codex", label: "Codex", accountId: "fixture-account")
        usage.fetchedAt = "2030-01-02T03:04:05Z"
        usage.accountEmail = "alice@example.test"
        usage.accountPlan = "plus"
        usage.extra["workspaceType"] = AnyCodable("Personal")
        usage.primary = input.primary
        usage.secondary = input.secondary
        usage.tertiary = input.tertiary

        let summary = UsageNormalizer.normalize(provider: FixtureCodexProvider(), usage: usage)
        return Projection(
            windows: summary.windows.map { window in
                WindowProjection(
                    label: window.label,
                    remainingPercent: window.remainingPercent,
                    usedPercent: window.usedPercent,
                    resetAt: window.resetAt
                )
            },
            remainingPercent: summary.remainingPercent,
            state: state(summary),
            nextResetAt: summary.nextResetAt
        )
    }

    static func normalizeMatrix(_ input: MatrixInput) -> MatrixExpected {
        MatrixExpected(
            cases: input.cases.map { scenario in
                NamedProjection(id: scenario.id, summary: normalize(scenario.input))
            }
        )
    }

    private static func state(_ summary: ProviderSummary) -> String {
        summary.status == "healthy" && summary.statusLabel == "Active"
            ? "active"
            : summary.status
    }

    private static func window(
        usedPercent: Double? = nil,
        remainingPercent: Double? = nil,
        resetAt: String? = nil,
        resetDescription: String? = nil
    ) -> RawQuotaWindow {
        var window = RawQuotaWindow()
        window.usedPercent = usedPercent
        window.remainingPercent = remainingPercent
        window.resetAt = resetAt
        window.resetDescription = resetDescription
        return window
    }

    private struct FixtureCodexProvider: ProviderFetcher {
        let id = "codex"
        let displayName = "Codex"
        let description = "Codex normalization fixture"

        func fetchUsage() async throws -> ProviderUsage {
            throw ProviderError("fixture_only", "Fixture provider does not fetch usage.")
        }
    }
}
