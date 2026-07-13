enum ContractsProxyGoldenInputs {
    static let claudeMessageRequestJSON = #"""
    {
      "model": "claude-fixture-1",
      "messages": [
        {
          "role": "user",
          "content": [
            { "type": "text", "text": "Inspect the fixture", "cache_control": { "type": "ephemeral" } },
            { "type": "image", "source": { "type": "url", "url": "https://example.test/fixture.png" } },
            {
              "type": "document",
              "source": { "type": "text", "media_type": "text/plain", "data": "Synthetic fixture document" },
              "title": "Fixture document",
              "context": "Contract roundtrip",
              "citations": { "enabled": true },
              "cache_control": { "type": "ephemeral" }
            },
            { "type": "tool_use", "id": "toolu_fixture_001", "name": "lookup_fixture", "input": { "query": "quota", "limit": 2 } }
          ]
        },
        {
          "role": "user",
          "content": [
            {
              "type": "tool_result",
              "tool_use_id": "toolu_fixture_001",
              "content": [
                { "type": "text", "text": "Fixture result" },
                { "type": "fixture_future_block", "note": "Preserve unknown content" }
              ],
              "is_error": false
            }
          ]
        }
      ],
      "system": [
        { "type": "text", "text": "You process synthetic fixtures.", "cache_control": { "type": "ephemeral" } }
      ],
      "max_tokens": 4096,
      "temperature": 0.2,
      "top_p": 0.9,
      "top_k": 40,
      "stop_sequences": ["<fixture-stop>"],
      "stream": true,
      "tools": [
        {
          "name": "lookup_fixture",
          "description": "Reads synthetic fixture data",
          "input_schema": {
            "type": "object",
            "properties": {
              "query": { "type": "string" },
              "limit": { "type": "integer" }
            },
            "required": ["query"]
          },
          "eager_input_streaming": true
        }
      ],
      "tool_choice": { "type": "tool", "name": "lookup_fixture", "disable_parallel_tool_use": true },
      "metadata": { "user_id": "fixture-user-001" },
      "thinking": { "type": "enabled", "budget_tokens": 128, "display": "summarized" },
      "output_config": {
        "effort": "medium",
        "format": {
          "type": "json_schema",
          "schema": {
            "type": "object",
            "properties": { "answer": { "type": "string" } },
            "required": ["answer"]
          }
        }
      }
    }
    """#

    static let claudeMessageResponseJSON = #"""
    {
      "id": "msg_fixture_001",
      "type": "message",
      "role": "assistant",
      "content": [
        { "type": "thinking", "thinking": "Use the fixture tool.", "signature": "<fixture-thinking-signature>" },
        { "type": "text", "text": "Fixture response" },
        { "type": "tool_use", "id": "toolu_fixture_002", "name": "lookup_fixture", "input": { "query": "usage" } },
        { "type": "fixture_future_block", "note": "Preserve unknown response content", "count": 3 }
      ],
      "model": "claude-fixture-1",
      "stop_reason": "tool_use",
      "stop_sequence": "<fixture-stop>",
      "usage": {
        "input_tokens": 4294967296,
        "output_tokens": 128,
        "cache_creation_input_tokens": 64,
        "cache_read_input_tokens": 32
      }
    }
    """#

    static let claudeContentBlockDeltaJSON = #"""
    {
      "type": "content_block_delta",
      "index": 1,
      "delta": {
        "type": "input_json_delta",
        "partial_json": "{\"query\":\"fixture"
      }
    }
    """#

    static let claudeTokenCountStructuredSystemJSON = #"""
    {
      "model": "claude-fixture-token-count",
      "messages": [
        {
          "role": "user",
          "content": "Count the synthetic fixture input."
        }
      ],
      "system": [
        {
          "type": "text",
          "text": "Use synthetic fixture data only."
        },
        {
          "type": "text",
          "text": "Preserve the structured system array.",
          "cache_control": { "type": "ephemeral" }
        }
      ],
      "tools": [
        {
          "name": "lookup_fixture",
          "description": "Reads synthetic fixture data",
          "input_schema": {
            "type": "object",
            "properties": { "query": { "type": "string" } },
            "required": ["query"]
          }
        }
      ]
    }
    """#

    static let codexOutputItemAddedEventJSON = #"""
    {
      "type": "response.output_item.added",
      "output_index": 4294967296,
      "item": {
        "type": "function_call",
        "id": "fc_fixture_added",
        "call_id": "call_fixture_added",
        "name": "lookup_fixture",
        "arguments": "",
        "status": "in_progress",
        "created_by": "assistant",
        "namespace": "fixture"
      }
    }
    """#

    static let codexOutputItemDoneEventJSON = #"""
    {
      "type": "response.output_item.done",
      "output_index": 4294967297,
      "item": {
        "type": "function_call",
        "id": "fc_fixture_done",
        "call_id": "call_fixture_done",
        "name": "lookup_fixture",
        "arguments": "{\"query\":\"quota\"}",
        "status": "completed"
      }
    }
    """#

    static let codexOutputTextDeltaEventJSON = #"""
    {
      "type": "response.output_text.delta",
      "item_id": "msg_fixture_delta",
      "output_index": 4294967298,
      "content_index": 4294967299,
      "delta": "Fixture 响应"
    }
    """#

    static let codexReasoningSummaryTextDeltaEventJSON = #"""
    {
      "type": "response.reasoning_summary_text.delta",
      "item_id": null,
      "output_index": 4294967300,
      "summary_index": null,
      "delta": "Inspect the synthetic fixture first."
    }
    """#

    static let codexFunctionCallArgumentsDeltaEventJSON = #"""
    {
      "type": "response.function_call_arguments.delta",
      "item_id": null,
      "output_index": 4294967301,
      "delta": "{\"query\":\"fixture"
    }
    """#

    static let codexFunctionCallArgumentsDoneEventJSON = #"""
    {
      "type": "response.function_call_arguments.done",
      "item_id": "fc_fixture_arguments_done",
      "output_index": 4294967302,
      "arguments": null,
      "name": null,
      "item": {
        "type": "function_call",
        "id": "fc_fixture_arguments_done",
        "call_id": "call_fixture_arguments_done",
        "name": "lookup_fixture",
        "arguments": "{\"query\":\"fixture\"}",
        "status": "completed"
      }
    }
    """#

    static let openAIChatRequestJSON = #"""
    {
      "model": "gpt-fixture-chat",
      "messages": [
        { "role": "system", "content": "Use synthetic fixture data only." },
        {
          "role": "user",
          "name": "fixture-user",
          "content": [
            { "type": "text", "text": "Inspect the attachment" },
            { "type": "image_url", "image_url": { "url": "https://example.test/fixture.png", "detail": "high" } },
            { "type": "file", "file": { "file_id": "file_fixture_001", "filename": "fixture.txt" } }
          ]
        },
        {
          "role": "assistant",
          "content": "Calling the fixture tool",
          "reasoning_content": "Synthetic reasoning summary",
          "tool_calls": [
            {
              "id": "call_fixture_001",
              "type": "function",
              "function": { "name": "lookup_fixture", "arguments": "{\"query\":\"usage\"}" }
            }
          ]
        },
        { "role": "tool", "content": "Fixture tool result", "tool_call_id": "call_fixture_001" }
      ],
      "temperature": 0.25,
      "top_p": 0.95,
      "max_tokens": 2048,
      "stop": ["<fixture-stop>"],
      "stream": true,
      "stream_options": { "include_usage": true },
      "tools": [
        {
          "type": "function",
          "function": {
            "name": "lookup_fixture",
            "description": "Reads synthetic fixture data",
            "parameters": {
              "type": "object",
              "properties": { "query": { "type": "string" } },
              "required": ["query"]
            }
          }
        }
      ],
      "tool_choice": { "type": "function", "function": { "name": "lookup_fixture" } },
      "parallel_tool_calls": false,
      "prompt_cache_key": "fixture-cache-route"
    }
    """#

    static let openAIChatResponseJSON = #"""
    {
      "id": "chatcmpl_fixture_001",
      "object": "chat.completion",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [
        {
          "index": 0,
          "message": {
            "role": "assistant",
            "content": "Fixture response",
            "reasoning_content": "Synthetic reasoning summary",
            "tool_calls": [
              {
                "id": "call_fixture_002",
                "type": "function",
                "function": { "name": "lookup_fixture", "arguments": "{\"query\":\"quota\"}" }
              }
            ]
          },
          "finish_reason": "tool_calls"
        }
      ],
      "usage": {
        "prompt_tokens": 4294967296,
        "completion_tokens": 128,
        "total_tokens": 4294967424,
        "prompt_cache_hit_tokens": 256,
        "prompt_cache_miss_tokens": 4294967040,
        "prompt_tokens_details": { "cached_tokens": 256 }
      }
    }
    """#

    static let openAIChatStreamChunkJSON = #"""
    {
      "id": "chatcmpl_fixture_stream_001",
      "object": "chat.completion.chunk",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [
        {
          "index": 0,
          "delta": {
            "role": "assistant",
            "reasoning_content": "Synthetic reasoning delta",
            "tool_calls": [
              {
                "index": 0,
                "id": "call_fixture_stream_001",
                "type": "function",
                "function": { "name": "lookup_fixture", "arguments": "{\"query\":" }
              }
            ]
          },
          "finish_reason": null
        }
      ],
      "usage": {
        "prompt_tokens": 4294967296,
        "completion_tokens": 16,
        "total_tokens": 4294967312,
        "prompt_tokens_details": { "cached_tokens": 256 }
      }
    }
    """#

    static let openAIChatMalformedUsageJSON = #"""
    {
      "id": "chatcmpl_fixture_malformed_usage",
      "object": "chat.completion",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [],
      "usage": {
        "prompt_tokens": "not-a-number",
        "completion_tokens": null
      }
    }
    """#

    static let openAIChatUsageOnlyStreamChunkJSON = #"""
    {
      "usage": {
        "prompt_tokens": null,
        "completion_tokens": 16
      }
    }
    """#

    static let codexResponsesRequestJSON = #"""
    {
      "model": "gpt-fixture-codex",
      "input": [
        {
          "type": "message",
          "role": "user",
          "content": [
            { "type": "input_text", "text": "Inspect the fixture workspace" },
            { "type": "input_image", "image_url": "https://example.test/fixture.png", "detail": "high", "status": "completed" },
            { "type": "input_file", "file_id": "file_fixture_002", "filename": "fixture.md", "status": "completed" }
          ]
        },
        {
          "type": "function_call",
          "id": "fc_fixture_001",
          "call_id": "call_fixture_003",
          "name": "lookup_fixture",
          "arguments": "{\"path\":\"/fixture/workspace/README.md\"}",
          "status": "completed",
          "created_by": "fixture-agent",
          "namespace": "fixture"
        },
        {
          "type": "function_call_output",
          "id": "fco_fixture_001",
          "call_id": "call_fixture_003",
          "output": [
            { "type": "input_text", "text": "Fixture README contents" },
            { "type": "input_file", "file_url": "https://example.test/fixture-result.txt", "filename": "fixture-result.txt" }
          ],
          "status": "completed",
          "created_by": "fixture-agent"
        }
      ],
      "temperature": 0.1,
      "top_p": 0.9,
      "max_output_tokens": 4096,
      "stream": true,
      "store": false,
      "tools": [
        {
          "type": "function",
          "name": "lookup_fixture",
          "description": "Reads synthetic fixture data",
          "parameters": {
            "type": "object",
            "properties": { "path": { "type": "string" } },
            "required": ["path"]
          },
          "strict": true,
          "defer_loading": false
        },
        { "type": "apply_patch" }
      ],
      "tool_choice": {
        "type": "allowed_tools",
        "mode": "required",
        "tools": [
          { "type": "function", "name": "lookup_fixture" },
          { "type": "apply_patch" }
        ]
      },
      "parallel_tool_calls": false
    }
    """#

    static let codexResponsesResponseJSON = #"""
    {
      "id": "resp_fixture_001",
      "object": "response",
      "created_at": 1893553445,
      "model": "gpt-fixture-codex",
      "output": [
        {
          "id": "rs_fixture_001",
          "type": "reasoning",
          "summary": [
            { "type": "summary_text", "text": "Inspect the synthetic workspace." }
          ],
          "content": [
            { "type": "reasoning_text", "text": "Use the fixture lookup tool." }
          ],
          "encrypted_content": "<fixture-encrypted-reasoning>",
          "status": "completed"
        },
        {
          "id": "msg_fixture_002",
          "type": "message",
          "role": "assistant",
          "status": "completed",
          "phase": "final_answer",
          "content": [
            { "type": "output_text", "text": "Fixture response" }
          ]
        },
        {
          "id": "fc_fixture_002",
          "type": "function_call",
          "call_id": "call_fixture_004",
          "name": "lookup_fixture",
          "arguments": "{\"path\":\"/fixture/workspace/config.json\"}",
          "status": "completed",
          "created_by": "fixture-agent",
          "namespace": "fixture"
        }
      ],
      "status": "completed",
      "usage": {
        "input_tokens": 4294967296,
        "output_tokens": 128,
        "total_tokens": 4294967424,
        "input_tokens_details": { "cached_tokens": 256 }
      }
    }
    """#

    static let codexResponsesCompletedEventJSON = #"""
    {
      "type": "response.completed",
      "response": {
        "id": "resp_fixture_stream_001",
        "object": "response",
        "created_at": 1893553445,
        "model": "gpt-fixture-codex",
        "output": [
          {
            "id": "msg_fixture_stream_001",
            "type": "message",
            "role": "assistant",
            "status": "completed",
            "content": [
              { "type": "output_text", "text": "Fixture stream completed" }
            ]
          }
        ],
        "status": "completed",
        "usage": {
          "input_tokens": 4294967296,
          "output_tokens": 64,
          "total_tokens": 4294967360,
          "input_tokens_details": { "cached_tokens": 128 }
        }
      }
    }
    """#
}
