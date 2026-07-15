import Foundation
@testable import QuotaBackend

extension CanonicalRequestGoldenScenarios {
    private static let claudeBuilderRichMessageParts: [CanonicalContentPart] = [
        .text(CanonicalTextPart(text: "Hello from canonical.")),
        .image(CanonicalImagePart(
            source: .base64,
            data: "AAAA",
            mediaType: "image/png"
        )),
        .document(CanonicalDocumentPart(
            source: .inlineText("Inline document text"),
            title: "Inline title",
            context: "Inline context",
            citations: AnyCodable([
                "source": AnyCodable("fixture")
            ] as [String: AnyCodable])
        )),
        .document(CanonicalDocumentPart(
            source: .url("https://example.test/fixture.pdf")
        )),
        .document(CanonicalDocumentPart(
            source: .base64(data: "BASE64", mediaType: nil)
        )),
        .document(CanonicalDocumentPart(
            source: .fileID("file_doc_001")
        )),
        .fileRef(CanonicalFileReference(
            fileID: "file_ref_001",
            filename: "report.txt"
        )),
        .reasoningText(CanonicalReasoningTextPart(text: "Reasoning text")),
        .refusal(CanonicalRefusalPart(text: "Refusal text")),
        .unknown(CanonicalUnknownPart(
            type: "future_part",
            payload: AnyCodable([
                "marker": AnyCodable("ignored")
            ] as [String: AnyCodable])
        )),
    ]

    private static let claudeBuilderBoundaryMessageParts: [CanonicalContentPart] = [
        .image(CanonicalImagePart(
            source: .url,
            data: "https://example.test/image.png",
            mediaType: "image/png"
        )),
        .image(CanonicalImagePart(
            source: .base64,
            data: "AAAA",
            mediaType: nil
        )),
        .unknown(CanonicalUnknownPart(type: "future_part")),
    ]

    private static let claudeBuilderRichItems: [CanonicalConversationItem] = [
        .message(CanonicalMessage(
            role: .assistant,
            parts: claudeBuilderRichMessageParts
        )),
        .toolCall(CanonicalToolCall(
            id: "toolu_fixture_response_001",
            name: "lookup",
            inputJSON: #"{"query":"fixture","limit":2}"#
        )),
        .reasoning(CanonicalReasoningItem(
            fullText: "Full reasoning",
            signature: "sig_fixture_response_001"
        )),
        .reasoning(CanonicalReasoningItem(
            encryptedContent: "encrypted-redacted",
            redacted: true,
            rawExtensions: [CanonicalVendorExtension(
                vendor: "claude",
                key: "redacted_data",
                value: AnyCodable("<redacted-fixture>")
            )]
        )),
        .toolResult(CanonicalToolResult(toolCallID: "toolu_fixture_response_001")),
        .compaction(CanonicalCompactionItem(
            id: "compact_fixture_001",
            encryptedContent: "encrypted-compaction"
        )),
        .hostedToolEvent(CanonicalHostedToolEvent(
            vendorType: "computer_call",
            callID: "call_hosted_001",
            status: .completed,
            payload: AnyCodable(["marker": AnyCodable("ignored")] as [String: AnyCodable])
        )),
        .message(CanonicalMessage(
            role: .user,
            parts: [.text(CanonicalTextPart(text: "ignored user message"))]
        )),
    ]

    private static let claudeBuilderBoundaryItems: [CanonicalConversationItem] = [
        .toolCall(CanonicalToolCall(
            id: "toolu_invalid_array",
            name: "invalid_array",
            inputJSON: "[1,2,3]"
        )),
        .toolCall(CanonicalToolCall(
            id: "toolu_invalid_scalar",
            name: "invalid_scalar",
            inputJSON: "42"
        )),
        .reasoning(CanonicalReasoningItem(summaryText: "Summary fallback")),
        .reasoning(CanonicalReasoningItem(fullText: "")),
        .reasoning(CanonicalReasoningItem()),
        .reasoning(CanonicalReasoningItem(
            encryptedContent: "encrypted-fallback",
            redacted: true,
            rawExtensions: [CanonicalVendorExtension(
                vendor: "other",
                key: "redacted_data",
                value: AnyCodable("wrong-vendor")
            )]
        )),
        .reasoning(CanonicalReasoningItem(
            encryptedContent: "encrypted-non-string",
            redacted: true,
            rawExtensions: [CanonicalVendorExtension(
                vendor: "claude",
                key: "redacted_data",
                value: AnyCodable(7)
            )]
        )),
        .reasoning(CanonicalReasoningItem(redacted: true)),
        .message(CanonicalMessage(
            role: .assistant,
            parts: claudeBuilderBoundaryMessageParts
        )),
        .toolResult(CanonicalToolResult(toolCallID: "toolu_skipped")),
        .compaction(CanonicalCompactionItem()),
        .hostedToolEvent(CanonicalHostedToolEvent(
            vendorType: "future_hosted",
            status: .unknown("future")
        )),
    ]

    static let claudeBuilderRichContent = builderInput(
        response: CanonicalResponse(
            id: "msg_fixture_claude_builder_response_001",
            model: "claude-source-model",
            items: claudeBuilderRichItems,
            stop: CanonicalStop(
                reason: .pauseTurn,
                sequence: "<fixture-response-sequence>"
            ),
            usage: CanonicalUsage(
                inputTokens: 4_294_967_296,
                outputTokens: 4_294_967_297,
                totalTokens: 8_589_934_593,
                cacheCreationInputTokens: 64,
                cacheReadInputTokens: 32,
                reasoningTokens: 16
            )
        ),
        originalModel: "claude-override-model"
    )

    static let claudeBuilderBoundaryLossy = builderInput(
        response: CanonicalResponse(
            id: "msg_fixture_claude_builder_boundary",
            model: nil,
            items: claudeBuilderBoundaryItems,
            stop: CanonicalStop(reason: .unknown("future_stop")),
            usage: nil
        )
    )

    private static func builderInput(
        response: CanonicalResponse,
        originalModel: String? = nil
    ) -> CanonicalClaudeResponseBuilderGoldenInput {
        CanonicalClaudeResponseBuilderGoldenInput(
            runtimeResponse: response,
            projectedResponse: project(response),
            originalModel: originalModel
        )
    }

    static func buildClaudeResponse(
        _ input: CanonicalClaudeResponseBuilderGoldenInput
    ) throws -> CanonicalClaudeResponseBuilderGoldenOutput {
        let built = try CanonicalClaudeResponseBuilder().buildMessageResponse(
            from: input.runtimeResponse,
            originalModel: input.originalModel
        )
        return CanonicalClaudeResponseBuilderGoldenOutput(
            payload: built.payload,
            lossyNotes: built.lossyNotes.map(CanonicalBridgeGoldenScenarios.project)
        )
    }
}

struct CanonicalClaudeResponseBuilderGoldenInput: Encodable {
    let runtimeResponse: CanonicalResponse
    let projectedResponse: AnyCodable
    let originalModel: String?

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(projectedResponse, forKey: .response)
        try container.encodeIfPresent(originalModel, forKey: .originalModel)
    }

    private enum CodingKeys: String, CodingKey {
        case response
        case originalModel
    }
}

struct CanonicalClaudeResponseBuilderGoldenOutput: Encodable {
    let payload: ClaudeMessageResponse
    let lossyNotes: [AnyCodable]
}

