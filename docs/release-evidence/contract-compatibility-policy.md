# Contract Compatibility and Deprecation Policy

This policy is the FR81 adopter-facing compatibility policy for Conversations contracts. It defines how release owners classify contract changes and how those classifications feed release evidence, versioned manifests, waiver aggregation, and release-gate decisions.

The policy applies to the public contract surfaces adopters consume: commands, projections, published events, typed errors, version discovery, the contracts package, and the .NET client package. Persisted event history remains replay-safe and authoritative for conversation state, but it is not the same surface as published events, command contracts, projection contracts, typed errors, version discovery metadata, or client package compatibility.

## Current Supported Versions

The active compatibility source is `ConversationContractCompatibility.Current`. Documentation and release checks must stay aligned with `ContractVersionInfo`, `ContractCompatibilityResult`, and `ConversationErrorCatalog`.

- Active schema version: `1`
- Minimum supported command schema version: `1`
- Minimum supported projection schema version: `1`
- Minimum supported published-event schema version: `1`
- Compatibility status: `supported`
- Contracts package: `Hexalith.Conversations.Contracts` `1.0.0`
- .NET client package: `Hexalith.Conversations.Client` `1.0.0`
- Deprecated package version currently represented by `ConversationContractCompatibility.Evaluate(...)`: `0.9.0`

The v1 minimum supported schema and package versions can be tightened or extended only through an approved policy update or ADR. This policy does not reserve or announce unsupported future versions.

## Policy Summary

| Policy ID | FR81 surface | Classification | Release-owner rule | Metadata and diagnostic alignment |
| --- | --- | --- | --- | --- |
| `POLICY-FR81-COMPAT-ADD` | Commands, projections, published events, typed errors, version discovery, contracts package, .NET client package | `additive` | Older supported consumers must continue to deserialize and use required v1 fields. Unknown optional fields are ignorable where the contract allows it. | Keep `ConversationContractCompatibility.Current` at `supported` and prove additive tolerance in tests. |
| `POLICY-FR81-COMPAT-BREAK` | Commands, projections, published events, typed errors, version discovery, contracts package, .NET client package | `breaking` | Removing or renaming required fields, changing field meanings, changing closed vocabulary semantics, weakening tenant, audit, redaction, freshness, idempotency, diagnostics, or client behavior is breaking. | Requires release evidence and cannot be hidden behind documentation-only changes. |
| `POLICY-FR81-COMPAT-DEPRECATE` | Contracts package and .NET client package | `deprecated` | Deprecated versions remain recognized but must guide adopters to the active v1 package. | `Evaluate(...)` returns `deprecated` with bounded remediation such as `upgrade-to-active-v1`. |
| `POLICY-FR81-COMPAT-UNSUPPORTED` | Schema and package inputs | `unsupported` | Unsupported or invalid inputs fail with typed, content-safe compatibility diagnostics. | `Evaluate(...)` maps to `schema_version_unsupported`, `versioning`, `use-supported-version`, safe field diagnostics, and remediation such as `use-supported-v1-package`. |
| `POLICY-FR81-COMPAT-WAIVER` | Release evidence classification only | `waiver-dependent` | A compatibility break can only proceed under the later named-waiver story; this story records the category but does not approve waivers. | No signed artifact, manifest row, waiver approval, or release-gate pass/fail is created by this policy. |

## Additive Change Rules

Commands are additive only when existing required command fields, schema version handling, idempotency expectations, tenant binding requirements, actor attribution, and validation invariants continue to behave the same for supported v1 consumers. New optional command fields must be safely ignorable by older consumers and cannot weaken existing fail-closed behavior.

Projections and read models are additive only when existing required projection fields, trust states, freshness semantics, and redaction behavior remain stable. Optional fields may be added when old readers can ignore them and still make the same trust-bearing decisions from v1 fields.

Published events are additive only when existing required event metadata and payload meanings remain stable. New optional fields must not change how supported consumers interpret existing event type, schema version, tenant scope, conversation identity, correlation, causation, actor attribution, or deduplication metadata.

Typed errors are additive only when new errors or safe diagnostics do not change the meaning of existing `ConversationErrorCode`, `ConversationErrorCategory`, `ConversationErrorClientAction`, retryability, safe message intent, or documentation pointers. Existing code/category/action combinations cannot be weakened.

Version discovery metadata is additive only when existing `ConversationContractCompatibility.Current`, `ContractVersionInfo`, `ContractCompatibilityResult`, status values, remediations, and HTTPS documentation pointers remain valid for supported v1 consumers. New optional metadata must not require adopters to branch around existing behavior.

The contracts package and .NET client package are additive only when supported consumers can keep using the required v1 contract fields and existing client behavior without code changes. Public client behavior changes that adopters must code around are breaking unless they are introduced through a compatible optional path.

## Breaking Change Rules

A change is breaking when it removes or renames a required field, changes the meaning of an existing field, changes closed vocabulary semantics, makes a previously valid payload invalid without deprecation, weakens tenant isolation, audit pairing, redaction replay, projection freshness, idempotency, safe diagnostics, or compatibility guarantees, leaks infrastructure detail, or changes public client behavior in a way adopters must handle specially.

Breaking changes are release-blocking unless a later named-waiver policy explicitly applies. Story 5.1 does not approve or aggregate waivers.

## Deprecation and Minimum Version Rules

The current active and minimum supported schema version is v1 for commands, projections, and published events. The current supported packages are `Hexalith.Conversations.Contracts` `1.0.0` and `Hexalith.Conversations.Client` `1.0.0`.

Package version `0.9.0` is represented as deprecated by `ConversationContractCompatibility.Evaluate(...)`. Deprecated package inputs remain recognized, return the `deprecated` status, and guide adopters to upgrade to active v1. Unsupported versions are not silently accepted.

## Unsupported Version Behavior

Unsupported schema or package inputs return typed compatibility results. Invalid schema inputs use safe diagnostics such as `invalid_positive_integer_required`; invalid package inputs use safe diagnostics such as `invalid_semantic_version_required`; unsupported schema or package inputs use bounded diagnostics such as `unsupported_schema_version` and `unsupported_package_version`.

Compatibility failures use `schema_version_unsupported`, category `versioning`, client action `use-supported-version`, safe field diagnostics that do not echo unsafe input values, and remediation guidance such as `send-positive-integer-schema-version`, `send-semantic-package-version`, or `use-supported-v1-package`.

Diagnostics must not echo unsafe input values, tenant data, Party data, conversation existence, provider content, protected conversation text, local paths, or operational failure text.

## Persisted Event and Public Contract Boundary

Persisted conversation event history is the replay-safe authority for conversation state. Public compatibility for adopters is expressed through published events, commands, projections, typed errors, version discovery, the contracts package, and the .NET client package. A release-evidence document or policy summary does not become conversation state, runtime health, tenant authorization proof, governance truth, or projection freshness proof.

This policy intentionally avoids internal persistence mechanics, replay implementation details, cursor internals, and private read-model wiring. Event schema evolution proof remains owned by later release-evidence work.

## Release Evidence Boundary

This policy does not approve waivers or signatures by itself. Epic 5 release evidence records the final behavior-preservation and public-contract-shape facts in `final-conformance-contract-diff-v1.*`, reconciles removed-test survivability in `removed-test-justification-ledger-reconciliation-v1.*`, and assembles the release-owner decision input in `success-metric-report-and-attestation-v1.*`. The attestation is signable but remains unsigned until a real release-owner decision is recorded.
