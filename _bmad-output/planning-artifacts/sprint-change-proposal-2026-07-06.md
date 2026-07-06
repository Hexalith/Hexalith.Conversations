# Sprint Change Proposal - Package Version Source of Truth

**Date:** 2026-07-06
**Project:** Conversations
**Requested by:** Jerome
**Workflow:** bmad-correct-course
**Mode:** Batch
**Status:** Draft, awaiting review

## 1. Issue Summary

The requested course correction is to ensure Hexalith.Conversations uses the Hexalith.Builds package versions file as the source of truth for NuGet package versions, with a pattern comparable to other Hexalith modules.

Current evidence:

- `Directory.Packages.props` already imports `Hexalith.Builds/Props/Directory.Packages.props` through a fallback chain and keeps only one local override: `Microsoft.Playwright` version `1.61.0`.
- A scan of `src` and `tests` found no inline `PackageReference Version=` entries. Package references are versionless and central package management is active.
- `Hexalith.Builds/Samples/Module.Directory.Packages.props` shows the intended module template: import the shared `Hexalith.Builds/Props/Directory.Packages.props` file, then define module-specific versions only when needed.
- The local `Hexalith.Tenants/Directory.Packages.props` snapshot is self-contained and does not currently import `Hexalith.Builds`. If "like tenants" means the current local Tenants file literally, that conflicts with the requested Hexalith.Builds source-of-truth direction.

The issue is therefore not a current inline-version defect. It is a planning and governance gap: the sprint artifacts do not explicitly state that Hexalith.Builds is the baseline package-version source, local package versions are exceptions, and validation must protect that rule.

## 2. Impact Analysis

### Epic Impact

**Epic 1 - Boilerplate Baseline & Behavior-Preservation Oracle:** No scope change. The package-version rule can be treated as another build-governance invariant for future work.

**Epic 2 - Consume Existing Technical-Module Surface:** No code rollback or story reopening required. Existing package references already comply with versionless project files.

**Epic 3 - Promote -> Adopt pipeline:** Minor impact. Promote/adopt stories should preserve the shared Hexalith.Builds package-version baseline and must not add local package pins without explicit exception rationale.

**Epic 4 - Thin Authoring Template & Authoring-Cost Proof:** Minor documentation impact. The domain-module authoring template should state the package-version source-of-truth rule.

**Epic 5 - Behavior-Preservation Attestation & Sign-off:** Minor release-governance impact. The final attestation already records no package-version changes; add a follow-up action so package-version source-of-truth validation is explicit before release-owner sign-off.

### Story Impact

No completed story must be rolled back. Recommended direct adjustment is a new follow-up story/action item:

**Story 5.4 or Post-Epic Corrective Action: Verify shared Hexalith.Builds package-version source of truth**

Scope:

- Confirm `Directory.Packages.props` imports `Hexalith.Builds/Props/Directory.Packages.props`.
- Confirm project files contain no inline `PackageReference Version=` entries.
- Confirm local `PackageVersion` entries are exception-only and justified.
- Update the authoring template and sprint status action items with this build-governance rule.
- Do not change public contracts, runtime behavior, package versions, AppHost topology, generated output, or sibling submodule source.

### Artifact Conflicts

**PRD:** No product-scope conflict. Existing NFR7 already requires Central Package Management. Optional clarification can add that Hexalith.Builds is the shared baseline for package versions when available.

**Epics:** No epic scope conflict. Existing NFR7 and Epic 3 NFR6 remain valid. Add a small follow-up action instead of reopening a completed epic unless the release owner wants the story tracked as 5.4.

**Architecture:** Update the build-process guidance to state the actual baseline import rule and local exception policy.

**UX:** Not applicable. No UI/UX behavior changes.

**Implementation artifacts:** Update `sprint-status.yaml` only after approval by adding a release-governance action item.

### Technical Impact

- Low implementation effort.
- Low runtime risk: this is build configuration and documentation.
- Restore/build validation is still required because Central Package Management changes can affect all projects.
- The Tenants comparison needs an explicit wording decision because the local Tenants snapshot is self-contained, not Hexalith.Builds-imported.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- Current Conversations package files mostly already follow the requested direction.
- No rollback is needed because there is no evidence of inline project package versions.
- No MVP or product-scope reduction is needed.
- The safest change is to add explicit build-governance wording, validation expectations, and a small follow-up item.

Effort estimate: Low, about 0.5 to 1 day including restore/build validation.

Risk level: Low. The only notable risk is accidentally changing package versions or submodule pointers while trying to "align" files. The implementation should avoid version bumps and sibling submodule edits unless separately approved.

## 4. Detailed Change Proposals

### Architecture Change

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: Build Process Structure

OLD:

```md
- `Hexalith.Conversations.slnx` is the solution entry point.
- Central package management controls dependency versions.
- CI runs contracts, domain, server, integration, and conformance test lanes separately.
```

NEW:

```md
- `Hexalith.Conversations.slnx` is the solution entry point.
- Central package management controls dependency versions.
- `Directory.Packages.props` imports `Hexalith.Builds/Props/Directory.Packages.props` as the shared Hexalith package-version baseline when the root-level `Hexalith.Builds` submodule is present.
- Local `PackageVersion` entries are exception-only and must be justified by module-specific tooling or compatibility needs.
- Project files must use versionless `PackageReference` entries; package additions or bumps must not add inline `Version` attributes.
- CI runs contracts, domain, server, integration, and conformance test lanes separately.
```

Rationale: Captures the actual source-of-truth rule and prevents future stories from adding local package pins casually.

### PRD Change

Artifact: `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md`

Section: NFR7, optional clarification

OLD:

```md
NFR7: **Language/runtime targets unchanged** - net10.0, nullable enabled, implicit usings, warnings-as-errors, Central Package Management (per project-context).
```

NEW:

```md
NFR7: **Language/runtime targets unchanged** - net10.0, nullable enabled, implicit usings, warnings-as-errors, Central Package Management through the shared Hexalith.Builds package-version baseline, with module-local package versions treated as explicit exceptions.
```

Rationale: Keeps PRD scope unchanged while making the dependency-version governance concrete.

### Story / Sprint Status Change

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `action_items`

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
    action: "Verify and document that NuGet package versions resolve from the shared Hexalith.Builds Directory.Packages.props baseline, with local PackageVersion entries treated as justified exceptions and no inline PackageReference Version attributes in project files."
    owner: "Developer/Release owner"
    status: open
```

Rationale: Adds the correction as a small release-governance action without reopening completed epics.

### Authoring Template Change

Artifact: `docs/domain-module-authoring-template.md`

Section: New or existing build/package-management guidance

OLD:

```md
Use central package management and keep package references versionless.
```

NEW:

```md
Use central package management and keep project package references versionless. For Hexalith modules, import the shared `Hexalith.Builds/Props/Directory.Packages.props` baseline through the module `Directory.Packages.props`; add local `PackageVersion` entries only as documented exceptions.
```

Rationale: Carries the rule into future module authoring guidance, which is the durable output of the boilerplate-reduction initiative.

### Implementation Verification

Add or run the following checks during implementation:

```bash
rg -n 'PackageReference[^>]*\sVersion=|<PackageVersion' src tests Directory.Packages.props --glob '!**/obj/**' --glob '!**/bin/**'
dotnet restore Hexalith.Conversations.slnx
dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore
```

Expected result:

- The scan reports no inline `PackageReference Version=` entries.
- Any `<PackageVersion>` output is limited to central package files and justified local exceptions.
- Restore and Release build pass without package downgrade or central-package-management errors.

## 5. Implementation Handoff

Scope classification: **Minor**.

Recommended recipient: Developer agent.

Responsibilities:

- Apply the approved documentation/action-item updates.
- Avoid package version bumps unless explicitly requested.
- Avoid modifying `Hexalith.Builds`, `Hexalith.Tenants`, or other sibling submodule source unless separately approved.
- Run the package-version scan and restore/build validation.
- Record any Tenants comparison ambiguity as a note rather than copying the local Tenants self-contained version file.

Success criteria:

- `Directory.Packages.props` remains the only local package-version file in Conversations.
- It continues to import `Hexalith.Builds/Props/Directory.Packages.props`.
- `.csproj` files remain versionless for NuGet package references.
- Local `PackageVersion` entries are documented exceptions.
- Restore/build validation passes or any failure is reported with exact package diagnostics.

## 6. Checklist Progress

### 1. Understand Trigger and Context

- [x] 1.1 Trigger identified: release-owner/user correction after Epic 5 attestation work.
- [x] 1.2 Core problem: build-governance requirement needs explicit artifact coverage; current code mostly complies.
- [x] 1.3 Evidence gathered: root `Directory.Packages.props`, Hexalith.Builds sample, local Tenants mismatch, package-reference scans, planning artifacts.

### 2. Epic Impact Assessment

- [x] 2.1 Current epic can remain complete; no rollback needed.
- [x] 2.2 No new epic required.
- [x] 2.3 Future/post-release action items impacted only.
- [N/A] 2.4 No planned epic invalidated.
- [N/A] 2.5 No priority resequencing required.

### 3. Artifact Conflict and Impact Analysis

- [x] 3.1 PRD: optional clarification only.
- [x] 3.2 Architecture: build-process guidance should be clarified.
- [N/A] 3.3 UX: no UI impact.
- [x] 3.4 Other artifacts: sprint status and authoring template should receive small updates after approval.

### 4. Path Forward Evaluation

- [x] 4.1 Direct Adjustment: viable; low effort and low risk.
- [N/A] 4.2 Potential Rollback: not justified.
- [N/A] 4.3 PRD MVP Review: not needed.
- [x] 4.4 Selected approach: Direct Adjustment.

### 5. Proposal Components

- [x] 5.1 Issue summary created.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact and action plan defined.
- [x] 5.5 Handoff plan defined.

### 6. Final Review and Handoff

- [x] 6.1 Checklist complete.
- [x] 6.2 Proposal drafted.
- [!] 6.3 User approval pending.
- [N/A] 6.4 Sprint status update pending approval.
- [!] 6.5 Handoff pending approval.

## 7. Approval Request

Review this proposal and choose:

- Continue: approve the proposal for implementation.
- Edit: provide changes to the proposal before implementation.

