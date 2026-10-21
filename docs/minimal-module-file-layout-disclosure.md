# Minimal-module authoring cost: file-layout disclosure

This addendum resolves Epic 11 retrospective action A-4 (2026-10-21). It reconciles
the frozen Story 11.2 inventory with Hexalith's normal one-type-per-file convention
and explains the resulting sensitivity in the accepted SM-2 comparison.

## Scoped convention exception

The [shared C# authoring rule](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
requires each C# type to live in its own file, named for that type. The approved
[Story 11.2 specification](../_bmad-output/implementation-artifacts/spec-11-2-build-the-reproducible-minimal-module-fixture-against-live-platform-apis.md)
instead freezes exactly 13 authored paths in `V9-MINIMAL-MODULE-FIXTURE-v1`. Its
Implementation Notes explain the combined registration/handler file. The
[Story 11.3 owner decision](../_bmad-output/implementation-artifacts/spec-11-3-generate-authoritative-sm-2-v2-evidence-and-decide-oq-2.md)
requires counting those paths and preserving the fixture and accepted predecessors.

The inventory-specific exception is confined to these two frozen source files:

| Frozen file | Types beside the type named by the file | Additional files if separated |
| --- | --- | ---: |
| [ConversationModule.cs](../tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationModule.cs) | `ConversationProjection`, `ConversationTitleQuery`, `ConversationObservations`, `ConversationCreatedObserver` | 4 |
| [ConversationAggregate.cs](../tests/fixtures/Hexalith.Conversations.MinimalModule/src/Hexalith.Conversations.MinimalModule.Server/ConversationAggregate.cs) | `ConversationState` | 1 |

`ConversationModule` and `ConversationAggregate` remain in their named files in this
calculation. Moving the five other types into five named `.cs` files would increase
the total by five: `13 + 4 + 1 = 18`. This disclosure preserves the approved fixture
layout as a historical exception; future comparable samples follow the normal rule
unless their own scope explicitly permits and discloses an exception.

## File-count sensitivity and confidence

The [accepted SM-2 v2 record](release-evidence/minimal-module-authoring-cost-sm2-v2.md)
and its [JSON measurements](release-evidence/minimal-module-authoring-cost-sm2-v2.json)
retain the owner-accepted reference of **58 estimated files and 1,460 estimated LOC**.
File reduction is `(58 - file count) / 58 * 100`.

| Layout | File count | Reduction against the 58-file estimate | Evidence status |
| --- | ---: | ---: | --- |
| Frozen approved inventory | 13 | 77.59% | Accepted source measurement |
| One type per file, with all other inventory choices held constant | 18 | 68.97% | Hypothetical file-layout sensitivity only |

This sensitivity varies only the measured sample's layout and holds the reference
count fixed. The estimated reference has no reconstructed source tree whose
type-to-file convention can be verified, so matching conventions on both sides
remain unproven.

Both counts meet the inclusive 50% file threshold before rounding. The accepted
record's decision remains `accepted`, qualified as `met-on-accepted-estimate`.
The 18-file row supplies no replacement measurement, build proof or acceptance
decision. No source files have been split for this calculation.

For the actual 13-file sample, measurement confidence remains **high** and the
recorded totals remain **451 physical LOC and 396 nonblank LOC**. Reference and
comparison confidence remain **low**: the 58-file reference is an accepted estimate,
not a reconstructed and compiled comparison fixture. Its estimated LOC metric is
also different from the fixture's physical LOC metric, so LOC supports the file
comparison without deciding attainment. No LOC total is measured for the hypothetical
18-file layout; splitting may change imports, namespace declarations and blank lines.

## Preservation and future measurements

This addendum leaves the 13-path fixture, generated lock files, accepted A/B/C
evidence and receipts, frozen specification intent, SM-2 v1 history and SM-1's
**13,289 LOC** unchanged. It also leaves the hash-bound authoring template unchanged.
The fixture has 17 tracked files: 13 authored inventory paths and four generated
`packages.lock.json` files. The generated locks are excluded from both the
hand-authored file count and LOC totals.
The historical count describes the accepted frozen sample; later maintenance
documentation and external compatibility tests do not enlarge that sample.

For a new comparable authoring-cost claim, use one type per file and count the
complete hand-authored inventory within the declared sample boundary at that claim's
candidate, including its repository configuration, documentation, tests and all
source/project files of its repository-only test AppHost. Explain
generated/platform exclusions and disclose the file-layout convention on both sides
of the comparison. Any explicit exception should identify the affected files/types,
its rationale and its effect on file-count comparability. Keep comparison confidence
low while the reference remains estimated.

The [retrospective's F-4 finding](../_bmad-output/implementation-artifacts/epic-11-retro-2026-10-21.md#f-4--file-count-comparability-needs-an-explicit-convention-exception)
records the source of this reconciliation. This disclosure closes A-4's documentation
scope; it does not change the retrospective's historical acceptance verdict.
