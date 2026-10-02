"""Keep the default `_bmad/scripts/tests` lane on current repository tooling.

Commit `0db6207`, the owner's accepted routine-change simplification (see
`docs/runbooks/current-change-validation.md`), retired these suites' subjects:

* It deleted the planning-authority preflight workflow and removed the V12
  lifecycle evidence gates from the routes (the lifecycle-gate preflight and the
  V9, V15, V16, V19-V21, V23, V27, V28, and V29 suites cannot pass at `HEAD`).
* It declared the V23-V29 publishers, resolver, and evidence verifier historical
  (the V25 and V26 suites still pass but are out of CI by that decision).
* It excluded the V8, V9, V15, V16, V18, architecture planning-authority, and
  live preservation-manifest binding checks from CI. The V18 suite still passes;
  the preservation-traceability rc2 suite rebinds published Epic 6 evidence to the
  live routes and SDK pin, so any route or SDK change turns it red.

They stay in Git for historical reproduction: name a file explicitly, for example
`python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py`.
Directory collection leaves them out, so they are not reported as skipped tests.
"""

collect_ignore = [
    "test_check_lifecycle_gate_preflight.py",
    "test_generate_preservation_traceability_manifest.py",
    "test_publish_story_7_1_committed_candidate_test_correction.py",
    "test_publish_story_7_1_entry_authority.py",
    "test_publish_story_7_1_lifecycle_evidence_authority.py",
    "test_publish_story_7_1_preservation_evidence_successor.py",
    "test_publish_story_7_1_successor_authorities.py",
    "test_publish_v15_planning_tooling_environment.py",
    "test_publish_v16_planning_tooling_lifecycle.py",
    "test_publish_v18_package_environment_authority.py",
    "test_publish_v28_five_root_gitlink_authority.py",
    "test_publish_v29_post_v28_root_gitlink_authority.py",
    "test_publish_v9_planning_authority.py",
    "test_resolve_current_planning_authority.py",
    "test_verify_evidence_boundary.py",
]
