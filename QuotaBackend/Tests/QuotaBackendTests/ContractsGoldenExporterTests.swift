import CryptoKit
import Foundation
import XCTest
@testable import QuotaBackend

final class ContractsGoldenExporterTests: XCTestCase {
    func testCatalogEncodesDeterministically() throws {
        let cases = try ContractsV1GoldenCatalog.makeCases()
        XCTAssertEqual(cases.count, 17)

        for fixtureCase in cases {
            let first = try fixtureCase.render()
            let second = try fixtureCase.render()
            XCTAssertEqual(first, second, fixtureCase.id)
            XCTAssertNoThrow(try JSONSerialization.jsonObject(with: first), fixtureCase.id)
        }
    }

    func testExportContractsV1Goldens() throws {
        guard let outputPath = ProcessInfo.processInfo.environment["AIUSAGE_GOLDEN_OUTPUT"],
              !outputPath.isEmpty else {
            throw XCTSkip("Set AIUSAGE_GOLDEN_OUTPUT to export contract fixtures")
        }

        try GoldenFixtureWriter.write(
            cases: ContractsV1GoldenCatalog.makeCases(),
            outputRoot: URL(fileURLWithPath: outputPath, isDirectory: true)
        )
    }
}

private enum ContractsV1GoldenCatalog {
    private static let fixedTime = "2030-01-02T03:04:05Z"

    static func makeCases() throws -> [GoldenFixtureCase] {
        let credential = try makeCredential()
        let usage = makeUsage()
        let summary = makeSummary(usage: usage)
        let success = ProviderResult(
            id: "codex:fixture-account",
            providerId: "codex",
            accountId: "fixture-account",
            ok: true,
            usage: usage,
            summary: summary
        )
        let failure = ProviderResult(
            id: "warp:fixture",
            providerId: "warp",
            ok: false,
            error: "[not_logged_in] Fixture credentials are unavailable"
        )
        let snapshot = makeDashboardSnapshot(success: success, failure: failure)
        let claudeRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestJSON
        )
        let claudeResponse = try decode(
            ClaudeMessageResponse.self,
            from: ContractsProxyGoldenInputs.claudeMessageResponseJSON
        )
        let claudeStreamDelta = try decode(
            ClaudeContentBlockDeltaEvent.self,
            from: ContractsProxyGoldenInputs.claudeContentBlockDeltaJSON
        )
        let openAIChatRequest = try decode(
            OpenAIChatCompletionRequest.self,
            from: ContractsProxyGoldenInputs.openAIChatRequestJSON
        )
        let openAIChatResponse = try decode(
            OpenAIChatCompletionResponse.self,
            from: ContractsProxyGoldenInputs.openAIChatResponseJSON
        )
        let openAIChatStreamChunk = try decode(
            OpenAIStreamChunk.self,
            from: ContractsProxyGoldenInputs.openAIChatStreamChunkJSON
        )
        let codexResponsesRequest = try decode(
            OpenAIResponsesRequest.self,
            from: ContractsProxyGoldenInputs.codexResponsesRequestJSON
        )
        let codexResponsesResponse = try decode(
            OpenAIResponsesResponse.self,
            from: ContractsProxyGoldenInputs.codexResponsesResponseJSON
        )
        let codexResponsesCompleted = try decode(
            OpenAIResponsesCompletedEvent.self,
            from: ContractsProxyGoldenInputs.codexResponsesCompletedEventJSON
        )

        return [
            .roundTrip(
                id: "contracts/account/account-credential-all-fields",
                path: "contracts/account/account-credential-all-fields.json",
                value: credential
            ),
            .roundTrip(
                id: "contracts/dashboard/dashboard-snapshot-mixed",
                path: "contracts/dashboard/dashboard-snapshot-mixed.json",
                value: snapshot
            ),
            .roundTrip(
                id: "contracts/provider/provider-result-failure-no-summary",
                path: "contracts/provider/provider-result-failure-no-summary.json",
                value: failure
            ),
            .roundTrip(
                id: "contracts/provider/provider-result-success",
                path: "contracts/provider/provider-result-success.json",
                value: success
            ),
            .roundTrip(
                id: "contracts/provider/provider-summary-full",
                path: "contracts/provider/provider-summary-full.json",
                value: summary
            ),
            .roundTrip(
                id: "contracts/provider/provider-usage-full",
                path: "contracts/provider/provider-usage-full.json",
                value: usage
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-request-full",
                path: "contracts/proxy/claude/message-request-full.json",
                value: claudeRequest
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-response-full",
                path: "contracts/proxy/claude/message-response-full.json",
                value: claudeResponse
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-delta",
                path: "contracts/proxy/claude/stream-content-block-delta.json",
                value: claudeStreamDelta
            ),
            .roundTrip(
                id: "contracts/proxy/codex/responses-request-tool-loop",
                path: "contracts/proxy/codex/responses-request-tool-loop.json",
                value: codexResponsesRequest
            ),
            .roundTrip(
                id: "contracts/proxy/codex/responses-response-mixed",
                path: "contracts/proxy/codex/responses-response-mixed.json",
                value: codexResponsesResponse
            ),
            .roundTrip(
                id: "contracts/proxy/codex/stream-completed",
                path: "contracts/proxy/codex/stream-completed.json",
                value: codexResponsesCompleted
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/chat-request-tool-loop",
                path: "contracts/proxy/opencode/chat-request-tool-loop.json",
                value: openAIChatRequest
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/chat-response-cache-usage",
                path: "contracts/proxy/opencode/chat-response-cache-usage.json",
                value: openAIChatResponse
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/stream-tool-delta",
                path: "contracts/proxy/opencode/stream-tool-delta.json",
                value: openAIChatStreamChunk
            ),
            .decodeTransform(
                id: "contracts/proxy/opencode/chat-response-malformed-usage",
                path: "contracts/proxy/opencode/chat-response-malformed-usage.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatMalformedUsageJSON,
                as: OpenAIChatCompletionResponse.self
            ),
            .decodeTransform(
                id: "contracts/proxy/opencode/stream-usage-only",
                path: "contracts/proxy/opencode/stream-usage-only.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatUsageOnlyStreamChunkJSON,
                as: OpenAIStreamChunk.self
            ),
        ]
    }

    private static func decode<Value: Decodable>(_ type: Value.Type, from json: String) throws -> Value {
        try JSONDecoder().decode(type, from: Data(json.utf8))
    }

    private static func makeCredential() throws -> AccountCredential {
        let json = #"{"id":"fixture-credential","providerId":"codex","accountLabel":"alice@example.test","authMethod":"authFile","credential":"<fixture-credential-a>","createdAt":"2030-01-02T03:04:05Z","lastUsedAt":"2030-01-02T03:04:05Z","metadata":{"email":"alice@example.test","workspace":"fixture-workspace"}}"#
        return try JSONDecoder().decode(AccountCredential.self, from: Data(json.utf8))
    }

    private static func makeUsage() -> ProviderUsage {
        var usage = ProviderUsage(
            provider: "codex",
            label: "Codex Fixture",
            accountId: "fixture-account",
            extra: [
                "nested": AnyCodable([
                    "count": AnyCodable(9),
                    "ok": AnyCodable(true),
                ] as [String: AnyCodable]),
                "nullable": AnyCodable(NSNull()),
                "ratio": AnyCodable(0.5),
            ]
        )
        usage.fetchedAt = fixedTime

        var source = SourceInfo(mode: "local", type: "authFile")
        source.browserName = "Fixture Browser"
        source.profile = "Fixture Profile"
        source.defaultsDomain = "com.example.fixture"
        source.roots = ["/fixture/codex"]
        source.envVar = "FIXTURE_HOME"
        usage.source = source

        usage.accountEmail = "alice@example.test"
        usage.accountName = "Fixture User"
        usage.accountLogin = "fixture-login"
        usage.accountPlan = "pro"

        var primary = RawQuotaWindow()
        primary.usedPercent = 25
        primary.remainingPercent = 75
        primary.resetAt = "2030-01-03T03:04:05+00:00"
        primary.resetDescription = "Resets tomorrow"
        primary.entitlement = 1_000
        primary.remaining = 750
        primary.unlimited = false
        primary.label = "5h"
        usage.primary = primary

        var secondary = RawQuotaWindow()
        secondary.usedPercent = 40
        secondary.remainingPercent = 60
        secondary.resetAt = "2030-01-09T03:04:05Z"
        secondary.label = "Weekly"
        usage.secondary = secondary

        return usage
    }

    private static func makeSummary(usage: ProviderUsage) -> ProviderSummary {
        let hourlyPoint = CostTimelinePoint(
            bucket: "2030-01-02T03:00:00Z",
            label: "03:00",
            usd: 0.12,
            tokens: 1_234,
            inputTokens: 1_000,
            outputTokens: 100,
            cacheReadTokens: 120,
            cacheCreateTokens: 14
        )
        let dailyPoint = CostTimelinePoint(
            bucket: "2030-01-02",
            label: "Jan 2",
            usd: 1.23,
            tokens: 4_294_967_296,
            inputTokens: 4_000_000_000,
            outputTokens: 200_000_000,
            cacheReadTokens: 90_000_000,
            cacheCreateTokens: 4_967_296
        )
        let modelCost = ModelCostInfo(
            model: "gpt-fixture",
            totalTokens: 4_294_967_296,
            inputTokens: 4_000_000_000,
            outputTokens: 200_000_000,
            cacheReadTokens: 90_000_000,
            cacheCreateTokens: 4_967_296,
            estimatedCostUsd: 12.34,
            percentage: 100
        )

        return ProviderSummary(
            id: "codex:fixture-account",
            providerId: "codex",
            accountId: "fixture-account",
            name: "Codex Fixture",
            label: "Codex Fixture",
            description: "Synthetic contract fixture",
            category: ProviderCategory.quota,
            channel: "cli",
            status: "healthy",
            statusLabel: "Healthy",
            theme: ThemeInfo(accent: "#4F46E5", glow: "#A5B4FC"),
            sourceLabel: "Fixture auth file",
            sourceType: "authFile",
            fetchedAt: fixedTime,
            accountLabel: "alice@example.test",
            membershipLabel: "Pro",
            workspaceLabel: "Fixture Workspace",
            remainingPercent: 75,
            nextResetAt: "2030-01-03T03:04:05Z",
            nextResetLabel: "Reset tomorrow",
            headline: HeadlineInfo(
                eyebrow: "Plan · Fixture",
                primary: "75%",
                secondary: "Remaining",
                supporting: "Synthetic fixture"
            ),
            metrics: [MetricInfo(label: "Requests", value: "9", note: "fixture")],
            windows: [
                WindowInfo(
                    label: "5h",
                    remainingPercent: 75,
                    usedPercent: 25,
                    value: "75%",
                    note: "rolling",
                    resetAt: "2030-01-03T03:04:05Z"
                ),
            ],
            costSummary: CostSummaryInfo(
                today: CostPeriod(usd: 1.23, tokens: 1_234, rangeLabel: "Today"),
                week: CostPeriod(usd: 4.56, tokens: 4_294_967_296, rangeLabel: "This week"),
                month: CostPeriod(usd: 12.34, tokens: 4_294_967_296, rangeLabel: "This month"),
                overall: CostPeriod(usd: 12.34, tokens: 4_294_967_296, rangeLabel: "Overall"),
                timeline: CostTimelineInfo(hourly: [hourlyPoint], daily: [dailyPoint]),
                modelBreakdown: [modelCost],
                modelBreakdownToday: [modelCost],
                modelBreakdownWeek: [modelCost],
                modelBreakdownOverall: [modelCost],
                modelTimelines: [ModelTimelineSeries(model: "gpt-fixture", hourly: [hourlyPoint], daily: [dailyPoint])]
            ),
            models: [ModelInfo(label: "Model", value: "gpt-fixture", note: "synthetic")],
            spotlight: "Fixture spotlight",
            unpricedModels: [],
            raw: usage,
            sourceFilePath: "/fixture/codex/auth.json",
            errorCode: nil
        )
    }

    private static func makeDashboardSnapshot(
        success: ProviderResult,
        failure: ProviderResult
    ) -> DashboardSnapshot {
        DashboardSnapshot(
            generatedAt: fixedTime,
            overview: DashboardOverview(
                generatedAt: fixedTime,
                activeProviders: 1,
                attentionProviders: 1,
                criticalProviders: 0,
                resetSoonProviders: 1,
                localCostMonthUsd: 12.34,
                localWeekTokens: 4_294_967_296,
                stats: [StatInfo(label: "Active", value: "1", note: "fixture")],
                alerts: [
                    AlertInfo(
                        id: "codex:fixture-account:reset",
                        tone: "watch",
                        providerId: "codex:fixture-account",
                        title: "Reset soon",
                        body: "Synthetic fixture alert"
                    ),
                ]
            ),
            providers: [success, failure]
        )
    }
}

private struct GoldenFixtureCase {
    let id: String
    let path: String
    let kind: String
    let render: () throws -> Data

    static func roundTrip<Value: Codable>(
        id: String,
        path: String,
        value: Value,
        sourceTest: String = "ContractsGoldenExporterTests.testCatalogEncodesDeterministically"
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-transform") {
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.sortedKeys, .withoutEscapingSlashes]
            let encoded = try encoder.encode(value)

            let decoder = JSONDecoder()
            let decoded = try decoder.decode(Value.self, from: encoded)
            let reencoded = try encoder.encode(decoded)

            return try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                inputData: encoded,
                expectedData: reencoded
            )
        }
    }

    static func decodeTransform<Value: Codable>(
        id: String,
        path: String,
        inputJSON: String,
        as type: Value.Type,
        sourceTest: String = "ContractsGoldenExporterTests.testCatalogEncodesDeterministically"
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-transform") {
            let inputData = Data(inputJSON.utf8)
            let decoded = try JSONDecoder().decode(type, from: inputData)
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.sortedKeys, .withoutEscapingSlashes]
            let expectedData = try encoder.encode(decoded)
            return try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                inputData: inputData,
                expectedData: expectedData
            )
        }
    }

    private static func renderEnvelope(
        id: String,
        sourceTest: String,
        inputData: Data,
        expectedData: Data
    ) throws -> Data {
        let input = try JSONSerialization.jsonObject(with: inputData)
        let expected = try JSONSerialization.jsonObject(with: expectedData)
        let envelope: [String: Any] = [
            "schemaVersion": 1,
            "id": id,
            "source": [
                "file": "ContractsGoldenExporterTests.swift",
                "test": sourceTest,
            ],
            "kind": "json-transform",
            "clock": "2030-01-02T03:04:05Z",
            "input": input,
            "expected": expected,
            "comparison": [
                "mode": "json-semantic",
                "orderedArrays": true,
            ],
        ]
        var data = try JSONSerialization.data(
            withJSONObject: envelope,
            options: [.sortedKeys, .withoutEscapingSlashes]
        )
        data.append(0x0A)
        return data
    }
}

private enum GoldenFixtureWriter {
    private struct Manifest: Encodable {
        let schemaVersion: Int
        let cases: [ManifestEntry]
    }

    private struct ManifestEntry: Encodable {
        let id: String
        let path: String
        let kind: String
        let sha256: String
    }

    enum ExportError: Error {
        case outputDirectoryIsNotEmpty(String)
    }

    static func write(cases: [GoldenFixtureCase], outputRoot: URL) throws {
        let fileManager = FileManager.default
        let versionRoot = outputRoot.appendingPathComponent("v1", isDirectory: true)
        if fileManager.fileExists(atPath: versionRoot.path) {
            let contents = try fileManager.contentsOfDirectory(atPath: versionRoot.path)
            guard contents.isEmpty else {
                throw ExportError.outputDirectoryIsNotEmpty(versionRoot.path)
            }
        } else {
            try fileManager.createDirectory(at: versionRoot, withIntermediateDirectories: true)
        }

        var entries: [ManifestEntry] = []
        for fixtureCase in cases.sorted(by: { $0.id < $1.id }) {
            let data = try fixtureCase.render()
            let destination = versionRoot.appendingPathComponent(fixtureCase.path, isDirectory: false)
            try fileManager.createDirectory(
                at: destination.deletingLastPathComponent(),
                withIntermediateDirectories: true
            )
            try data.write(to: destination, options: .atomic)
            entries.append(
                ManifestEntry(
                    id: fixtureCase.id,
                    path: fixtureCase.path,
                    kind: fixtureCase.kind,
                    sha256: SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined()
                )
            )
        }

        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys, .withoutEscapingSlashes]
        var manifestData = try encoder.encode(Manifest(schemaVersion: 1, cases: entries))
        manifestData.append(0x0A)
        try manifestData.write(
            to: versionRoot.appendingPathComponent("manifest.json", isDirectory: false),
            options: .atomic
        )
    }
}
