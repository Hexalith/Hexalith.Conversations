---
title: 'Refresh all package dependency graphs'
type: 'chore'
created: '2026-09-12'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'b819a7c43a7024295abaabd418a74f5f64cb5af0'
refresh_baseline_commit: '8793d26306f91d2cfe00116dc0b8eec41ec8900f'
refresh_authorized_date: '2026-10-08'
submodule_promotions:
  - path: 'references/Hexalith.Builds'
    require_remote: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
  - '{project-root}/docs/runbooks/evidence-boundary-validation.md'
  - '{project-root}/docs/runbooks/submodule-promotion-completion-gate.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Conversations' direct Node and Python pins are current, but their lock graphs contain compatible transitive updates; the repository also consumes two stale stable NuGet versions, an older compatible Dapr prerelease, and .NET SDK `10.0.400` while `10.0.401` is current. Python manifest and lock bytes are planning-authority inputs, so an ordinary lock refresh would invalidate evidence if it were not versioned additively.

**Approach:** Refresh every selected package surface to the newest compatible release, regenerate locks only with their owning tools, keep the Aspire family aligned, and publish additive authority for changed protected tooling bytes. Preserve repository ownership boundaries and verify the complete resulting graph.

**Decisions:** Keep the combined scope. Include the shared Hexalith.Builds NuGet catalog and its root gitlink promotion. Include tracked toolchains: update the .NET SDK patch and pinned CI uv package, while leaving GitHub Action major tags outside package scope.

## Boundaries & Constraints

**Always:** Preserve current release channels, including intentional prereleases; keep project `PackageReference` entries versionless and central NuGet management authoritative; update Microsoft.Extensions `10.0.x` coherently; use npm and uv to regenerate their locks; keep Aspire AppHost/Hosting/Testing on one version; retain accepted evidence byte-for-byte and add a successor authority for changed Python inputs; distinguish `PASS`, `FAIL`, `BLOCKED`, and `not-applicable`.

**Never:** Add local NuGet version overrides, downgrade the intentional CommunityToolkit Dapr prerelease, update product behavior, rewrite V15/V16 or other historical evidence, weaken validation, traverse nested submodules, leave a parent gitlink pointing at an uncommitted/unavailable commit, commit or push without explicit authorization, or claim this package refresh resolves the stale Story 7.1 candidate/hold binding.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Current direct package | Registry version equals manifest pin | Manifest remains byte-identical | Record it as current; do not churn the file |
| Compatible lock update | A transitive version satisfies the owning direct range | Lockfile deterministically selects the refreshed graph | Restore that ecosystem's lock if clean install or tests fail |
| Intentional prerelease | A newer compatible prerelease exists while stable is older | Preserve the prerelease channel; never downgrade | Block on incompatible Aspire constraints |
| Protected Python bytes | `uv.lock` changes | Additive authority/checker/tests bind exact new bytes | Existing V15/V16 evidence remains immutable |
| Unavailable SDK | `global.json` selects an uninstalled current patch | Validate with an isolated SDK install | Report exact environment blocker if installation cannot complete |
| Shared NuGet catalog | Required version is owned by Hexalith.Builds | Change it only in its owning repository and promote a valid gitlink | Block without separately authorized commit/promotion handling |

</frozen-after-approval>

## User-approved refresh amendment — 2026-10-08

The user explicitly requested: **"Refresh all selected packages to today's versions."**
This renegotiates the September version assumptions while retaining the selected npm,
Python, shared NuGet catalog, SDK, and CI uv surfaces and their compatible release
channels. The historical `baseline_commit`, C1/C2 identities, authority records, and
observed September results below remain unchanged. The current refresh starts at
`refresh_baseline_commit`; later user work outside this scope is preserved.

Use `docs/runbooks/current-change-validation.md` for current operational checks.
The retired planning-authority preflight workflow stays retired; update the two
active uv pins in `.github/workflows/ci.yml`. Publish only the additive package
environment evidence required for the changed Python bytes, without introducing
another historical authority resolver route. A working-tree preview must say
`BLOCKED`; canonical candidate and publication claims require the separately
authorized owning catalog and parent commits. On 2026-10-08, the user explicitly
authorized committing and pushing only these package changes in Builds and
Conversations to finish publication and gitlink promotion. Concurrent user work
remains outside the authorized commit scope.

### Current refresh tasks

- [x] Discover current compatible releases from npm, PyPI, official .NET metadata,
      and the owning Builds NuGet audit without changing historical evidence.
- [x] Update commitlint CLI/config to `21.2.3` and semantic-release/github to
      `12.0.10`; regenerate the compatible npm graph with npm.
- [x] Keep jsonschema `4.26.0` and pytest `9.1.1`; use uv `0.12.23` to refresh
      iniconfig to `2.3.1` and rpds-py to `2026.9.1`.
- [x] Update both active CI uv pins to `0.12.23`; keep current SDK `10.0.401`
      and its structural assertion byte-identical; align AppHost SDK `13.6.0`
      with the existing Hosting/Testing `13.6.1` family.
- [x] Update the owning Builds catalog's HotChocolate to `16.6.8` and coherent
      Verify/Verify.XunitV3 to `33.3.2`, preserving its BOM and CRLF bytes.
- [x] Add and verify a closed additive package-environment record, schema,
      checker, and focused mutation tests; retain accepted V15/V16/V18 bytes.
- [x] Run clean installs, focused tooling/catalog gates, Release restore/build,
      and every root test project individually; record precise non-success states.
- [x] Commit the owning catalog, regenerate its committed audit, and publish its
      clean remote-resolvable commit after explicit authorization. Promote the
      mode-`160000` gitlink in the parent source candidate and publish its record.

Discovery found the Microsoft `10.0.12`, Test SDK `18.10.1`, Aspire `13.6.1`, and
Toolkit Dapr `13.6.0-preview.1.261001-0243` selections already current. OpenApi
`2.12.2` is the newest compatible 2.x release. Six Parties packages advertise
`1.2.1`, but the legacy Server/UI indexes return 404; preserve the existing coherent
family selection rather than invent a verified upgrade for those unavailable IDs.
The full 304-package, 146-family registry discovery is diagnostic, not a committed
audit of the uncommitted catalog.

### Current refresh verification — 2026-10-08

- `npm ci --ignore-scripts`, `npm ls --all`, and `npm outdated --json` pass;
  outdated reports `{}`. `npm test` passes all 52 tests. A compatible
  `npm audit fix --package-lock-only --ignore-scripts` leaves 13 vulnerabilities
  (2 moderate, 11 high); suggested major downgrades were not applied.
- With isolated uv `0.12.23`, `uv lock --check --no-cache`,
  `uv sync --frozen --no-cache`, and `uv pip check` pass. The current CI Python
  lane passes 933 tests without skips. The final package-environment mutation
  suite passes 36 tests after parent diff review; the unchanged V18 Python suite
  separately passes 14.
- The new package-environment preview and `--check` both return `BLOCKED`
  (exit 1): 12 exact source bindings and a nonempty seven-row ledger, including
  `REFRESH_SOURCE_COMMIT_REQUIRED` and `REFRESH_BUILDS_COMMIT_REQUIRED`.
  Accepted V15, V16, and V18 artifacts retain their exact original SHA-256 bytes.
  The preview is not canonical publication evidence.
- Owning Builds gates pass: central catalog 304 packages; authoritative catalog
  50 approved identities and 3 shared versions; central fixtures 17 scenarios;
  Dapr catalog 8 aligned packages and Dapr fixtures 29 scenarios; audit-generator
  fixtures 115 scenarios; audit-validator fixtures 103 scenarios. The checked-in
  package audit fails precisely on the
  changed catalog hash and the three changed selections. Regeneration requires
  a separately authorized owning catalog commit; the existing audit stays intact.
- `dotnet restore Hexalith.Conversations.slnx -p:Configuration=Release -m:1`
  succeeds in the working tree. Its subsequent Release build encounters four
  CS0246 errors in concurrent conversation-deletion source work. Those files
  are preserved. A local isolated checkout of `refresh_baseline_commit`, with
  only this refresh applied and root-declared submodules cloned locally, restores
  and builds successfully with zero warnings and zero errors.
- In that isolated checkout, individual Release projects pass without skips:
  Contracts 618, Client 39, core 185, Server 721, Admin Web 14. The full Integration
  run exceeds 180 seconds and its own process group is canceled; the focused
  run excluding `SmC2HotPathBenchmark` passes all 25 remaining tests. AppHost
  topology passes 8 tests; explicitly enabling `HEXALITH_RUN_APPHOST_BOUNDARY_TESTS`
  separately passes the live boundary test. Current-policy conformance passes
  326 portable and 92 internal tests with the tracked CI exclusions, zero skips.
  The benchmark's finite workload is four paths times 2,000 operations times
  five warmup plus 30 measured repetitions, totaling 280,000 operations. Its
  isolated class-only retry passes the single test without skips in 239.779
  seconds (240.09 elapsed), under a 900-second ceiling. All isolated projects
  therefore execute 2,030 successful tests. The current-tree Story 9.2 verifier
  also passes against the portable/internal result files.
- Exact commands, statuses, and logs are retained under `/tmp/package-refresh-*`;
  the isolated checkout is `/tmp/package-refresh-validation-5y20qz24/repository`.
  The audit-validator fixture suite exceeded 90-, 300-, and 900-second bounds;
  the last run reached scenario 101. A focused repository-history/parameter run
  passes four selected scenarios. The final unchanged full fixture run, with only
  progress logging added outside the repository, passes all 103 scenarios under
  a 1,200-second ceiling. Its log is
  `/tmp/package-refresh-test-package-version-audit-validator-final.log` and its
  result is `/tmp/package-refresh-audit-validator-final-result.json`.
  The earlier timeouts are superseded incomplete runs, not environmental blockers.
  Independent parent preview/check runs remain `BLOCKED` with the same two
  commit-required codes after review fixes and exact-byte regeneration.

### Current refresh publication — 2026-10-08

The authorized Builds catalog commit is `eaa53b009f1547d1af97a8aa6a8ad6b214a3276d`.
Its incremental audit commit is `a283481c69393dcba911db6a0edcb152167238cc`, now published
on `origin/main`. The audit validates 304 packages, 146 families, and one source;
only HotChocolate and Verify are refreshed, with the other 144 family and package
observations preserved exactly. The audit retains the earlier Verify sponsorship
configuration failure as historical context and grants no downstream compatibility
acceptance. Conversations' isolated package-only Release build and tests pass.
The parent C1/C2 publication and final independent review are in progress.

The original sections below retain the September implementation and evidence.

## Code Map

- `package.json`, `package-lock.json` -- exact commitlint pins are current; npm reports compatible transitive lock changes only.
- `pyproject.toml`, `uv.lock` -- jsonschema `4.26.0` and pytest `9.1.1` are current; uv proposes Pygments `2.20.0` to `2.21.0`, and both files are authority-governed.
- `global.json` and `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs:48` -- SDK pin and its exact structural assertion move together if toolchains are included.
- `.github/workflows/planning-authority-preflight.yml` -- update uv `0.11.16` to `0.12.13`; it also consumes the repository SDK, npm lock, and frozen Python graph.
- `Directory.Packages.props` -- imports central versions without local overrides.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- separately owned catalog; available updates include Microsoft.Extensions.Http `10.0.12`, Microsoft.NET.Test.Sdk `18.10.0`, and CommunityToolkit Dapr `13.5.1-beta.751`.
- `_bmad/scripts/publish_v15_planning_tooling_environment.py`, `_bmad/scripts/publish_v16_planning_tooling_lifecycle.py`, their schemas/tests, and consuming C# validators -- preserve historical identities; reuse their closed, candidate-bound pattern for an additive successor when Python bytes change.

## Tasks & Acceptance

**Execution:**
- [x] `package-lock.json` -- refresh the compatible npm graph without changing already-current direct pins.
- [x] `uv.lock` -- refresh all compatible Python transitive packages without changing already-current direct pins.
- [x] `global.json`, `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`, `.github/workflows/planning-authority-preflight.yml` -- update SDK `10.0.400` to `10.0.401`, its exact assertion, and uv `0.11.16` to `0.12.13`.
- [x] `_bmad/schemas/v18-package-environment-authority-v1.schema.json`, `_bmad/scripts/publish_v18_package_environment_authority.py`, `_bmad/scripts/tests/test_publish_v18_package_environment_authority.py`, `_bmad-output/planning-artifacts/v18-package-environment-authority-v1.json`, `tests/Hexalith.Conversations.Conformance.Tests/PackageEnvironmentAuthorityV18ValidationTest.cs`, and `.github/workflows/planning-authority-preflight.yml` -- additively bind the refreshed Python/toolchain bytes and their non-vacuous consumers without rewriting V15/V16.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- update the coherent `10.0.11` Microsoft.AspNetCore, Microsoft.Extensions, and System families to `10.0.12`, Microsoft.NET.Test.Sdk to `18.10.0`, and CommunityToolkit Dapr to `13.5.1-beta.751`; run the owning repository's catalog gates.
- [x] `references/Hexalith.Builds` -- record and promote the exact clean, remote-resolvable Builds commit as a mode-`160000` gitlink; halt before commit or push unless the separately required authority is explicit.

**Acceptance Criteria:**
- Given every selected manifest and registry, when the refresh completes, then direct and transitive dependencies resolve to the newest compatible versions within their approved release channels.
- Given generated locks, when clean npm and uv installs run, then manifests and locks agree and their focused suites pass without skips.
- Given the .NET graph, when restore, Release build, and every root test project run individually, then all pass and no inline/local NuGet override exists.
- Given changed protected tooling bytes, when authority and evidence verifiers run, then a nonempty assertion ledger reports `PASS` or a precise non-success state without altering accepted evidence.
- Given the final boundary, when it is compared exactly with approved scope and the submodule-promotion gate, then the Builds commit is clean and remotely resolvable, its root entry is mode `160000`, and no product, historical-evidence, unrelated submodule, or undeclared path changed.

## Implementation Notes

- npm retained the direct commitlint pins and refreshed seven transitive entries. uv retained both direct Python pins and refreshed Pygments from `2.20.0` to `2.21.0`.
- The V18 implementation is staged as an exact two-commit C1/C2 authority. The C2 JSON cannot be truthfully generated before C1 is committed because it binds the canonical candidate commit and committed mode-`160000` Builds object.
- The Builds catalog and audit are bound by authorized local commits ending at `cf52f74c983bf88496cf0280cd9788b5ebcf50de`. A push is still required before the umbrella gitlink can satisfy remote resolvability, and this workflow does not authorize one.
- C1 `5aef27014170e1ed971aed4c49eeb723d3912c4c` records the exact eleven-path package change and C2 `d8e72d541838f7c9bfff8f60926389e8ae8d2d49` adds only the V18 authority. The descendant C# consumer strips the owning catalog's UTF-8 BOM before XML parsing.

## Spec Change Log

## Review Triage Log

- 2026-10-08 parent diff inspection: bind committed-record schema validation to
  the canonical source candidate; reject symlinks in every path component before
  Git reads or record writes; reject non-object records with a stable diagnostic.
  All 36 focused mutation tests pass. Package-only isolated build/test evidence
  remains applicable because these fixes affect only the new authority checker.
  The shared catalog/audit commits, remote publication, gitlink promotion, and
  parent C1/C2 publication remain blocked on explicit authorization. No files
  were staged, committed, or pushed; concurrent user work remains preserved.

## Design Notes

Current direct npm/Python versions and Aspire `13.5.3` are already current. Package-manager dry runs identify lock-only changes; NuGet updates cannot be represented correctly in Conversations without an owning Hexalith.Builds change and valid gitlink promotion.

## Verification

**Observed:**
- `npm ci --ignore-scripts && npm ls --all && npm outdated --json` -- PASS; 76 packages audited, no vulnerabilities, valid graph, `{}` outdated result.
- `uv run --frozen --no-cache pytest -q _bmad/scripts/tests/test_publish_v18_package_environment_authority.py` -- PASS; 14 passed.
- `dotnet build tests/Hexalith.Conversations.Conformance.Tests/Hexalith.Conversations.Conformance.Tests.csproj --configuration Release -m:1` -- PASS with 2 `MSB3277` warning groups for the pre-existing `Microsoft.IdentityModel.Tokens` 8.19.2/8.22.0 project-reference conflict.
- Builds central catalog, authoritative catalog, central fixture, Dapr catalog, and Dapr fixture gates -- PASS; 286 entries, 50 identities, 17 central scenarios, 8 aligned Dapr packages, and 29 Dapr scenarios.
- Builds package-audit generation and validation -- PASS after selecting CommunityToolkit Dapr `13.5.1-beta.751`; 286 packages across 141 families and one source, with generator and validator fixture suites passing.
- `uv lock --check --no-cache && uv sync --frozen --no-cache && uv pip check && uv pip list --outdated` -- PASS; all 11 installed dependencies are compatible and no outdated package is reported.
- Current Python preflight split -- PASS; 351 current tests and 23 unaffected V9 tests passed, with the eight historically governed V9 cases deselected by the tracked lane.
- Historical Python preflight split -- PASS in a detached local clone; 281 V9-era tests, nine V15 publication tests, and the V9 authority checker passed against their committed locks.
- V13/V14/V15/V16/V18 decision and authority chain -- PASS; V18 binds 13 Python packages and 12 combined paths, and its 14 Python mutation tests pass.
- Serialized solution restore -- PASS. Release solution build -- PASS with four `MSB3277` warning groups; warning-as-error -- FAIL because Conversations resolves `Microsoft.IdentityModel.Tokens` `8.19.2` while unchanged EventStore project references expose `8.22.0`.
- Compiled xUnit suites -- PASS for Admin Web 14, Client 29, Contracts 618, Server 684, core 185, and 13 non-benchmark Integration tests. V18/V15/V16 focused conformance classes pass 7/6/6 tests. Full Conformance has 15 unrelated installed-skill/traceability failures; AppHost has one environment failure after Dapr reports `no space left on device`; the SM-C2 benchmark exceeded the bounded local run and was canceled.
- Evidence-boundary verifier -- PASS for the exact 12-path combined boundary and single Builds gitlink. Submodule-promotion gate -- BLOCKED with `REMOTE_COMMIT_UNAVAILABLE`; the clean mode-`160000` commit is contained by no locally known remote-tracking ref.

**Commands:**
- `npm ci --ignore-scripts && npm ls --all && npm outdated --json` -- expected: clean install, valid graph, and `{}`.
- `uv lock --check --no-cache && uv sync --frozen --no-cache && uv run --frozen pytest -q _bmad/scripts/tests` -- expected: locked install and skip-free planning suite pass.
- `dotnet restore Hexalith.Conversations.slnx && dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore -m:1` -- expected: warning-free restore/build.
- Run each `tests/**/*.csproj` individually in Release -- expected: every configured test executes and passes, with documented focused fallback for environmental blockers.
- `python3 _bmad/scripts/verify_evidence_boundary.py --repository . --baseline <baseline> --candidate <candidate>` -- expected: applicable non-vacuous `PASS`, or explicit `not-applicable` only when valid.
