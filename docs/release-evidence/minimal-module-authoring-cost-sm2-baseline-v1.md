# Minimal Module Authoring Cost SM-2 Baseline v1

**Story:** 4.2 - Measure and record the minimal-module authoring cost (SM-2 baseline).
**Status:** accepted.
**Measurement date:** 2026-06-26.
**Machine-readable source:** `docs/release-evidence/minimal-module-authoring-cost-sm2-baseline-v1.json`.

## Scope

This artifact measures the minimal do-nothing-but-valid domain-module boundary handed off by Story 4.1.

Primary references:

- `docs/domain-module-authoring-template.md#Minimal-Project-Skeleton`
- `docs/release-evidence/thin-authoring-template-validation-v1.md#Story-42-handoff`
- `docs/release-evidence/consume-promote-keep-inventory-v1.md#Plumbing-derivation`
- `_bmad-output/planning-artifacts/prds/prd-Conversations-2026-06-02/prd.md#SM-2-New-module-authoring-cost`

Included categories are exactly the Story 4.1 SM-2 boundary: `Contracts`, `Client`, domain/core, `Server`, `AppHost`, `ServiceDefaults` only if present, `Testing` only if present, and focused test projects.

Excluded categories are exactly: `Admin.Web`, FrontComposer trust components, publication subscribers, governance workflows, generated output, local developer artifacts, shared platform libraries, and sibling submodule source.

## Counting Method

This is a manifest-only measurement. No product module was added under `src/`, and no throwaway source tree is committed. The manifest represents the minimal logical file set a new module author owns when following the Story 4.1 template.

Reproduction steps:

1. Read `templateMinimal.manifest` from the JSON artifact.
2. Count one file for each manifest row where `counted` is `true`.
3. Sum each row's `loc` value for total LOC.
4. Group rows by `category` to reproduce `templateMinimal.categoryTotals`.
5. Ignore `bin/`, `obj/`, generated output, package caches, local IDE artifacts, local machine paths, shared platform libraries, and sibling submodule source.

The build-validity claim is intentionally limited: the skeleton is valid against the accepted Story 4.1 template boundary, but this story did not compile a committed throwaway module.

## Baseline Totals

| Category | Files | LOC |
|---|---:|---:|
| `Contracts` | 7 | 90 |
| `Client` | 4 | 57 |
| domain/core | 4 | 72 |
| `Server` | 7 | 117 |
| `AppHost` | 3 | 56 |
| `ServiceDefaults` | 0 | 0 |
| `Testing` | 0 | 0 |
| Focused test projects | 4 | 76 |
| **Total** | **29** | **468** |

## Counted Manifest

| Category | Logical path | LOC |
|---|---|---:|
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/Hexalith.ExampleModule.Contracts.csproj` | 10 |
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/ExampleModuleAssemblyMarker.cs` | 8 |
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/Identifiers/ExampleItemId.cs` | 14 |
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/Commands/CreateExampleItem.cs` | 14 |
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/Events/ExampleItemCreated.cs` | 14 |
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/Queries/ExampleItemProjection.cs` | 18 |
| `Contracts` | `src/Hexalith.ExampleModule.Contracts/Serialization/ExampleModuleJsonContext.cs` | 12 |
| `Client` | `src/Hexalith.ExampleModule.Client/Hexalith.ExampleModule.Client.csproj` | 10 |
| `Client` | `src/Hexalith.ExampleModule.Client/ExampleModuleClientOptions.cs` | 16 |
| `Client` | `src/Hexalith.ExampleModule.Client/IExampleModuleClient.cs` | 13 |
| `Client` | `src/Hexalith.ExampleModule.Client/ExampleModuleClientServiceCollectionExtensions.cs` | 18 |
| domain/core | `src/Hexalith.ExampleModule/Hexalith.ExampleModule.csproj` | 12 |
| domain/core | `src/Hexalith.ExampleModule/Aggregates/ExampleItemAggregate.cs` | 30 |
| domain/core | `src/Hexalith.ExampleModule/Aggregates/ExampleItemState.cs` | 16 |
| domain/core | `src/Hexalith.ExampleModule/Validation/ExampleItemValidation.cs` | 14 |
| `Server` | `src/Hexalith.ExampleModule.Server/Hexalith.ExampleModule.Server.csproj` | 15 |
| `Server` | `src/Hexalith.ExampleModule.Server/Program.cs` | 14 |
| `Server` | `src/Hexalith.ExampleModule.Server/ServerAssemblyMarker.cs` | 8 |
| `Server` | `src/Hexalith.ExampleModule.Server/ExampleModuleServiceCollectionExtensions.cs` | 16 |
| `Server` | `src/Hexalith.ExampleModule.Server/TenantAccess/ExampleTenantAccessServiceCollectionExtensions.cs` | 18 |
| `Server` | `src/Hexalith.ExampleModule.Server/Projections/ExampleProjectionHandler.cs` | 24 |
| `Server` | `src/Hexalith.ExampleModule.Server/Queries/ExampleQueryHandler.cs` | 22 |
| `AppHost` | `src/Hexalith.ExampleModule.AppHost/Hexalith.ExampleModule.AppHost.csproj` | 12 |
| `AppHost` | `src/Hexalith.ExampleModule.AppHost/Program.cs` | 20 |
| `AppHost` | `src/Hexalith.ExampleModule.AppHost/ExampleModuleAppHostTopology.cs` | 24 |
| Focused test projects | `tests/Hexalith.ExampleModule.Tests/Hexalith.ExampleModule.Tests.csproj` | 14 |
| Focused test projects | `tests/Hexalith.ExampleModule.Tests/MinimalModuleContractShapeTest.cs` | 18 |
| Focused test projects | `tests/Hexalith.ExampleModule.Tests/ExampleAggregateTest.cs` | 24 |
| Focused test projects | `tests/Hexalith.ExampleModule.Tests/ExampleServerCompositionTest.cs` | 20 |

## Pre-Initiative Comparison

The pre-initiative equivalent is estimated, not measured. The estimate starts with the 29-file template baseline and adds the local capability files a pre-template module would likely have authored before the Epic 2 and Epic 3 shared capabilities existed. The basis is the accepted Story 1.4 inventory plus the Story 4.1 live capability mapping.

| Basis | Files | LOC | Confidence |
|---|---:|---:|---|
| Template minimal baseline | 29 | 468 | Accepted manifest |
| Pre-initiative equivalent | 58 | 1,460 | Low, estimated |
| Reduction | 50.00% | 67.95% | Directional only |

The reduction formula is `(preInitiativeEquivalent - templateMinimal) / preInitiativeEquivalent * 100`.

## OQ-2 Status

OQ-2 remains `unconfirmed`. The PRD target assumption is `>=50% fewer files for a minimal module`, but no accepted artifact reconstructs an exact buildable pre-template minimal skeleton. This artifact therefore records the baseline and a directional estimate without claiming the SM-2 target is resolved.

Story 5.3 should consume the JSON fields `templateMinimal`, `preInitiativeEquivalent`, `comparison`, `oq2Status`, `measurementDate`, and `sourceArtifactReferences`.
