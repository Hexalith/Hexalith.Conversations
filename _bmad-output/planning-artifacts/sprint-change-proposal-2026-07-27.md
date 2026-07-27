---
title: "Sprint Change Proposal Amendment — Retain a Module-Scoped Conversations Test AppHost"
project: "Conversations"
date: "2026-07-27"
status: "approved"
changeScope: "moderate"
mode: "incremental"
trigger: "Human clarification during the paused Story 6.2 bmad-loop resolution"
supersedesProposalScope: "sprint-change-proposal-2026-07-26.md production-composition interpretation"
affectedAuthority: "conversations-architecture-2026-07-15-v2 and epic-6-authority-2026-07-15-v2"
---

# Sprint Change Proposal Amendment — Retain a Module-Scoped Conversations Test AppHost

## Approval State

Jerome confirmed this amendment on 2026-07-27. The confirmed rule is narrower
than the July 26 proposal: every module keeps an AppHost for user testing limited
to that module, but that AppHost is not shipped and is not the production or
deployment composition root.

## 1. Issue Summary

Story 6.2 halted because the frozen authority required the existing
`Hexalith.Conversations.AppHost` to be removed and required an unnamed platform
AppHost to receive its topology. The human clarified that the local AppHost must
remain so Conversations can be exercised as a complete module during user and
end-to-end testing.

The existing project is currently `<IsPublishable>true</IsPublishable>`. Leaving
that unchanged would blur the confirmed boundary and conflict with the Hexalith
rule that a domain module must not ship its own AppHost. The correction therefore
retains the project but makes its test-only role mechanically explicit.

This is a hosting/test-boundary correction, not a product feature or UX change.
Projection population, tenant isolation, replay, SM-C2, promotion, public-contract,
and release-evidence gates remain binding.

## 2. Corrected Ownership Invariant

`src/Hexalith.Conversations.AppHost/` is the module-scoped composition harness for
local Conversations user tests and production-boundary end-to-end verification.
It composes only the Conversations surfaces and required platform dependencies
needed by those tests.

The harness is non-packable and non-publishable. It is not a production deployment
artifact and does not own reusable hosting capability. Production/deployment
composition remains platform-owned. `Hexalith.EventStore.DomainService`,
`Hexalith.EventStore.ServiceDefaults`, `Hexalith.EventStore.Aspire`, and applicable
Commons helpers continue to own generic hosting, endpoints, DAPR behavior, health,
telemetry, projection/query runtime, and subscription plumbing.

`Hexalith.Conversations.ServiceDefaults` remains removable when it is only a
facade over shared platform defaults. This decision does not authorize a
Conversations-owned reusable Aspire or ServiceDefaults library.

The canonical runtime integration remains
`AddEventStoreDomainService(...)` plus `UseEventStoreDomainService()`.

## 3. Impact Analysis

### PRD and UX

No PRD, feature, journey, interaction, accessibility, or FrontComposer behavior
changes. The AppHost is a test harness for exercising the existing contract.

### Epic 6

Epic 6 remains viable and keeps its binding dependency order. A versioned v3
authority amendment supersedes only AppHost ownership statements:

- Story 6.1 remains completed historical work; its absolute AppHost-removal rule
  is superseded without reopening or rewriting history.
- Story 6.2 retains and thins the local AppHost, makes it non-publishable, removes
  generic duplication, and does not modify FrontComposer.AppHost or
  EventStore.AppHost.
- Stories 6.3 and 6.4 are unaffected.
- Story 6.5 includes one non-publishable module test AppHost in the thin authoring
  template and counts its hand-authored cost in SM-2.
- Story 6.6 verifies the test harness boundary and unchanged production proofs.
- Story 6.7 remains a prerequisite for promotion-bearing work.

No new epic, story identifier, or sprint-status edit is required.

### Architecture

Architecture advances to v3 and distinguishes three responsibilities:

1. Conversations owns domain behavior and a non-shipping module test harness.
2. Platform libraries own reusable runtime capability.
3. Platform deployment owns production composition.

The corrected target tree keeps `src/Hexalith.Conversations.AppHost/`,
`tests/Hexalith.Conversations.AppHost.Tests/`, and their solution entries. It
continues to exclude an independently reusable Conversations Aspire library and
removes generic ServiceDefaults duplication.

### Universal Hexalith Baseline

No baseline change is required. The baseline forbids a domain module from
*shipping* an AppHost. A non-packable, non-publishable local test harness satisfies
that rule while allowing module-limited user testing.

### Evidence and Metrics

- SM-C2 keeps the same four-row inventory and `post P95 <= 1.05 x baseline P95`
  rule. Pre/post runs use the same module harness and production code boundaries.
- The production named-projection path, actual state-store end state, production
  query result, partial-write convergence, deletion, and replay proofs are unchanged.
- SM-2 includes the required test AppHost's hand-authored files and LOC.
- Signed v1 evidence remains byte-identical.

## 4. Recommended Path

Use a direct versioned authority adjustment. Preserve the v2 authority as history,
publish architecture/Epic 6 v3 clarification, amend the frozen Story 6.2 spec, and
re-drive from scratch after Story 6.7 and the pre-topology SM-C2 baseline are ready.

Rollback and MVP reduction are not warranted. The existing test harness is useful
and the product contract remains achievable.

Effort is moderate for planning and low for the AppHost boundary itself; the
projection-population and evidence work remains the larger Story 6.2 effort. Risk
is low once `IsPublishable=false` and conformance checks make the boundary explicit.

## 5. Detailed Change Proposals

### Architecture v3

Replace “remove every Conversations-owned AppHost” with:

> Conversations retains one non-packable, non-publishable AppHost solely for
> module-scoped local user and end-to-end testing. It consumes public platform
> helpers and production runtime paths but is not a production/deployment
> composition root. Platform libraries own reusable runtime capability and the
> platform owns production deployment composition.

Update the FR-13 landing-zone row, target tree, local workflow, deployment
boundary, validation text, and SM-C2 wording consistently.

### Epic 6 v3 amendment

Append, rather than rewrite, authority that:

- supersedes Story 6.1 AC 4 only for the test-harness exception;
- replaces Story 6.2 AppHost deletion with retention plus
  `IsPackable=false` and `IsPublishable=false`;
- keeps ServiceDefaults removal and generic-capability prohibitions;
- updates Story 6.5's template to include and measure the test AppHost; and
- keeps all other gates and dependency order unchanged.

### Frozen Story 6.2

The corrected intent must say exactly one thing:

- retain the existing Conversations AppHost only as a module-limited user/E2E
  harness;
- never publish or deploy it;
- never move the harness into FrontComposer or EventStore;
- never implement reusable platform plumbing behind Conversations;
- use it to exercise production Server/EventStore append, replay, projection,
  state-store, and query boundaries; and
- remove the generic Conversations ServiceDefaults facade when no domain-specific
  responsibility remains.

The code map and acceptance section keep AppHost source/tests/solution entries and
remove the two candidate central AppHosts from the implementation decision.

## 6. Implementation Handoff

### Scope Classification

**Moderate.** Binding architecture and Epic 6 language changes, but no product,
UX, new-story, or cross-repository baseline change is required.

### Responsibilities

- Architect/planning owner: publish v3 authority and regenerate Epic 6 context.
- Conversations developer: make the existing AppHost non-publishable, retain its
  focused composition tests, remove generic duplication, and implement the
  unchanged projection proof.
- Platform owners: provide public helpers for any generic gap.
- Test/release owner: include the harness in SM-2 and preserve every SM-C2,
  projection, tenant, replay, and evidence gate.

### Sequence

1. Record this v3 authority clarification and repair the frozen Story 6.2 spec.
2. Write the bmad-loop resolution marker last; do not change sprint status.
3. Complete Story 6.7 and capture/reconstruct the SM-C2 baseline before topology
   or runtime changes.
4. Re-drive Story 6.2 from scratch against the corrected spec.
5. Carry the measured test-harness cost into Story 6.5 and verify all gates in 6.6.

### Success Criteria

- The existing AppHost, its tests, and solution entries remain.
- Its project is non-packable and non-publishable.
- It composes only module-limited test topology through public platform helpers.
- It is not used or described as a production deployment composition root.
- No generic Conversations Aspire, ServiceDefaults, DAPR, health, telemetry,
  projection/query, publication, or subscription facade remains.
- Production append/replay/projection/state-store/query proof and all preservation
  gates remain unchanged.
