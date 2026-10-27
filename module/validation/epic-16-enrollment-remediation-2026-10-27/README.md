# Enrollment remediation source observations — 2026-10-27

Current source checkout: `dc5793db17fd6974200e80eb2b853187bc5f1669` with the recorded working-tree edits
and three untracked C# additions. The native scanner reads tracked source only;
owner integration must rerun both native formats on the intended reviewed/tracked
candidate. No staging, commits, dependency update, runtime launch or deployment
was performed. The public-materializer mapping harness is synthetic source
verification, not an ordinary Platform run or owner acceptance.

[summary.json](summary.json) indexes the final checks. Every command record retains
its arguments, working directory, UTC start, duration, exact exit, source HEAD,
and separate stdout/stderr SHA-256 hashes. Same-stem `.stdout.txt` and
`.stderr.txt` files retain raw output. Earlier failed attempts remain unchanged;
the final source results below do not reinterpret those attempts as PASS.

| Check | Actual result | Evidence |
| --- | --- | --- |
| Individual mapped Debug Contracts/Server/Integration builds | PASS | `build-contracts-final.json`, `build-server-envelope.json`, `build-integration-final.json` |
| Mapping matrix through Builds public materializer | 5/5 PASS | `xunit-mapping.xml`; source/Debug, package/Release evaluation, absent/missing/mismatched maps, competing layouts |
| Contracts metadata and Debug source-package inventory | 3/3 PASS | `xunit-contracts-inventory.xml` |
| Final affected Server namespaces | 753/753 PASS; zero failed/skipped/not-run | `xunit-server-affected-verified.xml` |
| Full offline Server assembly | 1369/1369 PASS; zero failed/skipped/not-run | `xunit-server-all.xml`; authoritative final source run, including configured advisory/native evidence and tenant envelope admission |
| In-process Integration projection population/replay | 2/2 PASS; no host start/listen | `xunit-integration-population.xml` |
| Actual package-mode project/reference/Compile evaluation | 8 consumer projects PASS; no Release compilation | `msbuild-package-*.json`; existing Contracts package inventory remains compiled, source-only non-packable materializer tests explicitly excluded |
| Native ordinary validation, text and JSON | FAIL, exit 1 in both formats | `ordinary-text.*`, `ordinary-json.*`; unchanged accepted-tool APP_API_TOKEN format failure |
| Native adoption, text and JSON | FAIL, exit 1; 6 PASS / 10 FAIL | `adoption-text-complete.*`, `adoption-json-complete.*`, `adoption-delta.json`; all 14 targeted identities absent, all ten excluded identities visible, no unexpected findings |
| Both Story 7.3 guards | PASS, exit 0 | `AC-7.3-01.result.json`, `AC-7.3-02.result.json`, `governance-mirror.json` |
| Six actual Story 16.1 contract commands | BLOCKED / ENTRY_QUALIFICATION_PENDING, exit 2 each | `story-AC-16.1-0*.json`; no runtime receipt/final pair |
| CI integration | pending owner mapping path; demonstrator FAIL, exit 1 as required | `ci-unmapped-contracts.*`, `ci-mapping-gate.json`; no CI run claimed |
| Protected files, raw root gitlinks, accepted pins and whitespace | PASS | `preservation-result.json`, `entry-preservation.json` |
| Runtime publication | not-applicable | no product runtime launch or publication; held entry null and trust empty |

The source-only Builds helper is non-packable. The maintained Debug mapping tests
consume its mapped project and evaluate package-mode references, while existing
Contracts metadata and package inventory assertions remain active in Release.
This does not establish package/Release runtime behavior or source/package/image
parity. [mapping-harness.md](mapping-harness.md) retains the disposable source
harness, and the `source-*`/`package-*` JSON files retain the chosen mappings and
complete generated import environments.

The first guard attempts rejected redirected output paths. Their observations
remain retained. The exact contracted commands then ran in a disposable shared
no-checkout clone with the same HEAD and byte-verified current governed source,
renderer/config and schema inputs. Only its contracted output paths were written;
original ignored historical guard receipts remain byte-identical. This mirror
scope is recorded in `governance-mirror.json`; it is not runtime qualification.

The earlier new Tenants test used an invalid raw message identity; its retained
FAIL results were corrected by using the producer fixture's sortable message and
correlation IDs, explicit system/tenants scope and user, producer serialization,
and SDK identity validation before dispatch. The final 11 registration cases and
full Server run pass. No production admission check was relaxed.

The exact final preservation audit compares 28,496 original
fixture/archive/hosting/pin bytes and all 12 raw root gitlinks and dependency
checkout HEADs. Public Contracts C# bytes, tenant-key algorithms and the
single-replica warning remain unchanged. Entry is null, trust is empty,
Story 16.1 draft/backlog and ACTIVE hold are untouched, and no runtime
publication files exist.

The current CI workflow and selected Builds reusable workflow still invoke
plain restore/build without generated mapping imports. Initialization does not
materialize a mapping. Those invocations now fail the approved mandatory-mapping
gate; fixing and qualifying the producer CI mapping path is a separate integration
gate, in addition to the ten native scanner failures. No workflow, pin, upstream
source or missing-mapping rule was changed to hide this limit.

Owner acceptance, published-tool compatibility, frozen captures/fixtures/hosts,
provider and deployment parity, key-ring restart, actual declarative delivery,
ACL/mTLS/token adapters, operational envelope/recovery/operator/capacity gates,
human SM-2 and Story 16.1 entry remain pending in
[the owner handoff](../../platform-handoff.md).
