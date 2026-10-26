from pathlib import Path
import json,subprocess,sys,os,tempfile,hashlib
root=Path.cwd();out=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-12/implementation';frozen=json.loads((out/'packaging-source.json').read_bytes())['files'];temp=Path(tempfile.mkdtemp(prefix='epic16-r12-packaging-'));(out/'packaging-test-workspaces.json').write_text(json.dumps(dict(root=str(temp)),indent=2)+'\n');commands=[]
for name,selector,files in [('packaging','binding_inventory_requires_exact_order_and_cardinality or contract_schema_predecessor_and_source_pins_are_independent or package_preparation_preserves_all_obligations_and_has_no_entry or clean_different_repository_cannot_claim_unexecuted_bound_source_bytes or runtime_evaluator_rejects_clean_bound_sources_that_differ_from_executing_tree or atomic_committed_publication_and_lifecycle_pass or round6_genuine_native_capture_publication_survives_fresh_checkout or round7_retained_pe_has_binary_attributes_and_safe_normal_git_diff or disposable_git_checkout_preserves_bound_root_bytes_with_autocrlf or rehashed_checkout_policy_cannot_redefine_pinned_bytes',['_bmad/scripts/tests/test_epic16_tooling.py','_bmad/scripts/tests/test_verify_epic_16_adoption_v2.py']),('legacy','story_111 or story_112 or standalone_generator',['_bmad/scripts/tests/test_generate_story_record.py','_bmad/scripts/tests/test_epic16_tooling.py'])]:
 for p,h in frozen.items():assert hashlib.sha256((root/p).read_bytes()).hexdigest()==h,p
 command=['python3','/tmp/epic16-r12-run.py',name,selector,*files]
 result=subprocess.run(command,cwd=root,env=dict(os.environ,EPIC16_BASETEMP=str(temp/name)))
 record=json.loads((out/name/'command.json').read_bytes());commands.append(record);(out/'packaging-test-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
 for p,h in frozen.items():assert hashlib.sha256((root/p).read_bytes()).hexdigest()==h,p
 if result.returncode:sys.exit(result.returncode)
