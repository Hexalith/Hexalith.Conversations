"""Executable regression guards for deferred final-record reporting scopes."""

import ast
import subprocess
import sys

import pytest

from test_generate_story_record import (
    GIT_ENV, SCRIPT, bundled, codes, historical_fixture, insert_generated_block,
    load_generator, run_git, umbrella, write_trx,
)


CLI_OPTIONS = [
    ("--repository", ["/fixture"], "repository", "/fixture"),
    ("--story", ["story.md"], "story", "story.md"),
    ("--baseline", ["a" * 40], "baseline", "a" * 40),
    ("--candidate", ["b" * 40], "candidate", "b" * 40),
    ("--test-results", ["Fixture=results.trx"], "test_results", ["Fixture=results.trx"]),
    ("--submodule", ["references/Example"], "submodule", ["references/Example"]),
    ("--require-remote", ["references/Example"], "require_remote", ["references/Example"]),
    ("--format", ["bundle"], "format", "bundle"),
    ("--historical", [], "historical", True),
    ("--verify-record-sha256", ["c" * 64], "verify_record_sha256", "c" * 64),
]


@pytest.mark.parametrize("flag,arguments,destination,expected", CLI_OPTIONS)
def test_registered_cli_option_parses_its_contracted_value(flag, arguments, destination, expected):
    module = load_generator()
    parser = module.build_parser()
    assert flag in parser.format_help()
    assert getattr(parser.parse_args([flag, *arguments]), destination) == expected


@pytest.mark.parametrize("flag,arguments,destination,expected", CLI_OPTIONS)
def test_cli_guard_detects_removed_registration_despite_retained_source_string(
    flag, arguments, destination, expected,
):
    module = load_generator()
    tree = ast.parse(SCRIPT.read_text(encoding="utf-8"))
    definition = next(node for node in tree.body if isinstance(node, ast.FunctionDef)
                      and node.name == "build_parser")
    retained = []
    removed = 0
    for statement in definition.body:
        if (isinstance(statement, ast.Expr) and isinstance(statement.value, ast.Call)
                and isinstance(statement.value.func, ast.Attribute)
                and statement.value.func.attr == "add_argument"
                and any(isinstance(arg, ast.Constant) and arg.value == flag
                        for arg in statement.value.args)):
            # Keep the very string the old source-substring guard would accept.
            retained.append(ast.Expr(value=ast.Constant(value=flag)))
            removed += 1
        else:
            retained.append(statement)
    assert removed == 1
    definition.body = retained
    mutant = ast.fix_missing_locations(ast.Module(body=[definition], type_ignores=[]))
    assert repr(flag) in ast.unparse(mutant)
    exec(compile(mutant, str(SCRIPT), "exec"), module.__dict__)
    parser = module.build_parser()
    assert flag not in parser.format_help()
    with pytest.raises(module.GateError):
        parser.parse_args([flag, *arguments])


def test_native_cli_help_exposes_every_contracted_option():
    result = subprocess.run([sys.executable, str(SCRIPT), "--help"],
                            capture_output=True, text=True, env=GIT_ENV, timeout=30)
    assert result.returncode == 0, result.stderr
    assert not result.stderr
    for flag, *_ in CLI_OPTIONS:
        assert flag in result.stdout


@pytest.mark.parametrize("status,mutation", [
    ("A", "edit"), ("A", "delete"),
    ("M", "edit"), ("M", "delete"), ("D", "recreate"),
])
def test_file_annotations_keep_committed_status_despite_allowed_output_dirt(
    umbrella, status, mutation,
):
    module = load_generator()
    repository = umbrella["repository"]
    target = repository / umbrella["story"]
    baseline = umbrella["baseline"]
    candidate = umbrella["candidate"]
    if status != "A":
        baseline = candidate
        if status == "D":
            target.unlink()
        else:
            target.write_text(target.read_text() + "\ncommitted change\n")
        run_git(repository, "add", umbrella["story"])
        run_git(repository, "commit", "-m", "test: record fixture status")
        candidate = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    if mutation == "delete":
        target.unlink()
    else:
        target.write_text("uncommitted output bytes\n")
    extra_output = repository / "extra-output.md"
    extra_output.write_text("untracked output\n")
    blockers = []
    entries = module.derive_file_list(
        repository, baseline, candidate, ["references/Example"], [],
        {umbrella["story"], "extra-output.md"}, set(), blockers, [], head=candidate,
    )
    assert not blockers
    actual = {entry["path"]: entry for entry in entries}
    assert actual[umbrella["story"]]["status"] == status
    assert actual[umbrella["story"]]["annotation"] == {
        "A": "new", "M": "modified", "D": "deleted",
    }[status]
    assert actual["extra-output.md"]["status"] == "M"


@pytest.mark.parametrize("changed", [False, True])
@pytest.mark.parametrize("declared", [False, True])
def test_promotion_report_separates_declared_scope_from_measured_changes(umbrella, changed, declared):
    repository = umbrella["repository"]
    path = "references/Example"
    if changed:
        run_git(repository / path, "commit", "--allow-empty", "-m", "test: advance fixture")
        run_git(repository, "add", path)
        run_git(repository, "commit", "-m", "test: promote fixture")
        write_trx(repository / umbrella["artifact"])
    candidate = run_git(repository, "rev-parse", "HEAD").stdout.strip()
    story = repository / umbrella["story"]
    story.write_text(story.read_text().replace(
        "baseline_commit:", f"file_list_commit: '{candidate}'\nbaseline_commit:",
    ))
    args = ("--submodule", path) if declared else ()
    code, bundle = bundled(umbrella, *args)
    assert code == 0, bundle["document"]["blockers"]
    promotions = bundle["document"]["promotions"]
    if not changed and not declared:
        assert promotions == []
        return
    assert promotions[0]["declared"] is declared
    assert promotions[0]["changed_in_range"] is changed
    assert "authorship is not inferred" in bundle["markdown"]
    rows = load_generator().parse_recorded_promotions(bundle["markdown"])
    assert rows[0]["declared"] is declared
    assert rows[0]["changed_in_range"] is changed
    insert_generated_block(umbrella, bundle["markdown"])
    code, document = historical_fixture(umbrella)
    assert code == 0, document["blockers"]
    original = "yes" if changed else "no"
    for invalid in ("no" if changed else "yes", "unknown", ""):
        tampered = bundle["markdown"].replace(f" | {original} |\n", f" | {invalid} |\n")
        assert tampered != bundle["markdown"]
        insert_generated_block(umbrella, tampered)
        code, document = historical_fixture(umbrella)
        assert code == 1
        assert "RECORD_CONTENT_DRIFT" in codes(document)
    # Accepted five-column layouts remain readable; Git still derives their range state.
    lines = []
    for line in bundle["markdown"].splitlines():
        if line.startswith("| ") and (path in line or "Declared scope" in line
                                      or line == "| --- | --- | --- | --- | --- | --- |"):
            line = line.rsplit("|", 2)[0] + "|"
        lines.append(line)
    insert_generated_block(umbrella, "\n".join(lines) + "\n")
    code, document = historical_fixture(umbrella)
    assert code == 0, document["blockers"]
    assert document["promotions"][0]["changed_in_range"] is changed
