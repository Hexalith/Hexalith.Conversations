# Sprint Change Proposal - Move Root Submodules Under `references/`

**Date:** 2026-07-06T21:34:22+0200  
**Project:** Conversations  
**Requested by:** Jerome  
**Workflow:** bmad-correct-course  
**Mode:** Batch  
**Status:** Approved and implemented
**Approved by:** Jerome on 2026-07-06

## 1. Issue Summary

The requested course correction is to move the Hexalith sibling-module submodules from repository-root paths such as `Hexalith.EventStore` to `references/Hexalith.EventStore`.

Current evidence:

- `.gitmodules` declares ten submodules at root-level paths: `Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, `Hexalith.EventStore`, `Hexalith.Folders`, `Hexalith.FrontComposer`, `Hexalith.Memories`, `Hexalith.Parties`, `Hexalith.Projects`, and `Hexalith.Tenants`.
- `git ls-files -s` records those same ten paths as `160000` gitlinks.
- `git submodule status --recursive` reports all ten current paths with a leading `-`, and each module directory contains its own `.git` directory while the superproject has no `.git/modules` directory. Implementation must therefore handle the current independent-checkout/submodule hybrid state carefully.
- `Directory.Build.props`, `Directory.Packages.props`, multiple `.csproj` files, and `ScaffoldSmokeTest` still resolve sibling modules through root-level paths.
- The shared Hexalith LLM instruction already states that submodules should be declared in `references/`; several sibling project contexts also assume root-declared submodules live under `references/`.

The core problem is a repository-layout mismatch: the Conversations superproject still uses root-level submodule paths even though the Hexalith standard now expects root-declared submodules under `references/`.

The default workflow output file `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06.md` already exists for a different package-version proposal. This proposal uses a slugged filename to avoid overwriting it.

## 2. Impact Analysis

### Epic Impact

**Epic 1 - Boilerplate Baseline & Behavior-Preservation Oracle:** No feature scope change. Existing scaffold and root-shape tests should be updated so the oracle proves `references/` layout instead of root-level submodule layout.

**Epic 2 - Consume Existing Technical-Module Surface:** No rollback. Existing source project references remain conceptually valid but their fallback paths must point to `references/`.

**Epic 3 - Promote -> Adopt pipeline:** Directly affected. Promote/adopt stories rely on sibling technical-module submodules, pointer bumps, and root-only submodule mechanics. The runbook and sprint action items should state `references/<module>` paths explicitly.

**Epic 4 - Thin Authoring Template & Authoring-Cost Proof:** Minor documentation impact. The authoring template should use `references/` in setup and dependency-source guidance.

**Epic 5 - Behavior-Preservation Attestation & Sign-off:** Moderate release-governance impact. Story 5.3 explicitly excluded accidental root gitlink changes; this correction is an intentional gitlink move and needs its own evidence boundary.

### Story Impact

No completed story needs rollback. Recommended direct adjustment is a new corrective implementation story or action item:

**Story 5.4 / Corrective Action: Move root submodules under `references/` and update build resolution**

Scope:

- Move all root gitlinks and working directories to `references/<module>`.
- Update `.gitmodules` and superproject config expectations.
- Update root MSBuild path discovery, package-version imports, project-reference fallback paths, scaffold tests, and documentation.
- Preserve current submodule SHAs exactly; this is a path move, not a dependency bump.
- Do not initialize nested submodules and do not modify sibling submodule source.

### Artifact Conflicts

**PRD:** No product capability conflict. Optional clarification can be added to the boilerplate-reduction PRD guardrails if desired.

**Epics:** Existing epic wording says root-level submodules and pointer bumps must be handled safely. Add a correction note that "root-level" means "root-declared under `references/`", not physically at repository root.

**Architecture:** Build process and project-structure guidance should be updated to describe `references/` as the local source dependency location.

**UX:** Not applicable. No UI/UX behavior changes.

**Implementation artifacts:** `sprint-status.yaml` should receive a new open corrective action after approval. Existing completed story status should not be rewritten.

**Code/build artifacts:** `.gitmodules`, `Directory.Build.props`, `Directory.Packages.props`, affected `.csproj` fallback paths, `ScaffoldSmokeTest`, `README.md`, `AGENTS.md`, and `CLAUDE.md` are impacted.

### Technical Impact

- Runtime/product behavior: no intended change.
- Build behavior: local source project references should resolve from `references/`.
- Git behavior: intentional gitlink path move across ten submodules.
- Risk level: Medium, because submodule path moves can accidentally record SHA changes or convert submodule directories into normal tracked files if handled incorrectly.
- Validation must include gitlink checks, path scans, restore/build, and targeted scaffold tests.

## 3. Recommended Approach

Recommended path: **Direct Adjustment with controlled gitlink migration**.

Rationale:

- The desired target layout is already established by Hexalith instructions and sibling module patterns.
- No product or MVP scope changes are needed.
- The change can be implemented as a repository-layout/build-governance correction, provided the implementation proves that submodule SHAs are preserved and no nested submodules are initialized.

Effort estimate: 0.5 to 1.5 days, depending on whether the independent `.git` directories need normalization before `git mv`.

Risk assessment:

- **Medium:** Incorrect submodule moves can produce deleted gitlinks plus added ordinary directories.
- **Medium:** Root-level fallback paths in `.csproj` files can mask incomplete migration if not updated.
- **Low:** Runtime behavior should not change.
- **Low:** UX and public contracts should not change.

## 4. Detailed Change Proposals

### Git Submodule Layout

Artifact: `.gitmodules` and superproject gitlinks

OLD:

```ini
[submodule "Hexalith.EventStore"]
	path = Hexalith.EventStore
	url = https://github.com/Hexalith/Hexalith.EventStore.git
```

NEW:

```ini
[submodule "references/Hexalith.EventStore"]
	path = references/Hexalith.EventStore
	url = https://github.com/Hexalith/Hexalith.EventStore.git
```

Apply the same path move to all ten submodules:

- `Hexalith.AI.Tools` -> `references/Hexalith.AI.Tools`
- `Hexalith.Builds` -> `references/Hexalith.Builds`
- `Hexalith.Commons` -> `references/Hexalith.Commons`
- `Hexalith.EventStore` -> `references/Hexalith.EventStore`
- `Hexalith.Folders` -> `references/Hexalith.Folders`
- `Hexalith.FrontComposer` -> `references/Hexalith.FrontComposer`
- `Hexalith.Memories` -> `references/Hexalith.Memories`
- `Hexalith.Parties` -> `references/Hexalith.Parties`
- `Hexalith.Projects` -> `references/Hexalith.Projects`
- `Hexalith.Tenants` -> `references/Hexalith.Tenants`

Rationale: Aligns Conversations with the Hexalith standard and sibling module assumptions.

Implementation note: before moving, record each submodule HEAD and status. After moving, verify `git ls-files -s` still records exactly ten `160000` entries and all paths start with `references/`.

### Root Build Path Discovery

Artifact: `Directory.Build.props`

OLD:

```xml
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts')">$(MSBuildThisFileDirectory)..\Hexalith.EventStore</HexalithEventStoreRoot>
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)Hexalith.EventStore\src\Hexalith.EventStore.Contracts')">$(MSBuildThisFileDirectory)Hexalith.EventStore</HexalithEventStoreRoot>
```

NEW:

```xml
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)references\Hexalith.EventStore\src\Hexalith.EventStore.Contracts')">$(MSBuildThisFileDirectory)references\Hexalith.EventStore</HexalithEventStoreRoot>
```

Apply the same primary `references\` detection to EventStore, Tenants, Parties, FrontComposer, and Commons. Keep any parent-directory fallback only if there is a documented external checkout workflow; otherwise remove old root-level sibling fallback to prevent accidental drift.

Rationale: Build source resolution should follow the new submodule location and fail predictably if required source dependencies are absent.

### Central Package Baseline Import

Artifact: `Directory.Packages.props`

OLD:

```xml
<Hexalith1BuildPackageProps>$(MSBuildThisFileDirectory)Hexalith.Builds/Props/Directory.Packages.props</Hexalith1BuildPackageProps>
```

NEW:

```xml
<Hexalith1BuildPackageProps>$(MSBuildThisFileDirectory)references/Hexalith.Builds/Props/Directory.Packages.props</Hexalith1BuildPackageProps>
```

Rationale: Package baseline import should resolve from the new `references/Hexalith.Builds` checkout.

### Project Reference Fallback Paths

Artifacts: affected `src/**/*.csproj` and `tests/**/*.csproj`

Representative OLD:

```xml
<ProjectReference Include="..\..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" Condition="'$(HexalithEventStoreRoot)' == ''" />
```

Representative NEW:

```xml
<ProjectReference Include="..\..\references\Hexalith.EventStore\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" Condition="'$(HexalithEventStoreRoot)' == ''" />
```

Affected fallback families:

- `Hexalith.EventStore`
- `Hexalith.Tenants`
- `Hexalith.Commons`

Rationale: If property-based root discovery fails or is unavailable in an isolated evaluation, the fallback should still point to the approved checkout location.

### Scaffold Test Update

Artifact: `tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs`

OLD:

```csharp
string[] candidates =
[
    Path.GetFullPath(Path.Combine(root, "..", moduleName)),
    Path.GetFullPath(Path.Combine(root, moduleName)),
];
```

NEW:

```csharp
string[] candidates =
[
    Path.GetFullPath(Path.Combine(root, "references", moduleName)),
    Path.GetFullPath(Path.Combine(root, "..", moduleName)),
];
```

Add a focused assertion:

```csharp
gitlinkPaths.ShouldAllBe(path => path.StartsWith("references/", StringComparison.Ordinal));
```

Rationale: The scaffold oracle should prove the new layout and stop passing against the old root-level paths.

### Documentation Updates

Artifacts: `README.md`, `AGENTS.md`, `CLAUDE.md`, `docs/domain-module-authoring-template.md`

README OLD:

```md
Root-level sibling modules are preserved through `.gitmodules`.
```

README NEW:

```md
Root-declared sibling modules are stored under `references/` through `.gitmodules`. Initialize only the needed `references/Hexalith.*` submodules and never use recursive submodule initialization unless nested submodules are explicitly requested.
```

AGENTS/CLAUDE OLD:

```md
Initialize only submodules at the root of the repository; never initialize nested submodules.
```

AGENTS/CLAUDE NEW:

```md
Initialize only root-declared submodules under `references/`; never initialize nested submodules.
```

Rationale: The agent-facing setup rules must match the physical repository layout.

### Sprint Status Change

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  - epic: 5
    action: "Keep release-facing documentation aligned with Epic 5 attestation state without claiming approval."
    owner: "Tech writer/Release owner"
    status: open
```

NEW:

```yaml
  - epic: 5
    action: "Keep release-facing documentation aligned with Epic 5 attestation state without claiming approval."
    owner: "Tech writer/Release owner"
    status: open
  - epic: 5
    action: "Move root-declared sibling submodules under references/, update build/test/documentation path assumptions, and prove the migration preserves all submodule SHAs without initializing nested submodules."
    owner: "Developer/Release owner"
    status: open
```

Rationale: Tracks the correction without reopening completed stories.

## 5. Implementation Handoff

Scope classification: **Moderate**.

Recommended recipients: Developer agent plus release-owner review.

Responsibilities:

- Record current submodule SHAs before any move.
- Move gitlinks and working directories with git-aware commands, not copy/delete.
- Preserve exact submodule SHAs; do not bump dependency pointers.
- Do not modify sibling submodule source.
- Do not initialize nested submodules and do not use `git submodule update --init --recursive`.
- Update all root-level path assumptions to `references/`.
- Run validation and report any environment-limited test lane honestly.

Suggested implementation sequence:

1. Verify root working tree and each submodule working tree are clean enough for a path-only move.
2. Create `references/`.
3. Move each submodule path into `references/` while preserving gitlink identity.
4. Update `.gitmodules` section names/paths and sync local config.
5. Update `Directory.Build.props`, `Directory.Packages.props`, `.csproj` fallback paths, tests, and docs.
6. Validate no root-level `Hexalith.*` gitlinks remain.
7. Restore/build and run scaffold-focused tests.

Validation commands:

```bash
git status --short
git ls-files -s | awk '$1 == "160000" { print $4 }'
git config --file .gitmodules --get-regexp 'submodule\..*\.path'
rg -n '(^|[\\/])Hexalith\.(AI\.Tools|Builds|Commons|EventStore|Folders|FrontComposer|Memories|Parties|Projects|Tenants)([\\/]|$)' Directory.Build.props Directory.Packages.props README.md AGENTS.md CLAUDE.md src tests docs _bmad-output/implementation-artifacts/sprint-status.yaml
dotnet restore Hexalith.Conversations.slnx
dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore
dotnet test tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj --configuration Release --no-restore
```

Success criteria:

- `.gitmodules` contains only `references/Hexalith.*` paths for sibling submodules.
- `git ls-files -s` shows exactly ten `160000` gitlinks and every gitlink path starts with `references/`.
- No root-level `Hexalith.*` submodule directories or gitlinks remain.
- Build/test path resolution prefers `references/`.
- Restore and Release build pass, or any failure is reported with exact diagnostics.
- No public contract, package version, runtime behavior, AppHost topology, generated output, or sibling submodule source changes are introduced.

## 6. Checklist Progress

### 1. Understand Trigger and Context

- [N/A] 1.1 Triggering story: no single story triggered this; the trigger is a release-owner/user correction after the boilerplate-reduction sprint.
- [x] 1.2 Core problem: repository submodule layout conflicts with Hexalith `references/` standard and sibling build assumptions.
- [x] 1.3 Evidence gathered: `.gitmodules`, gitlink list, submodule status, root build props, project references, scaffold tests, README/agent instructions, PRD, epics, architecture, UX map, project contexts, and ADR index.

### 2. Epic Impact Assessment

- [x] 2.1 Current epics remain valid.
- [x] 2.2 Add a corrective implementation action; no new product epic required.
- [x] 2.3 Future maintenance and promote/adopt mechanics are impacted by path assumptions.
- [N/A] 2.4 No planned epic is invalidated.
- [N/A] 2.5 No epic priority resequencing is needed.

### 3. Artifact Conflict and Impact Analysis

- [x] 3.1 PRD: optional clarification only.
- [x] 3.2 Architecture: build/project-structure path assumptions should be clarified.
- [N/A] 3.3 UX: no UI impact.
- [x] 3.4 Other artifacts: `.gitmodules`, build props, project files, tests, README, agent instructions, and sprint status need updates.

### 4. Path Forward Evaluation

- [x] 4.1 Direct Adjustment: viable with medium gitlink risk.
- [N/A] 4.2 Potential Rollback: not justified.
- [N/A] 4.3 PRD MVP Review: not needed.
- [x] 4.4 Selected approach: Direct Adjustment with controlled gitlink migration.

### 5. Proposal Components

- [x] 5.1 Issue summary created.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact and action plan defined.
- [x] 5.5 Handoff plan defined.

### 6. Final Review and Handoff

- [x] 6.1 Checklist complete.
- [x] 6.2 Proposal drafted.
- [x] 6.3 User approval received on 2026-07-06.
- [x] 6.4 Sprint status corrective action added.
- [x] 6.5 Handoff routed to Developer / Release owner.

## 7. Approval Result

Jerome approved this proposal and requested implementation to continue.

## 8. Implementation Result

The submodule layout correction was implemented on 2026-07-06:

- Moved all ten root-declared sibling submodules to `references/Hexalith.*`.
- Preserved the recorded submodule SHAs; no sibling submodule source changes were introduced.
- The working tree still reports an existing local change inside `references/Hexalith.Tenants/Directory.Packages.props` (`ByteAether.Ulid` 1.3.7 -> 1.3.8); the Tenants gitlink remains pinned at `c707cb8a08506a398be6cd8d5670fa2a28ca48b3`.
- Updated `.gitmodules`, build/package path discovery, project-reference fallbacks, scaffold tests, and agent/project documentation to use `references/`.
- Verified no root-level `Hexalith.*` directories remain and no old `..\..\Hexalith.*` fallback project-reference paths remain under `src` or `tests`.
- Validation passed with `dotnet restore Hexalith.Conversations.slnx`, `dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore`, and `dotnet test tests/Hexalith.Conversations.IntegrationTests/Hexalith.Conversations.IntegrationTests.csproj --configuration Release --no-restore`.
