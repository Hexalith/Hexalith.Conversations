# Sprint change proposal · 24 November 2026

**Hexalith.Conversations · Execution rules for Stories 12.3–15.2 · Minor scope · Approved by the owner on 24 November 2026**

## Issue summary

The sprint-planning readiness check on 24 November found that Stories 12.3–15.2 can run only through V9 machinery that current work no longer uses. Two shared blocks in the canonical epics apply to every Story 7.1–15.2. **Non-Goals And Active Hold** keeps the hold `ACTIVE` until a validator `PASS`, an independent IR-0 `READY` and a release-owner `LIFTED` decision all exist. The **Canonical Successor-Story Contract** blocks every result with `PC_UNBOUND` until it binds `v9-authority-bundle-v1.json`. It also fixes a ten-path gitlink inventory, while the root now declares twelve gitlinks, so `GITLINK_SCOPE_MISMATCH` is certain.

The story tables inherit the same assumptions. Across Stories 13.1–15.2 and RG-15, the 58 acceptance rows name 22 scripts, and 20 of those scripts do not exist. Fourteen rows run `bin/Release/net10.0` assemblies, which the net11.0 producer no longer builds. No `v9/story-contracts` file exists after 12.3. Every story ends with a v2 final-record scenario.

Current work follows the routine path in the current change validation runbook. The owner has already started equivalent work through story-scoped decisions: the Story 7.1 entry gates and Epic 7 exit, Stories 16.1–16.3, and the waivers of AC-12.1-01/06 and AC-12.2-05. Retro items 35 and 57 ask for this reconciliation. Without it, each remaining story would stop on the same ceremony. [Runbook](../../docs/runbooks/current-change-validation.md) · [Canonical epics](prds/prd-Conversations-2026-06-02/epics.md) · [Tracker](../implementation-artifacts/sprint-status.yaml)

## Impact

| Area | Impact |
| --- | --- |
| PRD | None. Requirements, the MVP, success metrics and release gates are unchanged. The PRD readiness snapshot already sends routine work to the runbook. |
| Epics | Stories 12.3–12.4, Epics 13–15 and RG-15 gain current execution rules. Scope, order, deliverables and fault cases stand. Epic 15's entry also waits for the 20 November additions and Stories 16.4–16.5. |
| Architecture | None. The 21 November current planning entry point already applies current policy to routine work and links the sprint supplement, which the new section follows. [Entry point](architecture.md#current-planning-entry-point) |
| UX | None. |
| Tracker | A pointer comment at 12.3, item 57 in progress and item 35 closed as superseded. No story or epic status changes. |
| Epic contexts | No edit. `epic-12-context.md` is already older than the planning artifacts, and this change makes `epic-16-context.md` older too. bmad-build recompiles both from current planning when 12.3 and 16.4 start. Editing them would mark them current and freeze older acceptance into them. |
| SPEC | `SPEC.md` CAP-1 still describes the V15 authority. bmad-spec owns it, so it is a follow-up below. |

## Approach

Direct adjustment, with low effort and low risk. One dated section goes at the end of the canonical epics, as on 20 November, so every earlier byte stays in place.

Rejected alternatives:

- **Edit the V9 blocks in place.** This rewrites text that carries history and still leaves the story tables wrong.
- **Keep per-story waivers.** This works, as six owner decisions show, but nine more stories would stop on the same question.
- **Rewrite Epics 13–15 now.** This is large and speculative. Those stories start after 12.x and 16.x, and their scope will move again.

Decisions taken with the owner on 24 November:

- **Stories 12.3–12.4 are included.** Without the new rules, 12.3 would block on `PC_UNBOUND` at entry. Its contract `v9/story-contracts/12.3.json` can still supply scenarios.
- **Epic 15 entry includes Stories 8.3, 9.3–9.5, 11.4, 16.4 and 16.5.** Story 15.1 revalidates the current state. This costs little, because Epic 15 already waits for Epics 13 and 14.
- **The v2 final record runs only when a story spec requires it.** Epics 13–15 already produce machine-readable deliverables: the proof v3 chain, the v3 manifest, and the revalidation and attestation v2.

## Detailed changes

### Canonical epics

**Before:** the file ended with the 20 November sprint supplement.

**After:** a new section, **Current execution rules · 24 November 2026**, is added at the end. It contains eight rules:

1. **Entry:** a story starts when its hard-entry predecessors are `done`. The archived hold is historical.
2. **Candidate:** the committed `HEAD` with its root-declared gitlinks.
3. **Scenarios and tools:** each story spec states its scenarios. Tools are built only when a scenario's evidence needs them.
4. **Commands:** use the current target framework and configuration.
5. **Records:** the final record runs only when the spec requires it, and waivers are never recorded as PASS.
6. **Completion:** follow the runbook's routine path.
7. **Epic 15 entry:** as decided above.
8. **Unchanged gates:** RG-15, the universal SM-C2 rule and the pending preservation approval keep their requirements.

[Section](prds/prd-Conversations-2026-06-02/epics.md#current-execution-rules--24-november-2026)

### Sprint tracker

```text
OLD   # Audit evidence and additional 12.3/12.4 acceptance: …sprint-change-proposal-2026-11-20.md
      12-3-implement-correctness-preserving-list-open-optimization-and-migration-behavior: backlog
NEW   # Audit evidence and additional 12.3/12.4 acceptance: …sprint-change-proposal-2026-11-20.md
      # Execution rules for 12.3-15.2 (no archived hold; HEAD candidate; root-declared gitlinks): …sprint-change-proposal-2026-11-24.md
      12-3-implement-correctness-preserving-list-open-optimization-and-migration-behavior: backlog
```

- **Item 35:** `open` → `done`, with a note. Its purpose, stopping later specs from importing waived ceremony again, is met by the new section. Epic 7 is done, and `epic-7-context.md` has no further reader. The file itself was not edited.
- **Item 57:** `open` → `in-progress`, with a note. The Epic 12–15 part is done. Three parts remain: `epic-16-prerequisites-v1/epic.md`, AR-2 and the 16.1 release-train ledger entry.
- `last_updated` is refreshed.

## Handoff

The scope is minor. The planning edits are applied.

| Owner | Responsibility |
| --- | --- |
| Developer (bmad-build) | Apply the rules when writing the 12.3–15.2 specs. State each scenario and record each command substitution and waiver. |
| Product and Release owner | Make the RG-15 decision. Close the remaining parts of item 57. |
| SPEC owner (bmad-spec) | Point SPEC CAP-1 at the current planning entry point and these rules. |

**Success criteria:** a developer starting 12.3, 13.1, 14.1 or 15.1 needs no IR-0, `LIFTED` decision, v9 bundle binding or entry waiver. Story deliverables, fault cases and release gates are unchanged, and no earlier byte of the canonical epics changes.

## Verification

- The committed canonical epics (368,384 bytes at `HEAD`) remain a byte-for-byte prefix of the updated file. The new section adds 13 lines.
- `sprint_plan.py validate` reports a valid tracker with no problems. Story counts are unchanged: 47 done, 1 in progress and 16 in backlog.
- `git diff --check` passed, and this proposal has no trailing whitespace.
- All seven new relative links resolve, and both cited anchors exist exactly once.
- No product code, test, dependency, submodule or gitlink changed.

## Workflow record

| Checklist | Disposition |
| --- | --- |
| 1.1–1.3 · Trigger and evidence | [x] Readiness concern 1 from sprint planning, with blocks, counts and missing tools identified. |
| 2.1–2.5 · Epic impact | [x] Epics 12–15 and RG-15 keep their scope and order. The Epic 15 entry is clarified. No epic is added or removed. |
| 3.1–3.4 · Artifact impact | [x] PRD, architecture and UX are unchanged. The tracker is updated, epic contexts recompile, and SPEC is handed off. |
| 4.1–4.4 · Options | [x] Direct adjustment selected. In-place edits, per-story waivers and a full rewrite were rejected. |
| 5.1–5.5 · Proposal and handoff | [x] Recorded above. |
| 6.1–6.3 · Review and approval | [x] The owner reviewed each option's mechanics, pros and cons, then approved the revised batch plan on 24 November. |
| 6.4 · Tracker | [x] No epics or stories were added or removed. The pointer comment and the item 35 and 57 updates are applied. |
| 6.5 · Next steps | [!] Follow up on SPEC CAP-1 through bmad-spec. |
