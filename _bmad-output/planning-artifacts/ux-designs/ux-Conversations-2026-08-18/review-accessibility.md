# Accessibility Review — Conversations UX Preservation Set

## Overall verdict

The preserved contract is **adequate but not activation-ready against WCAG 2.2 AA**. It is unusually strong on non-color trust states, keyboard and screen-reader parity, responsive disclosure safety, leakage prevention, and fail-closed state handling, but it still commits to WCAG 2.1 AA and leaves several trust-critical behaviors too implicit for deterministic implementation and testing. These are preservation-contract findings, not authorization to implement UI work: the set is explicitly `preserved-not-activated`, and remediation that changes decisions must follow the separate release-authority path (`ux-design-specification.md:32-47`; `ux-requirement-map.md:3-14`; `prds/prd-Conversations-2026-06-02/prd.md:406-408`).

## Adversarial findings

### 1. [high] Normative contract — the stated and testable floor is WCAG 2.1 AA, not WCAG 2.2 AA

**Impact.** All three operative statements pin conformance to 2.1 AA: visual contrast (`ux-design-specification.md:742-752`), the overall operator/admin baseline (`ux-design-specification.md:1422-1424`), and automated checks (`ux-design-specification.md:1497-1500`). UX-DR44 preserves the same floor (`ux-requirement-map.md:71-73`), matching Feature-NFR69 (`prds/prd-Conversations-2026-06-02/prd.md:695-703`). A downstream team could satisfy every written gate while omitting WCAG 2.2 AA criteria directly relevant here: Focus Not Obscured (2.4.11) for sticky trust bands/drawers and Target Size Minimum (2.5.8) for dense pointer controls. The contract's 44x44 target applies only “where touch operation is supported” (`ux-design-specification.md:1433-1436`), whereas 2.5.8 applies a 24x24-or-spacing floor to pointer targets generally.

**Fix.** At future activation, under the required approval path, rebaseline Feature-NFR69, UX-DR44, the spec baseline, and automated-test scope to WCAG 2.2 AA. Add explicit tests for focus not being entirely obscured by persistent headers, trust bands, drawers, or messages, and for pointer target size/spacing; retain 44x44 as the stronger touch target. Official anchors: [WCAG 2.2](https://www.w3.org/TR/WCAG22/), [Focus Not Obscured](https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum), [Target Size Minimum](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html).

### 2. [high] Normative contract — blocked actions have an outcome requirement but no accessible interaction mechanism

**Impact.** The contract repeatedly requires disabled actions to expose a safe reason (`ux-design-specification.md:744-747`, `ux-design-specification.md:1048-1050`, `ux-design-specification.md:1111-1112`, `ux-design-specification.md:1432-1434`) but never says how a keyboard or screen-reader user reaches that reason. A native disabled button normally leaves sequential focus, so adjacent text alone does not guarantee association or discoverability. The mock corroborates the ambiguity but is not normative: `.button.disabled` is only visual CSS (`ux-design-directions.html:299-318`), and “Redact” has neither `disabled` nor `aria-disabled` nor an associated reason (`ux-design-directions.html:1089-1097`).

**Fix.** Commit one Command Gate pattern: keep blocked controls focusable with `aria-disabled="true"` or provide an equivalent focusable explanation control; prevent activation in behavior; associate visible, permission-safe reason text programmatically; and expose independently authorized detail only on request. Test keyboard discovery, accessibility-tree output, activation prevention, contrast, and permission downgrade for every blocked state.

### 3. [medium] Normative contract — live trust updates are underspecified and only one transition has an acceptance criterion

**Impact.** Feature-NFR74 requires meaningful state-change announcements in error, degraded, evidence-review, and audit-search workflows (`prds/prd-Conversations-2026-06-02/prd.md:700-703`). The UX contract says trust, freshness, command-availability, and permission changes “should” use an appropriate live region (`ux-design-specification.md:1432-1434`) and gives safe-copy guidance (`ux-design-specification.md:1440-1444`), but it does not define which transitions announce, urgency, de-duplication, `aria-busy` handling, or focus behavior when content is replaced. Only permission downgrade is explicit in an acceptance criterion (`ux-design-specification.md:1515-1518`). A screen-reader operator can therefore continue relying on evidence that silently became stale, redacted, incomplete, or non-actionable.

**Fix.** Define a transition matrix for stale/current/rebuilding, completeness, redaction replacement, command-availability loss, permission downgrade, degraded/error, and rebuild completion. Specify announcement timing/urgency, safe copy (state class plus next action), focus handling, and duplicate suppression; promote each to acceptance tests, including changes while focus is in a timeline, drawer, or governance form.

### 4. [medium] Normative contract — zoom and reflow are asserted but not measurable

**Impact.** The contract says text remains readable under browser zoom (`ux-design-specification.md:1435-1436`) and zoom/narrow-view behavior preserves trust order (`ux-design-specification.md:1516-1519`), but it does not define 200% text resize, 400% browser zoom/equivalent 320 CSS-pixel reflow, permitted two-dimensional exceptions, or no loss of information/functionality. Its breakpoint list (`ux-design-specification.md:1399-1408`) is not a reflow test. Implementations can pass the breakpoint matrix yet force two-axis scrolling or hide evidence and reasons under magnification.

**Fix.** Add explicit WCAG 1.4.4 and 1.4.10 checks: 200% text resize without loss, and 320 CSS-pixel/equivalent 400% zoom without two-dimensional scrolling for non-exempt content. Exercise long localized strings, authorized identifiers, sticky regions, drawers, command gates, validation summaries, and focus visibility. Official anchor: [Reflow](https://www.w3.org/WAI/WCAG22/Understanding/reflow.html).

### 5. [medium] Normative contract — governance microcopy has no localization contract

**Impact.** The five English state words deliberately distinguish content existence and access semantics (`ux-design-specification.md:1448-1458`), while safe reasons and form summaries also carry governance meaning (`ux-design-specification.md:1027-1034`, `ux-design-specification.md:1277-1283`). Neither the spec nor map contains a localization/i18n requirement. A localized deployment can collapse “Redacted,” “Unavailable,” and “Restricted,” truncate load-bearing reasons, or produce accessible names that differ from visible labels.

**Fix.** Either bind the preserved surface explicitly to English-only operation or require reason codes plus localizable resource keys for every trust state, safe reason, error, and next action. Add pseudo-localization, long-string/reflow, visible-label/accessibility-name parity, language/direction metadata, and per-locale tests that state classes stay distinct; never put protected values in translation parameters.

### 6. [medium] Normative contract — the accessibility test plan does not trace every preserved human-trust workflow

**Impact.** The source names citation copy, evidence navigation, audit search, verification-result review, degraded-mode banners, error-state workflows, announcements, and two timed diagnostic scenarios (`prds/prd-Conversations-2026-06-02/prd.md:697-703`). The UX test list covers trust summary, timeline, redaction, citation, drawers, command gates, and focus changes (`ux-design-specification.md:1497-1506`), but omits named lanes for audit search, verification review, degraded/error correction, and the delayed/blocked-projection and failed-release-evidence scenarios. Forms distinguish failure classes (`ux-design-specification.md:1277-1283`) without requiring programmatic field-error association, summary announcement/focus, or pointer-free recovery.

**Fix.** Trace Feature-NFR69–75 and UX-DR44–52 to named automated, keyboard, screen-reader, zoom/reflow, forced-colors, reduced-motion, and manual scenarios. Add audit-search, verification-review, validation-recovery, delayed/blocked-projection, and failed-release-evidence cases, with a declared browser/assistive-technology matrix and retained evidence.

### 7. [low] Normative contract — temporal-cursor navigation is absent from the keyboard/focus contract

**Impact.** Temporal cursor navigation is a custom trust-critical surface (`ux-design-specification.md:296-313`, `ux-design-specification.md:347-359`) and appears in the governed header (`ux-design-specification.md:1042-1045`), but is absent from each keyboard enumeration (`ux-design-specification.md:742-746`, `ux-design-specification.md:968-972`, `ux-design-specification.md:1559-1562`). A cursor jump can replace the timeline and lose focus or screen-reader context even when ordinary timeline navigation passes.

**Fix.** Specify keyboard input, change announcement, busy state, and safe post-reconstruction focus (consistent with AC-RESP-013), then add temporal reconstruction to virtualized-timeline tests.

### 8. [low] Visual mock only — the direction artifact is not an accessible reference implementation

**Impact.** The artifact correctly declares its styling illustrative and non-normative (`ux-design-directions.html:15-18`, `ux-design-directions.html:676-679`). Even so, the chosen mock renders search and selectable rows as non-interactive `div`s (`ux-design-directions.html:823-853`), uses purely visual disabled styling (`ux-design-directions.html:299-318`, `ux-design-directions.html:1089-1097`), applies smooth scrolling without a reduced-motion override (`ux-design-directions.html:51-52`), and retains a fixed/minimum-width app bar without a matching narrow-layout rule (`ux-design-directions.html:234-260`, `ux-design-directions.html:643-657`). Its 32–34px controls (`ux-design-directions.html:109-117`, `ux-design-directions.html:299-307`) also do not illustrate the spec's 44x44 touch target. Copying the markup would produce inaccessible behavior, but these are mock defects, not normative product failures.

**Fix.** Label the artifact “visual direction only; interaction semantics and accessibility behavior come from the specification.” If kept as executable HTML, use real control semantics/keyboard behavior, add `prefers-reduced-motion`, reflow at 320 CSS pixels, and demonstrate committed target sizes and the blocked-action mechanism.

## Evidence and mechanical notes

- **Severity count:** critical 0, high 2, medium 4, low 2.
- **Keyboard/focus coverage is substantial:** pointer-free completion, drawer focus, permission transitions, and virtualized-timeline restoration are committed (`ux-design-specification.md:740-752`, `ux-design-specification.md:1428-1431`, `ux-design-specification.md:1460-1468`, `ux-design-specification.md:1515-1524`).
- **Screen-reader and disclosure safety are strong:** protected content is excluded from DOM, accessible names, live regions, copy, titles, telemetry, and responsive duplicates, with Leak Sentinel coverage (`ux-design-specification.md:755`, `ux-design-specification.md:1073-1077`, `ux-design-specification.md:1438-1446`, `ux-design-specification.md:1503-1508`).
- **State/error handling is strong:** fail-closed components are concrete, states are distinct and snapshot-tested, and downgrades clear protected content (`ux-design-specification.md:1027-1053`, `ux-design-specification.md:1293-1299`, `ux-design-specification.md:1323-1332`).
- **Responsive disclosure is strong:** each viewport is an independent disclosure surface, CSS hiding is rejected as authorization, and mobile defaults to read-only triage (`ux-design-specification.md:1373-1397`, `ux-design-specification.md:1512-1531`).
- **Motion is normatively covered at outcome level:** reduced-motion behavior is required and tested (`ux-design-specification.md:744-750`, `ux-design-specification.md:1497-1504`, `ux-design-specification.md:1516-1519`); the concrete defect is confined to the non-normative mock.
- **Illustrative contrast:** computed text/fill ratios are verified 4.74:1, warning 4.78:1, blocked 5.65:1, redacted 6.52:1, and info 5.30:1 from `ux-design-directions.html:26-45` and `ux-design-directions.html:211-227`; all clear 4.5:1. Disabled text is 4.09:1 and `--line` on white is 1.40:1 (`ux-design-directions.html:31`, `ux-design-directions.html:249-258`, `ux-design-directions.html:315-318`), but these values are explicitly non-normative and inactive controls are text-contrast exempt. An adjacent safe reason must carry the normative explanation.
- **Localization scan:** case-insensitive search for `localization`, `localisation`, `i18n`, and `translation` returned no matches in the spec or map.
- **Authority note:** `.memlog.md:9-11` records that the spec is byte-bound and decision-level remediation is deferred. Findings identify what must be dispositioned before future activation; they do not recommend editing the preserved source in place.
