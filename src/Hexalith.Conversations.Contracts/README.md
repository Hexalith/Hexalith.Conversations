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

## Supported v1 Integration Path

The supported v1 integration path is shared contracts plus the .NET client package. This package publishes the public DTOs and compatibility metadata. The client happy path is implemented separately, so this package does not define transport behavior, retries, onboarding diagnostics, or raw HTTP fallback examples.

Direct HTTP examples are intentionally outside normal v1 adopter guidance unless a later buyer-approved diagnostics scope records that exception.

## Safe Surface

The package is limited to public Conversations contracts. It does not publish runtime topology, local build paths, UI implementation details, provider-specific content fields, Party personal data, or operational failure text. Compatibility metadata is not tenant authorization, governance truth, projection freshness, or runtime health; it only tells adopters which contract and package versions are supported and where to find safe remediation guidance.
