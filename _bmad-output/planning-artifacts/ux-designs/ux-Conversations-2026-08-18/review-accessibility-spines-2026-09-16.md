# Accessibility Review — New UX Spines — 2026-09-16

- **DESIGN.md:** `DESIGN.md` — SHA-256 `d219fb8937ff40a4fcdc8544b7e6fa6a4cef36df417c589217aecb58e6070cf1`
- **EXPERIENCE.md:** `EXPERIENCE.md` — SHA-256 `9838e266a4c678b75e7b3fc1d724700c43f608e94798d9247aed5ab978916fd5`
- **Scope:** independent accessibility lens on the new spine pair only; no spine edits
- **Standard:** preserved WCAG 2.1 AA authority, checked against the [W3C WCAG 2.1 Recommendation](https://www.w3.org/TR/WCAG21/)

## Overall verdict

**PASS as a non-activated preservation draft; NOT READY for activation.** The
pair preserves WCAG 2.1 AA, keyboard/screen-reader parity, non-color state
communication, fail-closed redaction and disclosure behavior, responsive trust
ordering, high-contrast/forced-colors verification, touch sizing, and explicit
activation gates. No critical defect was found, and the open decision register
correctly prevents implementation from silently choosing load-bearing
accessibility behavior.

Before activation, one high, two medium, and one low ambiguity should be closed.
The intentionally open blocked-control, live-region, reflow, localization,
temporal-cursor, offline/session, and browser/assistive-technology decisions are
listed separately and are not double-counted as defects.

## Finding counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 2 |
| Low | 1 |

## Findings by severity

### Critical

None.

### High

#### A11Y-01 — The primary investigation and evidence regions lack a programmatically determinable structure and bypass contract

The wide experience is a split workspace with discovery beside a governed
record, and the evidence timeline is a custom, potentially virtualized primary
surface (`EXPERIENCE.md:31-48,80-86,131-140`; `DESIGN.md:59-60,118-126,150-157`).
The spines require chronological order, keyboard navigation, position context,
and a global focus order, but do not require named landmarks/regions, a heading
model, an efficient bypass route between Find, record summary, timeline, and
commands, or a programmatically determinable collection/entry relationship.
Two conforming-looking implementations could therefore produce materially
different screen-reader navigation and force keyboard users through large
result/timeline collections on every pass. This leaves WCAG 2.1 relationships,
meaningful sequence, bypass-block, headings/labels, and name/role/value outcomes
ambiguous.

**Fix:** Add a behavioral contract for the Investigation Workspace and
`Evidence Timeline Entry`: stable named regions/landmarks; one safe page heading;
a heading/label hierarchy; bypass navigation among Find, governed-record summary,
timeline, and command region; and a programmatically determinable ordered
collection whose entries expose authorized actor/time/state and accurate
position context under virtualization. Allow native list/table or an accessible
Fluent composition, but require equivalent keyboard and screen-reader outcomes
and tests without hidden protected entries.

### Medium

#### A11Y-02 — Forced drawer closure has no deterministic safe focus fallback

`Evidence Detail Drawer` authorizes before rendering, closes on permission
downgrade, and “return[s] focus safely” (`EXPERIENCE.md:73,86,119,135,141,147-151`).
The opener may disappear, become forbidden, or cease to be focusable during
that same downgrade. “Safely” does not select the fallback destination or the
announcement ordering, so one implementation may focus a removed node, another
the document body, and another protected record content.

**Fix:** Define the closure sequence: clear protected drawer content; move focus
to the still-authorized opener when it exists; otherwise move to a stable safe
summary/tenant-scope target; then announce only the safe state class and next
action. Cover authorization failure, session/permission downgrade, responsive
drawer removal, and route change.

#### A11Y-03 — Governance-form validation and failure recovery are not accessibility-complete

Forms separate local validation, server validation, and the pre-execution
authorization recheck, while governance-changing confirmation uses dialogs
(`EXPERIENCE.md:87,137-141`). The contract does not yet say how validation
errors are programmatically associated with fields, how a content-safe error
summary is announced and focused, how focus reaches the first invalid control,
or how a late server/recheck failure returns the operator to the affected field
or `Command Gate`. Inherited Fluent behavior cannot choose these
Conversations-specific disclosure-safe outcomes.

**Fix:** Require persistent visible error text, field/message programmatic
association, a content-safe summary announced without protected values,
deterministic focus to the summary or first invalid control, preservation of
only safe entered intent, and focus/announcement behavior for late permission,
freshness, audit, or command-availability failure. Add keyboard and
screen-reader tests for confirmation, cancellation, validation failure, and
post-submit rejection.

### Low

#### A11Y-04 — Full-value access for truncated identifiers could be implemented as hover-only

Authorized long identifiers may truncate only when full-value access and copy
remain available (`DESIGN.md:108-116`). The access mechanism is not required to
be keyboard operable, programmatically named, or available at zoom/reflow, so a
tooltip-only implementation would still appear textually compliant.

**Fix:** State that full value and copy are available without hover, through a
keyboard-operable and screen-reader-named mechanism, and remain usable at 200%
text resize, 320 CSS-pixel reflow, high contrast, and forced colors.

## Intentional activation-time open decisions — not defects

These decisions are explicitly identified as phase blockers, so they preserve a
safe draft rather than authorizing an implementer to improvise
(`EXPERIENCE.md:127,134,138,143-167,263-276`; `DESIGN.md:39-44,63-66,87-104`):

- Exact accessible blocked-control/reason mechanism and programmatic
  association. The required outcome is already fixed: visible without hover,
  keyboard/screen-reader available, source-owned, and fail-closed.
- Live-region transition matrix and urgency, including freshness, permission,
  command, reconstruction, and drawer state changes. The no-protected-detail
  announcement boundary is already fixed.
- Explicit 200% text-resize and 320 CSS-pixel/equivalent 400% reflow criteria.
- Exact Fluent 2 mappings for light, dark, high-contrast, and forced-colors
  modes. Color-only communication is already prohibited.
- Temporal-cursor keyboard, busy, announcement, and focus-restoration behavior.
- English-only versus localized resource-key behavior, including preservation
  of safe state distinctions.
- Offline/network-loss and authentication/session-expiry behavior. Optimistic
  trust and retained protected content are already forbidden.
- Browser/assistive-technology validation matrix and activation evidence.
- Whether WCAG 2.2 AA is promoted. WCAG 2.1 AA remains the binding floor until
  an approved decision raises it.

## Confirmed strengths

- **Authority is unambiguous.** Both spines are `draft` and
  `preserved-not-activated`; WCAG 2.1 AA is explicit and WCAG 2.2 is an open
  activation choice (`DESIGN.md:1-7,102`; `EXPERIENCE.md:1-6,19,29,143-156,268`).
- **Keyboard and focus foundations are strong.** Search, result selection,
  timeline navigation, focus order, authorized drawer entry, safe focus return,
  permission-downgrade closure, and inherited Fluent dialog/accordion behavior
  are covered (`EXPERIENCE.md:23,46-48,80-87,131-153`).
- **Non-color and contrast posture is safe.** Every load-bearing state uses
  text/structure in addition to inherited color/icon roles; exact mappings and
  forced-colors evidence remain gated (`DESIGN.md:87-104,128-134,140-161,165-174`).
- **Redaction and disclosure safety are excellent.** Protected values are absent
  from source data, hidden DOM, accessible names, tooltips, copy, title,
  telemetry, responsive duplicates, and unauthorized drawer transitions
  (`DESIGN.md:39-40,145,172`; `EXPERIENCE.md:54-63,71-90,145-154,167`).
- **Responsive and touch obligations are explicit.** Trust order and disclosure
  boundaries persist across breakpoints; CSS hiding is not authorization;
  mobile is read-only by default; touch targets are at least 44×44 px where
  touch is supported (`EXPERIENCE.md:44-48,126-127,152-167`).
- **Testing has a sound safety base.** Leak Sentinel covers visible/hidden DOM,
  accessibility tree, tooltips, URLs, title, clipboard, telemetry, screenshots,
  loading placeholders, and responsive duplicates (`EXPERIENCE.md:151-156`).
  The open browser/AT evidence matrix correctly remains a pre-activation gate.

## Gate recommendation

Keep the spines in `draft` / `preserved-not-activated`. Accept them as a safe
preservation contract after recording A11Y-01 through A11Y-04 for correction;
do not treat them as implementation-ready until those findings and every named
activation-time accessibility decision have an approved, testable outcome.
