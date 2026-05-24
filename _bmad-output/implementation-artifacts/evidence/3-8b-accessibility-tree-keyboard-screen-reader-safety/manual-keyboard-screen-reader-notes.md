# Story 3.8B — Manual Keyboard & Screen-Reader Evidence

Scope: accessibility tree, keyboard navigation, and screen-reader safety for the rendered
`Hexalith.Conversations.Admin.Web` investigation workspace. Story 3.8A (responsive) and Story
3.8C (leakage/clipboard/browser-title/telemetry disclosure) are owned elsewhere.

All content below is content-safe and synthetic. The host serves only
`BuyerAcceptanceDemoFixtures` data (tenant `demo-tenant`); the cross-tenant `poison-tenant`
record is mapped to an indistinguishable hidden read and never reaches any rendered or
accessible surface.

## How this evidence was produced

- **Automated (captured), reproducible:** the `AccessibilityEvidenceHarnessTest` lane drives a
  real headless Chromium against the running host. It captures the accessibility tree with
  `Locator.AriaSnapshotAsync()` (the same programmatic tree Narrator/NVDA render to speech),
  walks the keyboard Tab order, resolves accessible names/descriptions, and scans every
  accessible name/description/heading/live-region for the forbidden poison sentinels. Raw
  output: `aria-snapshots.json`, `focus-order-trace.json`, `accessibility-matrix.json`,
  `accessible-name-scan.json`.
- **Human audible sign-off (remaining):** an agent cannot listen to synthesized speech. The
  audible Narrator/NVDA confirmation is therefore listed below as a human checklist with the
  exact expected announcements, to be signed off by the accessibility evidence owner before
  release. The captured accessibility tree is the authoritative source those tools speak, so the
  audible pass is a confirmation of rendering, not a separate source of truth.

To run the host for a manual pass:

```
dotnet run --project src/Hexalith.Conversations.Admin.Web
# then open http://127.0.0.1:<port>/investigations?fixture=<id>
```

Fixture ids are listed in `fixture-matrix.json` (for example `TenantA_Admin_FullTrust`,
`TenantA_Reviewer_RedactedParticipants`, `TenantA_MobileTriage_ReadOnly`,
`UnauthorizedExisting_IndistinguishableFromMissing`).

## Keyboard-only Find → Read → Trust walkthrough (captured)

Observed against `TenantA_Admin_FullTrust` (and confirmed structurally across all fixtures):

1. **Tab 1 → "Skip to governed record" link** (`#governed-record`). The skip link is the first
   focusable control on every fixture and every mode (default, forced-colors, 200% zoom).
   Activating it moves focus to the `<main>` governed-record landmark (`tabindex="-1"`).
2. **Reading order (headings / landmarks)** then follows the trust hierarchy via the document
   structure — banner `h1` → region "Trust order" (`h2`) → Tenant Scope, Record Identity, Trust
   Posture, Evidence Completeness, Command Eligibility (`h3`, in that order) → "Evidence
   timeline" (`h2`) → timeline rows (`h3`). Tenant scope, trust posture, evidence completeness,
   and the command gate are all encountered before any timeline reliance.
3. **Command gates**: governance-changing and not-yet-current commands render as `disabled`
   buttons. They remain in the accessibility tree with an accessible description carrying the
   safe blocked reason (for example "Action requires current evidence and audit readiness."),
   so an assistive-technology user learns *why* an action is unavailable and what is required,
   without any signal of whether a protected conversation/participant/provider/file/event
   exists. Enabled read-only actions (for example `read-governed-record` on
   `TenantA_MobileTriage_ReadOnly`) are reachable by Tab after the skip link.
4. **Degraded / denied states**: redacted, stale, missing-citation, unresolved-participant and
   permission-downgrade fixtures expose the same trust-order structure; the safe live region and
   the safe-next-action text describe the class and the safe next step using the closed
   vocabulary (`Redacted`, `Unavailable`, `Restricted`, stale / "wait for current governed
   evidence", etc.). For the two hidden-read fixtures the timeline contains only the identical
   message "No governed record is visible for this tenant scope." and no evidence rows.

Focus visibility: a `:focus-visible` outline (3px) is applied to links, buttons, and the
programmatic skip target. Under `forced-colors: active` the outline switches to the system
`Highlight` colour, and panel/row/button borders use `CanvasText`. The 200% zoom rows
(`zoom-200`, halved CSS viewport to force a real reflow) preserve the heading outline, the trust
order, the landmarks, and the accessible blocked-reason descriptions.

## Accessibility-tree (screen-reader) reading transcript

This is the captured `AriaSnapshotAsync()` tree for `TenantA_Admin_FullTrust` — the structure a
screen reader narrates top to bottom. Full per-fixture trees are in `aria-snapshots.json`.

```
- link "Skip to governed record" (/url: #governed-record)
- banner:
  - heading "Conversations Investigation Workspace" [level=1]
  - search "Find governed conversations":
    - paragraph: Find, read, and trust governed evidence. Synthetic demo data only.
- status: "Trust posture: current trusted evidence. Evidence completeness: complete citations
  and audit references. Command eligibility: governance actions blocked from read surface."
- main "Conversations Investigation Workspace":
  - region "Trust order":
    - heading "Trust order" [level=2]
    - heading "Tenant scope: Tenant A administrator" [level=3]
    - heading "Record identity: governed conversation full trust" [level=3]
    - heading "Trust posture: current trusted evidence" [level=3]
    - heading "Evidence completeness: complete citations and audit references" [level=3]
    - heading "Command eligibility: governance actions blocked from read surface" [level=3]
    - list:
      - listitem: button "Blocked: read-governed-record" [disabled]
                  text: Command availability metadata is unavailable for this record.
      - listitem: button "Blocked: set-retention-policy" [disabled]
                  text: Action requires current evidence and audit readiness.
  - region "Evidence timeline": ... governed message rows (level=3 headings) ...
```

Key safety properties confirmed in the captured trees across all fixtures:

- Exactly one `h1`; heading outline follows trust order on every row.
- `banner`, `search`, and `main` landmarks are present; the redundant responsive summary
  surfaces are `aria-hidden` and do not appear in the tree (no duplicate trust announcements).
- No poison sentinel (`POISON-SENTINEL-alpha` / `POISON-SENTINEL-beta`) appears in any
  accessible name, description, heading, list text, or live region.
- The hidden-read tree is identical for the unauthorized-existing and the (renderer-level)
  nonexistent record — see `HiddenReadRendersIdenticallyForUnauthorizedAndNonexistentRecords`.

## Human audible confirmation checklist (Windows Narrator / NVDA)

To be performed and signed off by the accessibility evidence owner. Expected announcements:

- [ ] **Narrator** (Win+Ctrl+Enter) on `TenantA_Admin_FullTrust`: Tab announces
      "Skip to governed record, link" first. Heading navigation (H) reads the title, then
      "Trust order, heading level 2", then the five trust panels at level 3 in order, then
      "Evidence timeline, heading level 2".
- [ ] Landmark navigation (D in NVDA) reaches banner, search, and main; the responsive summary
      duplicates are NOT announced.
- [ ] On a disabled command button, the screen reader announces the button as dimmed/unavailable
      and reads the blocked reason (e.g. "Action requires current evidence and audit readiness.").
- [ ] On `TenantA_Reviewer_RedactedParticipants` / `MixedTimeline_PartialLoad_RedactedEvents`,
      redaction is announced with safe vocabulary only — no protected value is spoken.
- [ ] On `UnauthorizedExisting_IndistinguishableFromMissing` and the cross-tenant
      `TenantB_NoAccess_CrossTenantPoison`, the timeline announces only
      "No governed record is visible for this tenant scope." with no hint that a record exists.
- [ ] High contrast theme + 200% browser zoom: focus indicator stays visible, labels and trust
      order remain intact, blocked-action reasons stay readable.

Owner: UX / Test Architect accessibility evidence owner. Recommended tool: NVDA or Windows
Narrator. Review date: 2026-05-31 or before merge, whichever comes first.
