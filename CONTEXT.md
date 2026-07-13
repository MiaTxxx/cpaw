# AIUsage Domain

AIUsage observes provider usage, turns provider-specific facts into comparable summaries, and keeps authentication material distinct from provider account identity.

## Language

**Provider**:
An external product or local usage source whose account usage can be observed.
_Avoid_: Provider card, integration

**Provider ID**:
The stable identifier for a provider kind, such as `codex` or `claude`; it does not identify an account or a fetch result.
_Avoid_: Provider result ID, account ID

**Provider Account**:
A logical account within one provider whose usage is observed.
_Avoid_: Credential, credential record

**Credential Record**:
A reference and non-sensitive metadata describing how AIUsage can authenticate a Provider Account.
_Avoid_: Account, provider account

**Canonical Account Identity**:
The provider-specific identity facts used to decide whether credential records refer to the same Provider Account.
_Avoid_: Credential ID, file path

**Raw Usage Snapshot**:
The provider-reported facts captured by one successful fetch before cross-provider normalization.
_Avoid_: Usage summary, dashboard snapshot

**Raw Quota Window**:
A possibly partial or internally inconsistent quota observation reported by a provider.
_Avoid_: Normalized quota window

**Normalized Quota Window**:
A comparable quota window derived from a Raw Quota Window for use in a Provider Usage Summary.
_Avoid_: Raw quota window

**Provider Usage Summary**:
A normalized snapshot for one provider account at one observation time; it is not historical accumulated usage.
_Avoid_: Raw usage snapshot, dashboard overview

**Provider Fetch Result**:
The envelope associating a provider result identity with any raw snapshot, normalized summary, and fetch error available for that attempt.
_Avoid_: Provider ID

**Dashboard Overview**:
A derived aggregation across multiple Provider Usage Summaries.
_Avoid_: Provider usage summary
