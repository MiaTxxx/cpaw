import CryptoKit
import Foundation
import XCTest
@testable import QuotaBackend

final class ContractsGoldenExporterTests: XCTestCase {
    func testCatalogEncodesDeterministically() throws {
        let cases = try ContractsV1GoldenCatalog.makeCases()
        XCTAssertEqual(cases.count, 83)

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
