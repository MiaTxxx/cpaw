import CryptoKit
import CoreFoundation
import Foundation
import XCTest
@testable import QuotaBackend

final class ContractsGoldenExporterTests: XCTestCase {
    func testCatalogEncodesDeterministically() throws {
        let cases = try ContractsV1GoldenCatalog.makeCases()
        XCTAssertEqual(cases.count, 114)

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

    func testHostedGoldenPreservesZeroExitCodeAsNumber() throws {
        let fixtureCase = try XCTUnwrap(
            ContractsV1GoldenCatalog.makeCases().first {
                $0.id == "canonical/response/openai-responses/hosted-call-id-matrix"
            }
        )
        let envelope = try XCTUnwrap(
            JSONSerialization.jsonObject(with: fixtureCase.render()) as? [String: Any]
        )
        let expected = try XCTUnwrap(envelope["expected"] as? [String: Any])
        let items = try XCTUnwrap(expected["items"] as? [[String: Any]])
        let shellOutput = try XCTUnwrap(items.first {
            $0["vendorType"] as? String == "shell_call_output"
        })
        let payload = try XCTUnwrap(shellOutput["payload"] as? [String: Any])
        let output = try XCTUnwrap(payload["output"] as? [[String: Any]])
        let outcome = try XCTUnwrap(output.first?["outcome"] as? [String: Any])
        let exitCode = try XCTUnwrap(outcome["exit_code"] as? NSNumber)

        XCTAssertNotEqual(CFGetTypeID(exitCode), CFBooleanGetTypeID())
        XCTAssertEqual(exitCode.intValue, 0)
    }
}

private enum ContractsV1GoldenCatalog {
    private static let fixedTime = "2030-01-02T03:04:05Z"

    static func makeCases() throws -> [GoldenFixtureCase] {
        let credential = try makeCredential()
        let minimalCredential = try makeMinimalOAuthCredential()
        let safeCredentialMetadata = SafeAccountCredentialMetadataFixture(credential: credential)
        let safeCredentialMetadataJSONValues = makeSafeCredentialMetadataJSONValues()
        let usage = makeUsage()
        let minimalUsage = makeMinimalUsage()
        let allJSONKindsUsage = makeAllJSONKindsUsage()
        let summary = makeSummary(usage: usage)
        let sparseFutureSummary = makeSparseFutureSummary()
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
        let emptySnapshot = makeEmptyDashboardSnapshot()
        let futureToneSnapshot = makeFutureToneDashboardSnapshot()
        let claudeRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestJSON
        )
        let claudeResponse = try decode(
            ClaudeMessageResponse.self,
            from: ContractsProxyGoldenInputs.claudeMessageResponseJSON
        )
        let claudeStringSystemTextContentRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestStringSystemTextContentJSON
        )
        let claudeSystemBlocksMessageBlocksRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestSystemBlocksMessageBlocksJSON
        )
        let claudeImageSourcesRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestImageSourcesJSON
        )
        let claudeDocumentKnownRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestDocumentKnownJSON
        )
        let claudeToolResultStringRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestToolResultStringJSON
        )
        let claudeToolResultBlocksRequest = try decode(
            ClaudeMessageRequest.self,
            from: ContractsProxyGoldenInputs.claudeMessageRequestToolResultBlocksJSON
        )
        let claudeRedactedThinkingResponse = try decode(
            ClaudeMessageResponse.self,
            from: ContractsProxyGoldenInputs.claudeMessageResponseRedactedThinkingJSON
        )
        let claudeStreamDelta = try decode(
            ClaudeContentBlockDeltaEvent.self,
            from: ContractsProxyGoldenInputs.claudeContentBlockDeltaJSON
        )
        let claudeMessageStart = ClaudeMessageStartEvent(message: ClaudeMessageStart(
            id: "msg_fixture_stream_001",
            type: "message",
            role: "assistant",
            content: [.text(ClaudeTextBlock(text: ""))],
            model: "claude-fixture-1",
            stopReason: nil,
            stopSequence: nil,
            usage: ClaudeUsage(inputTokens: 4_294_967_296, outputTokens: 0)
        ))
        let claudeContentBlockStartText = ClaudeContentBlockStartEvent(
            index: 0,
            contentBlock: .text(ClaudeTextBlock(text: ""))
        )
        let claudeContentBlockStartThinking = ClaudeContentBlockStartEvent(
            index: 1,
            contentBlock: .thinking(ClaudeThinkingBlock(
                thinking: "Use the fixture tool.",
                signature: "<fixture-thinking-signature>"
            ))
        )
        let claudeContentBlockStartToolUse = ClaudeContentBlockStartEvent(
            index: 2,
            contentBlock: .toolUse(ClaudeToolUseBlock(
                id: "toolu_fixture_stream_001",
                name: "lookup_fixture",
                input: ["query": AnyCodable("quota")]
            ))
        )
        let claudeTextDelta = ClaudeContentBlockDeltaEvent(
            index: 0,
            delta: .text(ClaudeTextDelta(type: "text_delta", text: "Hello Windows"))
        )
        let claudeThinkingDelta = ClaudeContentBlockDeltaEvent(
            index: 1,
            delta: .thinking(ClaudeThinkingDelta(thinking: "Let me reason about this."))
        )
        let claudeSignatureDelta = ClaudeContentBlockDeltaEvent(
            index: 1,
            delta: .signature(ClaudeSignatureDelta(signature: "sig_fixture_123"))
        )
        let claudeCitationsDelta = ClaudeContentBlockDeltaEvent(
            index: 2,
            delta: .citations(ClaudeCitationsDelta(citation: AnyCodable([
                "type": AnyCodable("char_location"),
                "start_char_index": AnyCodable(7),
                "end_char_index": AnyCodable(19),
            ])))
        )
        let claudeContentBlockStop = ClaudeContentBlockStopEvent(index: 3)
        let claudeMessageDelta = ClaudeMessageDeltaEvent(
            delta: ClaudeMessageDeltaContent(
                stopReason: "pause_turn",
                stopSequence: "<fixture-stop>"
            ),
            usage: ClaudeUsageDelta(outputTokens: 4_294_967_296)
        )
        let claudeFilesList = ClaudeFilesListResponse(
            data: [
                ClaudeFileObject(
                    id: "file_fixture_001",
                    filename: "fixture-large.jsonl",
                    mimeType: "application/jsonl",
                    sizeBytes: 4_294_967_296,
                    createdAt: "2030-01-02T11:04:05+08:00",
                    downloadable: true,
                    scope: ClaudeFileScope(type: "workspace", id: "workspace_fixture_001")
                ),
                ClaudeFileObject(
                    id: "file_fixture_002",
                    filename: "fixture-small.txt",
                    mimeType: "text/plain",
                    sizeBytes: 7,
                    createdAt: "2030-01-02T03:04:05Z",
                    downloadable: false
                ),
            ],
            hasMore: true,
            firstId: "file_fixture_001",
            lastId: "file_fixture_002"
        )
        let claudeDeletedFile = ClaudeDeletedFileResponse(
            id: "file_fixture_deleted",
            deleted: true
        )
        let claudeTokenCountRequest = try decode(
            ClaudeTokenCountRequest.self,
            from: ContractsProxyGoldenInputs.claudeTokenCountStructuredSystemJSON
        )
        let claudeTokenCountResponse = ClaudeTokenCountResponse(inputTokens: 4_294_967_296)
        let openAIChatRequest = try decode(
            OpenAIChatCompletionRequest.self,
            from: ContractsProxyGoldenInputs.openAIChatRequestJSON
        )
        let openAIChatUnknownContentRequest = try decode(
            OpenAIChatCompletionRequest.self,
            from: ContractsProxyGoldenInputs.openAIChatUnknownContentRequestJSON
        )
        let openAIChatResponse = try decode(
            OpenAIChatCompletionResponse.self,
            from: ContractsProxyGoldenInputs.openAIChatResponseJSON
        )
        let openAIChatStreamChunk = try decode(
            OpenAIStreamChunk.self,
            from: ContractsProxyGoldenInputs.openAIChatStreamChunkJSON
        )
        let openAIFileObject = try decode(
            OpenAIFileObject.self,
            from: ContractsProxyGoldenInputs.openAIFileObjectFullJSON
        )
        let openAIFileList = try decode(
            OpenAIFileListResponse.self,
            from: ContractsProxyGoldenInputs.openAIFileListFullJSON
        )
        let openAIDeletedFile = try decode(
            OpenAIDeletedFileResponse.self,
            from: ContractsProxyGoldenInputs.openAIDeletedFileJSON
        )
        let codexResponsesRequest = try decode(
            OpenAIResponsesRequest.self,
            from: ContractsProxyGoldenInputs.codexResponsesRequestJSON
        )
        let codexResponsesResponse = try decode(
            OpenAIResponsesResponse.self,
            from: ContractsProxyGoldenInputs.codexResponsesResponseJSON
        )
        let codexResponsesCanonicalVariantsResponse = try decode(
            OpenAIResponsesResponse.self,
            from: ContractsProxyGoldenInputs.codexResponsesCanonicalVariantsResponseJSON
        )
        let codexResponsesHostedCallIDMatrix = try decode(
            AnyCodable.self,
            from: ContractsProxyGoldenInputs.codexResponsesHostedCallIDMatrixJSON
        )
        let codexResponsesStopPriorityMatrix = try decode(
            AnyCodable.self,
            from: ContractsProxyGoldenInputs.codexResponsesStopPriorityMatrixJSON
        )
        let codexResponsesCompleted = try decode(
            OpenAIResponsesCompletedEvent.self,
            from: ContractsProxyGoldenInputs.codexResponsesCompletedEventJSON
        )
        let claudeRateLimitError = ClaudeErrorResponse(
            error: ClaudeError(type: "rate_limit_error", message: "Fixture rate limit reached"),
            requestID: "req_fixture_429"
        )
        let claudeAPIError = ClaudeErrorResponse(
            error: ClaudeError(type: "api_error", message: "Fixture upstream failure")
        )
        let openAIError = OpenAIErrorResponse(
            error: OpenAIError(
                message: "Fixture request was rejected",
                type: "invalid_request_error",
                code: "fixture_invalid_request"
            )
        )
        let openAIMessageOnlyError = OpenAIErrorResponse(
            error: OpenAIError(message: "Fixture upstream error", type: nil, code: nil)
        )
        let codexError = CodexErrorResponse(
            error: CodexErrorResponse.Body(
                message: "Fixture request was rejected",
                type: "invalid_request_error",
                code: "fixture_invalid_request"
            ),
            requestID: "req_fixture_codex"
        )

        return [
            .throwingBehaviorTransform(
                id: "canonical/bridge/claude-to-openai-chat/document-url-lossy",
                path: "canonical/bridge/claude-to-openai-chat/document-url-lossy.json",
                input: CanonicalBridgeGoldenScenarios.claudeToOpenAIChatDocumentURLLossy,
                sourceTest: "CanonicalMiddleLayerTests.testCanonicalBuilderRecordsLossyDocumentDowngrade",
                transform: CanonicalBridgeGoldenScenarios.buildOpenAIChat
            ),
            .throwingBehaviorTransform(
                id: "canonical/bridge/claude-to-openai-chat/rich-tool-loop",
                path: "canonical/bridge/claude-to-openai-chat/rich-tool-loop.json",
                input: CanonicalBridgeGoldenScenarios.claudeToOpenAIChatRichToolLoop,
                sourceTest: "CanonicalMiddleLayerTests.testCanonicalChatBuilderMatchesClaudeDirectConverter",
                transform: CanonicalBridgeGoldenScenarios.buildOpenAIChat
            ),
            .throwingBehaviorTransform(
                id: "canonical/request/claude/rich-tool-loop",
                path: "canonical/request/claude/rich-tool-loop.json",
                input: CanonicalRequestGoldenScenarios.claudeRichToolLoop,
                sourceTest: "CanonicalMiddleLayerTests.testCanonicalClaudeRequestMappingPreservesToolConfigAndRichItems",
                transform: CanonicalRequestGoldenScenarios.mapClaude
            ),
            .throwingBehaviorTransform(
                id: "canonical/request/claude/content-variants",
                path: "canonical/request/claude/content-variants.json",
                input: CanonicalRequestGoldenScenarios.claudeContentVariants,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapClaude
            ),
            .throwingBehaviorTransform(
                id: "canonical/request/claude/empty-defaults",
                path: "canonical/request/claude/empty-defaults.json",
                input: CanonicalRequestGoldenScenarios.claudeEmptyDefaults,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapClaude
            ),
            .throwingBehaviorTransform(
                id: "canonical/request/openai-chat/content-variants",
                path: "canonical/request/openai-chat/content-variants.json",
                input: CanonicalRequestGoldenScenarios.openAIChatContentVariants,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIChat
            ),
            .throwingBehaviorTransform(
                id: "canonical/request/openai-chat/rich-tool-loop",
                path: "canonical/request/openai-chat/rich-tool-loop.json",
                input: CanonicalRequestGoldenScenarios.openAIChatRichToolLoop,
                sourceTest: "CanonicalMiddleLayerTests.testCanonicalOpenAIChatRequestMappingSeparatesSystemAndToolMessages",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIChat
            ),
            .throwingBehaviorTransform(
                id: "canonical/response/claude/mixed-blocks",
                path: "canonical/response/claude/mixed-blocks.json",
                input: CanonicalRequestGoldenScenarios.claudeResponseMixedBlocks,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapClaudeResponse
            ),
            .throwingBehaviorTransform(
                id: "canonical/response/openai-chat/rich-tool-loop",
                path: "canonical/response/openai-chat/rich-tool-loop.json",
                input: CanonicalRequestGoldenScenarios.openAIChatResponseRichToolLoop,
                sourceTest: "CanonicalMiddleLayerTests.testCanonicalClaudeResponseBuilderMatchesDirectOpenAIToClaudeConverter",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIChatResponse
            ),
            .throwingBehaviorTransform(
                id: "canonical/response/openai-responses/content-hosted-variants",
                path: "canonical/response/openai-responses/content-hosted-variants.json",
                input: codexResponsesCanonicalVariantsResponse,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIResponsesResponse
            ),
            .throwingBehaviorTransform(
                id: "canonical/response/openai-responses/mixed-tool-loop",
                path: "canonical/response/openai-responses/mixed-tool-loop.json",
                input: codexResponsesResponse,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIResponsesResponse
            ),
            .throwingBehaviorTransform(
                id: "canonical/response/openai-responses/hosted-call-id-matrix",
                path: "canonical/response/openai-responses/hosted-call-id-matrix.json",
                input: codexResponsesHostedCallIDMatrix,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapRawOpenAIResponsesResponse
            ),
            .throwingBehaviorTransform(
                id: "canonical/response/openai-responses/stop-priority-matrix",
                path: "canonical/response/openai-responses/stop-priority-matrix.json",
                input: codexResponsesStopPriorityMatrix,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIResponsesStopPriorityMatrix
            ),
            .throwingBehaviorTransform(
                id: "canonical/request/openai-chat/empty-defaults",
                path: "canonical/request/openai-chat/empty-defaults.json",
                input: CanonicalRequestGoldenScenarios.openAIChatEmptyDefaults,
                sourceTest: "ContractsGoldenExporterTests.testCatalogEncodesDeterministically",
                transform: CanonicalRequestGoldenScenarios.mapOpenAIChat
            ),
            .roundTrip(
                id: "contracts/account/account-credential-all-fields",
                path: "contracts/account/account-credential-all-fields.json",
                value: credential
            ),
            .roundTrip(
                id: "contracts/account/account-credential-metadata-safe-projection",
                path: "contracts/account/account-credential-metadata-safe-projection.json",
                value: safeCredentialMetadata
            ),
            .roundTrip(
                id: "contracts/account/account-credential-metadata-json-values",
                path: "contracts/account/account-credential-metadata-json-values.json",
                value: safeCredentialMetadataJSONValues
            ),
            .roundTrip(
                id: "contracts/account/account-credential-minimal-oauth",
                path: "contracts/account/account-credential-minimal-oauth.json",
                value: minimalCredential
            ),
            .roundTrip(
                id: "contracts/dashboard/dashboard-snapshot-empty",
                path: "contracts/dashboard/dashboard-snapshot-empty.json",
                value: emptySnapshot
            ),
            .roundTrip(
                id: "contracts/dashboard/dashboard-snapshot-future-alert-tone",
                path: "contracts/dashboard/dashboard-snapshot-future-alert-tone.json",
                value: futureToneSnapshot
            ),
            .roundTrip(
                id: "contracts/dashboard/dashboard-snapshot-mixed",
                path: "contracts/dashboard/dashboard-snapshot-mixed.json",
                value: snapshot
            ),
            .decodeFailure(
                id: "contracts/dashboard/dashboard-snapshot-null-provider",
                path: "contracts/dashboard/dashboard-snapshot-null-provider.json",
                inputJSON: #"{"generatedAt":"2030-01-02T03:04:05Z","overview":{"generatedAt":"2030-01-02T03:04:05Z","activeProviders":0,"attentionProviders":0,"criticalProviders":0,"resetSoonProviders":0,"localCostMonthUsd":0,"localWeekTokens":0,"stats":[],"alerts":[]},"providers":[null]}"#,
                as: DashboardSnapshot.self
            ),
            .decodeTransform(
                id: "contracts/provider/provider-result-explicit-null-optionals",
                path: "contracts/provider/provider-result-explicit-null-optionals.json",
                inputJSON: #"{"id":"warp:fixture-null-optionals","providerId":"warp","accountId":null,"ok":false,"usage":null,"summary":null,"error":null}"#,
                as: ProviderResult.self
            ),
            .roundTrip(
                id: "contracts/provider/provider-result-failure-no-summary",
                path: "contracts/provider/provider-result-failure-no-summary.json",
                value: failure
            ),
            .decodeFailure(
                id: "contracts/provider/provider-result-missing-required-id",
                path: "contracts/provider/provider-result-missing-required-id.json",
                inputJSON: #"{"providerId":"warp","ok":false,"error":"Fixture failure"}"#,
                as: ProviderResult.self
            ),
            .decodeFailure(
                id: "contracts/provider/provider-result-null-required-id",
                path: "contracts/provider/provider-result-null-required-id.json",
                inputJSON: #"{"id":null,"providerId":"warp","ok":false,"error":"Fixture failure"}"#,
                as: ProviderResult.self
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
                id: "contracts/provider/provider-summary-sparse-future-values",
                path: "contracts/provider/provider-summary-sparse-future-values.json",
                value: sparseFutureSummary
            ),
            .roundTrip(
                id: "contracts/provider/provider-usage-extra-all-json-kinds",
                path: "contracts/provider/provider-usage-extra-all-json-kinds.json",
                value: allJSONKindsUsage
            ),
            .roundTrip(
                id: "contracts/provider/provider-usage-full",
                path: "contracts/provider/provider-usage-full.json",
                value: usage
            ),
            .roundTrip(
                id: "contracts/provider/provider-usage-minimal-offset-time",
                path: "contracts/provider/provider-usage-minimal-offset-time.json",
                value: minimalUsage
            ),
            .decodeFailure(
                id: "contracts/provider/provider-usage-null-source-root",
                path: "contracts/provider/provider-usage-null-source-root.json",
                inputJSON: #"{"provider":"fixture-roots","label":"Roots Fixture","fetchedAt":"2030-01-02T03:04:05Z","source":{"mode":"local","type":"authFile","roots":["/fixture/root",null]},"extra":{}}"#,
                as: ProviderUsage.self
            ),
            .behaviorTransform(
                id: "normalization/provider/claude/model-breakdowns",
                path: "normalization/provider/claude/model-breakdowns.json",
                input: ClaudeCostNormalizerGoldenScenarios.modelBreakdowns,
                sourceTest: "UsageNormalizerTests.testClaudeCostNormalizationPreservesAllModelBreakdownsAndNumericConversions",
                transform: ClaudeCostNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/claude/periods-account-unpriced",
                path: "normalization/provider/claude/periods-account-unpriced.json",
                input: ClaudeCostNormalizerGoldenScenarios.periodsAccountUnpriced,
                sourceTest: "UsageNormalizerTests.testClaudeCostNormalizationPreservesPeriodsAccountAndUnpricedModels",
                transform: ClaudeCostNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/claude/timelines-defaults",
                path: "normalization/provider/claude/timelines-defaults.json",
                input: ClaudeCostNormalizerGoldenScenarios.timelinesDefaults,
                sourceTest: "UsageNormalizerTests.testClaudeCostNormalizationFiltersTimelinesAndDefaultsMalformedValues",
                transform: ClaudeCostNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/codex/all-windows-order",
                path: "normalization/provider/codex/all-windows-order.json",
                input: CodexUsageNormalizerGoldenScenarios.allWindowsOrder,
                sourceTest: "UsageNormalizerTests.testCodexNormalizationPreservesAllWindowSlotsAndOrder",
                transform: CodexUsageNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/codex/missing-slot-matrix",
                path: "normalization/provider/codex/missing-slot-matrix.json",
                input: CodexUsageNormalizerGoldenScenarios.missingSlotMatrix,
                sourceTest: "UsageNormalizerTests.testCodexNormalizationPreservesSemanticWindowLabelsWhenSlotsAreMissing",
                transform: CodexUsageNormalizerGoldenScenarios.normalizeMatrix
            ),
            .behaviorTransform(
                id: "normalization/provider/codex/secondary-reset-fallback",
                path: "normalization/provider/codex/secondary-reset-fallback.json",
                input: CodexUsageNormalizerGoldenScenarios.secondaryResetFallback,
                sourceTest: "UsageNormalizerTests.testCodexNormalizationFallsBackToSecondaryResetAndIgnoresTertiary",
                transform: CodexUsageNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/opencode/model-breakdowns",
                path: "normalization/provider/opencode/model-breakdowns.json",
                input: OpenCodeCostNormalizerGoldenScenarios.modelBreakdowns,
                sourceTest: "UsageNormalizerTests.testOpenCodeCostNormalizationPreservesModelBreakdownsAndNumericConversions",
                transform: OpenCodeCostNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/opencode/periods-account-range",
                path: "normalization/provider/opencode/periods-account-range.json",
                input: OpenCodeCostNormalizerGoldenScenarios.periodsAccountRange,
                sourceTest: "UsageNormalizerTests.testOpenCodeCostNormalizationPreservesPeriodsAndOverallRangeWhileIgnoringAccountAndUnpriced",
                transform: OpenCodeCostNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/provider/opencode/timelines-defaults",
                path: "normalization/provider/opencode/timelines-defaults.json",
                input: OpenCodeCostNormalizerGoldenScenarios.timelinesDefaults,
                sourceTest: "UsageNormalizerTests.testOpenCodeCostNormalizationFiltersTimelinesAndDefaultsMalformedValues",
                transform: OpenCodeCostNormalizerGoldenScenarios.normalize
            ),
            .behaviorTransform(
                id: "normalization/quota/entitlement-missing-values",
                path: "normalization/quota/entitlement-missing-values.json",
                input: UsageNormalizerGoldenScenarios.entitlementMissingValues,
                sourceTest: "UsageNormalizerTests.testEntitlementWindowDefaultsMissingCountsToZero",
                transform: UsageNormalizerGoldenScenarios.createEntitlementWindow
            ),
            .behaviorTransform(
                id: "normalization/quota/entitlement-unlimited",
                path: "normalization/quota/entitlement-unlimited.json",
                input: UsageNormalizerGoldenScenarios.entitlementUnlimited,
                sourceTest: "UsageNormalizerTests.testEntitlementWindowTreatsUnlimitedAsUnbounded",
                transform: UsageNormalizerGoldenScenarios.createEntitlementWindow
            ),
            .behaviorTransform(
                id: "normalization/quota/percent-derived-used",
                path: "normalization/quota/percent-derived-used.json",
                input: UsageNormalizerGoldenScenarios.percentDerived,
                sourceTest: "UsageNormalizerTests.testPercentWindowDerivesUsedPercentFromRemaining",
                transform: UsageNormalizerGoldenScenarios.createPercentWindow
            ),
            .behaviorTransform(
                id: "normalization/quota/percent-explicit-used",
                path: "normalization/quota/percent-explicit-used.json",
                input: UsageNormalizerGoldenScenarios.percentExplicit,
                sourceTest: "UsageNormalizerTests.testPercentWindowPreservesExplicitUsedPercent",
                transform: UsageNormalizerGoldenScenarios.createPercentWindow
            ),
            .behaviorTransform(
                id: "normalization/quota/quota-missing-used",
                path: "normalization/quota/quota-missing-used.json",
                input: UsageNormalizerGoldenScenarios.quotaMissingUsed,
                sourceTest: "UsageNormalizerTests.testQuotaWindowDoesNotDeriveMissingUsedPercent",
                transform: UsageNormalizerGoldenScenarios.createQuotaWindow
            ),
            .behaviorTransform(
                id: "normalization/quota/quota-unlimited",
                path: "normalization/quota/quota-unlimited.json",
                input: UsageNormalizerGoldenScenarios.quotaUnlimited,
                sourceTest: "UsageNormalizerTests.testQuotaWindowTreatsUnlimitedAsUnbounded",
                transform: UsageNormalizerGoldenScenarios.createQuotaWindow
            ),
            .behaviorTransform(
                id: "normalization/quota/status-boundaries",
                path: "normalization/quota/status-boundaries.json",
                input: UsageNormalizerGoldenScenarios.statusBoundaries,
                sourceTest: "UsageNormalizerTests.testStatusResolutionPreservesNilAndThresholdBoundaries",
                transform: UsageNormalizerGoldenScenarios.resolveStatuses
            ),
            .behaviorTransform(
                id: "normalization/quota/tightest-remaining",
                path: "normalization/quota/tightest-remaining.json",
                input: UsageNormalizerGoldenScenarios.smallestRemaining,
                sourceTest: "UsageNormalizerTests.testSmallestRemainingIgnoresWindowsWithoutRemainingPercent",
                transform: UsageNormalizerGoldenScenarios.pickSmallestRemaining
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
                id: "contracts/proxy/claude/message-request-system-string-message-text",
                path: "contracts/proxy/claude/message-request-system-string-message-text.json",
                value: claudeStringSystemTextContentRequest,
                sourceTest: "ClaudeProxyConverterTests.testConvertSystemMessage"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-request-system-blocks-message-blocks",
                path: "contracts/proxy/claude/message-request-system-blocks-message-blocks.json",
                value: claudeSystemBlocksMessageBlocksRequest,
                sourceTest: "ClaudeProxyConverterTests.testDecodeStructuredSystemBlocksPreservesJoinedTextAndRawBlocks"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-request-image-sources",
                path: "contracts/proxy/claude/message-request-image-sources.json",
                value: claudeImageSourcesRequest,
                sourceTest: "ClaudeProxyConverterTests.testConvertImageMessage"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-request-document-known",
                path: "contracts/proxy/claude/message-request-document-known.json",
                value: claudeDocumentKnownRequest,
                sourceTest: "ClaudeProxyConverterTests.testConvertClaudeDocumentURLSourceFallsBackToExplicitText"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-request-tool-result-string",
                path: "contracts/proxy/claude/message-request-tool-result-string.json",
                value: claudeToolResultStringRequest,
                sourceTest: "ClaudeProxyConverterTests.testConvertMultipleToolResultsPreservesEachToolMessage"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-request-tool-result-blocks-known",
                path: "contracts/proxy/claude/message-request-tool-result-blocks-known.json",
                value: claudeToolResultBlocksRequest,
                sourceTest: "ClaudeProxyConverterTests.testConvertStructuredToolResultBlocksPreservesImageParts"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/message-response-redacted-thinking",
                path: "contracts/proxy/claude/message-response-redacted-thinking.json",
                value: claudeRedactedThinkingResponse,
                sourceTest: "ClaudeProxyConverterTests.testClaudeContentBlockDecodesRedactedThinking"
            ),
            .decodeTransform(
                id: "contracts/proxy/claude/message-request-known-optional-nulls-omitted",
                path: "contracts/proxy/claude/message-request-known-optional-nulls-omitted.json",
                inputJSON: ContractsProxyGoldenInputs.claudeMessageRequestKnownOptionalNullsJSON,
                as: ClaudeMessageRequest.self
            ),
            .decodeFailure(
                id: "contracts/proxy/claude/message-request-text-cache-control-invalid",
                path: "contracts/proxy/claude/message-request-text-cache-control-invalid.json",
                inputJSON: #"{"model":"claude-fixture-content","messages":[{"role":"user","content":[{"type":"text","text":"fixture","cache_control":[]}]}],"max_tokens":64}"#,
                as: ClaudeMessageRequest.self
            ),
            .decodeFailure(
                id: "contracts/proxy/claude/message-request-document-cache-control-invalid",
                path: "contracts/proxy/claude/message-request-document-cache-control-invalid.json",
                inputJSON: #"{"model":"claude-fixture-content","messages":[{"role":"user","content":[{"type":"document","source":{"type":"text","data":"fixture"},"cache_control":"invalid"}]}],"max_tokens":64}"#,
                as: ClaudeMessageRequest.self
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-delta",
                path: "contracts/proxy/claude/stream-content-block-delta.json",
                value: claudeStreamDelta
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-message-start-full",
                path: "contracts/proxy/claude/stream-message-start-full.json",
                value: claudeMessageStart,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIConvertProxyStreamingRoundTrip"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-start-text",
                path: "contracts/proxy/claude/stream-content-block-start-text.json",
                value: claudeContentBlockStartText,
                sourceTest: "ClaudeProxyConverterTests.testClaudeContentBlockStartEventSupportsTextBlocks"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-start-thinking",
                path: "contracts/proxy/claude/stream-content-block-start-thinking.json",
                value: claudeContentBlockStartThinking,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIResponsesProxyStreamingEmitsThinkingDeltaBeforeText"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-start-tool-use",
                path: "contracts/proxy/claude/stream-content-block-start-tool-use.json",
                value: claudeContentBlockStartToolUse,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIResponsesProxyBuffersToolArgumentDeltasUntilRealToolMetadataArrives"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-delta-text",
                path: "contracts/proxy/claude/stream-content-block-delta-text.json",
                value: claudeTextDelta,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIConvertProxyStreamingRoundTrip"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-delta-thinking",
                path: "contracts/proxy/claude/stream-content-block-delta-thinking.json",
                value: claudeThinkingDelta,
                sourceTest: "ClaudeProxyConverterTests.testClaudeContentBlockDeltaEventDecodesThinkingAndSignatureDeltas"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-delta-signature",
                path: "contracts/proxy/claude/stream-content-block-delta-signature.json",
                value: claudeSignatureDelta,
                sourceTest: "ClaudeProxyConverterTests.testClaudeContentBlockDeltaEventDecodesThinkingAndSignatureDeltas"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-delta-citations",
                path: "contracts/proxy/claude/stream-content-block-delta-citations.json",
                value: claudeCitationsDelta
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-content-block-stop",
                path: "contracts/proxy/claude/stream-content-block-stop.json",
                value: claudeContentBlockStop,
                sourceTest: "ClaudeProxyConverterTests.testClaudeContentBlockStopEventPreservesIndex"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/stream-message-delta",
                path: "contracts/proxy/claude/stream-message-delta.json",
                value: claudeMessageDelta,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIResponsesProxyFineGrainedToolStreamingAllowsPartialJSONAndMaxTokensStopReason"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/files-list-full",
                path: "contracts/proxy/claude/files-list-full.json",
                value: claudeFilesList,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testFilesListAndMetadataEndpointsBridgeOpenAIFileMetadata"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/file-deleted",
                path: "contracts/proxy/claude/file-deleted.json",
                value: claudeDeletedFile,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testFilesDeleteEndpointBridgesOpenAIDelete"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/openai-upstream-file-object-full",
                path: "contracts/proxy/claude/openai-upstream-file-object-full.json",
                value: openAIFileObject,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testFilesCreateEndpointBridgesAnthropicMultipartUploadToOpenAI"
            ),
            .decodeTransform(
                id: "contracts/proxy/claude/openai-upstream-file-list-full",
                path: "contracts/proxy/claude/openai-upstream-file-list-full.json",
                inputJSON: ContractsProxyGoldenInputs.openAIFileListFullJSON,
                as: OpenAIFileListResponse.self,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testFilesListAndMetadataEndpointsBridgeOpenAIFileMetadata"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/openai-upstream-file-deleted",
                path: "contracts/proxy/claude/openai-upstream-file-deleted.json",
                value: openAIDeletedFile,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testFilesDeleteEndpointBridgesOpenAIDelete"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/token-count-request-structured-system",
                path: "contracts/proxy/claude/token-count-request-structured-system.json",
                value: claudeTokenCountRequest,
                sourceTest: "ClaudeProxyConverterTests.testEncodeStructuredTokenCountSystemBlocksPreservesArrayShape"
            ),
            .roundTrip(
                id: "contracts/proxy/claude/token-count-response-large",
                path: "contracts/proxy/claude/token-count-response-large.json",
                value: claudeTokenCountResponse,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testCountTokensEndpointReturnsHeuristicEstimate"
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
            .decodeTransform(
                id: "contracts/proxy/codex/stream-output-item-added",
                path: "contracts/proxy/codex/stream-output-item-added.json",
                inputJSON: ContractsProxyGoldenInputs.codexOutputItemAddedEventJSON,
                as: OpenAIResponsesOutputItemAddedEvent.self,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIResponsesProxyFineGrainedToolStreamingAllowsPartialJSONAndMaxTokensStopReason"
            ),
            .decodeTransform(
                id: "contracts/proxy/codex/stream-output-item-done",
                path: "contracts/proxy/codex/stream-output-item-done.json",
                inputJSON: ContractsProxyGoldenInputs.codexOutputItemDoneEventJSON,
                as: OpenAIResponsesOutputItemDoneEvent.self,
                sourceTest: "OpenAIResponsesTests.testResponsesStreamingUsesOutputItemDoneForToolCalls"
            ),
            .decodeTransform(
                id: "contracts/proxy/codex/stream-output-text-delta",
                path: "contracts/proxy/codex/stream-output-text-delta.json",
                inputJSON: ContractsProxyGoldenInputs.codexOutputTextDeltaEventJSON,
                as: OpenAIResponsesOutputTextDeltaEvent.self,
                sourceTest: "QuotaHTTPServerProxyIntegrationTests.testOpenAIResponsesProxyStreamingRoundTrip"
            ),
            .decodeTransform(
                id: "contracts/proxy/codex/stream-reasoning-summary-text-delta",
                path: "contracts/proxy/codex/stream-reasoning-summary-text-delta.json",
                inputJSON: ContractsProxyGoldenInputs.codexReasoningSummaryTextDeltaEventJSON,
                as: OpenAIResponsesReasoningSummaryTextDeltaEvent.self,
                sourceTest: "OpenAIResponsesTests.testResponsesStreamingEmitsReasoningSummaryAndKeepsToolIndicesRelative"
            ),
            .decodeTransform(
                id: "contracts/proxy/codex/stream-function-call-arguments-delta",
                path: "contracts/proxy/codex/stream-function-call-arguments-delta.json",
                inputJSON: ContractsProxyGoldenInputs.codexFunctionCallArgumentsDeltaEventJSON,
                as: OpenAIResponsesFunctionCallArgumentsDeltaEvent.self,
                sourceTest: "OpenAIResponsesTests.testResponsesStreamingNormalizesFunctionArgumentDeltaIndicesAfterReasoning"
            ),
            .decodeTransform(
                id: "contracts/proxy/codex/stream-function-call-arguments-done",
                path: "contracts/proxy/codex/stream-function-call-arguments-done.json",
                inputJSON: ContractsProxyGoldenInputs.codexFunctionCallArgumentsDoneEventJSON,
                as: OpenAIResponsesFunctionCallArgumentsDoneEvent.self,
                sourceTest: "OpenAIResponsesTests.testResponsesStreamingNormalizesFunctionArgumentDeltaIndicesAfterReasoning"
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/chat-request-tool-loop",
                path: "contracts/proxy/opencode/chat-request-tool-loop.json",
                value: openAIChatRequest
            ),
            .decodeTransform(
                id: "contracts/proxy/opencode/chat-request-input-file-normalized",
                path: "contracts/proxy/opencode/chat-request-input-file-normalized.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatInputFileRequestJSON,
                as: OpenAIChatCompletionRequest.self,
                sourceTest: "ClaudeProxyConverterTests.testEncodeOpenAIInputFilePartAsChatCompletionsFileShape"
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/chat-request-unknown-content-preserved",
                path: "contracts/proxy/opencode/chat-request-unknown-content-preserved.json",
                value: openAIChatUnknownContentRequest
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/chat-request-invalid-content-scalar",
                path: "contracts/proxy/opencode/chat-request-invalid-content-scalar.json",
                inputJSON: #"{"model":"gpt-fixture-chat","messages":[{"role":"user","content":7}]}"#,
                as: OpenAIChatCompletionRequest.self
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/chat-request-content-part-missing-type",
                path: "contracts/proxy/opencode/chat-request-content-part-missing-type.json",
                inputJSON: #"{"model":"gpt-fixture-chat","messages":[{"role":"user","content":[{"text":"fixture"}]}]}"#,
                as: OpenAIChatCompletionRequest.self
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
            .decodeTransform(
                id: "contracts/proxy/opencode/stream-choice-usage",
                path: "contracts/proxy/opencode/stream-choice-usage.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatChoiceUsageStreamChunkJSON,
                as: OpenAIStreamChunk.self
            ),
            .decodeTransform(
                id: "contracts/proxy/opencode/stream-malformed-choice-dropped",
                path: "contracts/proxy/opencode/stream-malformed-choice-dropped.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatMalformedChoiceStreamChunkJSON,
                as: OpenAIStreamChunk.self
            ),
            .decodeTransform(
                id: "contracts/proxy/opencode/stream-choice-malformed-usage-ignored",
                path: "contracts/proxy/opencode/stream-choice-malformed-usage-ignored.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatMalformedChoiceUsageStreamChunkJSON,
                as: OpenAIStreamChunk.self
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/chat-response-malformed-choice-rejected",
                path: "contracts/proxy/opencode/chat-response-malformed-choice-rejected.json",
                inputJSON: ContractsProxyGoldenInputs.openAIChatMalformedResponseChoiceJSON,
                as: OpenAIChatCompletionResponse.self
            ),
            .roundTrip(
                id: "contracts/proxy/claude/error-api-without-request-id",
                path: "contracts/proxy/claude/error-api-without-request-id.json",
                value: claudeAPIError
            ),
            .roundTrip(
                id: "contracts/proxy/claude/error-rate-limit-with-request-id",
                path: "contracts/proxy/claude/error-rate-limit-with-request-id.json",
                value: claudeRateLimitError
            ),
            .roundTrip(
                id: "contracts/proxy/codex/error-all-fields",
                path: "contracts/proxy/codex/error-all-fields.json",
                value: codexError
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/error-all-fields",
                path: "contracts/proxy/opencode/error-all-fields.json",
                value: openAIError
            ),
            .roundTrip(
                id: "contracts/proxy/opencode/error-message-only",
                path: "contracts/proxy/opencode/error-message-only.json",
                value: openAIMessageOnlyError
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/error-null-body",
                path: "contracts/proxy/opencode/error-null-body.json",
                inputJSON: #"{"error":null}"#,
                as: OpenAIErrorResponse.self
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/error-null-message",
                path: "contracts/proxy/opencode/error-null-message.json",
                inputJSON: #"{"error":{"message":null}}"#,
                as: OpenAIErrorResponse.self
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/error-missing-body",
                path: "contracts/proxy/opencode/error-missing-body.json",
                inputJSON: "{}",
                as: OpenAIErrorResponse.self
            ),
            .decodeFailure(
                id: "contracts/proxy/opencode/error-missing-message",
                path: "contracts/proxy/opencode/error-missing-message.json",
                inputJSON: #"{"error":{}}"#,
                as: OpenAIErrorResponse.self
            ),
            .encodedSSELifecycle(
                id: "contracts/proxy/claude/sse-lifecycle",
                path: "contracts/proxy/claude/sse-lifecycle.json",
                frames: ContractsSSEGoldenInputs.claudeLifecycleFrames
            ),
            .parsedSSELifecycle(
                id: "contracts/proxy/codex/sse-lifecycle",
                path: "contracts/proxy/codex/sse-lifecycle.json",
                framing: .responsesEventData,
                lines: ContractsSSEGoldenInputs.codexResponsesLines
            ),
            .parsedSSELifecycle(
                id: "contracts/proxy/opencode/sse-lifecycle",
                path: "contracts/proxy/opencode/sse-lifecycle.json",
                framing: .chatDataLines,
                lines: ContractsSSEGoldenInputs.openCodeChatLines
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

    private static func makeMinimalOAuthCredential() throws -> AccountCredential {
        let json = #"{"id":"fixture-oauth-credential","providerId":"codex","authMethod":"oauth","credential":"<fixture-credential-oauth>","createdAt":"2030-01-02T11:04:05+08:00","metadata":{}}"#
        return try JSONDecoder().decode(AccountCredential.self, from: Data(json.utf8))
    }

    private static func makeSafeCredentialMetadataJSONValues() -> SafeAccountCredentialMetadataFixture {
        SafeAccountCredentialMetadataFixture(
            id: "fixture-safe-json-metadata",
            providerId: "future-provider",
            accountLabel: nil,
            authMethod: .oauth,
            createdAt: fixedTime,
            lastUsedAt: nil,
            metadata: [
                "count": AnyCodable(7),
                "enabled": AnyCodable(true),
                "labels": AnyCodable([AnyCodable("alpha"), AnyCodable("beta")]),
                "nested": AnyCodable([
                    "ratio": AnyCodable(0.5),
                    "region": AnyCodable("fixture-region"),
                ] as [String: AnyCodable]),
                "nullable": AnyCodable(NSNull()),
            ]
        )
    }

    private static func makeMinimalUsage() -> ProviderUsage {
        var usage = ProviderUsage(provider: "fixture-minimal", label: "Minimal Fixture")
        usage.fetchedAt = "2030-01-02T11:04:05+08:00"
        return usage
    }

    private static func makeAllJSONKindsUsage() -> ProviderUsage {
        var usage = ProviderUsage(
            provider: "fixture-json-kinds",
            label: "JSON Kinds Fixture",
            extra: [
                "array": AnyCodable([
                    AnyCodable("fixture-item"),
                    AnyCodable(7),
                    AnyCodable(2.5),
                    AnyCodable(false),
                    AnyCodable(NSNull()),
                ]),
                "boolean": AnyCodable(true),
                "double": AnyCodable(0.125),
                "integer": AnyCodable(4_294_967_296),
                "null": AnyCodable(NSNull()),
                "object": AnyCodable([
                    "nestedArray": AnyCodable([AnyCodable("alpha"), AnyCodable(3)]),
                    "nestedNull": AnyCodable(NSNull()),
                ] as [String: AnyCodable]),
                "string": AnyCodable("fixture-value"),
            ]
        )
        usage.fetchedAt = fixedTime
        return usage
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

    private static func makeSparseFutureSummary() -> ProviderSummary {
        ProviderSummary(
            id: "future:fixture-account",
            providerId: "future-provider",
            accountId: nil,
            name: "Future Provider Fixture",
            label: "Future Provider Fixture",
            description: "Sparse synthetic contract fixture",
            category: "future-category",
            channel: nil,
            status: "future-provider-state",
            statusLabel: "Future state",
            theme: ThemeInfo(accent: "#334155", glow: "#94A3B8"),
            sourceLabel: "Synthetic source",
            sourceType: "future-source",
            fetchedAt: nil,
            accountLabel: nil,
            membershipLabel: nil,
            workspaceLabel: nil,
            remainingPercent: nil,
            nextResetAt: nil,
            nextResetLabel: nil,
            headline: HeadlineInfo(
                eyebrow: "Future",
                primary: "--",
                secondary: "Unknown",
                supporting: "Forward-compatible fixture"
            ),
            metrics: [],
            windows: [],
            costSummary: nil,
            models: nil,
            spotlight: "Future provider state",
            unpricedModels: nil,
            raw: nil,
            sourceFilePath: nil,
            errorCode: nil
        )
    }

    private static func makeEmptyDashboardSnapshot() -> DashboardSnapshot {
        DashboardSnapshot(
            generatedAt: fixedTime,
            overview: DashboardOverview(
                generatedAt: fixedTime,
                activeProviders: 0,
                attentionProviders: 0,
                criticalProviders: 0,
                resetSoonProviders: 0,
                localCostMonthUsd: 0,
                localWeekTokens: 0,
                stats: [],
                alerts: []
            ),
            providers: []
        )
    }

    private static func makeFutureToneDashboardSnapshot() -> DashboardSnapshot {
        DashboardSnapshot(
            generatedAt: fixedTime,
            overview: DashboardOverview(
                generatedAt: fixedTime,
                activeProviders: 0,
                attentionProviders: 1,
                criticalProviders: 0,
                resetSoonProviders: 0,
                localCostMonthUsd: 0,
                localWeekTokens: 0,
                stats: [],
                alerts: [
                    AlertInfo(
                        id: "future-provider:fixture-alert",
                        tone: "investigate",
                        providerId: "future-provider",
                        title: "Future alert tone",
                        body: "Preserve unknown alert tone values"
                    ),
                ]
            ),
            providers: []
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

private enum CanonicalRequestGoldenScenarios {
    static let claudeRichToolLoop = ClaudeMessageRequest(
        model: "claude-sonnet-4-5",
        messages: [
            ClaudeMessage(role: "user", content: .blocks([
                .text(ClaudeTextBlock(text: "Read this doc")),
                .document(ClaudeDocumentBlock(
                    source: [
                        "type": AnyCodable("file"),
                        "file_id": AnyCodable("file_123"),
                    ],
                    title: "Spec"
                )),
            ])),
            ClaudeMessage(role: "assistant", content: .blocks([
                .thinking(ClaudeThinkingBlock(thinking: "Need tool", signature: "sig_1")),
                .toolUse(ClaudeToolUseBlock(
                    id: "toolu_1",
                    name: "lookup",
                    input: ["query": AnyCodable("quota")]
                )),
            ])),
            ClaudeMessage(role: "user", content: .blocks([
                .toolResult(ClaudeToolResultBlock(
                    toolUseId: "toolu_1",
                    contentBlocks: [
                        .text(ClaudeTextBlock(text: "Found it")),
                    ]
                )),
            ])),
        ],
        systemBlocks: [
            ClaudeSystemBlock(
                type: "text",
                text: "You are precise.",
                cacheControl: ["type": AnyCodable("ephemeral")]
            ),
        ],
        maxTokens: 2048,
        stream: true,
        tools: [
            ClaudeTool(
                name: "lookup",
                description: "Lookup docs",
                inputSchema: ["type": AnyCodable("object")],
                eagerInputStreaming: true
            ),
        ],
        toolChoice: ClaudeToolChoice(
            type: "tool",
            name: "lookup",
            disableParallelToolUse: true
        ),
        metadata: ClaudeMetadata(userId: "user_123")
    )

    static let claudeContentVariants = ClaudeMessageRequest(
        model: "claude-fixture-variants",
        messages: [
            ClaudeMessage(role: "user", content: .blocks([
                .image(ClaudeImageBlock(source: ClaudeImageSource(
                    mediaType: "image/png",
                    data: "<fixture-image-base64>"
                ))),
                .image(ClaudeImageBlock(source: ClaudeImageSource(
                    url: "https://example.test/fixture.png"
                ))),
                .document(ClaudeDocumentBlock(source: [
                    "type": AnyCodable("text"),
                    "text": AnyCodable("Inline fixture document"),
                ])),
                .document(ClaudeDocumentBlock(source: [
                    "type": AnyCodable("url"),
                    "url": AnyCodable("https://example.test/fixture.txt"),
                ])),
                .document(ClaudeDocumentBlock(source: [
                    "type": AnyCodable("base64"),
                    "data": AnyCodable("<fixture-document-base64>"),
                    "media_type": AnyCodable("application/pdf"),
                ])),
                .document(ClaudeDocumentBlock(source: [
                    "type": AnyCodable("future_document"),
                    "marker": AnyCodable("fixture-marker"),
                ])),
                .unknown(ClaudeUnknownContentBlock(
                    type: "future_content",
                    payload: [
                        "type": AnyCodable("future_content"),
                        "marker": AnyCodable("fixture-unknown"),
                    ]
                )),
            ])),
            ClaudeMessage(role: "assistant", content: .blocks([
                .redactedThinking(ClaudeRedactedThinkingBlock(data: "<fixture-redacted>")),
            ])),
            ClaudeMessage(role: "user", content: .blocks([
                .toolResult(ClaudeToolResultBlock(
                    toolUseId: "toolu_variants",
                    contentBlocks: [
                        .text(ClaudeTextBlock(
                            text: "Tool text",
                            cacheControl: ["type": AnyCodable("ephemeral")]
                        )),
                        .image(ClaudeImageBlock(source: ClaudeImageSource(
                            url: "https://example.test/tool.png"
                        ))),
                        .document(ClaudeDocumentBlock(source: [
                            "type": AnyCodable("text"),
                            "text": AnyCodable("Tool document"),
                        ])),
                        .thinking(ClaudeThinkingBlock(thinking: "Tool reasoning", signature: nil)),
                        .redactedThinking(ClaudeRedactedThinkingBlock(data: "<fixture-tool-redacted>")),
                        .unknown(ClaudeUnknownContentBlock(
                            type: "future_tool_content",
                            payload: [
                                "type": AnyCodable("future_tool_content"),
                                "value": AnyCodable(7),
                            ]
                        )),
                        .toolUse(ClaudeToolUseBlock(
                            id: "nested_toolu",
                            name: "nested_lookup",
                            input: ["query": AnyCodable("nested")]
                        )),
                        .toolResult(ClaudeToolResultBlock(
                            toolUseId: "nested_toolu",
                            content: "Nested result"
                        )),
                    ],
                    isError: true
                )),
            ])),
        ],
        systemBlocks: [
            ClaudeSystemBlock(
                type: "future_system_text",
                text: "Future system text",
                cacheControl: nil
            ),
        ],
        maxTokens: 512,
        stream: false
    )

    static let claudeEmptyDefaults = ClaudeMessageRequest(
        model: "claude-fixture-empty-defaults",
        messages: [
            ClaudeMessage(role: "user", content: .text("Plain fixture text")),
            ClaudeMessage(role: "user", content: .blocks([
                .toolResult(ClaudeToolResultBlock(
                    toolUseId: "toolu_empty_blocks",
                    contentBlocks: []
                )),
            ])),
            ClaudeMessage(role: "user", content: .blocks([
                .toolResult(ClaudeToolResultBlock(
                    toolUseId: "toolu_missing_content",
                    content: nil
                )),
            ])),
        ],
        system: "",
        maxTokens: 1,
        toolChoice: ClaudeToolChoice(
            type: "auto",
            disableParallelToolUse: false
        )
    )

    static let openAIChatRichToolLoop = OpenAIChatCompletionRequest(
        model: "gpt-4.1",
        messages: [
            OpenAIChatMessage(role: "system", content: .text("You are strict.")),
            OpenAIChatMessage(role: "user", content: .parts([
                .text(OpenAITextPart(text: "Summarize this")),
                .inputFile(OpenAIFilePart(fileId: "file_42", filename: "notes.txt")),
            ])),
            OpenAIChatMessage(
                role: "assistant",
                content: .text("I'll call a tool."),
                toolCalls: [
                    OpenAIToolCall(
                        id: "call_1",
                        function: OpenAIFunctionCall(
                            name: "lookup",
                            arguments: "{\"query\":\"quota\"}"
                        )
                    ),
                ]
            ),
            OpenAIChatMessage(
                role: "tool",
                content: .text("done"),
                toolCallId: "call_1"
            ),
        ],
        stream: true,
        tools: [
            OpenAITool(function: OpenAIFunction(
                name: "lookup",
                description: "Lookup docs",
                parameters: ["type": AnyCodable("object")]
            )),
        ],
        toolChoice: .function("lookup"),
        parallelToolCalls: false
    )

    static let openAIChatContentVariants = OpenAIChatCompletionRequest(
        model: "gpt-fixture-content-variants",
        messages: [
            OpenAIChatMessage(
                role: "system",
                content: .parts([
                    .text(OpenAITextPart(text: "System fixture")),
                    .unknown(OpenAIUnknownContentPart(
                        type: "future_system_part",
                        payload: [
                            "type": AnyCodable("future_system_part"),
                            "marker": AnyCodable("ignored-system-marker"),
                        ]
                    )),
                ]),
                name: "ignored-system-name"
            ),
            OpenAIChatMessage(role: "user", content: .text(""), name: "ignored-empty-name"),
            OpenAIChatMessage(
                role: "user",
                content: .parts([
                    .text(OpenAITextPart(text: "")),
                    .imageUrl(OpenAIImageUrlPart(imageUrl: OpenAIImageUrl(
                        url: "https://example.test/fixture.png",
                        detail: "high"
                    ))),
                    .imageUrl(OpenAIImageUrlPart(imageUrl: OpenAIImageUrl(
                        url: "data:image/png;base64,<fixture-image-base64>",
                        detail: "low"
                    ))),
                    .unknown(OpenAIUnknownContentPart(
                        type: "future_user_part",
                        payload: [
                            "type": AnyCodable("future_user_part"),
                            "marker": AnyCodable("ignored-user-marker"),
                        ]
                    )),
                ]),
                name: "fixture-user"
            ),
            OpenAIChatMessage(
                role: "assistant",
                content: .parts([
                    .text(OpenAITextPart(text: "Assistant fixture")),
                ]),
                name: "fixture-assistant",
                toolCalls: [
                    OpenAIToolCall(
                        id: "call_ordered",
                        function: OpenAIFunctionCall(
                            name: "ordered_tool",
                            arguments: "{\"order\":true}"
                        )
                    ),
                ],
                reasoningContent: "Reasoning fixture"
            ),
            OpenAIChatMessage(
                role: "assistant",
                toolCalls: [
                    OpenAIToolCall(
                        id: "call_content_only",
                        function: OpenAIFunctionCall(
                            name: "content_only_tool",
                            arguments: "{}"
                        )
                    ),
                ]
            ),
            OpenAIChatMessage(
                role: "tool",
                content: .parts([
                    .text(OpenAITextPart(text: "first tool line")),
                    .text(OpenAITextPart(text: "")),
                    .unknown(OpenAIUnknownContentPart(
                        type: "future_tool_part",
                        payload: [
                            "type": AnyCodable("future_tool_part"),
                            "marker": AnyCodable("ignored-tool-marker"),
                        ]
                    )),
                ])
            ),
            OpenAIChatMessage(
                role: "user",
                content: .parts([
                    .unknown(OpenAIUnknownContentPart(
                        type: "future_only_part",
                        payload: [
                            "type": AnyCodable("future_only_part"),
                            "marker": AnyCodable("ignored-only-marker"),
                        ]
                    )),
                ]),
                name: "ignored-unknown-only-name",
                reasoningContent: "ignored-user-reasoning"
            ),
        ]
    )

    static let openAIChatEmptyDefaults = OpenAIChatCompletionRequest(
        model: "gpt-fixture-empty-defaults",
        messages: [
            OpenAIChatMessage(role: "system", content: .text("")),
            OpenAIChatMessage(role: "user"),
            OpenAIChatMessage(role: "user", content: .parts([])),
            OpenAIChatMessage(role: "assistant", reasoningContent: ""),
            OpenAIChatMessage(role: "assistant", reasoningContent: " "),
            OpenAIChatMessage(
                role: "assistant",
                toolCalls: [
                    OpenAIToolCall(
                        id: "call_empty_defaults",
                        function: OpenAIFunctionCall(
                            name: "empty_defaults_tool",
                            arguments: ""
                        )
                    ),
                ]
            ),
            OpenAIChatMessage(role: "tool"),
        ],
        streamOptions: OpenAIChatCompletionRequest.StreamOptions(includeUsage: false),
        parallelToolCalls: true,
        promptCacheKey: "fixture-cache-key"
    )

    static let openAIChatResponseRichToolLoop = OpenAIChatCompletionResponse(
        id: "chatcmpl_fixture_canonical_001",
        created: 1_893_553_445,
        model: "gpt-fixture-canonical-response",
        choices: [
            OpenAIChoice(
                index: 7,
                message: OpenAIChatMessage(
                    role: "assistant",
                    content: .text("Fixture response"),
                    toolCalls: [
                        OpenAIToolCall(
                            id: "call_fixture_canonical_001",
                            function: OpenAIFunctionCall(
                                name: "lookup_fixture",
                                arguments: "{\"query\":\"quota\"}"
                            )
                        ),
                    ],
                    reasoningContent: "Synthetic reasoning summary"
                ),
                finishReason: "tool_calls"
            ),
            OpenAIChoice(
                index: 0,
                message: OpenAIChatMessage(
                    role: "assistant",
                    content: .text("Ignored second choice"),
                    reasoningContent: "Ignored second reasoning"
                ),
                finishReason: "length"
            ),
        ],
        usage: OpenAIUsage(
            promptTokens: 4_294_967_296,
            completionTokens: 128,
            totalTokens: 4_294_967_424,
            promptCacheHitTokens: 256,
            promptCacheMissTokens: 4_294_000_000,
            promptTokensDetails: OpenAIUsage.PromptTokensDetails(cachedTokens: 64)
        )
    )

    static let claudeResponseMixedBlocks = ClaudeMessageResponse(
        id: "msg_fixture_canonical_response_001",
        role: "assistant",
        content: [
            .text(ClaudeTextBlock(
                text: "Preface before reasoning.",
                cacheControl: ["type": AnyCodable("ephemeral")]
            )),
            .document(ClaudeDocumentBlock(
                source: [
                    "type": AnyCodable("text"),
                    "text": AnyCodable("Inline response document"),
                ],
                title: "Response fixture",
                context: "Preserve document metadata",
                citations: AnyCodable([
                    "enabled": AnyCodable(true),
                ] as [String: AnyCodable]),
                cacheControl: ["type": AnyCodable("ephemeral")]
            )),
            .unknown(ClaudeUnknownContentBlock(
                type: "future_response_content",
                payload: [
                    "type": AnyCodable("future_response_content"),
                    "marker": AnyCodable("fixture-unknown-response"),
                ]
            )),
            .thinking(ClaudeThinkingBlock(
                thinking: "Need the fixture tool.",
                signature: "sig_fixture_response_001"
            )),
            .text(ClaudeTextBlock(text: "Calling the tool now.")),
            .toolUse(ClaudeToolUseBlock(
                id: "toolu_fixture_response_001",
                name: "lookup_fixture",
                input: ["query": AnyCodable("quota")]
            )),
            .redactedThinking(ClaudeRedactedThinkingBlock(
                data: "<fixture-response-redacted>"
            )),
            .toolResult(ClaudeToolResultBlock(
                toolUseId: "toolu_fixture_response_001",
                contentBlocks: [
                    .text(ClaudeTextBlock(text: "First tool line")),
                    .document(ClaudeDocumentBlock(source: [
                        "type": AnyCodable("url"),
                        "url": AnyCodable("https://example.test/tool-result.txt"),
                    ])),
                    .text(ClaudeTextBlock(text: "Second tool line")),
                    .unknown(ClaudeUnknownContentBlock(
                        type: "future_tool_result_content",
                        payload: [
                            "type": AnyCodable("future_tool_result_content"),
                            "value": AnyCodable(7),
                        ]
                    )),
                ],
                isError: true
            )),
            .text(ClaudeTextBlock(text: "Tail after tool result.")),
        ],
        model: "claude-fixture-canonical-response",
        stopReason: "pause_turn",
        stopSequence: "<fixture-stop-sequence>",
        usage: ClaudeUsage(
            inputTokens: 4_294_967_296,
            outputTokens: 513,
            cacheCreationInputTokens: 64,
            cacheReadInputTokens: 32
        )
    )

    static func mapClaude(_ request: ClaudeMessageRequest) throws -> AnyCodable {
        let canonical = try CanonicalRequestMapper().mapClaude(request)
        return project(canonical)
    }

    static func mapOpenAIChat(_ request: OpenAIChatCompletionRequest) throws -> AnyCodable {
        let canonical = try CanonicalRequestMapper().mapOpenAIChatCompletions(request)
        return project(canonical)
    }

    static func mapOpenAIChatResponse(_ response: OpenAIChatCompletionResponse) throws -> AnyCodable {
        let canonical = try CanonicalResponseMapper().mapOpenAIChatCompletions(response)
        return project(canonical)
    }

    static func mapOpenAIResponsesResponse(_ response: OpenAIResponsesResponse) throws -> AnyCodable {
        let canonical = try CanonicalResponseMapper().mapOpenAIResponses(response)
        return project(canonical)
    }

    static func mapRawOpenAIResponsesResponse(_ raw: AnyCodable) throws -> AnyCodable {
        let response = try decodeRaw(OpenAIResponsesResponse.self, from: raw)
        return try mapOpenAIResponsesResponse(response)
    }

    static func mapOpenAIResponsesStopPriorityMatrix(_ raw: AnyCodable) throws -> AnyCodable {
        let matrix = try decodeRaw(OpenAIResponsesStopPriorityMatrix.self, from: raw)
        let mapper = CanonicalResponseMapper()
        let results = try matrix.cases.map { fixtureCase in
            let response = try mapper.mapOpenAIResponses(fixtureCase.response)
            return AnyCodable([
                "label": AnyCodable(fixtureCase.label),
                "reason": AnyCodable(response.stop.reason.value),
            ] as [String: AnyCodable])
        }
        return AnyCodable([
            "results": AnyCodable(results),
        ] as [String: AnyCodable])
    }

    static func mapClaudeResponse(_ response: ClaudeMessageResponse) throws -> AnyCodable {
        let canonical = try CanonicalResponseMapper().mapClaude(response)
        return project(canonical)
    }

    private static func decodeRaw<Value: Decodable>(
        _ type: Value.Type,
        from raw: AnyCodable
    ) throws -> Value {
        let data = try JSONEncoder().encode(raw)
        return try JSONDecoder().decode(type, from: data)
    }

    private struct OpenAIResponsesStopPriorityMatrix: Decodable {
        let cases: [OpenAIResponsesStopPriorityCase]
    }

    private struct OpenAIResponsesStopPriorityCase: Decodable {
        let label: String
        let response: OpenAIResponsesResponse
    }

    private static func project(_ request: CanonicalRequest) -> AnyCodable {
        var value: [String: AnyCodable] = [
            "modelHint": AnyCodable(request.modelHint),
            "system": AnyCodable(request.system.map(project)),
            "items": AnyCodable(request.items.map(project)),
            "tools": AnyCodable(request.tools.map(project)),
            "generationConfig": project(request.generationConfig),
            "metadata": project(request.metadata),
            "rawExtensions": AnyCodable(request.rawExtensions.map(project)),
        ]
        if let toolConfig = request.toolConfig {
            value["toolConfig"] = project(toolConfig)
        }
        return AnyCodable(value)
    }

    private static func project(_ response: CanonicalResponse) -> AnyCodable {
        var value: [String: AnyCodable] = [
            "items": AnyCodable(response.items.map(project)),
            "stop": project(response.stop),
            "rawExtensions": AnyCodable(response.rawExtensions.map(project)),
        ]
        value["id"] = response.id.map { AnyCodable($0) }
        value["model"] = response.model.map { AnyCodable($0) }
        value["usage"] = response.usage.map(project)
        return AnyCodable(value)
    }

    private static func project(_ stop: CanonicalStop) -> AnyCodable {
        var value: [String: AnyCodable] = [
            "reason": AnyCodable(stop.reason.value),
        ]
        value["sequence"] = stop.sequence.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ usage: CanonicalUsage) -> AnyCodable {
        var value: [String: AnyCodable] = [:]
        value["inputTokens"] = usage.inputTokens.map { AnyCodable($0) }
        value["outputTokens"] = usage.outputTokens.map { AnyCodable($0) }
        value["totalTokens"] = usage.totalTokens.map { AnyCodable($0) }
        value["cacheCreationInputTokens"] = usage.cacheCreationInputTokens.map { AnyCodable($0) }
        value["cacheReadInputTokens"] = usage.cacheReadInputTokens.map { AnyCodable($0) }
        value["reasoningTokens"] = usage.reasoningTokens.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ item: CanonicalConversationItem) -> AnyCodable {
        switch item {
        case .message(let message):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("message"),
                "role": AnyCodable(message.role.value),
                "parts": AnyCodable(message.parts.map(project)),
                "metadata": project(message.metadata),
                "rawExtensions": AnyCodable(message.rawExtensions.map(project)),
            ]
            if let phase = message.phase {
                value["phase"] = AnyCodable(phase.value)
            }
            if let name = message.name {
                value["name"] = AnyCodable(name)
            }
            return AnyCodable(value)

        case .toolCall(let toolCall):
            return AnyCodable([
                "type": AnyCodable("tool_call"),
                "id": AnyCodable(toolCall.id),
                "name": AnyCodable(toolCall.name),
                "inputJSON": AnyCodable(toolCall.inputJSON),
                "status": AnyCodable(toolCall.status.value),
                "partial": AnyCodable(toolCall.partial),
                "rawExtensions": AnyCodable(toolCall.rawExtensions.map(project)),
            ] as [String: AnyCodable])

        case .toolResult(let toolResult):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("tool_result"),
                "toolCallID": AnyCodable(toolResult.toolCallID),
                "parts": AnyCodable(toolResult.parts.map(project)),
                "rawExtensions": AnyCodable(toolResult.rawExtensions.map(project)),
            ]
            if let isError = toolResult.isError {
                value["isError"] = AnyCodable(isError)
            }
            if let rawTextFallback = toolResult.rawTextFallback {
                value["rawTextFallback"] = AnyCodable(rawTextFallback)
            }
            return AnyCodable(value)

        case .reasoning(let reasoning):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("reasoning"),
                "rawExtensions": AnyCodable(reasoning.rawExtensions.map(project)),
            ]
            value["summaryText"] = reasoning.summaryText.map { AnyCodable($0) }
            value["fullText"] = reasoning.fullText.map { AnyCodable($0) }
            value["encryptedContent"] = reasoning.encryptedContent.map { AnyCodable($0) }
            value["signature"] = reasoning.signature.map { AnyCodable($0) }
            value["redacted"] = reasoning.redacted.map { AnyCodable($0) }
            return AnyCodable(value)

        case .compaction(let compaction):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("compaction"),
                "rawExtensions": AnyCodable(compaction.rawExtensions.map(project)),
            ]
            value["id"] = compaction.id.map { AnyCodable($0) }
            value["encryptedContent"] = compaction.encryptedContent.map { AnyCodable($0) }
            return AnyCodable(value)

        case .hostedToolEvent(let event):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("hosted_tool_event"),
                "vendorType": AnyCodable(event.vendorType),
                "status": AnyCodable(event.status.value),
                "rawExtensions": AnyCodable(event.rawExtensions.map(project)),
            ]
            value["callID"] = event.callID.map { AnyCodable($0) }
            value["payload"] = event.payload.map(project)
            return AnyCodable(value)
        }
    }

    private static func project(_ part: CanonicalContentPart) -> AnyCodable {
        switch part {
        case .text(let text):
            return AnyCodable([
                "type": AnyCodable("text"),
                "text": AnyCodable(text.text),
                "rawExtensions": AnyCodable(text.rawExtensions.map(project)),
            ] as [String: AnyCodable])

        case .image(let image):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("image"),
                "source": AnyCodable(imageSourceValue(image.source)),
                "data": AnyCodable(image.data),
                "rawExtensions": AnyCodable(image.rawExtensions.map(project)),
            ]
            value["mediaType"] = image.mediaType.map { AnyCodable($0) }
            value["detail"] = image.detail.map { AnyCodable($0) }
            return AnyCodable(value)

        case .document(let document):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("document"),
                "source": project(document.source),
                "rawExtensions": AnyCodable(document.rawExtensions.map(project)),
            ]
            value["title"] = document.title.map { AnyCodable($0) }
            value["context"] = document.context.map { AnyCodable($0) }
            value["citations"] = document.citations.map(project)
            return AnyCodable(value)

        case .fileRef(let file):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("file_ref"),
                "rawExtensions": AnyCodable(file.rawExtensions.map(project)),
            ]
            value["fileID"] = file.fileID.map { AnyCodable($0) }
            value["filename"] = file.filename.map { AnyCodable($0) }
            value["mimeType"] = file.mimeType.map { AnyCodable($0) }
            value["downloadable"] = file.downloadable.map { AnyCodable($0) }
            return AnyCodable(value)

        case .reasoningText(let reasoning):
            return AnyCodable([
                "type": AnyCodable("reasoning_text"),
                "text": AnyCodable(reasoning.text),
                "rawExtensions": AnyCodable(reasoning.rawExtensions.map(project)),
            ] as [String: AnyCodable])

        case .refusal(let refusal):
            return AnyCodable([
                "type": AnyCodable("refusal"),
                "text": AnyCodable(refusal.text),
                "rawExtensions": AnyCodable(refusal.rawExtensions.map(project)),
            ] as [String: AnyCodable])

        case .unknown(let unknown):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("unknown"),
                "vendorType": AnyCodable(unknown.type),
                "rawExtensions": AnyCodable(unknown.rawExtensions.map(project)),
            ]
            value["payload"] = unknown.payload.map(project)
            return AnyCodable(value)
        }
    }

    private static func project(_ source: CanonicalDocumentSource) -> AnyCodable {
        switch source {
        case .inlineText(let text):
            return AnyCodable([
                "type": AnyCodable("inline_text"),
                "text": AnyCodable(text),
            ] as [String: AnyCodable])
        case .contentParts(let parts):
            return AnyCodable([
                "type": AnyCodable("content_parts"),
                "parts": AnyCodable(parts.map(project)),
            ] as [String: AnyCodable])
        case .url(let url):
            return AnyCodable([
                "type": AnyCodable("url"),
                "url": AnyCodable(url),
            ] as [String: AnyCodable])
        case .base64(let data, let mediaType):
            var value: [String: AnyCodable] = [
                "type": AnyCodable("base64"),
                "data": AnyCodable(data),
            ]
            value["mediaType"] = mediaType.map { AnyCodable($0) }
            return AnyCodable(value)
        case .fileID(let fileID):
            return AnyCodable([
                "type": AnyCodable("file_id"),
                "fileID": AnyCodable(fileID),
            ] as [String: AnyCodable])
        case .unknown(let value):
            return AnyCodable([
                "type": AnyCodable("unknown"),
                "value": project(value),
            ] as [String: AnyCodable])
        }
    }

    private static func project(_ tool: CanonicalToolDefinition) -> AnyCodable {
        var value: [String: AnyCodable] = [
            "kind": AnyCodable(toolKindValue(tool.kind)),
            "execution": AnyCodable(toolExecutionValue(tool.execution)),
            "flags": project(tool.flags),
            "rawExtensions": AnyCodable(tool.rawExtensions.map(project)),
        ]
        value["name"] = tool.name.map { AnyCodable($0) }
        value["description"] = tool.description.map { AnyCodable($0) }
        value["inputSchema"] = tool.inputSchema.map(project)
        value["vendorType"] = tool.vendorType.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ flags: CanonicalToolDefinitionFlags) -> AnyCodable {
        var value: [String: AnyCodable] = [:]
        value["eagerInputStreaming"] = flags.eagerInputStreaming.map { AnyCodable($0) }
        value["strict"] = flags.strict.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ config: CanonicalToolConfig) -> AnyCodable {
        var value: [String: AnyCodable] = [:]
        if let choice = config.choice {
            value["choice"] = project(choice)
        }
        value["parallelCallsAllowed"] = config.parallelCallsAllowed.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ choice: CanonicalToolChoice) -> AnyCodable {
        switch choice {
        case .none:
            return AnyCodable(["type": AnyCodable("none")] as [String: AnyCodable])
        case .auto:
            return AnyCodable(["type": AnyCodable("auto")] as [String: AnyCodable])
        case .required:
            return AnyCodable(["type": AnyCodable("required")] as [String: AnyCodable])
        case .specific(let name):
            return AnyCodable([
                "type": AnyCodable("specific"),
                "name": AnyCodable(name),
            ] as [String: AnyCodable])
        case .allowed(let names):
            return AnyCodable([
                "type": AnyCodable("allowed"),
                "names": AnyCodable(names.map { AnyCodable($0) }),
            ] as [String: AnyCodable])
        case .hosted(let name):
            return AnyCodable([
                "type": AnyCodable("hosted"),
                "name": AnyCodable(name),
            ] as [String: AnyCodable])
        case .custom(let name):
            return AnyCodable([
                "type": AnyCodable("custom"),
                "name": AnyCodable(name),
            ] as [String: AnyCodable])
        case .unknown(let raw):
            return AnyCodable([
                "type": AnyCodable("unknown"),
                "value": AnyCodable(raw),
            ] as [String: AnyCodable])
        }
    }

    private static func project(_ config: CanonicalGenerationConfig) -> AnyCodable {
        var value: [String: AnyCodable] = [
            "stopSequences": AnyCodable(config.stopSequences.map { AnyCodable($0) }),
        ]
        value["maxOutputTokens"] = config.maxOutputTokens.map { AnyCodable($0) }
        value["temperature"] = config.temperature.map { AnyCodable($0) }
        value["topP"] = config.topP.map { AnyCodable($0) }
        value["topK"] = config.topK.map { AnyCodable($0) }
        value["stream"] = config.stream.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ extensionValue: CanonicalVendorExtension) -> AnyCodable {
        AnyCodable([
            "vendor": AnyCodable(extensionValue.vendor),
            "key": AnyCodable(extensionValue.key),
            "value": project(extensionValue.value),
        ] as [String: AnyCodable])
    }

    private static func project(_ values: CanonicalJSONMap) -> AnyCodable {
        AnyCodable(values.mapValues(project))
    }

    private static func project(_ value: AnyCodable) -> AnyCodable {
        projectJSONValue(value.value)
    }

    private static func projectJSONValue(_ value: Any) -> AnyCodable {
        switch value {
        case let wrapped as AnyCodable:
            return project(wrapped)
        case let dictionary as [String: AnyCodable]:
            return AnyCodable(dictionary.mapValues(project))
        case let dictionary as [String: Any]:
            return AnyCodable(dictionary.mapValues(projectJSONValue))
        case let array as [AnyCodable]:
            return AnyCodable(array.map(project))
        case let array as [Any]:
            return AnyCodable(array.map(projectJSONValue))
        case let number as NSNumber:
            return projectFoundationJSONNumber(number)
        case let boolean as Bool:
            return AnyCodable(boolean)
        case let integer as Int:
            return AnyCodable(integer)
        case let number as Double:
            return AnyCodable(number)
        case let string as String:
            return AnyCodable(string)
        case is NSNull:
            return AnyCodable(NSNull())
        default:
            preconditionFailure("Unsupported canonical fixture JSON value: \(type(of: value))")
        }
    }

    private static func imageSourceValue(_ source: CanonicalImageSource) -> String {
        switch source {
        case .base64: return "base64"
        case .url: return "url"
        case .fileID: return "file_id"
        case .unknown(let raw): return raw
        }
    }

    private static func toolKindValue(_ kind: CanonicalToolDefinitionKind) -> String {
        switch kind {
        case .function: return "function"
        case .hosted: return "hosted"
        case .custom: return "custom"
        case .unknown(let raw): return raw
        }
    }

    private static func toolExecutionValue(_ execution: CanonicalToolExecution) -> String {
        switch execution {
        case .client: return "client"
        case .server: return "server"
        case .hosted: return "hosted"
        case .unknown(let raw): return raw
        }
    }
}

private enum CanonicalBridgeGoldenScenarios {
    static let claudeToOpenAIChatDocumentURLLossy = ClaudeMessageRequest(
        model: "claude-sonnet-4-5",
        messages: [
            ClaudeMessage(role: "user", content: .blocks([
                .document(ClaudeDocumentBlock(
                    source: [
                        "type": AnyCodable("url"),
                        "url": AnyCodable("https://example.com/spec"),
                    ],
                    title: "Spec"
                )),
            ])),
        ],
        maxTokens: 512
    )

    static let claudeToOpenAIChatRichToolLoop = ClaudeMessageRequest(
        model: "claude-sonnet-4-5",
        messages: [
            ClaudeMessage(role: "user", content: .blocks([
                .text(ClaudeTextBlock(text: "Summarize this file")),
                .document(ClaudeDocumentBlock(
                    source: [
                        "type": AnyCodable("file"),
                        "file_id": AnyCodable("file_123"),
                    ],
                    title: "report.pdf"
                )),
            ])),
            ClaudeMessage(role: "assistant", content: .blocks([
                .text(ClaudeTextBlock(text: "Let me inspect that")),
                .toolUse(ClaudeToolUseBlock(
                    id: "toolu_123",
                    name: "lookup",
                    input: ["topic": AnyCodable("quota")]
                )),
            ])),
            ClaudeMessage(role: "user", content: .blocks([
                .toolResult(ClaudeToolResultBlock(
                    toolUseId: "toolu_123",
                    contentBlocks: [
                        .text(ClaudeTextBlock(text: "Here is the chart")),
                        .image(ClaudeImageBlock(source: ClaudeImageSource(
                            type: "base64",
                            mediaType: "image/png",
                            data: "AAAA"
                        ))),
                    ]
                )),
            ])),
        ],
        system: "You are precise.",
        maxTokens: 1024,
        stream: true,
        tools: [
            ClaudeTool(
                name: "lookup",
                description: "Lookup docs",
                inputSchema: ["type": AnyCodable("object")]
            ),
        ],
        toolChoice: ClaudeToolChoice(
            type: "tool",
            name: "lookup",
            disableParallelToolUse: true
        )
    )

    static func buildOpenAIChat(_ request: ClaudeMessageRequest) throws -> AnyCodable {
        let canonical = try CanonicalRequestMapper().mapClaude(request)
        let built = try CanonicalOpenAIRequestBuilder().buildChatCompletionRequest(
            from: canonical,
            modelOverride: "gpt-4o-mini"
        )

        let encoder = JSONEncoder()
        encoder.outputFormatting = [.sortedKeys, .withoutEscapingSlashes]
        let payloadData = try encoder.encode(built.payload)
        let decodedPayload = try JSONDecoder().decode(AnyCodable.self, from: payloadData)
        guard var payload = decodedPayload.value as? [String: AnyCodable] else {
            preconditionFailure("Expected OpenAI chat request object")
        }
        payload.removeValue(forKey: "prompt_cache_key")

        return AnyCodable([
            "payload": AnyCodable(payload),
            "lossyNotes": AnyCodable(built.lossyNotes.map(project)),
        ] as [String: AnyCodable])
    }

    private static func project(_ note: CanonicalLossyNote) -> AnyCodable {
        var value: [String: AnyCodable] = [
            "code": AnyCodable(note.code),
            "message": AnyCodable(note.message),
            "severity": AnyCodable(note.severity.value),
            "rawExtensions": AnyCodable(note.rawExtensions.map(project)),
        ]
        value["itemIndex"] = note.itemIndex.map { AnyCodable($0) }
        value["path"] = note.path.map { AnyCodable($0) }
        return AnyCodable(value)
    }

    private static func project(_ extensionValue: CanonicalVendorExtension) -> AnyCodable {
        AnyCodable([
            "vendor": AnyCodable(extensionValue.vendor),
            "key": AnyCodable(extensionValue.key),
            "value": projectJSONValue(extensionValue.value.value),
        ] as [String: AnyCodable])
    }

    private static func projectJSONValue(_ value: Any) -> AnyCodable {
        switch value {
        case let wrapped as AnyCodable:
            return projectJSONValue(wrapped.value)
        case let dictionary as [String: AnyCodable]:
            return AnyCodable(dictionary.mapValues { projectJSONValue($0.value) })
        case let dictionary as [String: Any]:
            return AnyCodable(dictionary.mapValues(projectJSONValue))
        case let array as [AnyCodable]:
            return AnyCodable(array.map { projectJSONValue($0.value) })
        case let array as [Any]:
            return AnyCodable(array.map(projectJSONValue))
        case let number as NSNumber:
            return projectFoundationJSONNumber(number)
        case let boolean as Bool:
            return AnyCodable(boolean)
        case let integer as Int:
            return AnyCodable(integer)
        case let number as Double:
            return AnyCodable(number)
        case let string as String:
            return AnyCodable(string)
        case is NSNull:
            return AnyCodable(NSNull())
        default:
            preconditionFailure("Unsupported bridge fixture JSON value: \(type(of: value))")
        }
    }
}

private func projectFoundationJSONNumber(_ number: NSNumber) -> AnyCodable {
    if CFGetTypeID(number) == CFBooleanGetTypeID() {
        return AnyCodable(number.boolValue)
    }

    let objectiveCType = String(cString: number.objCType)
    if objectiveCType == "f" || objectiveCType == "d" {
        return AnyCodable(number.doubleValue)
    }

    return AnyCodable(number.intValue)
}

/// Windows UI/daemon RPC exposes account metadata only. The secret-bearing
/// `credential` field intentionally never crosses that process boundary.
private struct SafeAccountCredentialMetadataFixture: Codable {
    let id: String
    let providerId: String
    let accountLabel: String?
    let authMethod: AuthMethod
    let createdAt: String
    let lastUsedAt: String?
    let metadata: [String: AnyCodable]

    init(credential: AccountCredential) {
        id = credential.id
        providerId = credential.providerId
        accountLabel = credential.accountLabel
        authMethod = credential.authMethod
        createdAt = credential.createdAt
        lastUsedAt = credential.lastUsedAt
        metadata = credential.metadata.mapValues { AnyCodable($0) }
    }

    init(
        id: String,
        providerId: String,
        accountLabel: String?,
        authMethod: AuthMethod,
        createdAt: String,
        lastUsedAt: String?,
        metadata: [String: AnyCodable]
    ) {
        self.id = id
        self.providerId = providerId
        self.accountLabel = accountLabel
        self.authMethod = authMethod
        self.createdAt = createdAt
        self.lastUsedAt = lastUsedAt
        self.metadata = metadata
    }
}

enum GoldenSSEFraming: String, Codable {
    case encodedFrames = "encoded-frames"
    case responsesEventData = "responses-event-data"
    case chatDataLines = "chat-data-lines"
}

struct GoldenSSEFrame: Codable, Equatable {
    let event: String?
    let data: String
}

private struct GoldenSSELifecycleInput: Codable {
    let framing: GoldenSSEFraming
    let lines: [String]?
    let frames: [GoldenSSEFrame]?
}

private struct GoldenSSELifecycleExpected: Codable {
    let frames: [GoldenSSEFrame]
    let wire: String
}

private struct GoldenDecodeFailureExpected: Codable {
    let accepted: Bool
}

private enum GoldenFixtureError: Error {
    case expectedDecodeFailure(String)
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

    static func behaviorTransform<Input: Encodable, Expected: Encodable>(
        id: String,
        path: String,
        input: Input,
        sourceTest: String,
        transform: @escaping (Input) -> Expected
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-transform") {
            try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                inputData: try encodeFixtureValue(input),
                expectedData: try encodeFixtureValue(transform(input))
            )
        }
    }

    static func throwingBehaviorTransform<Input: Encodable, Expected: Encodable>(
        id: String,
        path: String,
        input: Input,
        sourceTest: String,
        transform: @escaping (Input) throws -> Expected
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-transform") {
            try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                inputData: try encodeFixtureValue(input),
                expectedData: try encodeFixtureValue(transform(input))
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

    static func decodeFailure<Value: Decodable>(
        id: String,
        path: String,
        inputJSON: String,
        as type: Value.Type,
        sourceTest: String = "ContractsGoldenExporterTests.testCatalogEncodesDeterministically"
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-decode-failure") {
            let inputData = Data(inputJSON.utf8)
            do {
                _ = try JSONDecoder().decode(type, from: inputData)
                throw GoldenFixtureError.expectedDecodeFailure(id)
            } catch is DecodingError {
                // Expected: only the stable acceptance outcome is frozen, not
                // Swift's localized or implementation-specific error message.
            }

            return try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                kind: "json-decode-failure",
                inputData: inputData,
                expectedData: try encodeFixtureValue(GoldenDecodeFailureExpected(accepted: false))
            )
        }
    }

    static func encodedSSELifecycle(
        id: String,
        path: String,
        frames: [GoldenSSEFrame],
        sourceTest: String = "ContractsGoldenExporterTests.testCatalogEncodesDeterministically"
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-transform") {
            let input = GoldenSSELifecycleInput(framing: .encodedFrames, lines: nil, frames: frames)
            let expected = GoldenSSELifecycleExpected(frames: frames, wire: encodeSSEWire(frames))
            return try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                inputData: try encodeFixtureValue(input),
                expectedData: try encodeFixtureValue(expected)
            )
        }
    }

    static func parsedSSELifecycle(
        id: String,
        path: String,
        framing: GoldenSSEFraming,
        lines: [String],
        sourceTest: String = "ContractsGoldenExporterTests.testCatalogEncodesDeterministically"
    ) -> GoldenFixtureCase {
        GoldenFixtureCase(id: id, path: path, kind: "json-transform") {
            let frames: [GoldenSSEFrame]
            switch framing {
            case .responsesEventData:
                frames = parseResponsesFrames(lines)
            case .chatDataLines:
                frames = parseChatDataFrames(lines)
            case .encodedFrames:
                preconditionFailure("Use encodedSSELifecycle for encoded frames")
            }

            let input = GoldenSSELifecycleInput(framing: framing, lines: lines, frames: nil)
            let expected = GoldenSSELifecycleExpected(frames: frames, wire: encodeSSEWire(frames))
            return try renderEnvelope(
                id: id,
                sourceTest: sourceTest,
                inputData: try encodeFixtureValue(input),
                expectedData: try encodeFixtureValue(expected)
            )
        }
    }

    private static func parseResponsesFrames(_ lines: [String]) -> [GoldenSSEFrame] {
        var frames: [GoldenSSEFrame] = []
        var currentEvent: String?
        var dataLines: [String] = []

        func flushFrame() {
            guard !dataLines.isEmpty else { return }
            frames.append(GoldenSSEFrame(event: currentEvent, data: dataLines.joined(separator: "\n")))
            currentEvent = nil
            dataLines = []
        }

        for line in lines {
            if line.isEmpty {
                flushFrame()
            } else if line.hasPrefix("event:") {
                flushFrame()
                currentEvent = String(line.dropFirst("event:".count)).trimmingCharacters(in: .whitespaces)
            } else if line.hasPrefix("data:") {
                dataLines.append(String(line.dropFirst("data:".count)).trimmingCharacters(in: .whitespaces))
            }
        }

        flushFrame()
        return frames
    }

    private static func parseChatDataFrames(_ lines: [String]) -> [GoldenSSEFrame] {
        lines.compactMap { line in
            guard line.hasPrefix("data:") else { return nil }
            return GoldenSSEFrame(
                event: nil,
                data: String(line.dropFirst("data:".count)).trimmingCharacters(in: .whitespaces)
            )
        }
    }

    private static func encodeSSEWire(_ frames: [GoldenSSEFrame]) -> String {
        let encoder = SSEEncoder()
        return frames.map { encoder.encode(event: $0.event, data: $0.data) }.joined()
    }

    private static func encodeFixtureValue<T: Encodable>(_ value: T) throws -> Data {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.sortedKeys, .withoutEscapingSlashes]
        return try encoder.encode(value)
    }

    private static func renderEnvelope(
        id: String,
        sourceTest: String,
        kind: String = "json-transform",
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
            "kind": kind,
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
