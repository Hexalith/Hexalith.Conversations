---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- Push only when the user's request authorizes it.

## INSTRUCTIONS

### Prepare Committed Candidate

Only when the spec explicitly requires a generated final record, stage the exact story-owned candidate paths and create a validated Conventional Commit if the user's request authorizes a commit. Resolve committed `HEAD` once into `{candidate_revision}`. Never stage unrelated paths or pass a moving `HEAD` token to a completion gate. If a required commit is not authorized, leave the work uncommitted and report its state without asking for repeat authorization.

When Story 7.2's task requires preserving its historical pair, do not create a replacement source candidate or retract the pair to make the old record pass. Follow the task's commit scope; recording a workflow repair does not complete the story.

### Current change validation

For new work under `docs/runbooks/current-change-validation.md`, run `python3 {project-root}/scripts/check-root-submodules.py --repository {project-root}` and focused tests for the change. Record failures honestly and do not mark a failing change complete. Run historical promotion or evidence-boundary verifiers only when the spec explicitly requires them.

### Final Record Generation Gate

For new routine work, record the change and test results in the spec and skip this section unless the spec explicitly requires a generated final record. The procedure below applies only to that explicit requirement.

#### Story 7.2: contract-bound v2

When the spec is Story 7.2 (`spec-7-2-derive-test-path-candidate-submodule-and-gitlink-facts.md`), select this route **before** the legacy procedure below. Read the frozen `_bmad-output/planning-artifacts/v9/story-contracts/7.2.json` and section 9 of `docs/runbooks/story-final-record-generation.md`. Work from the repository root with the pinned Python environment. The frozen `AC-7.2-11` invocation is:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py \
  --repository . \
  --contract _bmad-output/planning-artifacts/v9/story-contracts/7.2.json \
  --format bundle \
  --output-json docs/release-evidence/story-7.2-final-record-v2.json \
  --output-markdown docs/release-evidence/story-7.2-final-record-v2.md
```

Use only those five options. The generator derives its candidate; do not pass `--story`, `--candidate`, `--test-results`, `--historical`, or `--verify-record-sha256` to v2. Its successful stdout is the authoritative JSON record itself, with no legacy nested `document.result`, `markdown`, or `markdown_sha256` fields.

For this completion repair, preserve the committed pair and all existing JUnit/TRX result artifacts. Save their bytes and existing filesystem timestamps outside the working tree before verification. Do not regenerate prerequisites over archived results, retract the pair, fabricate result timestamps, change candidate-retention rules, or commit a new source candidate to make the historical record pass. Use the runbook's isolated historical reproduction procedure when checking that pair; label that evidence historical. A separate invocation against the current repository is still required, and a historical `PASS` cannot satisfy it. This repair grants no successor or regeneration authorization; any future successor needs its own explicit scope and must pass the current contract gates. Broader completion-route integration remains Story 7.3 work.

Require exit `0`, validate stdout against `_bmad/schemas/story-final-record-v2.schema.json` through pinned `uv`, require `schemaVersion == "hexalith.conversations.story-final-record.v2"` and `storyId == "7.2"`, and compare `summary` for exact equality with the frozen contract's `finalRecord.summary` (`required: 11`, `passed: 11`, `failed: 0`, `blocked: 0`, `skipped: 0`, `notRun: 0`). Require every scenario to report `PASS` with a nonempty assertion ledger. Verify that stdout equals the declared JSON output bytes. For this historical-preservation repair only, also require **both** JSON and Markdown to equal the saved committed pair bytes; that archived-byte comparison does not govern a future separately authorized successor. Invoke the identical command a second time and require exit `0`, the same schema/identity/summary checks, and byte-identical stdout and both files. The generator rechecks the pair's digest bindings; do not apply the v1 digest-verification flags.

Any nonzero exit, invalid record, or required byte-comparison drift fails this gate. Report the exact command, exit, and stable diagnostics. For this preservation repair, capture any differing output bytes for diagnosis, then restore and verify both outputs' saved original bytes and access/modification timestamps after verification, even if generation replaced them with identical bytes. Compare the bytes before restoring and checking the saved timestamps, so verification reads do not leave changed access times. Leave Story 7.2 `in-progress` and preserve sprint status when the task prohibits editing it. Whether v2 passes or fails, skip the legacy procedure and collect the independent Story 7.2 terminal-gate diagnostics below; an unsatisfied gate must HALT before Completion Scope, Mark Spec Done, and Commit and Complete.

#### Legacy final-record procedure (other explicitly required records)

Clean-rebuild the committed candidate with `dotnet build <root-solution> -c Release -t:Rebuild -p:SourceRevisionId={candidate_revision}` and rerun every root-owned test project into fresh TRX artifacts. Invoke `python3 {project-root}/_bmad/scripts/generate_story_record.py --repository {project-root} --story {spec_file} --candidate {candidate_revision} --format bundle`, with the trustworthy baseline, all declared test-result artifacts, and the exact submodule scope. Require `TEST_BUILD_NOT_BOUND` and `RECORD_NOT_DERIVED` to block the gate. Any nonzero exit or nested result other than `pass` returns the spec and sprint lifecycle to `in-progress`; Never write `done`, and HALT with the stable diagnostics.

On success, insert bundle field `markdown` VERBATIM into the story's final-record region and retain `markdown_sha256`. Run the generator again with `--verify-record-sha256 <markdown_sha256> --format json`. Any nonzero exit, result other than `pass`, or `RECORD_CONTENT_DRIFT` returns lifecycle state to `in-progress` and HALTs. Only a candidate-bound, digest-verified final record permits the terminal transition below.

### Story 7.2 Terminal Gate

For Story 7.2, all explicit spec gates remain required before any review/done transition or terminal completion action. Routine-change policy and focused regression tests do not replace them. Record the current v2 invocation, boundary verification, and authority resolution independently even when one fails, then HALT before the terminal sections if any required gate is unsatisfied.

Resolve the current committed `HEAD` once to its full `{candidate_revision}` and read `{baseline_revision}` from the spec. Run:

```bash
uv run --frozen --no-sync python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline {baseline_revision} --candidate {candidate_revision}
uv run --frozen --no-sync python3 _bmad/scripts/resolve_current_planning_authority.py --repository . --candidate {candidate_revision} --check
```

Retain exact commands, exits, result states, stable blocker codes, and assertion ledgers. Require boundary exit `0`/`PASS` and a nonempty applicable ledger. Independently require resolver exit `0`/`PASS` **and** verified separate terminal `ACCEPTED` authority for Story 7.1 binding its final-record digest and protected-main commit. A resolver `PASS` alone, Story 7.1's `done` row, its raw passing final record, a historical lift, and a routine-change waiver cannot establish that terminal authority. If no such accepted publication is available, record the missing authority as a blocker. Do not invent a `--trusted-host` value from `HEAD`, a parent, or a historical successful run; only genuine protected-event provenance may supply it.

Apply Architecture AD-4, "Story 7.1 Integration And Terminal Transition", in `{project-root}/_bmad-output/planning-artifacts/architecture.md`: a separate atomic terminal authority/pointer publication must reach `ACCEPTED` after the post-integration result and candidate-matched final record pass. Its proof binds the Story candidate, verified merge tree and parent identities, admissible integration paths, every root gitlink, the final-record digest, and the actual protected-main commit; that accepted commit's in-scope tree and gitlinks must equal the verified integration result. The repository inspected for the 2026-09-27 repair has no verified Story 7.1 terminal publication, so this prerequisite remains blocked. Do not invent an accepted artifact path or a checker to stand in for that missing publication.

If any required evidence is missing, stale, `FAIL`, or `BLOCKED`, retain Story 7.2 `in-progress`, leave the hold and planning authority unchanged, preserve the pair and result artifacts, and report the blockers. Respect the task's prohibition on sprint-status edits. Do not run Mark Spec Done, sync sprint status, or claim Story 7.2 complete. A separately authorized, validated workflow repair commit is not a Story 7.2 lifecycle transition and cannot satisfy the missing gates.

### Completion Scope

Identify the exact task-owned source, completion-record, and lifecycle-status paths. Use the authorization already given for the task; do not request a separate per-commit approval. If the task does not authorize a commit, leave the paths uncommitted and report them. Keep unrelated paths out of the commit.

### Mark Spec Done

Change `{spec_file}` status to `done` in the frontmatter.

If `{story_key}` is not empty and `{{.implementation_artifacts}}/sprint-status.yaml` exists, read `[[bmad-snapshot:sync-sprint-status.md]]` with `{target_status}` = `review`.

### Commit and Complete

When the task authorizes a commit, stage only the identified task-owned paths, create a validated Conventional Commit, and verify that every intended path is committed. Any commit failure returns the spec and sprint lifecycle to `in-progress` and HALTs.

{workflow.open_spec}

### Display Summary

Display a very short completion summary — one or two sentences — including:

- What changed.
- The verification and review result, including whether anything was deferred.
- The commit hash, if one was created.

Do not list changed files, repeat details from the spec, or narrate the process unless the user asks.

Offer applicable next actions in one short line: check CI after an authorized direct push to `main`; use `bmad-walkthrough`; or make another change.

Workflow complete.

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
