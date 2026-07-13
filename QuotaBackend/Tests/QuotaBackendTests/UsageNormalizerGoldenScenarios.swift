import Foundation
@testable import QuotaBackend

enum UsageNormalizerGoldenScenarios {
    struct WindowInput: Codable {
        let label: String
        let window: RawQuotaWindow
    }

    static let percentDerived = WindowInput(
        label: "5h Window",
        window: rawWindow(
            remainingPercent: 72,
            resetAt: "2030-01-03T03:04:05Z",
            resetDescription: "Resets after the fixture window"
        )
    )

    static let percentExplicit = WindowInput(
        label: "Weekly Window",
        window: rawWindow(
            usedPercent: 7,
            remainingPercent: 80,
            resetDescription: "Explicit provider usage wins"
        )
    )

    static let entitlementUnlimited = WindowInput(
        label: "Premium Requests",
        window: rawWindow(
            usedPercent: 91,
            remainingPercent: 9,
            resetAt: "2030-01-04T03:04:05Z",
            resetDescription: "Synthetic unlimited entitlement",
            entitlement: 1_000,
            remaining: 90,
            unlimited: true
        )
    )

    static let entitlementMissingValues = WindowInput(
        label: "Standard Requests",
        window: rawWindow()
    )

    static let quotaUnlimited = WindowInput(
        label: "Completions",
        window: rawWindow(unlimited: true)
    )

    static let quotaMissingUsed = WindowInput(
        label: "Model Family",
        window: rawWindow(
            remainingPercent: 55,
            resetDescription: "Provider omitted used percent"
        )
    )

    static func createPercentWindow(_ input: WindowInput) -> WindowInfo {
        UsageNormalizer.createPercentWindow(label: input.label, window: input.window)
    }

    static func createEntitlementWindow(_ input: WindowInput) -> WindowInfo {
        UsageNormalizer.createEntitlementWindow(label: input.label, window: input.window)
    }

    static func createQuotaWindow(_ input: WindowInput) -> WindowInfo {
        UsageNormalizer.createQuotaWindow(label: input.label, window: input.window)
    }

    struct SmallestRemainingInput: Codable {
        let windows: [WindowInfo]
    }

    struct SmallestRemainingExpected: Codable {
        let remainingPercent: Double?
    }

    static let smallestRemaining = SmallestRemainingInput(
        windows: [
            normalizedWindow(label: "Unknown", remainingPercent: nil),
            normalizedWindow(label: "Primary", remainingPercent: 42),
            normalizedWindow(label: "Tightest", remainingPercent: 17.5),
            normalizedWindow(label: "Secondary", remainingPercent: 30),
        ]
    )

    static func pickSmallestRemaining(_ input: SmallestRemainingInput) -> SmallestRemainingExpected {
        SmallestRemainingExpected(
            remainingPercent: UsageNormalizer.pickSmallestRemaining(input.windows)
        )
    }

    struct StatusMatrixInput: Codable {
        let remainingPercentages: [Double?]
    }

    struct StatusMatrixExpected: Codable {
        struct Entry: Codable {
            let remainingPercent: Double?
            let status: String
            let statusLabel: String
        }

        let entries: [Entry]
    }

    static let statusBoundaries = StatusMatrixInput(
        remainingPercentages: [nil, 0, 12, 12.1, 30, 30.1, 100]
    )

    static func resolveStatuses(_ input: StatusMatrixInput) -> StatusMatrixExpected {
        StatusMatrixExpected(
            entries: input.remainingPercentages.map { remainingPercent in
                let (status, statusLabel) = UsageNormalizer.resolveStatus(remainingPercent)
                return StatusMatrixExpected.Entry(
                    remainingPercent: remainingPercent,
                    status: status,
                    statusLabel: statusLabel
                )
            }
        )
    }

    private static func rawWindow(
        usedPercent: Double? = nil,
        remainingPercent: Double? = nil,
        resetAt: String? = nil,
        resetDescription: String? = nil,
        entitlement: Int? = nil,
        remaining: Int? = nil,
        unlimited: Bool? = nil
    ) -> RawQuotaWindow {
        var window = RawQuotaWindow()
        window.usedPercent = usedPercent
        window.remainingPercent = remainingPercent
        window.resetAt = resetAt
        window.resetDescription = resetDescription
        window.entitlement = entitlement
        window.remaining = remaining
        window.unlimited = unlimited
        return window
    }

    private static func normalizedWindow(
        label: String,
        remainingPercent: Double?
    ) -> WindowInfo {
        WindowInfo(
            label: label,
            remainingPercent: remainingPercent,
            usedPercent: nil,
            value: "Synthetic fixture",
            note: "Synthetic fixture",
            resetAt: nil
        )
    }
}
