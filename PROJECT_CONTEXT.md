<!-- bmad:context -->
<!-- Verified 2026-11-22 against f517ee6f. Managed by bmad-project-context. -->

# Hexalith.Conversations project context

Conversations owns tenant-scoped conversation records and governance evidence. EventStore owns durable writes, Tenants owns membership, and Parties owns participant identity. Product decisions live in `docs/adrs/` and BMAD planning lives in `_bmad-output/planning-artifacts/`.

## Domain boundaries

- Authorize tenant access before looking up a retry, replaying an outcome, loading an aggregate, or reading a projection. Use `docs/adrs/0001-idempotency-contract.md` when changing retry behavior.
- Persist stable `PartyId` references in participant events. Hydrate Party details at the application boundary. Use provider session and thread IDs only for correlation.
- Pair retention, redaction, and sensitivity changes with their required audit evidence. Reject the change when that evidence cannot be recorded.
- Use `ProjectionFreshnessV1.AllowsTrustBearingDecision()` before relying on a projection for a privileged decision. It checks the public state, reason code, and stale flag together.

## Hosting

- Point packaged clients at the Platform resource `conversations-api`. Keep the `conversation` domain service and its sidecar private. See `docs/integration-guide.md` and `module/platform-handoff.md`.
- Preserve both AppHosts and their consumers listed in `module/hosting-inventory.json` until their owners approve replacement. Use the Platform source profile for the pilot.

## Running and verifying

- Use the SDK selected by `global.json` (11.0.100, latest patch) and the Platform-selected dependency mapping required by `Directory.Build.props` for `net11.0` producer builds.
- To verify an installed client, point `PACKAGE_FEED` at the directory containing the Client and Contracts packages and set `PACKAGE_VERSION` to their exact version. Run `python3 tests/packaged-client-journey/run.py --feed "$PACKAGE_FEED" --version "$PACKAGE_VERSION" --build-only` to check restore and compilation in an empty package cache.
- For the live journey, provision the host and identities described in `docs/integration-guide.md` and omit `--build-only`. Record the deployment tested. Published images require their own verification.

<!-- /bmad:context -->
