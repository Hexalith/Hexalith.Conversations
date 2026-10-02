"""Keep the default `_bmad/scripts/tests` lane on current repository tooling.

Commit `0db6207` (the owner's routine-change simplification, see
`docs/runbooks/current-change-validation.md`) deleted the planning-authority
preflight workflow, removed the V12 lifecycle evidence gates from the routes,
declared the V23-V29 publishers, resolver, and evidence verifier historical, and
excluded the V9, V15, V16, and V18 authority classes from CI. The suites below
test those retired subjects; most cannot pass at `HEAD`.

They stay in Git for historical reproduction: name a file explicitly, for example
`python3 -m pytest -q _bmad/scripts/tests/test_verify_evidence_boundary.py`.
Directory collection leaves them out, so they are not reported as skipped tests.
"""

collect_ignore = [
    "test_check_lifecycle_gate_preflight.py",
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
