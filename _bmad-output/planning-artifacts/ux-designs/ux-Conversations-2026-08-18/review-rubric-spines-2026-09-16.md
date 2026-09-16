# UX Spine Rubric Review — 2026-09-16

## Summary

overall: **broken as a downstream implementation contract; adequate as an intentional preserved-not-activated draft**

The spine pair is concise, structurally valid, source-linked, internally mirrored, and faithful to the frozen disposition. It must remain `draft` / `preserved-not-activated`: one critical visual-token gap and several explicitly recorded interaction, state, and traceability gaps still require upstream decisions.

Finding counts: **1 critical, 5 high, 2 medium, 0 low**.

| # | Rubric category | Verdict | Evidence |
|---|---|---|---|
| 1 | Flow coverage | Adequate | Seven numbered preserved-product flows cover all nine named preserved actors, and all three current refactor/tooling journeys are explicitly excluded from product UX (`EXPERIENCE.md:176-261`). Requirement-ID and action-capability closure are still missing. |
| 2 | Token completeness | Broken | Spacing references resolve, and inherited type/radius behavior is explicit, but the load-bearing state-role palette is intentionally empty and unmapped (`DESIGN.md:16`, `DESIGN.md:87-104`). |
| 3 | Component coverage | Thin | All 20 canonical custom component names match exactly across both spines and have visual plus behavioral rules (`DESIGN.md:32-72`, `DESIGN.md:136-161`, `EXPERIENCE.md:65-90`). `Command Gate` still lacks its accessible blocked-control contract. |
| 4 | State coverage | Thin | The V15 mappings and surface-state closure are strong (`EXPERIENCE.md:94-127`), but public reason codes, asynchronous accessibility transitions, and global offline/session behavior remain open. |
| 5 | Visual reference coverage | Strong | No `imports/`, `mockups/`, or `wireframes/` directory is configured. The referenced HTML direction is listed, described as illustrative, and subordinated to the spine (`DESIGN.md:8-15`, `DESIGN.md:77`, `DESIGN.md:104`). |
| 6 | Bloat | Strong | The documents stay within their intended split: visual rules in DESIGN, behavior/flows in EXPERIENCE. Repeated component names support the machine-readable/human-readable pairing rather than adding narrative duplication. |
| 7 | Inheritance discipline | Adequate | All seven declared sources resolve, FrontComposer/Blazor Fluent UI V5 inheritance is explicit, and all `{...}` references resolve (`DESIGN.md:8-31`, `DESIGN.md:81-89`, `EXPERIENCE.md:8-29`). The V5 release-channel binding remains unresolved. |
| 8 | Shape fit | Strong | DESIGN uses the required section order (`DESIGN.md:79-163`); EXPERIENCE includes all required sections plus responsive guidance, inspiration, source-authorized key flows, and open decisions (`EXPERIENCE.md:21-276`). Draft/non-activation metadata is consistent in both frontmatters. |

## Critical

- **[Token completeness — load-bearing trust-state colors have no reproducible bindings]**
  - Evidence: `colors` is empty (`DESIGN.md:16`); eight semantic roles are prose-only and exact Fluent mappings are expressly unapproved (`DESIGN.md:87-102`); the same issue is an activation blocker (`EXPERIENCE.md:267`).
  - Impact: an implementation cannot reproduce or verify current, stale, rebuilding, unavailable, forbidden, redacted, degraded, and blocked distinctions without inventing design decisions. This is intentional restraint, but it makes the current draft unfit as an implementation contract.
  - Fix: keep activation blocked until Product/UX and the platform UI owner approve explicit Fluent token references or custom values, including foreground/background combinations and light, dark, forced-colors, and high-contrast evidence. Then populate `colors` and bind each load-bearing component state to those tokens.

## High

- **[Flow coverage — named requirement closure is not auditable from the spines]**
  - Evidence: the primary preservation source contains 52 numbered UX decisions and 28 numbered acceptance criteria (`_bmad-output/planning-artifacts/ux-requirement-map.md:25-118`), while the PRD preserves `Feature-FR1` through `Feature-FR104` and `Feature-NFR1` through `Feature-NFR77` (`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md:402-408`). The spines link the source files and disposition journeys but do not map or explicitly exclude those named requirements (`DESIGN.md:8-15`, `EXPERIENCE.md:176-261`).
  - Impact: a downstream consumer cannot prove that compression preserved every UX obligation or distinguish a deliberate omission from an accidental one.
  - Fix: add a compact requirement-closure appendix or companion trace artifact mapping each UX-DR/AC group and each UX-relevant PRD requirement to an exact spine section, `deferred`, or `not product UX`; reference it from both spines without restating requirement prose.

- **[Flow coverage — supported action capability is unresolved]**
  - Evidence: flows invoke citation copy, accept/reject/waiver outcomes, and privileged operational action (`EXPERIENCE.md:180-251`), while the activation-time matrix for read-only, copy, export, retention, redaction, replay, restore, escalation, and governance mutations is still open (`EXPERIENCE.md:272`).
  - Impact: actors, surfaces, and happy paths are present, but downstream design cannot determine which actions exist for which role/surface/breakpoint or construct complete forbidden and recovery branches.
  - Fix: approve a role × surface × capability × breakpoint matrix, including source-owned availability, safe failure, reauthorization, and audit consequences; then link each flow action to the matrix.

- **[Component coverage — `Command Gate` blocked-control behavior is incomplete]**
  - Evidence: visual and behavioral rows fix the safe outcome but explicitly leave the blocked-control mechanics open (`DESIGN.md:65-66`, `DESIGN.md:158`, `EXPERIENCE.md:87`, `EXPERIENCE.md:137-138`, `EXPERIENCE.md:270`).
  - Impact: implementers must guess whether the blocked action remains focusable, how its reason is programmatically associated, and how keyboard and screen-reader users reach the explanation at a critical governance decision.
  - Fix: choose and test one inherited Fluent-compatible pattern, specifying focusability, disabled versus `aria-disabled` semantics, activation suppression, reason association, announcement, and permission-downgrade behavior.

- **[State coverage — four public reason codes and the visible forbidden label remain unmapped]**
  - Evidence: the canonical table maps the core state/reason combinations (`EXPERIENCE.md:94-107`), then leaves `out_of_order_event`, `mixed_generation`, `poison_event`, and `metadata_write_failed` without a source-owned display mapping (`EXPERIENCE.md:109`, `EXPERIENCE.md:271`). The voice table says “Restricted” while the public state table says `Forbidden` (`EXPERIENCE.md:54-61`, `EXPERIENCE.md:106`).
  - Impact: fail-closed behavior is preserved, but the UI cannot render stable, testable state labels and recovery guidance for every public value without client inference.
  - Fix: publish the authoritative state + reason → presentation-label + severity + allowed-next-action map, including whether `Forbidden` intentionally presents as “Restricted,” and bind `Safe State Message`, `Freshness Marker`, and `Command Gate` to it.

- **[State coverage — asynchronous focus and announcement transitions are not specified]**
  - Evidence: temporal reconstruction keyboard, busy/change announcement, and post-reconstruction focus are open (`EXPERIENCE.md:134`); the live-region transition matrix, resize/reflow criteria, temporal-cursor focus, and browser/assistive-technology matrix remain open (`EXPERIENCE.md:143-156`, `EXPERIENCE.md:275`). The source specifically requires meaningful state-change announcements (`_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md:697-703`).
  - Impact: important loading, degradation, permission-downgrade, evidence-review, and reconstruction changes cannot be implemented or acceptance-tested consistently for keyboard and screen-reader users.
  - Fix: define a transition matrix for trigger, busy state, announcement content/urgency, focus destination/retention, protected-content clearing, and recovery across each affected surface; attach the required automated and manual evidence.

## Medium

- **[State coverage — global offline and session-expiry closure is missing]**
  - Evidence: the surface-state table explicitly leaves offline/network loss and authentication/session expiry open (`EXPERIENCE.md:127`), repeated as an activation decision (`EXPERIENCE.md:273`).
  - Impact: the safe prohibition on optimistic trust and retained protected content is clear, but operators still lack defined detection, content clearing, recovery, and reauthentication behavior.
  - Fix: specify the global shell transitions, retained/cleared state, focus and announcement behavior, safe retry/reauthentication path, and interaction with open drawers and copied/exportable evidence.

- **[Inheritance discipline — the Fluent UI V5 release binding is unresolved]**
  - Evidence: the visual contract inherits Blazor Fluent UI V5 (`DESIGN.md:81-89`), but prerelease/stable dependency policy is explicitly open (`EXPERIENCE.md:269`).
  - Impact: component semantics, token availability, accessibility behavior, and upgrade compatibility are not tied to a reproducible downstream dependency contract.
  - Fix: bind activation to an approved stable/prerelease policy and supported package range, or state that the FrontComposer host owns and supplies the exact version; record the validation owner and upgrade gate.

## Low

None.

## Open Questions

1. Which authority will approve and publish the exact Fluent state-role bindings and contrast evidence?
2. Where will the full reason-code/presentation-label contract and action-capability matrix live?
3. Which inherited Fluent blocked-control pattern will be the tested canonical implementation?
4. Does FrontComposer own the exact Blazor Fluent UI V5 version, or must Conversations pin an approved range?
5. Who accepts the final source-binding/tombstone artifact before these drafts can become implementation contracts (`EXPERIENCE.md:276`)?

## Passed Areas

- Both frontmatters consistently declare `status: draft`, `currentDisposition: preserved-not-activated`, and separate activation authority.
- All seven source paths resolve.
- All 20 canonical component names match exactly across DESIGN frontmatter, DESIGN component rules, and EXPERIENCE component patterns.
- All brace-delimited token references resolve to declared spacing tokens.
- The V15 canonical public-state mappings and freshness timestamp semantics are recorded without client-side invention (`EXPERIENCE.md:94-111`).
- Every named preserved product actor has a numbered flow with context, climax, and failure; Nadia, Sam, and Priya are visibly separated as refactor/tooling actors (`EXPERIENCE.md:176-261`).
- The pair correctly preserves WCAG 2.1 AA and treats WCAG 2.2 promotion as an open decision rather than silently inventing a new baseline (`EXPERIENCE.md:143-156`, `EXPERIENCE.md:268`).
- No visual-reference files are silently orphaned, and the illustrative HTML is explicitly non-normative.
