from pathlib import Path
import json,hashlib,re,sys
p=Path('_bmad-output/planning-artifacts/epic-16-tooling-v1/README.md');s=p.read_text();print(s[:1500]);
s=s.replace('exactly five', 'exactly six')
# Correct the primary shape explicitly, regardless of surrounding prose.
s=s.replace('`scenarioId`, `candidate`, `sources`, `contractSha256`, and `observations`', '`scenarioId`, `candidate`, `sources`, `contractSha256`, `observations`, and `loadedProducts`')
s+='''

Iteration 11 completes executed outcome observations. The runtime native JSON has
exactly six fields: `scenarioId`, `candidate`, `sources`, `contractSha256`,
`observations`, and `loadedProducts`. Only the synthetic `tooling-fixture` shape
may omit `loadedProducts` and contain five fields. Runtime capture version 2
retains the selected test binary and all seven required owned product binaries,
correlated with raw TRX; baseline/current compiled API artifacts also join the
exact deduplicated publication inventory. Synthetic fixture success grants no
runtime entry, owner qualification or product execution claim.

AC-16.1-04's controlled durable DTO retains source state/event, delivered native
fingerprint, complete before/after projection/checkpoint/health/pending state and
CAS expectation. Its executed commit and acknowledgement identify one durable
transaction and the same actual persisted contents. The duplicate case observes
an existing committed outcome with zero new writes; failure/exhaustion have no
commit, acknowledgement or durable evidence. Operation order permits equal ticks.
These are evidence captures, not public product DTOs or new physical storage keys.

Each AC-16.3-04 stale branch now contains a request, executed CAS attempt and
completed result with one operation identity and exact scope. Canonical digests
bind its expected/proposed and stored before/after snapshots to those operations.
Each branch remains an independent reset/fork without a cross-branch clock order.
Duplicate retry executes after the current wire response, correlates that request
and clearance transaction, and returns AlreadyCompleted without mutation.
Multi-resource erase derives index dispatch references by concatenating each
source-derived resource component's references, retaining multiplicity.
'''
p.write_text(s)
p=Path('_bmad/scripts/epic16_native_helper/README.md');s=p.read_text();s+='''

Iteration 11 retains each InterfaceImplementation's annotations, and every retained
attribute's complete declaring type, selected constructor name/signature and value
blob, for local and referenced constructors. Method implementation flags (including
PreserveSig) and owning assembly flags participate in preservation. Owning assembly
and module contract attributes are compared exactly. The only excluded external
framework build stamps are AssemblyInformationalVersion and AssemblyFileVersion,
and AssemblyMetadata whose exact key is SourceRevisionId. Informational/source
revision is independently validated; other AssemblyMetadata keys remain contracts.
Owning assembly versions keep the existing upstream policy and may change. Public
root declarations remain unchanged; upstream additions remain conservative.

Test discovery requires one plain pinned Fact and exactly one resolvable Fact-derived
attribute. Theory and local direct/indirect Fact-derived conflicts reject. Unresolved
external custom method-attribute inheritance is outside the supported boundary and
fails explicitly. Attribute constructors and test code are never executed.
''';p.write_text(s)
