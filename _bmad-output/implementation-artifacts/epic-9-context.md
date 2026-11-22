# Epic 9 Context: Portable Conformance Oracle

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give domain authors a conformance suite they can run using shipped packages, and give consuming applications a verified way to create, append to and read conversations. Preserve the module’s existing guarantees while testing the same client package, authenticated HTTP routes and persisted results an adopter uses.

## Stories

- Story 9.1: Freeze the conformance assertion inventory, tier decisions, digest, and approvals
- Story 9.2: Make the portable tier structural and prove complete monotonic tier execution
- Story 9.3: Prove the packaged conversation client against its supported host
- Story 9.4: Preserve typed client errors and enforce read scope consistently
- Story 9.5: Bind discovery compatibility to shipped package versions

## Requirements & Constraints

- Preserve the approved per-assertion inventory, including case identity, assertion strength, negative coverage and tier assignment. The original FR-20 floor contains 14 suites and 214 tests covering tenant isolation, idempotency, contract validation, redaction replay, provider portability, projection freshness and governance audit pairing. Both tiers must execute, retain their combined identities and counts, and report failures or skipped assertions explicitly.
- The portable tier uses the shipped Contracts, Client and Testing packages. Its evaluated dependencies and transitive compile assets must demonstrate portability. Keep assertions that require internal types at full strength in the module tier and retain the recorded rationale. Quality owns tier reclassification. Preserve approved baselines, prior receipts and the manifest membership of telemetry and conformance-status suites.
- An independent consumer must install the built client, authenticate against a documented address and exercise the actual command bus and projection store. Record the host artifact, authentication contract and runtime dependencies so another adopter can reproduce the journey. The client uses a distinct caller-supplied key per mutation. A retry sends the original command, metadata and key. A changed request after an idempotency conflict requires a new key.
- Retried creation must resolve to the same conversation. Retried append must leave exactly one stored message. Within a bounded wait the authorized caller must see Current detail and a Current list containing the conversation. Both a mismatched identity and another tenant must receive the appropriate hidden response without private content or an existence signal.
- Discovery must describe the wire versions supported by the installed Client and Contracts packages separately. Verify supported, deprecated and unsupported boundaries and an unknown wire version using isolated package installations. The package owner must explicitly classify package 2.1.0 against wire v1.

## Technical Decisions

- Conversations owns command semantics, query results and public errors. EventStore owns domain writes and provides the domain-service host. Platform owns production orchestration, identity configuration and provider bindings. For the pilot, implement a Conversations-owned external API for Platform to host. Keep the repository AppHost available as a non-packable test harness.
- For reads, authenticated claims establish tenant and caller identity. Optional scope headers can be omitted or contain a single matching value. Check conflicting or repeated values before accessing the query store. A hidden detail returns HTTP 404 without the detail. A hidden list returns HTTP 200 with an empty collection. Apply the same rule to other routes using the shared scope resolver.
- Decode a forward-only response body once. Preserve an operation-specific list or detail result with its wire status, freshness and reason. Preserve structured service errors with their wire status, code, category and correlation. When an unsuccessful body is empty, malformed or ambiguous, use the operation’s existing safe fallback and request correlation. Keep a typed unavailable projection distinguishable from a service error with the same HTTP status.
- Preserve predecessor evidence for the tier split. Its generated records and fault-injection requirements belong to the stories that specified them. Routine product changes use current-tree validation and focused tests. Report runtime verification separately from successful compilation and package installation.

## Cross-Story Dependencies

Story 9.2 used the Quality-approved inventory from Story 9.1, which followed Story 7.4. The accepted tier evidence remains input to later preservation and release checks. Release and Builds own clean qualification of the five packages in Story 11.4. The route and identity work in Story 9.3 can proceed alongside that qualification. Story 9.5 uses qualified packages for its compatibility matrix. Story 16.5 uses the supported host to verify freshness telemetry.
