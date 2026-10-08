# Historical V21 review disposition

This 2026-10-08 disposition explains the halted loop-11 review in the
[V21 publication spec](../../_bmad-output/implementation-artifacts/spec-publish-v20-story-7-1-release-owner-authority.md)
under the [current-change policy](current-change-validation.md). It reconciles
the documentation for current `main`; it does not repair or approve the archived
implementation. The original spec's `in-review` status and findings remain the
historical review outcome, not a current work queue.

## Governing changes

Architecture V16's [AR-15 amendment](../../_bmad-output/planning-artifacts/architecture.md)
replaced the V21 external-ruleset mechanism for recovery. The
[V22 recovery spec](../../_bmad-output/implementation-artifacts/spec-v22-current-authority-recovery.md)
explicitly freezes the historical V21 record, schema, publisher and direct tests.
The later current-change policy retired authority enforcement for routine work;
commit `0db6207a4b1466371bedde7d09caa6c686f44ff1` deleted
`.github/workflows/planning-authority-preflight.yml`.

The current [CI workflow](../../.github/workflows/ci.yml) runs root-submodule
checks, the Story 7.3 completion-gate surface verifier and the current tooling
lane. Its [collection policy](../../_bmad/scripts/tests/conftest.py) excludes the
V19–V21 suite from directory runs while allowing explicit historical reproduction.
No current workflow invokes the V21 publisher. Its recorded historical result
does not determine whether a new routine change can proceed.

## Loop-11 findings

Every finding below retains its original verdict and evidence in the publication
spec's Review Triage Log. “Retired” means the affected route no longer governs
current work; it does not mean the defect was fixed or the earlier review passed.

| Findings | Historical issue | Disposition for current work |
| --- | --- | --- |
| `R11-BLIND-01`, `R11-BLIND-02`, `R11-BLIND-03`, `R11-BLIND-04`, `R11-BLIND-12`, `R11-EDGE-03`, `R11-EDGE-04`, `R11-EDGE-08`, `R11-VERIFY-01`, `R11-VERIFY-OTHER-01` | Ruleset detail acquisition, credential authorization, pagination, default-branch binding, result provenance and missing negative fixtures. | Unresolved historical findings in the retired external mechanism. Current work does not acquire ruleset credentials, activate that workflow or amend its frozen tests. |
| `R11-BLIND-06`, `R11-BLIND-07`, `R11-BLIND-15` | Integration topology, synthetic merge evaluation and exit from V21's temporary descendant restriction. | Historical intent gaps. Later recovery and owner decisions govern subsequent work; this disposition grants no integration or terminal acceptance authority. |
| `R11-BLIND-11`, `R11-EDGE-01`, `R11-EDGE-02`, `R11-EDGE-07` | Root acquisition races and loss of root identity between validation and publication. | Unresolved defects in the archived publisher. Its source remains frozen; it is not adopted as a publisher for new work. |
| `R11-EDGE-05` | Delete/re-add inventory history can bypass publication uniqueness checks. | Unresolved archived lifecycle defect; no current routine gate relies on that effective-hold result. |
| `R11-BLIND-10`, `R11-EDGE-06` | Bootstrap ledgers report skipped assertions as PASS. | Unresolved archived reporting defect. Historical output must be read with the original review's qualification. |
| `R11-BLIND-05`, `R11-BLIND-08`, `R11-BLIND-09`, `R11-BLIND-13`, `R11-BLIND-14` | Previously rejected claims about organization origin, implementation-path scope, the immutable V19 schema and spec lifecycle metadata. | Preserve the existing rejection reasons. No new code-fix or acceptance claim is made. |

## Recorded checks

The following read-only checks were observed on 2026-10-08. Both used the
unchanged repository publisher; they were not a rerun of the entire historical
test suite or proof of external CI activation.

```bash
uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py \
  --repository . v21 --candidate d297f96dc39b04008f391b404021899431585b6d --check
```

At the original V21 publication, exit `0` reported
`V21_STORY_7_1_AUTHORITY_CORRECTION_OK`, tooling
`239758d396d28372687b73f5dc128405892cb520` and `EFFECTIVE_HOLD=LIFTED`.
That mechanical historical result coexists with the unresolved review findings.

```bash
uv run --frozen --no-cache python3 _bmad/scripts/publish_story_7_1_successor_authorities.py \
  --repository . v21 --candidate 5e4abc6f87692e634319c1d5612c91647ae6eae7 --check
```

At the current-main investigation baseline, exit `1` reported `FAIL`,
`V21_DESCENDANT_GITLINK_DRIFT` and `effectiveHold=ACTIVE`. Later root-gitlink
history violates V21's frozen descendant boundary. The current-change policy
does not reinterpret that historical failure as PASS or require a new authority
successor to accommodate routine changes.

The current root-submodule command,
`python3 scripts/check-root-submodules.py --repository .`, exited `0` with
`root submodules: PASS`.

## Current use

Use the current-change policy for new routine work. Consult the original spec and
its bound Git revisions when reproducing V21; preserve signed predecessors,
authority records, schemas, publisher and tests. The dated pointer appended to
the original spec leaves all its earlier content intact, and V21 continues to
bind the spec blob at its historical tooling commit.

Story 7.1's existing `done` sprint row is unchanged. That row and historical V21
PASS do not establish terminal AD-4 acceptance. The separate
[AD-4 preparation runbook](story-7.1-ad4-acceptance.md) describes an inspector that
cannot authenticate or publish terminal acceptance. This documentation change
does not alter that contract or authorize release, push, a new authority, or a
Story 7.2 transition.
