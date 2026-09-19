# V16 Pragmatic AR-15 Good-Spine Rubric Re-review

- **Target:** corrected Architecture V16 AR-15 append, matching Epic 7 append,
  and current Epic 7 context
- **Architecture append SHA-256:**
  `7d438ccb69391805973ff827f02457a4441b1e540f1fff0249e4eee0bb26b2ea`
  over the 7,022 marker-delimited bytes, excluding the delimiter's terminal LF
- **Epic correction SHA-256:**
  `a05b382ba9d7bcec09d8b2e67e410c75979f41f544920396b84608550dc4179b`
  over the 2,997 marker-delimited bytes, excluding the delimiter's terminal LF
- **Epic context SHA-256:**
  `d3a75979662d5f1c0638fa09c4e94200d62c7822618af478b52a15a6633864ca`
- **Scope:** corrected V16 pragmatic AR-15 decision only; inherited
  architecture is checked only where V16 retains or supersedes it
- **Date:** 2026-09-19
- **Mutation policy:** review only; no source artifact was changed

## Final Verdict

**PASS — 0 critical, 0 high.** Both prior high findings are resolved. The
correction now removes the unwanted GitHub-ruleset route without leaving an
obsolete owner dependency or an ambiguous integration path. Its human trust
root, exact one-commit boundary, machine validation, authenticated approval,
stale-tip rule, and fast-forward-only merge form a small but enforceable
recovery contract. The implementation hold remains `ACTIVE` throughout.

## Prior Finding Resolution

### H1 — V15 joint ownership remained plausibly binding: RESOLVED

Architecture V16 now expressly supersedes AR-15's V15 joint ownership,
`BLOCKED_BOOTSTRAP_AUTHORITY` status, deferred action, and ruleset/external
mechanism. It identifies the repository owner as the sole AR-15 approval role
and makes the organization-ruleset administrator historical for this recovery
(`architecture.md:3212-3218`).

The Epic 7 append replaces entry-gate item 2 and the prior current-status
sentence, explicitly retires their joint ownership and external mechanism, and
names the repository owner as the sole approval role (`epics.md:456-461`). The
Epic 7 context carries the same supersession and owner (`epic-7-context.md:35`).
No old ruleset-administrator dependency remains live.

### H2 — Approval and merge identity were ambiguous: RESOLVED

The amended rule now fixes one executable sequence:

1. V22 is exactly one commit whose sole parent is the selected current `main`.
2. The repository owner independently checks raw Git graph, exact paths/modes,
   unchanged V1-V16 prefix, and root-gitlink tuples.
3. The exact candidate's committed resolver must pass locally and in the
   ordinary-CI `planning-authority` job for that candidate SHA.
4. The resolver reports technical facts only; candidate bytes cannot assert
   owner approval.
5. The owner records authenticated repository-host approval of the emitted
   parent/candidate tuple; a changed SHA or `main` tip makes it stale.
6. Integration is fast-forward only from the approved parent to the exact
   approved candidate. Merge commits, squash, and rebase are excluded.
7. The resolver reruns from the same SHA now committed on `main`; only its
   `PASS` completes AR-15.

This resolves both previously open questions: owner approval is deliberately a
human merge precondition rather than a candidate assertion, and candidate,
approved, merged, and post-merge identities remain identical by construction
(`architecture.md:3239-3248`, `3263-3277`, `3279-3300`; `epics.md:463-484`).

## Good-Spine Checklist

| Check | Result | Evidence |
| --- | --- | --- |
| Real divergence points fixed | **PASS** | Ownership, trust root, exact candidate shape, validation order, approval staleness, integration method, post-merge evaluation, and next authority step are singular and explicit. |
| AD decision shape | **PASS** | AR-15 remains an adopted AD-3 amendment with nonempty **Binds**, **Prevents**, and **Rule** fields. No inherited AD is renumbered. |
| AD-3 current-state semantics | **PASS** | V15's last-complete-marker selection, historical-sidecar qualification, and successor fields remain inherited. V16 adds a closed result contract with `PASS`/`FAIL`/`BLOCKED`, `effectiveHold=ACTIVE`, observed commits/trees, raw-Git manifests, and a nonempty all-`PASS` assertion ledger. |
| Rule enforceability | **PASS** | Exact eight-path/mode scope, direct-child graph, closed schema, direct tests, exit meanings, raw root-gitlink equality, authenticated approval, stale-tip rule, and fast-forward-only integration are checkable without a GitHub ruleset or external service. |
| Minimality | **PASS** | The correction relies on existing repository-owner authority, normal repository-host approval, local validation, and ordinary CI. It introduces no external validator, nonce, ruleset, new approval service, or product technology. |
| Deferred work safe | **PASS** | Missing V22 records, schemas, resolver, and tests keep AR-15 at dated publication state `PENDING_V22_RECOVERY`; live state must be recomputed. A failed post-merge resolver keeps recovery incomplete and prohibits an AD-4 request. |
| Historical integrity | **PASS** | V1-V15 architecture bytes, V1-V21 authority evidence, and the old V21 `ci-trust` publisher/tests remain immutable historical verification. V22 removes their invocation rather than rewriting them. |
| Scope and hold | **PASS** | Architecture, epics, and context preserve `implementationHold=ACTIVE` and authorize no Story 7.1 implementation, product/dependency/submodule/gitlink change, sprint transition, release, or agent push. |
| Next step | **PASS** | A post-merge V22 `PASS` ends only the authority deadlock. The next request is a separate owner-approved AD-4 `EXECUTION_ALLOWED` authority, not Story 7.1 implementation. |
| Source reconciliation | **PASS** | Architecture, Epic 7, and compiled context select V16 and describe the same owner, one-commit/direct-parent boundary, local+CI validation, authenticated approval, stale-tip behavior, fast-forward-only integration, post-merge rerun, hold, and next step. |

## Historical And Mechanical Evidence

- The current architecture file's pre-V16 prefix is byte-identical to committed
  `HEAD`.
- The marker-delimited V15 block remains 35,772 bytes excluding its terminal LF
  and hashes to
  `85c7418ca55b1c67be90eed78e280c792ce92360e0cf2817237e41a3bcdfccbe`,
  exactly matching the V16 marker.
- The selected V21 sidecar hashes to
  `296b0307bdaea35dbe62972000693de4f244b4af36bdc440bbda2e74e3963636`,
  matching V16 and the Epic 7 context.
- The V22 records, schemas, generic resolver, and direct test remain absent, so
  V16 correctly makes no present-tense recovery `PASS` claim.

## Final Resolution

No critical or high good-spine finding remains in the corrected V16 change.
The decision is sufficiently enforceable for its planning-only recovery scope
without reintroducing the rejected GitHub ruleset or adding another governance
layer.
