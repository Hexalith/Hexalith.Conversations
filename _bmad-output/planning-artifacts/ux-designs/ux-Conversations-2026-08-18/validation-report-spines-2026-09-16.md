# Validation Report — Hexalith.Conversations UX Spines

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-Conversations-2026-08-18/EXPERIENCE.md`
- **Run at:** 2026-09-16T20:27:59+02:00
- **Disposition:** `draft` / `preserved-not-activated`

## Overall verdict

The spine pair is structurally coherent, source-linked, faithful to the non-activation hold, and useful as a compact preservation draft. It remains broken as a downstream implementation contract: load-bearing trust-role tokens are unbound, and requirement closure, capability slicing, state-display mappings, accessible interaction details, and source-owned field bindings remain incomplete.

The extra lenses materially reinforce that conclusion. Authority alignment requires revision because the current precedence statement overstates the rejected PRD, Architecture V15 targets read as delivered behavior, the trust strip implies a client-owned rollup, and some component contracts claim fields not present in named DTOs. Accessibility passes for preservation but identifies a high-severity structural-navigation gap. Downstream consumability is adequate for orientation and thin as a standalone planning or QA handoff.

## Category verdicts

- Flow coverage — **adequate**
- Token completeness — **broken**
- Component coverage — **thin**
- State coverage — **thin**
- Visual reference coverage — **strong**
- Bloat & overspecification — **strong**
- Inheritance discipline — **adequate**
- Shape fit — **strong**

## Finding counts

Counts preserve each independent lens; related findings may overlap.

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 11 |
| Medium | 11 |
| Low | 2 |

## Findings by severity

### Critical (1)

**[Rubric · Token completeness] — Load-bearing trust-state colors have no reproducible bindings** (`DESIGN.md:16,87-104`; `EXPERIENCE.md:267`)

The draft intentionally leaves `colors` empty and does not bind current, stale, rebuilding, unavailable, forbidden, redacted, degraded, and blocked roles to exact Fluent tokens or approved values. An implementation would have to invent visual decisions.

Fix: keep activation blocked until Product/UX and the platform UI owner approve exact Fluent token references or custom values, foreground/background pairs, and light/dark/high-contrast/forced-colors evidence.

### High (11)

**[Rubric · Flow coverage] — Named requirement closure is not auditable** (`DESIGN.md:8-15`; `EXPERIENCE.md:176-261`)

The spines do not map the 52 preserved UX decisions, 28 acceptance IDs, or UX-relevant PRD requirements to a spine location or explicit disposition.

Fix: add a compact trace artifact mapping every preserved item to a section, component, state, flow, deferred decision, or non-product-UX disposition.

**[Rubric · Flow coverage] — Supported action capability remains unresolved** (`EXPERIENCE.md:180-251,272`)

Flows invoke citation, acceptance, waiver, and privileged actions while the role/surface/capability/breakpoint matrix remains open.

Fix: approve a source-owned capability matrix with failure, reauthorization, freshness, and audit consequences.

**[Rubric · Component coverage] — `Command Gate` blocked-control behavior is incomplete** (`DESIGN.md:65-66,158`; `EXPERIENCE.md:87,137-138,270`)

The safe outcome is fixed, but focusability, disabled semantics, reason association, announcement, and downgrade behavior are not.

Fix: select and test one Fluent-compatible accessible pattern.

**[Rubric · State coverage] — Four public reason codes and the forbidden display label remain unmapped** (`EXPERIENCE.md:54-61,94-109,271`)

`out_of_order_event`, `mixed_generation`, `poison_event`, and `metadata_write_failed` lack source-owned display mappings; “Restricted” is not explicitly bound to `Forbidden/forbidden`.

Fix: publish a state + reason to display label, visual role, severity, and allowed-next-action map.

**[Rubric · State coverage] — Asynchronous focus and announcement transitions are unspecified** (`EXPERIENCE.md:134,143-156,275`)

Loading, reconstruction, degradation, permission downgrade, and evidence-review transitions cannot be implemented or tested consistently.

Fix: define a transition matrix covering busy state, announcement, focus, protected-content clearing, recovery, and required evidence.

**[Accessibility] — Split-workspace and virtualized-timeline structure lacks landmarks and bypass behavior** (`EXPERIENCE.md:31-48,80-86,131-140`; `DESIGN.md:59-60,118-126,150-157`)

Named regions, heading hierarchy, bypass navigation, and ordered collection semantics are not required.

Fix: define stable landmarks/regions, one safe page heading, bypass routes, and an accessible ordered collection with accurate position context under virtualization.

**[Authority alignment] — Precedence elevates a PRD that current validation rejects** (`EXPERIENCE.md:19`; both source manifests)

The PRD is described as controlling preserved obligations even though its current validation bars release, architecture, and story-generation authority. Qualifying epics, validation, reconciliations, and memlog sources are omitted.

Fix: declare ordered authority: V4 map for disposition; V15 for technical target state/ownership; legacy UX for preserved detail; PRD/addendum/epics for qualified preservation context; separate authority for activation.

**[Authority alignment] — Architecture V15 target mappings read as current delivered behavior** (`EXPERIENCE.md:94-111`)

Enum presence is not proof that runtime handlers and persistence conform to AD-7/AD-8.

Fix: label the mappings “V15 normative target; not current-runtime compliance evidence” and link activation to successor Epic 16 carriers and conformance evidence.

**[Authority alignment] — `Trust Posture Strip` invents an unowned client rollup** (`DESIGN.md:153`; `EXPERIENCE.md:27,82`)

The UI is told to compute a conservative winner even though the named DTO exposes separate fields and no approved aggregate precedence.

Fix: render source-owned fields in fixed order without computing a winner until an owned aggregate DTO and tests exist.

**[Authority alignment] — Component claims exceed exact public DTO fields** (`DESIGN.md:142-154`; `EXPERIENCE.md:75,83`)

Freshness “source,” completeness categories, generic source, and hydration-source claims are not bound to named properties.

Fix: add an exact component binding table covering DTO, property, nullability, display mapping, and owner; remove or defer unsupported claims.

**[Downstream consumability] — Preserved requirements are not traceable from the spines** (`DESIGN.md:8-15`; `EXPERIENCE.md:7-14,176-261`)

Planning and QA cannot distinguish intentional compression from accidental omission without rereading legacy prose.

Fix: add the companion trace artifact requested by the rubric and reference it from both spines.

### Medium (11)

**[Rubric · State coverage] — Global offline and session-expiry behavior is open** (`EXPERIENCE.md:127,273`)

Fix: define detection, protected-content clearing, focus/announcement, safe retry or reauthentication, and interaction with open detail surfaces.

**[Rubric · Inheritance] — Fluent UI V5 release binding is unresolved** (`DESIGN.md:81-89`; `EXPERIENCE.md:269`)

Fix: assign version ownership to FrontComposer or bind an approved package policy and upgrade gate.

**[Accessibility] — Forced drawer closure lacks a deterministic focus fallback** (`EXPERIENCE.md:73,86,119,135,141,147-151`)

Fix: clear protected content, return focus to an authorized opener when possible, otherwise target a stable safe summary or tenant-scope control, then announce only safe state and next action.

**[Accessibility] — Governance-form validation and recovery are incomplete** (`EXPERIENCE.md:87,137-141`)

Fix: require persistent visible errors, field association, a safe error summary, deterministic focus, preservation of safe intent, and tested late-recheck failure behavior.

**[Authority alignment] — “Restricted” is not explicitly mapped to `Forbidden/forbidden`** (`EXPERIENCE.md:54-63,98-107`)

Fix: state the presentation mapping and keep nonexistent and unauthorized tenants indistinguishable.

**[Authority alignment] — Governance capabilities appear selected before slicing** (`EXPERIENCE.md:33-46,87-90,160-165,213-221`)

Fix: mark evidence-review outcomes conceptual/read-only and make mutations conditional on an approved capability matrix and source-owned commands.

**[Authority alignment] — FR-20 trust-preservation proof blocker is absent** (current PRD validation; spine Foundation/Open Decisions)

Fix: record that freshness and audit-pairing are preservation obligations whose proof denominator is incomplete and cannot support acceptance or hold lift.

**[Authority alignment] — `Admin.Web` and FrontComposer ownership boundary is incomplete** (spine Foundation; Architecture V15 ownership sections)

Fix: state the optional `Admin.Web` composition boundary, FrontComposer composition ownership, Conversations contract/projection ownership, and separate approval for shared promotion.

**[Downstream consumability] — IA mixes screens, contracts, tooling, and boundaries** (`EXPERIENCE.md:31-46`)

Fix: add delivery-form, owner, and UI-artifact-expectation columns.

**[Downstream consumability] — Vocabulary layers are not explicitly namespaced** (`DESIGN.md:87-102`; `EXPERIENCE.md:50-63,92-127`)

Fix: distinguish contract state, contract reason, display label, visual role, and composite posture; only the first two are serialized.

**[Downstream consumability] — Open decisions are not actionable planning records** (`EXPERIENCE.md:263-276`)

Fix: assign stable IDs, owner, decision authority, affected surfaces/components, closure artifact, evidence, and activation gate.

### Low (2)

**[Accessibility] — Truncated identifier access could become hover-only** (`DESIGN.md:108-116`)

Fix: require a keyboard-operable, screen-reader-named full-value and copy mechanism that survives resize, reflow, high contrast, and forced colors.

**[Downstream consumability] — IA journey closure uses protagonist names instead of Flow IDs** (`EXPERIENCE.md:33-44,176-251`)

Fix: reference exact stable Flow names/IDs while retaining persona names for readability.

## Reviewer verdicts

- Rubric walker — broken for implementation; adequate as a preservation draft.
- Accessibility — pass as a non-activated preservation draft; not activation-ready.
- Authority alignment — revise.
- Downstream consumability — adequate as a non-activated draft; thin as a standalone handoff.

## Reviewer files

- `review-rubric-spines-2026-09-16.md`
- `review-accessibility-spines-2026-09-16.md`
- `review-authority-alignment-spines-2026-09-16.md`
- `review-downstream-consumability-spines-2026-09-16.md`

