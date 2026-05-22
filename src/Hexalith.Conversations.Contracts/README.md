# Hexalith.Conversations.Contracts

`Hexalith.Conversations.Contracts` contains the adopter-safe command, projection, domain event, typed error, schema version, and compatibility metadata contracts for Hexalith Conversations.

## Compatibility Discovery

The active v1 metadata is exposed by `ConversationContractCompatibility.Current`.

- `commandContracts`, `projectionContracts`, and `eventContracts` identify the active and minimum supported schema versions.
- `contractsPackage` identifies `Hexalith.Conversations.Contracts`.
- `clientPackage` identifies the aligned `Hexalith.Conversations.Client` package.
- `status` uses the closed vocabulary `supported`, `deprecated`, `unsupported`, or `invalid`.
- `remediations` contain bounded guidance codes and HTTPS documentation pointers.

Use `ConversationContractCompatibility.Evaluate(...)` to evaluate adopter-supplied schema and package versions without leaking implementation details. Malformed schema versions, zero or negative schema versions, unsupported schema versions, and unsupported package versions return typed compatibility results.

Compatibility failures use the same `ConversationError` shape as runtime command and client failures. Unsupported or invalid schema/package inputs return `schema_version_unsupported` with `versioning`, `use-supported-version`, a safe message, and the package documentation pointer.

## Supported v1 Integration Path

The supported v1 integration path is shared contracts plus the .NET client package. This package publishes the public DTOs and compatibility metadata. The client happy path is implemented separately, so this package does not define transport behavior, retries, onboarding diagnostics, or raw HTTP fallback examples.

Direct HTTP examples are intentionally outside normal v1 adopter guidance unless a later buyer-approved diagnostics scope records that exception.

## Typed Errors

`ConversationError` is the canonical adopter-safe error contract. It exposes structured fields only: `code`, `category`, `isRetryable`, `clientAction`, `safeMessage`, `correlationId`, optional allowed `auditHandle`, `safeFieldDiagnostics`, and an HTTPS `documentation` pointer. `developerGuidance` remains for backward-compatible safe text, but adopter applications should branch on `code`, `category`, and `clientAction`.

| Code | Category | Retryable | Client action | Safe message intent | Documentation |
| --- | --- | --- | --- | --- | --- |
| `tenant_binding_missing` | `authorization` | `false` | `provide-context` | Provide authenticated tenant and caller context. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `tenant_isolation_violation` | `authorization` | `false` | `check-access` | The supplied access context cannot complete the request. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `tenant_projection_stale` | `freshness` | `true` | `retry-later` | Retry after tenant access state is current. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `audit_sink_unavailable` | `audit` | `true` | `retry-later` | Retry after audit recording is available. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `audit_pairing_required` | `audit` | `false` | `provide-audit-evidence` | Provide required audit evidence before retrying. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `idempotency_conflict` | `conflict` | `false` | `use-new-idempotency-key` | Use a new idempotency key for a changed command payload. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `idempotency_outcome_unknown` | `uncertainty` | `true` | `retry-same-request` | Retry with the same idempotency metadata. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `idempotency_key_missing` | `validation` | `false` | `provide-idempotency-key` | Provide idempotency metadata before sending the command. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `aggregate_not_found` | `hidden` | `false` | `hide-or-refresh` | The requested conversation is not available. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `schema_version_unsupported` | `versioning` | `false` | `use-supported-version` | Use supported Conversations contract and client versions. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `command_validation_failed` | `validation` | `false` | `correct-request` | Correct the request and retry. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `duplicate_participant` | `conflict` | `false` | `correct-request` | Correct participant membership and retry. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `unsupported_participant` | `validation` | `false` | `correct-request` | Use a supported participant type and role. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `participant_validation_unavailable` | `validation` | `true` | `retry-later` | Retry after participant validation is available. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `tenant_context_mismatch` | `authorization` | `false` | `align-context` | Align the request context with the authenticated context. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |
| `provider_only_identity_forbidden` | `validation` | `false` | `use-party-identity` | Use a Conversations Party identity for participant attribution. | `https://docs.hexalith.local/conversations/contracts/v1/errors` |

## Safe Surface

The package is limited to public Conversations contracts. It does not publish runtime topology, local build paths, UI implementation details, provider-specific content fields, Party personal data, or operational failure text. Compatibility metadata is not tenant authorization, governance truth, projection freshness, or runtime health; it only tells adopters which contract and package versions are supported and where to find safe remediation guidance.
