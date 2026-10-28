"""Immutable package policy, imported only after an exact v2 contract selection."""
from types import MappingProxyType

from contained_reader import ContainedReader
import epic16_tooling as tooling
from epic16_trust_v2 import CONTRACT_PINS, PREDECESSOR_PINS, SCHEMA_PIN, TRUSTED_ENTRIES

BASE = '_bmad-output/planning-artifacts/epic-16-tooling-v2'
SCHEMA = '_bmad/schemas/epic-16-tooling-v2.schema.json'
# Independently frozen complete source inventory. The binding cannot add its own row.
TOOL_SOURCES = ('_bmad/schemas/.gitattributes', 'global.json', '_bmad/scripts/epic16_native_helper/Epic16.NativeHelper.csproj', '_bmad/scripts/epic16_native_helper/Program.cs', '_bmad/scripts/epic16_native_helper/TestConstruction.cs', '_bmad/scripts/epic16_native_helper/ProductMetadata.cs', '_bmad/scripts/epic16_native_helper/SignatureNames.cs', '_bmad/scripts/epic16_native_helper/ReadModelBatchCanonicalJson.cs', '_bmad/scripts/epic16_native_helper/README.md', '_bmad/scripts/epic16_native_helper/EventStore-LICENSE', '_bmad/scripts/epic16_native.py', '_bmad/scripts/epic16_tooling.py', '_bmad/scripts/epic16_measurements.py', '_bmad/scripts/epic16_domain.py', '_bmad/scripts/epic16_lifecycle.py', '_bmad/scripts/epic16_schedules.py', '_bmad/scripts/epic16_capture.py', '_bmad/scripts/epic16_faults.py', '_bmad/scripts/epic16_relationships.py', '_bmad/scripts/epic16_execution.py', '_bmad/scripts/epic16_products.py', '_bmad/scripts/tests/fixtures/epic16-protocol-goldens.json', '_bmad/scripts/tests/fixtures/epic16-domain-sources.json', '_bmad/scripts/tests/fixtures/epic16-contract-wire-goldens.json', '_bmad/scripts/tests/fixtures/epic16-contract-wire-provenance.json', '_bmad/scripts/epic16_trust.py', '_bmad/scripts/generate_story_record.py', '_bmad/scripts/contained_reader.py', '_bmad/scripts/evidence_boundary_current.py', '_bmad/scripts/authoring_outputs.py', '_bmad/scripts/config_utils.py', '_bmad/scripts/verify_story_completion_workflows.py', '_bmad/scripts/verify_story_16_1.py', '_bmad/scripts/verify_story_16_2.py', '_bmad/scripts/verify_story_16_3.py', '_bmad/schemas/epic-16-tooling-v2.schema.json', '.gitattributes', '.config/dotnet-tools.json', 'module/hexalith.module-manifest.v2.json', 'module/hosting-inventory.json', 'module/sm-2-checklist.md', 'docs/runbooks/current-change-validation.md', '_bmad/scripts/epic16_packages.py', '_bmad/scripts/epic16_trust_v2.py', '_bmad-output/planning-artifacts/epic-16-tooling-v2/README.md')
_ADDITIONAL_EXECUTABLES = ('_bmad/scripts/epic16_packages.py', '_bmad/scripts/epic16_trust_v2.py')
with ContainedReader(tooling._EXECUTION_ROOT) as _reader:
    _bytes = {**tooling._EXECUTION_BYTES,
              **{path: _reader.read(path) for path in _ADDITIONAL_EXECUTABLES}}
POLICY = tooling.PackagePolicy(
    base=BASE, binding=BASE + '/binding.json', schema=SCHEMA,
    contract_schema='hexalith.conversations.epic16-tooling-contract.v2',
    binding_schema='hexalith.conversations.epic16-tooling-binding.v2',
    contract_pins=CONTRACT_PINS, predecessor_pins=PREDECESSOR_PINS,
    schema_pin=SCHEMA_PIN, tool_sources=TOOL_SOURCES,
    execution_bytes=MappingProxyType(_bytes), trusted_entries=TRUSTED_ENTRIES)
