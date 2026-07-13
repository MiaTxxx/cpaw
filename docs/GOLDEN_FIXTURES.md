# Golden fixture specification

## Purpose

Golden fixtures freeze observable Swift behavior before that behavior is rebuilt in C#. The Swift XCTest suite produces the fixtures on macOS; the committed fixture set is validated on both macOS and Windows and consumed by xUnit compatibility tests.

Fixtures are compatibility evidence, not production samples. They must contain only synthetic identities, paths, credentials, timestamps, ports, and upstream responses.

## Layout

```text
QuotaBackend/Tests/QuotaBackendTests/Fixtures/
  v1/
    manifest.json
    contracts/
    normalization/
    identity/
    protocol/
    proxy-transcripts/
    storage/
    exact/
```

Every case is listed in `manifest.json`:

```json
{
  "schemaVersion": 1,
  "cases": [
    {
      "id": "protocol/claude/convert-simple-text-message",
      "path": "protocol/claude/convert-simple-text-message.json",
      "kind": "json-transform",
      "sha256": "lowercase-sha256"
    }
  ]
}
```

Case IDs must be unique and sorted. Paths are relative to `v1/` and may not escape that directory. SHA-256 is calculated over the exact committed bytes.

## Case kinds

- `json-transform`: JSON input and expected JSON output.
- `sequence-transform`: ordered input and output event sequences.
- `http-transcript`: client request, upstream frames, upstream request, and client response.
- `virtual-filesystem`: relative file tree, fixed clock, output summary, and output file tree.
- `exact-text`: whitespace, comments, line endings, or SSE boundaries are behavior.
- `exact-bytes`: binary HTTP behavior.
- `behavior-result`: stable result or error code when a byte snapshot is inappropriate.

## Determinism

- Clock: `2030-01-02T03:04:05Z`, UTC, `en_US_POSIX`.
- IDs, UUIDs, ports, and paths are fixed synthetic values.
- JSON object keys are sorted by UTF-8 bytes; array order is preserved.
- JSON is UTF-8 without BOM, uses LF, and ends with one newline.
- Dynamic ports, temporary directories, generation timestamps, and commit IDs never enter committed case files.
- Integers are exact. Floating-point comparisons use case-specific tolerance unless the case freezes an exact wire representation.

## Secret safety

The fixture validator fails closed on private keys, JWTs, common credential prefixes, non-placeholder bearer tokens, real user paths, real email addresses, sensitive fields without fixture placeholders, and suspicious high-entropy values.

Approved examples include `<fixture-upstream-key>`, `Bearer <fixture-client-key-a>`, paths under `/fixture/` or `C:\fixture\`, and email addresses under `example.test`.

Validate locally with:

```text
dotnet run --project eng/AIUsage.FixtureTool -- validate QuotaBackend/Tests/QuotaBackendTests/Fixtures
```

Only an explicitly reviewed behavior change may regenerate committed expected output.
