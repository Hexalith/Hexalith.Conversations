---
title: Architecture V15 reconciliation
date: 2026-09-16
status: reconciled
activation: preserved-not-activated
source: _bmad-output/planning-artifacts/architecture.md
sourceAuthority: conversations-architecture-2026-09-16-v15
---

# Architecture V15 Reconciliation

Citation keys:

- `ARCH` — `_bmad-output/planning-artifacts/architecture.md`
- `ARCH-RUN` — `_bmad-output/planning-artifacts/architecture/architecture-Conversations-2026-08-02`
- `UX-LEGACY` — `_bmad-output/planning-artifacts/ux-design-specification.md`
- `UX-MAP` — `_bmad-output/planning-artifacts/ux-requirement-map.md`

## Outcome

Architecture V15 is accepted as an input to this UX update. It changes the
state, freshness, ownership, and public-boundary contracts that UX must render;
it does **not** activate product UI implementation or lift any hold. V15 is
`FINAL` with the implementation hold `ACTIVE`, keeps the operator UI optional,
and explicitly authorizes no UX-scope change
(`ARCH:2702-2716,3175-3202`). The
legacy preservation posture therefore remains in force
(`UX-LEGACY:44-54`; `UX-MAP:14-23`).

The 2026-09-16 validation report records the pre-update V14 failure
(`ARCH-RUN/validation-report-2026-09-16.md:9-14`).
It is a correction source, not the V15 verdict. The final V15 rubric, reality,
and adversarial re-reviews report no remaining critical or high findings
(`ARCH-RUN/reviews/update-2026-09-16/review-rubric.md:9-19`;
`ARCH-RUN/reviews/update-2026-09-16/review-reality-checked.md:9-28`;
`ARCH-RUN/reviews/update-2026-09-16/review-adversarial.md:3-13`).

## Confirmed UX Implications

### Foundation and ownership

- FrontComposer owns UI composition; Conversations owns its domain contracts
  and projections; Tenants owns tenant lifecycle and projection capability;
  Parties owns personal data; production topology remains platform-owned
  (`ARCH:2765-2785,2798-2810`).
- UI views, caches, indexes, exports, and evidence remain derived and
  rebuildable. No UI or query path may repair or become a second source of
  Conversation truth (`ARCH:2787-2796`).
- Recovery is not query-triggered, and Conversations/UI cannot clear an unsafe
  tenant-projection state (`ARCH:2981-2986`). A retry control may
  refresh status; it must not imply that viewing a page repairs the projection.

### Required state mapping

The list/search/empty/rebuild experiences render the server-owned mapping in
AD-7; they do not infer state from an empty array, a timestamp, or local cache
age (`ARCH:3030-3043`):

| Observed condition | Public state / reason | UX effect |
| --- | --- | --- |
| v2 `Ready`, zero summaries, no pending dispatch | `Current` / `current` | Render a genuine current empty page. |
| v1, `Erasing`, `Erased`, or `Rebuilding` | `Rebuilding` / `rebuilding` | Render rebuild status, not empty/current content. |
| Lifecycle or source-position gap | `Rebuilding` / `gap_detected` | Block reliance and governed actions. |
| Missing record for an authorized tenant or unavailable provider | `Unavailable` / `unavailable` | Render unavailable, never an empty result. |
| Corrupt, mixed-generation, or contradictory lifecycle record | `Unavailable` / `metadata_contradictory` | Fail closed with content-safe explanation. |
| Nonexistent or unauthorized tenant | `Forbidden` / `forbidden` | Use a non-disclosing state; perform no index read. |

Queries accept projected content only when detail, dispatch, and index
generations match; otherwise the public result is `Rebuilding`
(`ARCH:3024-3034`). Tenant projection states are internally
`Healthy`, `Gapped`, `Corrupt`, or `Unavailable`, and every state except
`Healthy` denies authorization (`ARCH:2941-2986`). Thus a tenant
trust downgrade clears or withholds protected content across search, detail,
drawers, commands, copy/export, and responsive duplicates.

### Freshness and time

AD-8 gives the freshness UI exact semantics
(`ARCH:3051-3080,3097-3105`):

- `LastAppliedEventTimestamp` is the applicable replay anchor.
- `ProjectionGeneratedAt` is the actual UTC instant generation completed.
- `LagDuration` is the interval between those values; stale state and reason
  also account for explicit gap, corruption, and rebuild state.
- A generation instant earlier than its replay anchor, an invalid/default
  selected time, or reuse of an identity with a different time binding maps
  fail closed to `Unavailable` / `metadata_contradictory`.
- Observation-time freshness is regenerated and tested semantically; it is not
  part of deterministic replay-byte identity.

The UX must label last-applied and generated times distinctly and render the
server-owned state/reason. It must not derive freshness from wall-clock time.

### Security, accessibility, and public boundary

- Non-healthy tenant state denies before any protected projection content is
  rendered. Nonexistent and unauthorized tenants remain indistinguishable at
  the public boundary (`ARCH:2981-2986,3036-3043`).
- Conversations-owned routes, DTO fields, serialized contracts, safe error
  codes, client-safe logs, OpenAPI descriptions, and the approved public
  position are valid UX inputs. Raw EventStore envelopes, stream/storage
  topology, provider payloads, internal exceptions, and internal positions are
  not (`ARCH:3137-3163`).
- State labels, reasons, loading announcements, focus changes, and responsive
  duplicates remain disclosure surfaces. Existing keyboard, screen-reader,
  non-color, clipboard, DOM, title, telemetry, and responsive leakage
  safeguards remain binding; V15 does not weaken them.

## Conflicts and Overrides

1. **Authority reference.** `UX-MAP:2-9` still names Architecture
   V14. V15 supersedes that source reference for current technical constraints,
   but does not change the map's `preserved-not-activated` disposition.
2. **Freshness timing.** Architecture memlog entry 68 describes
   `ProjectionGeneratedAt` as the maximum contributing event time
   (`ARCH-RUN/.memlog.md:68`). Final
   AD-8 supersedes that intermediate wording: it is the actual generation
   completion instant (`ARCH:3067-3080`). The final spine wins.
3. **Empty versus unavailable.** The legacy safe-empty pattern is now bounded:
   only v2 `Ready` plus zero summaries is a current empty page. Missing,
   rebuilding, erased, corrupt, mixed-generation, or unavailable state must not
   be collapsed into “No accessible matches.”
4. **Vocabulary.** Existing UX copy uses terms such as denied, restricted,
   degraded, unknown, and incomplete. These may remain presentation copy or a
   composite posture only when explicitly mapped to source-owned fields; they
   must not become parallel public states. The canonical vocabulary forbids
   local state synonyms (`ARCH:1257-1284`). In particular,
   “Restricted” copy maps to `Forbidden` / `forbidden`.
5. **Preserved-content loading.** The legacy rule allowing last-known content to
   remain visible “where safe” does not apply after tenant health becomes
   non-`Healthy` or projection generations disagree. Those cases deny or return
   rebuild status and cannot preserve protected content as a usable view.

## Deferred or Unresolved

- The exact Fluent UI Blazor V5 prerelease/stable policy and promotion from the
  current WCAG 2.1 AA floor to WCAG 2.2 AA remain Product/UX and platform-owner
  decisions before optional UI activation. V15 does not silently decide either
  (`ARCH:3195`;
  `ARCH-RUN/reviews/validate-2026-09-16/review-reality-checked.md:221-261`).
- Exact redaction/hydration condition-to-surface mappings remain gated on the
  missing `conversations-vocabulary-v1.json`. Existing public value sets must
  not be widened merely to close that gap (`ARCH:3189`).
- V15 introduces no offline mode or optimistic local authority. Any future
  offline/cache design requires its own approved contract and must remain
  derived, visibly non-current, tenant-safe, and unable to enable governed
  actions.
- UI implementation remains subject to separate approved activation/release
  authority; this reconciliation is a design-contract update only.
