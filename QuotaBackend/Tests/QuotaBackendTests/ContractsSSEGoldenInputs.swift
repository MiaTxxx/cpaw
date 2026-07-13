enum ContractsSSEGoldenInputs {
    static let claudeLifecycleFrames: [GoldenSSEFrame] = [
        GoldenSSEFrame(
            event: "message_start",
            data: #"{"type":"message_start","message":{"id":"msg_fixture_lifecycle_001","type":"message","role":"assistant","content":[],"model":"claude-fixture-1","stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":4294967296,"output_tokens":0}}}"#
        ),
        GoldenSSEFrame(
            event: "content_block_start",
            data: #"{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"#
        ),
        GoldenSSEFrame(
            event: "content_block_delta",
            data: #"{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello \"Windows\"\n你好"}}"#
        ),
        GoldenSSEFrame(
            event: "content_block_stop",
            data: #"{"type":"content_block_stop","index":0}"#
        ),
        GoldenSSEFrame(
            event: "message_delta",
            data: #"{"type":"message_delta","delta":{"stop_reason":"end_turn","stop_sequence":null},"usage":{"output_tokens":7}}"#
        ),
        GoldenSSEFrame(event: "message_stop", data: #"{"type":"message_stop"}"#),
    ]

    // AsyncBytes.lines can hide blank separators. A new event line must flush
    // the previous frame, and the final frame must be flushed at EOF.
    static let codexResponsesLines: [String] = [
        "event: response.created",
        #"data: {"type":"response.created","response":{"id":"resp_fixture_lifecycle_001","object":"response","created_at":1893553445,"model":"gpt-fixture-codex","status":"in_progress","output":[]}}"#,
        "event: response.output_item.added",
        #"data: {"type":"response.output_item.added","output_index":0,"item":{"id":"msg_fixture_lifecycle_001","type":"message","role":"assistant","status":"in_progress","content":[]}}"#,
        "event: response.output_text.delta",
        #"data: {"type":"response.output_text.delta","item_id":"msg_fixture_lifecycle_001","output_index":0,"content_index":0,"delta":"Fixture response"}"#,
        "event: response.output_text.done",
        #"data: {"type":"response.output_text.done","item_id":"msg_fixture_lifecycle_001","output_index":0,"content_index":0,"text":"Fixture response"}"#,
        "event: response.output_item.done",
        #"data: {"type":"response.output_item.done","output_index":0,"item":{"id":"msg_fixture_lifecycle_001","type":"message","role":"assistant","status":"completed","content":[{"type":"output_text","text":"Fixture response"}]}}"#,
        "event: response.completed",
        #"data: {"type":"response.completed","response":{"id":"resp_fixture_lifecycle_001","object":"response","created_at":1893553445,"model":"gpt-fixture-codex","status":"completed","output":[{"id":"msg_fixture_lifecycle_001","type":"message","role":"assistant","status":"completed","content":[{"type":"output_text","text":"Fixture response"}]}],"usage":{"input_tokens":4294967296,"output_tokens":7,"total_tokens":4294967303,"input_tokens_details":{"cached_tokens":256}}}}"#,
    ]

    static let openCodeChatLines: [String] = [
        #"data:{"id":"chatcmpl_fixture_lifecycle_001","object":"chat.completion.chunk","created":1893553445,"model":"gpt-fixture-chat","choices":[{"index":0,"delta":{"role":"assistant","content":"Hello"},"finish_reason":null}],"usage":null}"#,
        #"data:   {"id":"chatcmpl_fixture_lifecycle_001","object":"chat.completion.chunk","created":1893553445,"model":"gpt-fixture-chat","choices":[{"index":0,"delta":{"content":" Windows"},"finish_reason":"stop"}],"usage":null}"#,
        #"data: {"id":"chatcmpl_fixture_lifecycle_001","object":"chat.completion.chunk","created":1893553445,"model":"gpt-fixture-chat","choices":[],"usage":{"prompt_tokens":4294967296,"completion_tokens":7,"total_tokens":4294967303,"prompt_tokens_details":{"cached_tokens":256}}}"#,
        "data: [DONE]",
    ]
}
