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

    static let claudeMessageRequestStringSystemTextContentJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        { "role": "user", "content": "Inspect the synthetic fixture." }
      ],
      "system": "Use synthetic fixture data only.",
      "max_tokens": 64
    }
    """#

    static let claudeMessageRequestSystemBlocksMessageBlocksJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        {
          "role": "user",
          "content": [
            { "type": "text", "text": "Preserve the message block array." }
          ]
        }
      ],
      "system": [
        { "type": "text", "text": "First fixture instruction." },
        { "type": "text", "text": "Second fixture instruction.", "cache_control": { "type": "ephemeral" } }
      ],
      "max_tokens": 64
    }
    """#

    static let claudeMessageRequestImageSourcesJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "image",
              "source": { "type": "base64", "media_type": "image/png", "data": "<fixture-base64-image>" }
            },
            {
              "type": "image",
              "source": { "type": "url", "url": "https://example.test/fixture-image.png" }
            }
          ]
        }
      ],
      "max_tokens": 64
    }
    """#

    static let claudeMessageRequestDocumentKnownJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "document",
              "source": { "type": "text", "media_type": "text/plain", "data": "Synthetic document body" },
              "title": "Fixture document",
              "context": "Known document branch",
              "citations": { "enabled": true },
              "cache_control": { "type": "ephemeral" }
            }
          ]
        }
      ],
      "max_tokens": 64
    }
    """#

    static let claudeMessageRequestToolResultStringJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "tool_result",
              "tool_use_id": "toolu_fixture_string",
              "content": "Synthetic tool result",
              "is_error": true
            }
          ]
        }
      ],
      "max_tokens": 64
    }
    """#

    static let claudeMessageRequestToolResultBlocksJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "tool_result",
              "tool_use_id": "toolu_fixture_blocks",
              "content": [
                { "type": "text", "text": "Synthetic structured result" },
                {
                  "type": "image",
                  "source": { "type": "url", "url": "https://example.test/tool-result.png" }
                }
              ],
              "is_error": false
            }
          ]
        }
      ],
      "max_tokens": 64
    }
    """#

    static let claudeMessageResponseRedactedThinkingJSON = #"""
    {
      "id": "msg_fixture_redacted",
      "type": "message",
      "role": "assistant",
      "content": [
        { "type": "redacted_thinking", "data": "<fixture-redacted-thinking>" }
      ],
      "model": "claude-fixture-content",
      "stop_reason": "end_turn",
      "usage": { "input_tokens": 16, "output_tokens": 8 }
    }
    """#

    static let claudeMessageRequestKnownOptionalNullsJSON = #"""
    {
      "model": "claude-fixture-content",
      "messages": [
        {
          "role": "user",
          "content": [
            { "type": "text", "text": "Null optionals fixture", "cache_control": null },
            {
              "type": "document",
              "source": { "type": "text", "data": "Synthetic document" },
              "citations": null,
              "cache_control": null
            },
            {
              "type": "tool_result",
              "tool_use_id": "toolu_fixture_nulls",
              "content": null,
              "is_error": null
            }
          ]
        }
      ],
      "max_tokens": 64
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

    static let openAIFileObjectFullJSON = #"""
    {
      "id": "file_fixture_upstream_001",
      "object": "file",
      "bytes": 4294967296,
      "created_at": 4294967297,
      "filename": "fixture-large.jsonl",
      "purpose": "user_data",
      "status": "processed",
      "mime_type": "application/jsonl",
      "deleted": false
    }
    """#

    static let openAIFileListFullJSON = #"""
    {
      "object": "list",
      "data": [
        {
          "id": "file_fixture_upstream_001",
          "object": "file",
          "bytes": 4294967296,
          "created_at": 4294967297,
          "filename": "fixture-large.jsonl",
          "purpose": "user_data",
          "status": "processed",
          "mime_type": "application/jsonl",
          "deleted": false
        },
        {
          "id": "file_fixture_upstream_002",
          "object": "file",
          "bytes": null,
          "created_at": null,
          "filename": null,
          "purpose": null,
          "status": null,
          "mime_type": null,
          "deleted": null
        }
      ],
      "has_more": true
    }
    """#

    static let openAIDeletedFileJSON = #"""
    {
      "id": "file_fixture_upstream_deleted",
      "object": "file",
      "deleted": true
    }
    """#

    static let openAIChatInputFileRequestJSON = #"""
    {
      "model": "gpt-fixture-chat",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "input_file",
              "file_id": "file_fixture_input_001",
              "filename": "fixture-input.txt"
            }
          ]
        }
      ],
      "max_tokens": 4294967296
    }
    """#

    static let openAIChatUnknownContentRequestJSON = #"""
    {
      "model": "gpt-fixture-chat",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "future_audio",
              "audio": { "id": "audio_fixture_001", "duration_ms": 1250 },
              "nullable": null
            }
          ]
        }
      ]
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

    static let openAIChatChoiceUsageStreamChunkJSON = #"""
    {
      "id": "chatcmpl_fixture_choice_usage",
      "object": "chat.completion.chunk",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [
        {
          "index": 0,
          "delta": { "content": "Fixture choice usage" },
          "finish_reason": null,
          "usage": {
            "prompt_tokens": 4294967296,
            "completion_tokens": 8,
            "total_tokens": 4294967304,
            "prompt_tokens_details": { "cached_tokens": 256 }
          }
        }
      ]
    }
    """#

    static let openAIChatMalformedChoiceStreamChunkJSON = #"""
    {
      "id": "chatcmpl_fixture_malformed_choice",
      "object": "chat.completion.chunk",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [
        { "index": 0, "delta": null, "finish_reason": "stop" }
      ]
    }
    """#

    static let openAIChatMalformedChoiceUsageStreamChunkJSON = #"""
    {
      "id": "chatcmpl_fixture_malformed_choice_usage",
      "object": "chat.completion.chunk",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [
        {
          "index": 0,
          "delta": { "content": "Keep this choice" },
          "finish_reason": null,
          "usage": "not-an-object"
        }
      ]
    }
    """#

    static let openAIChatMalformedResponseChoiceJSON = #"""
    {
      "id": "chatcmpl_fixture_malformed_choice",
      "object": "chat.completion",
      "created": 1893553445,
      "model": "gpt-fixture-chat",
      "choices": [
        { "index": 0, "message": null, "finish_reason": "stop" }
      ]
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

    static let codexResponsesCanonicalVariantsResponseJSON = #"""
    {
      "id": "resp_fixture_canonical_variants_001",
      "object": "response",
      "created_at": 1893553445,
      "model": "gpt-fixture-codex-variants",
      "output": [
        {
          "id": "msg_fixture_variants_001",
          "type": "message",
          "role": "assistant",
          "status": "completed",
          "phase": "commentary",
          "content": [
            { "type": "output_text", "text": "Working on the hosted tool." },
            { "type": "refusal", "refusal": null },
            { "type": "future_output_content", "marker": "discarded-by-wire-model" }
          ]
        },
        {
          "id": "cmp_fixture_variants_001",
          "type": "compaction",
          "encrypted_content": "<fixture-compaction>"
        },
        {
          "id": "fc_fixture_variants_001",
          "type": "function_call",
          "call_id": "call_fixture_variants_in_progress",
          "name": "inspect_fixture",
          "arguments": "{\"path\":\"fixture.txt\"}",
          "status": "in_progress"
        },
        {
          "id": "fc_fixture_variants_002",
          "type": "function_call",
          "call_id": "call_fixture_variants_future",
          "name": "future_fixture",
          "arguments": "",
          "status": "FUTURE_STATE"
        },
        {
          "id": "fco_fixture_variants_001",
          "type": "function_call_output",
          "call_id": "call_fixture_empty_output",
          "output": "",
          "status": "completed"
        },
        {
          "id": "fco_fixture_variants_002",
          "type": "function_call_output",
          "call_id": "call_fixture_content_output",
          "output": [
            { "type": "input_text", "text": "tool input text" },
            {
              "type": "input_image",
              "file_id": "file_fixture_image_001",
              "image_url": "https://example.test/ignored.png",
              "detail": "high"
            },
            {
              "type": "input_file",
              "file_id": "file_fixture_document_001",
              "filename": "fixture.txt",
              "file_url": "https://example.test/ignored.txt"
            },
            { "type": "output_text", "text": "tool output text" }
          ],
          "status": "completed"
        },
        {
          "id": "fs_fixture_variants_001",
          "type": "file_search_call",
          "queries": ["fixture quota"],
          "status": "in_progress"
        },
        {
          "id": "ws_fixture_variants_001",
          "type": "web_search_call",
          "status": "completed",
          "action": {
            "type": "search",
            "query": "fixture docs"
          }
        },
        {
          "type": "future_hosted_tool_event",
          "marker": "discarded-by-wire-model"
        }
      ],
      "status": "incomplete",
      "usage": {
        "input_tokens": 10,
        "output_tokens": 3,
        "total_tokens": 13,
        "input_tokens_details": { "cached_tokens": 20 }
      }
    }
    """#

    static let codexResponsesHostedCallIDMatrixJSON = #"""
    {
      "id": "resp_fixture_hosted_matrix_001",
      "object": "response",
      "created_at": 1893553445,
      "model": "gpt-fixture-hosted-matrix",
      "output": [
        {
          "id": "computer_item_001",
          "type": "computer_call",
          "call_id": "call_computer_001",
          "status": "IN_PROGRESS",
          "action": { "type": "click", "x": 7, "y": 9 },
          "extra_marker": "discard-computer-call"
        },
        {
          "id": "computer_output_001",
          "type": "computer_call_output",
          "call_id": "call_computer_001",
          "output": {
            "type": "computer_screenshot",
            "file_id": "file_screenshot_001"
          },
          "extra_marker": "discard-computer-output"
        },
        {
          "id": "image_generation_001",
          "type": "image_generation_call",
          "status": "FAILED",
          "result": "image-result-base64",
          "extra_marker": "discard-image-generation"
        },
        {
          "id": "code_interpreter_001",
          "type": "code_interpreter_call",
          "status": "incomplete",
          "code": "print('fixture')",
          "container_id": "container_fixture_001",
          "outputs": [
            { "type": "logs", "logs": "fixture log" },
            { "type": "image", "url": "https://example.test/fixture.png" },
            { "type": "future_code_output" }
          ],
          "extra_marker": "discard-code-interpreter"
        },
        {
          "id": "tool_search_call_001",
          "type": "tool_search_call",
          "call_id": "call_tool_search_001",
          "arguments": { "query": "fixture" },
          "execution": "hosted",
          "status": "completed",
          "extra_marker": "discard-tool-search-call"
        },
        {
          "id": "tool_search_output_001",
          "type": "tool_search_output",
          "call_id": "call_tool_search_001",
          "execution": "hosted",
          "status": "completed",
          "tools": [],
          "extra_marker": "discard-tool-search-output"
        },
        {
          "id": "local_shell_call_001",
          "type": "local_shell_call",
          "call_id": "call_local_shell_001",
          "status": "in_progress",
          "action": {
            "type": "exec",
            "command": ["pwd"],
            "env": { "FIXTURE": "1" },
            "timeout_ms": 1200.5,
            "user": "fixture-user",
            "working_directory": "/fixture"
          },
          "extra_marker": "discard-local-shell-call"
        },
        {
          "id": "local_shell_output_001",
          "type": "local_shell_call_output",
          "output": "fixture local shell output",
          "status": "FUTURE_STATE",
          "extra_marker": "discard-local-shell-output"
        },
        {
          "id": "shell_call_001",
          "type": "shell_call",
          "call_id": "call_shell_001",
          "status": "completed",
          "action": {
            "commands": ["echo fixture"],
            "max_output_length": 4096,
            "timeout_ms": 2000
          },
          "environment": {
            "type": "container",
            "container_id": "container_fixture_002"
          },
          "extra_marker": "discard-shell-call"
        },
        {
          "id": "shell_output_001",
          "type": "shell_call_output",
          "call_id": "call_shell_001",
          "max_output_length": 4096,
          "status": "completed",
          "output": [
            {
              "outcome": { "type": "exit", "exit_code": 0 },
              "stdout": "fixture stdout",
              "stderr": ""
            },
            {
              "outcome": { "type": "timeout" }
            }
          ],
          "extra_marker": "discard-shell-output"
        },
        {
          "id": "apply_patch_call_001",
          "type": "apply_patch_call",
          "call_id": "call_apply_patch_001",
          "status": "in_progress",
          "operation": {
            "type": "update_file",
            "path": "fixture.txt",
            "diff": "@@ fixture @@"
          },
          "extra_marker": "discard-apply-patch-call"
        },
        {
          "id": "apply_patch_output_001",
          "type": "apply_patch_call_output",
          "call_id": "call_apply_patch_001",
          "status": "completed",
          "output": "Done!",
          "extra_marker": "discard-apply-patch-output"
        },
        {
          "id": "mcp_list_tools_001",
          "type": "mcp_list_tools",
          "server_label": "fixture-server",
          "tools": [],
          "error": null,
          "extra_marker": "discard-mcp-list"
        },
        {
          "id": "mcp_approval_request_001",
          "type": "mcp_approval_request",
          "arguments": "{\"path\":\"fixture.txt\"}",
          "name": "read_fixture",
          "server_label": "fixture-server",
          "extra_marker": "discard-mcp-approval-request"
        },
        {
          "id": "mcp_approval_response_001",
          "type": "mcp_approval_response",
          "approval_request_id": "mcp_approval_request_001",
          "approve": false,
          "reason": null,
          "extra_marker": "discard-mcp-approval-response"
        },
        {
          "id": "mcp_call_001",
          "type": "mcp_call",
          "arguments": "{}",
          "name": "lookup_fixture",
          "server_label": "fixture-server",
          "approval_request_id": "mcp_approval_request_001",
          "output": "fixture mcp output",
          "extra_marker": "discard-mcp-call"
        },
        {
          "id": "custom_tool_call_001",
          "type": "custom_tool_call",
          "call_id": "call_custom_tool_001",
          "input": "fixture custom input",
          "name": "fixture_custom_tool",
          "status": "in_progress",
          "namespace": "fixture",
          "extra_marker": "discard-custom-call"
        },
        {
          "id": "custom_tool_output_001",
          "type": "custom_tool_call_output",
          "call_id": "call_custom_tool_001",
          "output": "fixture custom output",
          "status": null,
          "extra_marker": "discard-custom-output"
        }
      ],
      "status": "completed"
    }
    """#

    static let codexResponsesStopPriorityMatrixJSON = #"""
    {
      "cases": [
        {
          "label": "incomplete-empty",
          "response": {
            "id": "stop_incomplete_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [],
            "status": "incomplete"
          }
        },
        {
          "label": "incomplete-function",
          "response": {
            "id": "stop_incomplete_function",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "type": "function_call",
                "call_id": "call_stop_001",
                "name": "fixture",
                "arguments": "{}"
              }
            ],
            "status": "incomplete"
          }
        },
        {
          "label": "completed-function",
          "response": {
            "id": "stop_completed_function",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "type": "function_call",
                "call_id": "call_stop_002",
                "name": "fixture",
                "arguments": "{}"
              }
            ],
            "status": "completed"
          }
        },
        {
          "label": "failed-function",
          "response": {
            "id": "stop_failed_function",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "type": "function_call",
                "call_id": "call_stop_003",
                "name": "fixture",
                "arguments": "{}"
              }
            ],
            "status": "failed"
          }
        },
        {
          "label": "failed-empty",
          "response": {
            "id": "stop_failed_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [],
            "status": "failed"
          }
        },
        {
          "label": "completed-empty",
          "response": {
            "id": "stop_completed_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [],
            "status": "completed"
          }
        },
        {
          "label": "future-status-empty",
          "response": {
            "id": "stop_future_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [],
            "status": "future_status"
          }
        },
        {
          "label": "missing-status-empty",
          "response": {
            "id": "stop_missing_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": []
          }
        },
        {
          "label": "uppercase-incomplete-empty",
          "response": {
            "id": "stop_upper_incomplete_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [],
            "status": "INCOMPLETE"
          }
        },
        {
          "label": "uppercase-failed-empty",
          "response": {
            "id": "stop_upper_failed_empty",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [],
            "status": "FAILED"
          }
        },
        {
          "label": "in-progress-pending-output",
          "response": {
            "id": "stop_in_progress_output",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "id": "computer_output_stop_001",
                "type": "computer_call_output",
                "call_id": "call_computer_stop_001",
                "output": { "type": "computer_screenshot" },
                "status": "in_progress"
              }
            ],
            "status": "in_progress"
          }
        },
        {
          "label": "incomplete-pending-output",
          "response": {
            "id": "stop_incomplete_output",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "id": "computer_output_stop_002",
                "type": "computer_call_output",
                "call_id": "call_computer_stop_002",
                "output": { "type": "computer_screenshot" },
                "status": "in_progress"
              }
            ],
            "status": "incomplete"
          }
        },
        {
          "label": "incomplete-terminal-call",
          "response": {
            "id": "stop_incomplete_terminal_call",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "id": "computer_call_stop_001",
                "type": "computer_call",
                "call_id": "call_computer_stop_003",
                "status": "COMPLETED"
              }
            ],
            "status": "incomplete"
          }
        },
        {
          "label": "incomplete-custom-call",
          "response": {
            "id": "stop_incomplete_custom_call",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "id": "custom_call_stop_001",
                "type": "custom_tool_call",
                "call_id": "call_custom_stop_001",
                "input": "",
                "name": "fixture_custom",
                "status": "in_progress"
              }
            ],
            "status": "incomplete"
          }
        },
        {
          "label": "uppercase-incomplete-pending-call",
          "response": {
            "id": "stop_upper_incomplete_pending",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "id": "computer_call_stop_002",
                "type": "computer_call"
              }
            ],
            "status": "INCOMPLETE"
          }
        },
        {
          "label": "pause-before-max-and-tool-use",
          "response": {
            "id": "stop_pause_priority",
            "object": "response",
            "created_at": 0,
            "model": "gpt-fixture-stop",
            "output": [
              {
                "id": "computer_call_stop_003",
                "type": "computer_call"
              },
              {
                "type": "function_call",
                "call_id": "call_stop_004",
                "name": "fixture",
                "arguments": "{}"
              }
            ],
            "status": "incomplete"
          }
        }
      ]
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
