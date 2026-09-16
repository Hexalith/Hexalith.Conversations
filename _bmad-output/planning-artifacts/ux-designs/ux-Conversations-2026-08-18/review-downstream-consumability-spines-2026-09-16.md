# Downstream Consumability Review — New UX Spines — 2026-09-16

- **DESIGN.md:** `DESIGN.md` — SHA-256 `d219fb8937ff40a4fcdc8544b7e6fa6a4cef36df417c589217aecb58e6070cf1`
- **EXPERIENCE.md:** `EXPERIENCE.md` — SHA-256 `9838e266a4c678b75e7b3fc1d724700c43f608e94798d9247aed5ab978916fd5`
- **Scope:** downstream use of the new spine pair only; no legacy prose used as a substitute contract and no spine edits
- **Consumers:** architecture, epic/story planning, implementation agents, QA, and human reviewers

## Overall verdict

**ADEQUATE as a `preserved-not-activated` draft; THIN as a standalone downstream
handoff.** The pair is concise, internally coherent, and safe against accidental
activation. All declared source paths resolve, the visual/behavioral ownership
split is clear, canonical component names align across both spines, Architecture
V15 state mappings are extractable, and intentional implementation choices are
collected as activation blockers.

The pair does not yet meet the stated goal of clean source extraction without
legacy prose. It contains no traceability from the preserved UX decision and
acceptance-criterion inventory into the new sections, components, states, flows,
or tests. That prevents planning and QA from proving that the compact spines
fully preserve—or deliberately retire—every inherited obligation.

## Finding counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 3 |
| Low | 1 |

## Findings by severity

### Critical

None. The explicit draft/non-activation posture prevents the current gaps from
authorizing unsafe implementation (`DESIGN.md:1-7,75-77`;
`EXPERIENCE.md:1-6,17-29,263-276`).

### High

#### DC-01 — Preserved requirements and acceptance obligations are not traceable from the spines

The frontmatter points to the UX requirement map, legacy UX specification, PRD,
addendum, architecture, design directions, and repository UX rules
(`DESIGN.md:8-15`; `EXPERIENCE.md:7-14`). The body then says the requirement map
controls disposition, the PRD controls obligations, and the legacy specification
preserves detailed intent (`DESIGN.md:77`; `EXPERIENCE.md:19`). Neither spine
maps preserved UX decision IDs, safety/responsive/accessibility acceptance IDs,
PRD requirement groups, or source sections to the new contract. The only source
IDs present are the three refactor/tooling journeys that are expressly excluded
from product UX (`EXPERIENCE.md:253-261`).

Architecture can extract the high-level constraints, but epic/story planners
cannot establish requirement coverage, QA cannot derive a closed acceptance
inventory, and reviewers cannot tell whether an omitted legacy rule was
intentionally retired or accidentally dropped. Requiring those consumers to
re-read legacy prose defeats the spine handoff.

**Fix:** Add a compact traceability appendix or companion referenced from both
spines. For every preserved UX decision/acceptance ID and relevant PRD
requirement group, record exactly one disposition: owned by a named
DESIGN/EXPERIENCE section, component, state, or Flow; intentionally out of
product-UX scope; deferred behind a named Open Decision; or superseded by an
exact authority citation. Include stable test IDs for normative accessibility,
leakage, tenant-isolation, state-mapping, focus, responsive, and command-safety
outcomes. Do not restate source prose.

### Medium

#### DC-02 — Information Architecture mixes product screens, adopter contracts, tooling, and domain boundaries without classifying delivery form

The IA table calls all entries “Surface,” but it mixes operator web views with
an adopter-owned continuity contract, developer package/quickstart tooling, a
read-time module boundary, and a machine-readable incident workflow
(`EXPERIENCE.md:31-46`). A story planner could reasonably create UI work for
`Developer Integration Surface` or `Stable-reference Boundary`, while another
could treat `Evidence Acceptance Review` and `Operational Degradation Review`
as reports or CLI workflows. The later tooling-journey disposition clarifies
three actors but does not classify every IA row (`EXPERIENCE.md:253-261`).

**Fix:** Add columns for delivery form/owner and UI artifact expectation, using
values such as FrontComposer screen, adopter-owned UI contract, documentation/
SDK/conformance workflow, machine-readable evidence, or no product screen.
Retain each named concern, but make screen creation and ownership unambiguous.

#### DC-03 — Public state, reason code, display copy, visual role, and composite posture are not explicitly namespaced

The canonical table correctly separates public state and reason
(`EXPERIENCE.md:92-111`), while Voice and Tone uses display labels such as
“Restricted” (`EXPERIENCE.md:50-63`) and DESIGN uses aggregate visual roles such
as current/success, warning/stale, error/blocked, and degraded
(`DESIGN.md:87-102`). Surface-state closure also mixes contract and presentation
terms such as forbidden, denied, incomplete, unknown, and degraded
(`EXPERIENCE.md:113-127`). Open Decision 5 correctly blocks final label mapping
(`EXPERIENCE.md:271`), but it does not tell downstream consumers which existing
terms are serialized values, reason codes, presentation copy, or composite UI
postures.

**Fix:** Add a small vocabulary-layer table now, without deciding the open
labels: `contract state`, `contract reason`, `display label`, `visual role`, and
`composite posture`. Mark every term used in the spines with its layer and state
that only the first two are serialized. Keep the exact reason-to-label mapping
as the existing activation-time decision.

#### DC-04 — Open decisions are safe blockers but not actionable planning records

The ten Open Decisions clearly prevent silent implementation choices
(`EXPERIENCE.md:263-276`), and individual sections repeat the affected unknowns
for colors, blocked controls, offline/session behavior, temporal focus,
localization, live regions, zoom/reflow, and browser/AT evidence
(`DESIGN.md:87-104,136-161`; `EXPERIENCE.md:109,127,134,138,143-156`). They have
no stable decision IDs, owner, resolution authority, affected surfaces/
components, required evidence, or closure artifact. Epic/story planning cannot
schedule closure consistently, and reviewers cannot verify that a later answer
closed the intended gap rather than a nearby one.

**Fix:** Convert the numbered list into a decision register with stable IDs,
owner, decision authority, affected components/surfaces, required artifact or
test evidence, and activation gate. Other sections should cite the ID instead
of restating the open issue.

### Low

#### DC-05 — IA journey closure uses protagonist names instead of stable Flow identifiers

IA rows close on “Sarah,” “Helen,” “Daniel,” and similar names
(`EXPERIENCE.md:33-44`), while the executable journeys are named `Flow 1`
through `Flow 7` (`EXPERIENCE.md:176-251`). Names are readable but not unique
contract identifiers: Sarah closes two surfaces, Helen participates in one
shared flow, and later persona renaming would break mechanical extraction.

**Fix:** Use exact references such as `Flow 1 — Governed investigation (Sarah)`
in the IA table. Keep persona names for narrative readability.

## Consumer-by-consumer assessment

| Consumer | Verdict | What is usable now | What blocks clean handoff |
|---|---|---|---|
| Architecture | Strong | UI-system inheritance, source-owned trust, tenant/disclosure boundary, canonical public state/freshness mapping, and non-activation are explicit. | Exact source binding remains an Open Decision. |
| Epic/story planning | Thin | Named surfaces, components, flows, states, and activation blockers provide useful decomposition seams. | No inherited requirement-to-spine traceability; mixed delivery forms; open decisions lack owners and closure evidence. |
| Implementation agents | Adequate for orientation; intentionally blocked | Twenty canonical components have aligned visual and behavioral contracts; token references resolve; fail-closed behavior is clear. | Draft status plus open mappings/mechanics correctly forbid implementation-ready interpretation. |
| QA | Thin | State table, failure paths, disclosure rules, responsive boundary, and accessibility floor are testable foundations. | No stable acceptance inventory connects source obligations to components/states/flows/tests. |
| Human review | Adequate | Compact structure, explicit precedence, state matrix, component tables, and Open Decisions are easy to inspect. | Coverage completeness still requires consulting legacy artifacts. |

## Mechanical and naming checks

- All seven frontmatter source paths in each spine resolve from the repository
  root (`DESIGN.md:8-15`; `EXPERIENCE.md:7-14`).
- All four `{spacing.*}` references resolve to frontmatter tokens
  (`DESIGN.md:27-31,118-126`). No unresolved token reference was found.
- The same twenty canonical component names appear in DESIGN frontmatter,
  `DESIGN.md.Components`, and `EXPERIENCE.md.Component Patterns`
  (`DESIGN.md:32-73,136-161`; `EXPERIENCE.md:65-90`).
- Visual and behavioral ownership is explicit: DESIGN owns visual identity;
  EXPERIENCE owns behavior and public state handling (`DESIGN.md:77-89`;
  `EXPERIENCE.md:19-29,65-67`).
- Architecture V15’s exact empty/rebuilding/unavailable/forbidden mappings and
  freshness meanings are extractable from one state section
  (`EXPERIENCE.md:92-111`).
- Seven preserved product flows have named protagonists, numbered steps, climax
  beats, and failure paths (`EXPERIENCE.md:176-251`).
- Both spines consistently state `draft`, `preserved-not-activated`, separate
  release authority, and the active implementation hold
  (`DESIGN.md:1-7,75-77`; `EXPERIENCE.md:1-6,17-29`).

## Intentional open decisions — correctly not treated as defects

The following incompleteness is safe because it is explicit, fail-closed, and
activation-blocking: exact Fluent color/contrast role mappings; WCAG 2.2
promotion; Fluent V5 prerelease/stable policy; accessible blocked-control
mechanics; remaining reason-to-label mappings; governance capability matrix;
offline/network/session behavior; localization; live-region/zoom/reflow/
temporal-focus/browser-AT evidence; and exact source bindings
(`EXPERIENCE.md:263-276`). These decisions still need the actionable register
requested by DC-04 before downstream planning can own their closure.

## Gate recommendation

Keep the pair in `draft` / `preserved-not-activated`. It is safe and useful as a
compact direction contract, but do not present it as a standalone planning, QA,
or implementation handoff until DC-01 is closed. Address DC-02 through DC-05 in
the same traceability/handoff pass, then rerun downstream-consumability review
against the exact bound source revisions.
