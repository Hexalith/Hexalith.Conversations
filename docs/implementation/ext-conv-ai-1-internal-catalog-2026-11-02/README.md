Final parent reruns: **1,586 Server, 648 Contracts, 189 Domain, 28 release-tooling tests plus three subtests pass**. All 38 Conversations catalogue/enrollment cases pass. [Raw commands, logs, XML and comparison](parent-final-20261102T195034Z-c2fdcdb8c402476d82c8402242adaf8f/summary.json) retain the full result: an additional eight default-graph Platform cases have five passes and three restore failures. Those failures report NU1109 because Tenants requires EventStore 4.0.0 and McpCli 2.0.0 while the generated reader centrally selects 3.133.0 and 1.1.0. They fail before compiling or executing the reader. No downgrade suppression or source-selection substitution was applied.

The final reruns preserve all 38 source files and all four complete test-output inventories unchanged: 626 Server, 227 Domain, 214 Contracts and 862 Platform files. This measures the final reruns only; pre-implementation binary measurements remain unavailable. The corrected source uses mapping `9b32bb15ce13f9e80220453b04d1ab040a78eda02b35c2a80ba93bb19209b953` and catalogue root `3e085305868619e208d27c021f9bf52de9ff858e78e6dd4154cc123568323a67`. These are source observations, never installed qualification pins.

The [focused correction summary](post-review-20261102T194238Z-99eed439d54943489e21d04011237923/SUMMARY.md) and [machine provenance](post-review-20261102T194238Z-99eed439d54943489e21d04011237923/SUMMARY.json) record the reviewed schema/guard/receipt/host-test corrections. Focused results were 24 owner tests, one real Platform method, one release-count test; normal CI syntax and one-owner/multi-owner checks passed. The full published Release/NuGet CI lane was not run locally.

The supplement's complete evidence inventory is its point-in-time capture, including ignored XML/log/reader files. Original attempts are unchanged. A final package inventory additionally includes the parent reruns. Retained files disable Git line-ending conversion to preserve measured bytes. The earlier README is preserved in the supplement; the following counts and hashes describe the earlier pre-review patch, not the corrected source.

The current result is **passed**: 13 internal command receivers, seven queries and two projections agree with SDK discovery and their canonical adapters. Full Server tests passed 1,575; Domain 189; Contracts 648; final catalog controls 5; release tooling 28 plus three subtests; Platform enrollment and source composition 38.

[Final owner evidence](owner-20261102T191823Z-0c74653d3f224300b704223078cf579d/completion-summary.md) records the result and limits. [Machine summary](owner-20261102T191823Z-0c74653d3f224300b704223078cf579d/completion-summary.json) binds mapping `03f9973201e25c0b62245606d27fd3d9f7e793e2a329596756eba0a5f8d97177`, catalogue root `c3bef1002ed878971532954aca16414bb91847dc82aa3da81f0c19bde44fef17`, and receipt SHA-256 `82b5c21ca7f8abbb7772d106fb7fb80c3c5d04e5281e4d0b42ccd944e8ad0b23`.

[Final Platform artifacts](platform-composition/20261102T191920Z-d4362bea912c452b9b8df657c9aeb6db/) retain the real catalogue, receipt, reader request/build/generation logs, mapping, negative requests and runtime hashes. Eight ordinary resources and six catalogue participants were composed. The actual Builds gateway Program registered all 13 Conversations adapters and its installed admission observations matched the generated policies. No test manually registered those adapters.

The in-process gateway registration test removes all hosted workers. No DistributedApplication Build/Start/Run, sidecar, container, unavailable dependency operation, Live/OQ verifier, publication, deployment, commit or push ran. Runtime availability and independent owner acceptance remain unmeasured. Before-binary measurement was unavailable and was never inferred. Original failures and chronological intermediate observations remain retained.

The following reproduces the checks with fresh producer imports and a fresh Platform identity. Run from the Conversations repository. The environment wrapper passes every producer-returned entry unchanged. The small retained helper only calls existing Builds mapping APIs; the real catalogue still uses Platform's private reader.

```bash
catalog_workspace=$(mktemp -d /tmp/conversations-catalog-rerun.XXXXXX)
conversation_root=/home/administrator/projects/hexalith/conversations
platform_root=/home/administrator/projects/hexalith/platform
owner_evidence="$conversation_root/docs/implementation/ext-conv-ai-1-internal-catalog-2026-11-02/owner-20261102T191823Z-0c74653d3f224300b704223078cf579d"
dotnet "$conversation_root/references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net10.0/Hexalith.Builds.Module.Cli.dll" prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --output json --workspace "$catalog_workspace/owner-prepared" > "$catalog_workspace/owner-prepare.json"
cat > "$catalog_workspace/run-prepared.py" <<'PYRUN'
import json, os, subprocess, sys
prepared = json.load(open(sys.argv[1]))
entries = prepared.get("buildPreparation", prepared)["environment"]
environment = os.environ.copy()
environment.update(entries)
raise SystemExit(subprocess.run(sys.argv[2:], env=environment).returncode)
PYRUN
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/owner-prepare.json" dotnet build tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Debug -m:1
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/owner-prepare.json" dotnet tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0/Hexalith.Conversations.Server.Tests.dll -result-xml "$catalog_workspace/server.xml"
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/owner-prepare.json" dotnet build tests/Hexalith.Conversations.Tests/Hexalith.Conversations.Tests.csproj -c Debug -m:1
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/owner-prepare.json" dotnet tests/Hexalith.Conversations.Tests/bin/Debug/net10.0/Hexalith.Conversations.Tests.dll -result-xml "$catalog_workspace/domain.xml"
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/owner-prepare.json" dotnet build tests/Hexalith.Conversations.Contracts.Tests/Hexalith.Conversations.Contracts.Tests.csproj -c Debug -m:1
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/owner-prepare.json" dotnet tests/Hexalith.Conversations.Contracts.Tests/bin/Debug/net10.0/Hexalith.Conversations.Contracts.Tests.dll -result-xml "$catalog_workspace/contracts.xml"
python3 -m pytest tests/tooling/test_release_tooling.py -q
mkdir "$catalog_workspace/helper"
cp "$owner_evidence/platform-prepare-helper/Preparation.csproj" "$owner_evidence/platform-prepare-helper/Program.cs" "$catalog_workspace/helper/"
dotnet build "$catalog_workspace/helper/Preparation.csproj" -c Debug -m:1
producer_hash=$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["buildPreparation"]["mappingHash"])' "$catalog_workspace/owner-prepare.json")
dotnet "$catalog_workspace/helper/bin/Debug/net10.0/Preparation.dll" "$catalog_workspace/owner-prepared/source-mapping/mapping.json" "$producer_hash" "$platform_root" "$catalog_workspace/platform-prepared" "$catalog_workspace/platform-prepare.json"
python3 - "$catalog_workspace" "$conversation_root" <<'PYINPUT'
import json, pathlib, sys
workspace = pathlib.Path(sys.argv[1])
producer = json.load(open(workspace / "owner-prepare.json"))["buildPreparation"]
platform = json.load(open(workspace / "platform-prepare.json"))
inputs = dict(producerMappingPath=str(workspace / "owner-prepared/source-mapping/mapping.json"),
    producerMappingHash=producer["mappingHash"], mappingPath=str(workspace / "platform-prepared/source-mapping/mapping.json"),
    mappingHash=platform["mappingHash"], evidenceRoot=str(pathlib.Path(sys.argv[2]) / "docs/implementation/ext-conv-ai-1-internal-catalog-2026-11-02/platform-composition"))
(workspace / "platform-inputs.json").write_text(json.dumps(inputs))
PYINPUT
cd "$platform_root"
python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/platform-prepare.json" dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -c Debug -m:1 -p:ConversationsCatalogSourceVerification=true
HEXALITH_CONVERSATIONS_CATALOG_INPUTS="$catalog_workspace/platform-inputs.json" python3 "$catalog_workspace/run-prepared.py" "$catalog_workspace/platform-prepare.json" dotnet tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0/Hexalith.Platform.Composition.Tests.dll -class Hexalith.Platform.Composition.Tests.ConversationsEnrollmentTests -result-xml "$catalog_workspace/platform.xml"
```

The opt-in test throws if explicit input is absent. Producer and combined-source warnings remain part of the preparation records. If the exact Platform-pinned Builds catalogue object is unavailable, preparation must refuse; do not change selected roots or bypass identity validation.
