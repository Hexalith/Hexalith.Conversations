# Thin-Module Authoring Guidance v2

Schema: `hexalith.conversations.thin-authoring-guidance.v2`

## Candidate

- Story: `11.1` (Checkpoint A)
- Contract: `_bmad-output/planning-artifacts/v9/story-contracts/11.1.json` (`cab730317c6dd72d815c37e07dc5713ef66d4348ce8aa7fc23b7700cc611594d`)
- Planning candidate: `1e9a61126d3b7a55b514b7c7c8942d5af03355e5`

## Source

- `docs/domain-module-authoring-template.md` (`6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb`)

## Module-owned

- `contracts`: Contracts: public commands, events, identifiers, query/result DTOs
- `domain-behavior`: Domain behavior: aggregate, state, validation, idempotency, domain rules
- `module-registration`: Module registration: domain assembly marker and domain-specific dependency graph
- `module-tests`: Module tests: unit, contract, server, conformance, and integration tests

## Platform-owned

- `service-defaults`: ServiceDefaults
- `dapr-wiring`: Dapr implementation and wiring
- `event-subscription`: Event subscription
- `projection-query-runtime`: Projection and query runtime
- `publication`: Publication
- `health`: Health
- `telemetry`: Telemetry runtime

## Prohibited

- `reusable-aspire-library`: A reusable module Aspire library is prohibited.
- `module-service-defaults`: A module-owned `ServiceDefaults` project is prohibited.
- `module-dapr-implementation`: Module-owned Dapr implementation or wiring is prohibited.
- `module-event-subscription`: Module-owned event subscription is prohibited.
- `module-projection-query-runtime`: Module-owned projection or query runtime is prohibited.
- `module-publication`: Module-owned publication is prohibited.
- `module-health`: Module-owned health endpoints or checks are prohibited.
- `module-telemetry-runtime`: Module-owned telemetry runtime is prohibited.

## Test AppHost

- Count: 1
- IsPackable: false
- IsPublishable: false
- Repository-only: true
- Counted in authoring cost: true

## Validation

- `ownership-inventory-exact`: PASS
- `prohibited-inventory-exact`: PASS
- `test-apphost-boundary`: PASS
- `skeleton-no-module-aspire-or-service-defaults`: PASS
- `deterministic-json-markdown`: PASS

### Authority check substitution

- Criterion: `AC-11.1-01`
- Decision: Owner decision 2026-10-18: AC-11.1-01 does not build the absent verify_story_candidate.py; current-policy checks are substituted.
- Reason: verify_story_candidate.py is absent from the repository and is not built by Story 11.1.
- Check: The candidate is the committed HEAD.
- Check: The Story 7.4 and Story 10.4 final-record v2 digests are recomputed from the committed records.
- Check: Story 6.2 is bound through its existing release evidence; it has no v2 record.
- Predecessor `6.2` (release-evidence): `docs/release-evidence/consume-promote-keep-story-6-2-disposition-v1.json` (`d18d81286e171f4df1330677bc236ecdc388764ff58a0ed4be32111fdba31b76`)
- Predecessor `7.4` (final-record-v2): `docs/release-evidence/story-7.4-final-record-v2.json` (`1739a8daf93955fe31050b659151f1d45a8555ff58fe3a130b8a389c049d8290`)
- Predecessor `10.4` (final-record-v2): `docs/release-evidence/story-10.4-final-record-v2.json` (`f6a670f7a9d6be89b765757c0d480879353e033217980c28329e6cdf3f5d38b9`)

## Review decision

- Decision: approved
- Reviewer: Jerome Piquot
- Decided at (UTC): 2026-10-18T16:27:28Z
- Bound source digest: `6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb`
- Bound signable-payload digest: `9ce146a26cd693261392928a2f767d3752b0101f6097e8a0bc7757784a07ef80`

## Digests

- Source: `6302bc195901d91a318195787c5a576d14206af19bf74cd47729366084b8b9bb`
- Signable payload: `9ce146a26cd693261392928a2f767d3752b0101f6097e8a0bc7757784a07ef80`
