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

For release-owner additive-change, breaking-change, deprecation-window, minimum-version, unsupported-version, and FR81 classification rules, see the [Contract Compatibility and Deprecation Policy](../../docs/release-evidence/contract-compatibility-policy.md).

## Supported v1 Integration Path

The supported v1 integration path is shared contracts plus the .NET client package. This package publishes the public DTOs and compatibility metadata. The client happy path is implemented separately, so this package does not define transport behavior, retries, onboarding diagnostics, or raw HTTP fallback examples.

Direct HTTP examples are intentionally outside normal v1 adopter guidance unless a later buyer-approved diagnostics scope records that exception.

## Conversation Project Assignment

`ReassignConversationProjectCommand` is the additive v1 command for setting, changing, or explicitly clearing the Conversations-owned `ProjectId` after a conversation has been created. The matching public event is `ConversationProjectChanged`, which carries the previous and current `ProjectId` values as stable identifiers only.

The command target is explicit: use `ConversationProjectAssignmentOperation.Assign` with a non-null target `ProjectId`, or `ConversationProjectAssignmentOperation.Clear` with no target `ProjectId`. Missing target fields are validation failures, not implicit clear requests.

Adopters should call the aligned client method `IConversationClient.ReassignConversationProjectAsync(...)` and branch on `ConversationCommandAcceptedResult`, `idempotency_conflict`, `idempotency_outcome_unknown`, and the standard tenant/validation errors. The opt-in HTTP command route used by the client is `POST /api/v1/conversations/{conversationId}/project`; route and body conversation identities must match.

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

## CORE Preconditions and Onboarding Diagnostics

CORE preconditions describe the environment state an adopter integration depends on before relying on Conversations behavior. The canonical, contract-owned catalog is `ConversationCorePreconditionCatalog.All`; each entry carries a `preconditionId`, the diagnostic `check` that evaluates it, the required trust state (only `Current` is trust-bearing), the typed `unmetErrorCode` reused from `ConversationErrorCatalog`, and a safe-failure description. When a precondition is unknown, failing, stale, or unsupported, dependent operations return a typed safe failure or a degraded-read result and never silently weaken tenant isolation, audit pairing, freshness, or schema compatibility.

| Precondition | Required state | Safe failure behavior | Unmet error code |
| --- | --- | --- | --- |
| `projection-freshness` | `Current` | Stale tenant access degrades reads to a non-trust-bearing state and fails writes closed; retry after the projection is current. | `tenant_projection_stale` |
| `audit-sink-availability` | `Current` | Governed mutations fail closed when audit recording is unavailable; retry after audit recording is available. | `audit_sink_unavailable` |
| `supported-schema-versions` | `Current` | Unsupported schema versions are rejected with a versioning error rather than processed under an incompatible contract. | `schema_version_unsupported` |
| `contract-compatibility` | `Current` | Unsupported or invalid contract/package versions return a typed versioning error and bounded remediation. | `schema_version_unsupported` |
| `participant-identity-validation` | `Current` | Writes fail closed when validation is unavailable; authorized reads may degrade hydration to a safe unresolved state without disclosing personal data. | `participant_validation_unavailable` |
| `idempotency-key-behavior` | `Current` | Commands missing idempotency metadata are rejected before processing so retries cannot duplicate accepted outcomes. | `idempotency_key_missing` |
| `projection-subscription-health` | `Current` | A stale, rebuilding, or unavailable subscription degrades reads and fails writes closed; retry after the subscription is current. | `tenant_projection_stale` |
| `required-configuration` | `Current` | Missing required configuration yields a bounded configuration-gap status with no provider content or secret values exposed. | `command_validation_failed` |

Onboarding diagnostics evaluate the closed `OnboardingDiagnosticCheck` set (tenant context, contract version, provider configuration, projection subscription, schema compatibility, audit availability, Parties integration) and return an `OnboardingDiagnosticRunResultV1`. Each `OnboardingDiagnosticCheckResultV1` carries a closed `OnboardingDiagnosticStatus` (`ready`, `degraded`, `blocked`, `unknown`) mapped to the shared trust/freshness vocabulary, a safe message, a bounded remediation guidance code, an HTTPS documentation pointer, and the same typed `ConversationError` shape used elsewhere. Diagnostics never disclose tenant data, Party data, conversation existence, provider payloads, or production secrets; a denied, missing, or cross-tenant request collapses to a single `unknown` result.

## Adopter Conformance Tests and CORE Fixture

Adopters can prove their integration respects Conversations contracts before deployment by running the adopter-facing conformance suite against the synthetic CORE fixture. The suite reuses the contracts in this package (compatibility metadata, the typed error catalog, and the CORE precondition catalog) plus the shared trust/freshness vocabulary; it does not introduce a parallel model.

The conformance contracts live under `Hexalith.Conversations.Contracts.Conformance`:

- `ConformanceCheck` is the closed vocabulary covering the CORE integration surface.
- `ConformanceOutcome` reuses the Story 4.4 readiness language (`ready`, `degraded`, `blocked`, `unknown`) aligned to the shared trust/freshness vocabulary; only `ready` is trust-bearing. No conformance-only synonyms are introduced.
- `ConformanceFailureClassification` distinguishes `conformant` from `product-invariant`, `infrastructure`, `configuration`, `unavailable-dependency`, and `execution` failures.
- `ConformanceCheckResultV1` and `ConformanceRunResultV1` carry only structured, content-safe data and serialize to deterministic camelCase web JSON for CI consumption. Typed failures embed the shared `ConversationError`.

Each check maps to the relevant requirement, CORE precondition, and release-gate category so release-gate aggregation can consume the local evidence without rework:

| Conformance check | Requirement | CORE preconditions | Release-gate category |
| --- | --- | --- | --- |
| `create-conversation` | FR73, FR74 | `supported-schema-versions`, `idempotency-key-behavior` | `release-gate-commands-queries-events` |
| `append-message` | FR73, FR74 | `idempotency-key-behavior` | `release-gate-commands-queries-events` |
| `read-timeline` | FR73, FR74 | `projection-subscription-health` | `release-gate-commands-queries-events`, `release-gate-projection-freshness` |
| `tenant-binding` | FR74 | `projection-freshness` | `release-gate-tenant-isolation` |
| `party-identity` | FR74 | `participant-identity-validation` | `release-gate-tenant-isolation` |
| `idempotency` | FR74 | `idempotency-key-behavior` | `release-gate-idempotent-commands` |
| `error-envelope` | FR74 | `required-configuration` | `release-gate-error-envelope` |
| `projection-freshness` | FR74 | `projection-freshness`, `projection-subscription-health` | `release-gate-projection-freshness` |
| `event-publication` | FR74 | `supported-schema-versions` | `release-gate-event-schema-evolution` |
| `governance-precondition` | FR74 | `audit-sink-availability`, `required-configuration` | `release-gate-tenant-isolation` |
| `compatibility-discovery` | FR74 | `contract-compatibility`, `supported-schema-versions` | `release-gate-version-discovery` |

The reusable synthetic CORE fixture ships in `Hexalith.Conversations.Testing` as `ConversationConformanceCoreFixtures`. It is deterministic, content-safe, and clearly marked (`synthetic-conformance-data`); it requires no production tenant data, provider credentials, or nested submodule initialization. It provides one authorized tenant-scoped happy-path conversation (participants, message attribution, business references, and `Current` projection freshness) plus typed failure cases for unsupported schema/version, stale projection, cross-tenant denial (hidden shape), duplicate-command idempotency conflict, and a sanitized error. Unique cross-tenant poison sentinel values are scanned by content-safety tests and must never appear in any authorized-tenant output.

To run the adopter suite in CI, execute the conformance test project and consume the serialized `ConformanceRunResultV1`:

```bash
dotnet test tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj
```

Conformance assertions and the fixture target Conversations contracts and the supported `Hexalith.Conversations.Client`, not raw HTTP fallback. Release signing, versioned conformance manifests, and waiver aggregation are intentionally out of scope for the local-evidence slice.

## Caller Metadata (Provenance Only)

`CallerMetadata` is an optional, bounded, content-safe provenance bag adopters can attach to `CreateConversationCommand`, `AppendMessageCommand`, `UpdateConversationMetadataCommand`, and `ReassignConversationProjectCommand`. It carries client/composer/origin attribution for audit, downstream projection, and Hexalith front-end composition surfaces. Correlation and causation identifiers are NOT part of caller metadata; they remain first-class on `ConversationCommandMetadata` and propagate onto events through `ConversationEventMetadata`.

Caller metadata is **provenance only**. It is never authorization, tenant truth, governance truth, or UI-inferred trust state. Tenant access stays decided by the claims-derived tenant context and the local Tenants projection; caller-supplied values can never override tenant scope, command availability, or freshness/trust signals. Every displayed trust claim still maps to Conversations-owned projection freshness or command-availability metadata.

| Approved field | Bound / limit | Policy |
| --- | --- | --- |
| `clientName` | optional; <= 256 chars; no control characters; content-safe | reject if invalid |
| `clientVersion` | optional; <= 256 chars; no control characters; content-safe | reject if invalid |
| `composerSource` | optional; <= 256 chars; no control characters; content-safe | reject if invalid |
| `origin` | optional; <= 256 chars; no control characters; content-safe | reject if invalid |
| `integrationContext` | optional; <= 256 chars; no control characters; content-safe | reject if invalid |
| `extensionData` | optional opaque string bag; <= 32 entries; each key/value <= 256 chars; non-empty key; non-null value; content-safe | reject if invalid |

Malformed, oversized, unbounded, sensitive, or unsupported metadata is **rejected** with a typed `command_validation_failed` diagnostic rather than silently truncated, because a truncated content-unsafe value cannot guarantee a safe residual fragment. Forbidden as caller-metadata keys or values: tenant identity, user/Party identity, tokens, claims, provider payloads, raw prompts, message/redacted text, business-reference values, secrets, local paths, and raw exception text. The same free-text content-safety guardrail used by `ConversationError` rejects these by construction, and the command boundary re-bounds caller metadata (and the existing `UpdateConversationMetadataCommand.Attributes` bag) before dispatch.

## Safe Surface

The package is limited to public Conversations contracts. It does not publish runtime topology, local build paths, UI implementation details, provider-specific content fields, Party personal data, or operational failure text. Compatibility metadata is not tenant authorization, governance truth, projection freshness, or runtime health; it only tells adopters which contract and package versions are supported and where to find safe remediation guidance.
