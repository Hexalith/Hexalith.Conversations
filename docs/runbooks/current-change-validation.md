# Current Change Validation

This policy takes effect when the change that adds this file is pushed to `main`. It governs
future changes to the Conversations repository, including ordinary root submodule
updates. It does not change the meaning or bytes of earlier V23–V29 records.

## Routine path

1. Make a focused change on `main`. Run the relevant local checks before committing,
   including `python3 scripts/check-root-submodules.py --repository .` for root
   submodule changes. That checker reads both `.gitmodules` and gitlinks from the
   index. Stage the intended declarations and exact root gitlink paths before
   running it; unstaged declaration edits do not affect its result.
   The checker verifies the staged index structure, so it can run without
   initializing or fetching submodules. Before recording a changed gitlink,
   push the dependency commit from its owning repository and verify the exact
   target is reachable from a branch or tag advertised by its declared remote.
   A matching local checkout or cached remote-tracking ref alone does not
   establish publication.
2. Run `python3 references/Hexalith.Builds/check.py` from the module root
   on the committed change before pushing to `main`. GitHub Actions delegates
   the module workflow to the released Hexalith.Builds CI. The Conversations
   after-unit-tests action checks the package contract and minimal-module
   compatibility, then validates the built and installed packages.
3. For a gitlink update, review the before and after commit IDs in the staged
   Git diff and explain the affected paths and reason in the commit message.
4. Check CI after the push. Fix a failed product or repository check before the
   next change. Keep linear history and disallow force pushes and deletion.

CI reports the state of a direct push after it lands. GitHub branch protection
does not require a pull request or a status check for this single-contributor
workflow.

Repository-local BMad build and review workflows use focused checks during
implementation and review. Before marking a product story done, build the root
solution and run the maintained test projects individually in the producer-selected
configuration, including the integration projects. Record the build and test
results. A failing or blocked full run leaves the product story open. Generated
final records and historical evidence checks run when a spec explicitly requires
them. The tracked pre-commit
hook checks staged whitespace. The Builds guard validates commit messages
against the shared Conventional Commit policy.

## Mapped build and tests

From a fresh checkout, initialize the submodules declared at the repository
root. Install the SDK selected by `global.json` and Python 3.11 or later. For
the tooling suite, also install Node.js as required by `package.json` and `uv`.

```bash
git submodule update --init
dotnet tool restore
dotnet tool run hexalith-module -- --version
```

The published workspace tool is Hexalith.Builds Module CLI 6.9.1. Start a
fresh Bash session from the repository root. Prepare a source mapping and pass
its complete environment to each command. Source mode selects Debug.

```bash
scratch=$(mktemp -d)
trap 'rm -rf -- "$scratch"' EXIT
export TMPDIR="$scratch" TMP="$scratch" TEMP="$scratch"

mapped() {
    local result="$1"
    shift
    python3 - "$result" "$@" <<'PY'
import json
import os
import sys

with open(sys.argv[1], encoding="utf-8") as result:
    os.environ.update(json.load(result)["buildPreparation"]["environment"])
os.execvp(sys.argv[2], sys.argv[2:])
PY
}

source_result="$TMPDIR/source.json"
dotnet tool run hexalith-module -- prepare-build \
    --manifest module/hexalith.module-manifest.v2.json --mode source \
    --workspace "$TMPDIR/source-mapping" --output json > "$source_result"
contracts=tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj
mapped "$source_result" dotnet restore "$contracts" -m:1
mapped "$source_result" dotnet build "$contracts" --no-restore -m:1
mapped "$source_result" dotnet "$(dirname "$contracts")/bin/Debug/net11.0/Hexalith.Conversations.Contracts.Tests.dll" \
    -class Hexalith.Conversations.Contracts.Tests.PublicQueryCompatibilityTests
```

Prepare a separate package mapping in the same shell. Package mode selects
Release. The maintained tooling suite uses real mapped Release assets for its
portable surface checks.

```bash
package_result="$TMPDIR/package.json"
dotnet tool run hexalith-module -- prepare-build \
    --manifest module/hexalith.module-manifest.v2.json --mode package \
    --workspace "$TMPDIR/package-mapping" --output json > "$package_result"
mapped "$package_result" dotnet restore Hexalith.Conversations.slnx -m:1
mapped "$package_result" dotnet build Hexalith.Conversations.slnx --no-restore -m:1
mapped "$package_result" npm test
```

`npm test` runs `uv run --frozen pytest`, using the `tests/tooling` discovery
configured in `pyproject.toml`. Run each native test project under the same
mapping with `dotnet test --project <project.csproj> --no-build --no-restore`.
The integration lanes use Docker and Dapr. After the Release build, install
Chromium for the Admin Web tests with
`mapped "$package_result" pwsh -File tests/install-playwright.ps1 -Configuration Release -SkipBuild`.

Run the live AppHost boundary case with the package mapping after the Release
build. It starts the AppHost and exercises the Dapr-backed admission boundary.

```bash
apphost_tests=tests/Hexalith.Conversations.AppHost.Tests/bin/Release/net11.0/Hexalith.Conversations.AppHost.Tests.dll
HEXALITH_RUN_APPHOST_BOUNDARY_TESTS=true mapped "$package_result" dotnet "$apphost_tests" \
    -method Hexalith.Conversations.AppHost.Tests.ConversationsAppHostRuntimeBoundaryTest.RetainedAppHostShouldRefuseCommandsWithoutCatalogAuthority \
    -noLogo -failSkips
```

## Release package qualification

Hexalith.CiCd took ownership of the release workflow in
`18fc9c90403b457ef81fedbd3c069577f8041e09`, which also removed `.releaserc.json`.
Commit `ebf0f5f0ac9e1a61b7fed199ab66065c3596c17a` removed
`scripts/verify-semantic-release-plan.mjs`. The `verify:release-plan` npm
alias was retired after those changes.

After the mapped Release build, run
`mapped "$package_result" python3 scripts/validate-built-packages.py`. The job
validates the five candidate packages and their portable symbol archives. The
public consumer restores Conversations, Contracts, Client, and
EventStoreExtensions at the exact candidate version. The testing consumer
restores Testing at that version. Each uses its own empty package and HTTP
caches, maps the five exact IDs to the candidate feed, and gets dependencies
from nuget.org with NuGet auditing enabled.

Qualify the minimal-module fixture locally with its published-feed procedure.
It restores the four successor locks, builds the fixture and AppHost in Release,
and runs seven native assertions. Builds CI supplies the managed authority for
the isolated adapter. [Minimal-module qualification](../../tests/compatibility/MinimalModule/README.md)

## Published symbol check

After NuGet.org indexes a release, pass the directory containing that release's
published symbol archives and its package version to
`python3 scripts/verify-published-symbols.py <package-directory> <version>`.
The command checks every portable PDB through NuGet.org's symbol endpoint and
compares the returned bytes with the release archive. Releases before 1.5.0
contain four packages. Later releases also include EventStoreExtensions.
A missing or mismatched symbol produces a nonzero exit. If indexing is still in progress, repeat the
check after NuGet.org reports the symbols available.

## Historical boundary

The V19–V21 successor-authority tooling and the V23–V29 publishers, resolver,
evidence verifier, schemas, records, and their tests remain in Git for historical
inspection. They are not required for future pushes, CI, or routine lifecycle
transitions. Do not create another authority successor solely because a new
commit touches a path they governed.
The retired Story 5.4 installed-delivery packet test used evidence held
on the capture machine. The [delivery spec](../../_bmad-output/implementation-artifacts/spec-5-4-ext-conv-ai-1-installed-delivery.md)
records the delivery attempt and its evidence status. Run the relevant BMad tests locally for
a tooling or workflow change, for example
`uv run --frozen pytest _bmad/scripts/tests/test_epic16_tooling.py -k scope_only_dispatch`.
`_bmad/scripts/tests/conftest.py` excludes retired authority and evidence suites
from directory collection. Name a historical test file explicitly when
reproducing its result with the toolchain and dependencies that it requires.
The V29 result for `0bb017e641eb7bd03864466526c4918f9c55ef70` remains a
historical `FAIL`; this policy makes no retroactive `PASS` or authorization claim.
The recorded V29 hold remains `ACTIVE`, with all four authority flags false.

The halted V21 publication spec retains its historical `in-review` metadata.
For current-main work, use the [V21 review disposition](v21-review-disposition.md)
rather than restarting its retired workflow or repairing its frozen tooling.

The 2026-08-22 IR-0 continuation spec retains its historical `in-progress`
metadata. Commit `6400c09d0ab8352d2ed9dd0221ffe6f4f96b91c4` recorded its
completed tasks and the independent IR-0 report. For current-main work, use
its [2026-10-08 disposition](../../_bmad-output/implementation-artifacts/spec-e6-current-candidate-ir0-continuation.md#current-policy-disposition-2026-10-08).
Leave its frozen approval block, V1–V14 sidecars, and IR-0 report unchanged.
Do not resume it into another IR-0, restore its retired publication or
evidence-boundary gates, lift the implementation hold, or start a successor.

The old [evidence-boundary runbook](evidence-boundary-validation.md) is retained
for reproducing historical results. It is no longer the operational route for
new work. Release decisions and product safety checks continue to use their own
specific requirements.

The Epic 16 tooling-v3 source binding is historical. Its held-runtime regression
uses a disposable repository with its own matching source inputs. Reproduce the
published binding using the source versions it records. The verifier reports
`SOURCE_DRIFT` when current source differs from those pins.

The [Epic 16 prerequisite spec](../../_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md)
links the retained transcripts under
[`module/validation/epic-16-prerequisite-implementation/`](../../module/validation/epic-16-prerequisite-implementation/).
Absolute paths inside a transcript identify locations on the capture machine.
For historical reproduction, use the source revision or source inventory linked
from the prerequisite spec for the run in question. Read the invocation in
[`commands.json`](../../module/validation/epic-16-prerequisite-implementation/commands.json)
and use [`artifact-identities.json`](../../module/validation/epic-16-prerequisite-implementation/artifact-identities.json)
to identify the recorded tool package by digest. A blocked receipt with a null
candidate supplies no source revision. If the run's source or another required
input cannot be recovered, record it as unavailable.

### October 8 package-environment record

Replay the recorded profile in a disposable checkout at publication commit
`f6f8c4fea84c728568bfae4c4a5a58262a01c49d`. It contains the checker and record
for source candidate `680d31f6f577e77d0e2eb55af5eb7c9fb8e3d871`, with SDK
10.0.401 and Microsoft servicing version 10.0.12. Populate only the direct
Builds checkout at `a283481c69393dcba911db6a0edcb152167238cc`. Use a clean
Builds checkout with a fetched remote-tracking ref containing that commit. With
the historical checkout's locked Python dependencies installed, run from its root:

```bash
uv run --frozen python _bmad/scripts/check_package_refresh_environment.py --repository . --check
```

A `PASS` recomputes the static package and toolchain bindings from committed
Git objects. It does not execute the historical .NET SDK. Running the current
checker against October 8's record can report `REFRESH_SDK_DRIFT` because that
checker expects a later profile.

## GitHub branch setting

Keep the existing linear-history requirement, force-push ban, and deletion ban
on `main`. Do not add a pull-request requirement or required status check while
direct pushes are the normal workflow. CI failures require follow-up fixes but
cannot prevent the push that triggered them.

## Explicit evidence work

Story 10.3 and later specs that explicitly require evidence-boundary verification
use their effective scoped contract before review or completion. Story 10.3 is
bound by `_bmad-output/planning-artifacts/story-10.3-current-binding-v1.json` and
`_bmad-output/planning-artifacts/story-10.3-current-contract-v1.json`. Run the
contract commands with its approved baseline and committed candidate. Preserve
`PASS`, `FAIL`, `BLOCKED`, and `not-applicable` as distinct recorded states.
`FAIL` and `BLOCKED` halt the transition; valid `not-applicable` continues and is
recorded. This scoped gate does not change the routine path above.

The ten surviving installed workflow routes and their in-memory renders share
the scoped gate. Legacy `bmad-dev-story` and `bmad-quick-dev` aliases stay retired.
The Story 7.3 final-record completion guard remains required independently.
