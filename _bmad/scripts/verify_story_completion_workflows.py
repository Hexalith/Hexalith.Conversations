#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = ["jsonschema>=4.0"]
# ///
"""Verify that every governed completion route carries the one Story 7.3 completion gate.

Story 7.3 governs four completion routes, each installed in `.agents/skills` and
`.claude/skills`, plus the in-memory `render_skill.py` renders ("twins") of the
three routes that are rendered before use. Each of those eleven surfaces must
carry one byte-identical, marker-delimited completion-gate block inside its
final-record gate span, ahead of the route's `review`/`done` transition.

* `AC-7.3-01` proves presence and placement: a surface without a complete block,
  or whose block lacks the generator invocation, is `WORKFLOW_INTEGRATION_MISSING`;
  a block outside the gate span, after the transition, or duplicated is
  `WORKFLOW_INTEGRATION_DISPLACED`. Only the block counts: generator text anywhere
  else in the file never satisfies the check.
* `AC-7.3-02` proves parity: every surface's block bytes must be identical and
  carry the exact command, blocker branch, halt, output-path, and inserted-marker
  clauses. Any difference is `SURFACE_PARITY_DRIFT`.

The verifier reads the installed working tree and renders the twins in memory. It
never writes into `_bmad/render/`, commits, or traverses a submodule; its only
write is the declared acceptance-result output. Exit codes: `0` PASS, `1` FAIL,
`2` BLOCKED.
"""

from __future__ import annotations

import hashlib
import importlib.util
import json
import os
import re
import secrets
import shlex
import subprocess
import sys
from pathlib import Path, PurePosixPath
from typing import Any, NamedTuple, NoReturn, Sequence

# Installed scripts are consumer files, not a location for interpreter caches.
sys.dont_write_bytecode = True

TOOLING_DIRECTORY = Path(__file__).resolve().parent
SCHEMA_DIRECTORY = TOOLING_DIRECTORY.parent / "schemas"
RENDERER_PATH = TOOLING_DIRECTORY / "render_skill.py"
VERIFIER_PATH = "_bmad/scripts/verify_story_completion_workflows.py"
ACCEPTANCE_SCHEMA_FILE = "v9-acceptance-result-v1.schema.json"
CONTRACT_SCHEMA_FILE = "v9-story-contract-v1.schema.json"
ACCEPTANCE_SCHEMA_VERSION = "hexalith.conversations.acceptance-result.v1"
CONTRACT_SCHEMA_VERSION = "hexalith.conversations.story-contract.v1"
FAILURE_SCHEMA_VERSION = "hexalith.conversations.story-completion-workflow-verifier-failure.v1"
STORY_ID = "7.3"
PRESENCE_SCENARIO = "AC-7.3-01"
PARITY_SCENARIO = "AC-7.3-02"
SCENARIOS = (PRESENCE_SCENARIO, PARITY_SCENARIO)
OPTIONS = ("--repository", "--contract", "--scenario", "--output")
GIT_TIMEOUT_SECONDS = 20
MESSAGE_LIMIT = 1900
GIT_ENVIRONMENT_OVERRIDES = (
    "GIT_DIR",
    "GIT_WORK_TREE",
    "GIT_INDEX_FILE",
    "GIT_OBJECT_DIRECTORY",
    "GIT_COMMON_DIR",
    "GIT_ALTERNATE_OBJECT_DIRECTORIES",
    "GIT_NAMESPACE",
)

BLOCK_BEGIN = "<!-- STORY-COMPLETION-GATE:BEGIN v1 -->"
BLOCK_END = "<!-- STORY-COMPLETION-GATE:END v1 -->"


class Route(NamedTuple):
    """One governed completion route and the anchors of its final-record gate span.

    `gate`, `follower`, and `transition` are the markers of the
    `StoryFinalRecordGenerationValidationTest` contract table: the block must lie
    inside [gate, follower) and precede every occurrence of `transition`.
    """

    path: str
    gate: str
    follower: str
    transition: str
    rendered_skill: str | None


# The owner rebound the frozen Story 7.3 inventory one-for-one to these routes:
# bmad-build 05/oneshot replace bmad-quick-dev 05/oneshot, bmad-build-auto
# step-04 replaces bmad-dev-story step 9, and bmad-code-review is unchanged.
ROUTES = (
    Route(
        "bmad-build/step-05-present.md",
        "### Final Record Generation Gate",
        "### Mark Spec Done",
        "Change `{spec_file}` status to `done`",
        "bmad-build",
    ),
    Route(
        "bmad-build/step-oneshot.md",
        "### Final Record Generation Gate",
        "### Finalize Spec",
        "Set `status: 'done'`",
        "bmad-build",
    ),
    Route(
        "bmad-build-auto/step-04-review.md",
        "### Final Record Generation Gate",
        "## Finalize",
        "write `status: done`",
        "bmad-build-auto",
    ),
    Route(
        "bmad-code-review/steps/step-04-present.md",
        "#### Final record generation gate",
        "#### Determine new status based on review outcome",
        "`record_gate_failed` is not true",
        None,
    ),
)
SKILL_TREES = (".agents/skills", ".claude/skills")
# The Claude Code tree is the one this repository's agents render and follow.
TWIN_TREE = ".claude/skills"
TWIN_PREFIX = "render:"
BODY_PATHS = tuple(sorted(f"{tree}/{route.path}" for tree in SKILL_TREES for route in ROUTES))
TWIN_LABELS = tuple(
    sorted(f"{TWIN_PREFIX}{TWIN_TREE}/{route.path}" for route in ROUTES if route.rendered_skill)
)

GENERATOR_COMMAND = (
    "uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py "
    "--repository . --contract <contract> --format bundle --output-json <json> "
    "--output-markdown <md>"
)
VERIFY_COMMAND = (
    "uv run --frozen --no-sync python3 _bmad/scripts/generate_story_record.py "
    "--repository . --contract <contract> --verify-inserted-record {spec_file}"
)
# The contract every surface's block must state, beyond the generator invocation.
REQUIRED_CLAUSES = (
    ("applicability", "`_bmad-output/planning-artifacts/v9/story-contracts/<story-id>.json`"),
    ("no-opt-out", "the spec cannot opt out"),
    ("output-paths", "`finalRecord.paths`"),
    ("exit-zero", "Require exit `0`"),
    ("exact-summary", "`finalRecord.summary`"),
    ("record-only-commit", "record-only commit"),
    ("record-begin-marker", "`<!-- STORY-FINAL-RECORD:BEGIN -->`"),
    ("record-end-marker", "`<!-- STORY-FINAL-RECORD:END -->`"),
    ("inserted-record-verification", VERIFY_COMMAND),
    ("blocker-state", "to `in-progress`"),
    ("no-transition", "never write `review` or `done`"),
    ("blocker-report", "Report the exact command, its exit, and every stable blocker code"),
    ("halt", "then HALT"),
    ("no-ci-claim", "no CI job or hook enforces it"),
)
# Affirmative CI-enforcement wording. Nothing in this repository runs the gate
# automatically, so a block that claims otherwise misstates the limitation.
CI_CLAIM = re.compile(
    r"(?i)(?:enforced by (?:the )?CI|CI (?:enforces|runs|blocks|requires)|required status check)"
)

FAIL_CODES = frozenset(
    {"WORKFLOW_INTEGRATION_MISSING", "WORKFLOW_INTEGRATION_DISPLACED", "SURFACE_PARITY_DRIFT"}
)
BLOCKED_CODES = frozenset(
    {
        "ARGUMENT_INVALID",
        "CONTRACT_UNSUPPORTED",
        "GIT_UNAVAILABLE",
        "CANDIDATE_UNRESOLVABLE",
        "SURFACE_UNREADABLE",
        "RENDER_UNAVAILABLE",
        "SCHEMA_UNAVAILABLE",
        "SCHEMA_VALIDATOR_UNAVAILABLE",
        "OUTPUT_WRITE_FAILED",
        "INTERNAL_ERROR",
    }
)


class Blocked(Exception):
    """Stop with one environmental or invocation blocker (exit 2)."""

    def __init__(self, code: str, subject: str, message: str) -> None:
        super().__init__(message)
        self.finding = finding(code, subject, message)


class Surface(NamedTuple):
    label: str
    text: str
    route: Route


def clean_text(value: str, limit: int = MESSAGE_LIMIT) -> str:
    text = value.encode("utf-8", errors="backslashreplace").decode("utf-8")
    text = "".join("?" if ord(character) < 0x20 else character for character in text)
    if len(text) > limit:
        text = text[: limit - 3] + "..."
    return text or "?"


def finding(code: str, subject: str, message: str) -> dict[str, str]:
    if code not in FAIL_CODES | BLOCKED_CODES:  # pragma: no cover - programming error guard
        raise ValueError(f"undocumented verifier code: {code}")
    return {"code": code, "subject": clean_text(subject, 400), "message": clean_text(message)}


def sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def safe_relative_path(value: str) -> str | None:
    """A normalized slash-separated repository-relative path, or None."""
    if not value or "\\" in value or value.startswith("/") or "\0" in value:
        return None
    parts = PurePosixPath(value).parts
    if not parts or any(part in ("", ".", "..") for part in parts):
        return None
    normalized = PurePosixPath(*parts).as_posix()
    return normalized if normalized == value else None


def parse_arguments(raw: Sequence[str]) -> dict[str, str]:
    """Parse the exact option set; no prefix abbreviation, no repetition, no positional."""
    values: dict[str, str] = {}
    tokens = list(raw)
    index = 0
    while index < len(tokens):
        token = tokens[index]
        index += 1
        name, separator, inline = token.partition("=")
        if name not in OPTIONS:
            raise Blocked(
                "ARGUMENT_INVALID",
                "argv",
                "the verifier accepts exactly " + ", ".join(OPTIONS) + ", each once with a value",
            )
        if separator:
            value = inline
        elif index < len(tokens) and not tokens[index].startswith("--"):
            value = tokens[index]
            index += 1
        else:
            raise Blocked("ARGUMENT_INVALID", name, f"{name} requires a value")
        if name in values or not value:
            raise Blocked(
                "ARGUMENT_INVALID", name, f"{name} must appear once with a non-empty value"
            )
        values[name] = value
    missing = [name for name in OPTIONS if name not in values]
    if missing:
        raise Blocked("ARGUMENT_INVALID", missing[0], f"{missing[0]} is required")
    for name in ("--contract", "--output"):
        if safe_relative_path(values[name]) is None:
            raise Blocked(
                "ARGUMENT_INVALID", name, f"{name} must be a normalized repository-relative path"
            )
    if values["--scenario"] not in SCENARIOS:
        raise Blocked(
            "ARGUMENT_INVALID",
            "--scenario",
            "the verifier proves only " + ", ".join(SCENARIOS),
        )
    if not values["--output"].endswith(".json"):
        raise Blocked("ARGUMENT_INVALID", "--output", "--output must name a .json result file")
    return values


def git(repository: Path, *arguments: str) -> subprocess.CompletedProcess[bytes]:
    environment = dict(os.environ)
    for name in GIT_ENVIRONMENT_OVERRIDES:
        environment.pop(name, None)
    try:
        return subprocess.run(
            ["git", "-c", "core.quotepath=false", "-C", str(repository), *arguments],
            capture_output=True,
            check=False,
            env=environment,
            timeout=GIT_TIMEOUT_SECONDS,
        )
    except FileNotFoundError:
        raise Blocked("GIT_UNAVAILABLE", "git", "git is not available on PATH") from None
    except subprocess.TimeoutExpired:
        raise Blocked("GIT_UNAVAILABLE", "git", "a git command timed out") from None


def resolve_repository(value: str) -> tuple[Path, str]:
    """The repository root and its committed `HEAD` candidate."""
    repository = Path(value).expanduser()
    if not repository.is_dir():
        raise Blocked("ARGUMENT_INVALID", "--repository", "--repository is not a directory")
    repository = repository.resolve()
    top = git(repository, "rev-parse", "--show-toplevel")
    root = Path(top.stdout.decode("utf-8", "replace").strip())
    if top.returncode != 0 or root.resolve() != repository:
        raise Blocked(
            "ARGUMENT_INVALID",
            "--repository",
            "--repository must name the root of a Git repository",
        )
    head = git(repository, "rev-parse", "--verify", "-q", "HEAD^{commit}")
    candidate = head.stdout.decode("utf-8", "replace").strip()
    if head.returncode != 0 or re.fullmatch(r"[0-9a-f]{40}", candidate) is None:
        raise Blocked(
            "CANDIDATE_UNRESOLVABLE", "HEAD", "HEAD does not resolve to a committed candidate"
        )
    return repository, candidate


def contained_file(repository: Path, relative: str) -> Path | None:
    """A regular, non-symlink file lexically and physically inside the repository."""
    path = repository / relative
    try:
        if path.is_symlink() or not path.is_file():
            return None
        path.resolve(strict=True).relative_to(repository)
    except (OSError, ValueError):
        return None
    return path


def load_validators() -> tuple[Any, Any]:
    """The tooling's own schema copies: never the evaluated repository's."""
    try:
        import jsonschema  # noqa: PLC0415 - optional until a result is emitted
    except ImportError:
        raise Blocked(
            "SCHEMA_VALIDATOR_UNAVAILABLE",
            "jsonschema",
            "the jsonschema package is unavailable; run through `uv run --frozen --no-sync`",
        ) from None
    validators = []
    for name in (CONTRACT_SCHEMA_FILE, ACCEPTANCE_SCHEMA_FILE):
        try:
            schema = json.loads((SCHEMA_DIRECTORY / name).read_text(encoding="utf-8"))
            jsonschema.Draft202012Validator.check_schema(schema)
        except (OSError, ValueError, jsonschema.SchemaError):
            raise Blocked(
                "SCHEMA_UNAVAILABLE", f"_bmad/schemas/{name}", "a tooling schema is unavailable"
            ) from None
        validators.append(jsonschema.Draft202012Validator(schema))
    return validators[0], validators[1]


def reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    document: dict[str, Any] = {}
    for key, value in pairs:
        if key in document:
            raise ValueError("duplicate JSON object key")
        document[key] = value
    return document


def reject_constant(value: str) -> NoReturn:
    raise ValueError("non-finite JSON number")


def load_scenario(
    repository: Path, options: dict[str, str], contract_validator: Any
) -> dict[str, Any]:
    """The declared scenario, required to be this exact verifier invocation."""
    contract_path = options["--contract"]
    path = contained_file(repository, contract_path)
    if path is None:
        raise Blocked(
            "CONTRACT_UNSUPPORTED", contract_path, "the story contract is not a readable file"
        )
    try:
        contract = json.loads(
            path.read_bytes().decode("utf-8"),
            object_pairs_hook=reject_duplicate_keys,
            parse_constant=reject_constant,
        )
    except (OSError, UnicodeDecodeError, ValueError):
        raise Blocked(
            "CONTRACT_UNSUPPORTED",
            contract_path,
            "the story contract is not well-formed UTF-8 JSON",
        ) from None
    if (
        not isinstance(contract, dict)
        or contract.get("schemaVersion") != CONTRACT_SCHEMA_VERSION
        or not contract_validator.is_valid(contract)
    ):
        raise Blocked(
            "CONTRACT_UNSUPPORTED", contract_path, "the story contract violates its closed schema"
        )
    if contract["storyId"] != STORY_ID:
        raise Blocked(
            "CONTRACT_UNSUPPORTED",
            contract_path,
            f"the contract is for story {contract['storyId']}; "
            f"this verifier proves Story {STORY_ID}",
        )
    scenario = next(
        (item for item in contract["scenarios"] if item["id"] == options["--scenario"]), None
    )
    if scenario is None:
        raise Blocked(
            "CONTRACT_UNSUPPORTED",
            options["--scenario"],
            "the contract does not declare this scenario",
        )
    try:
        tokens = shlex.split(scenario["command"])
    except ValueError:
        tokens = []
    declared: dict[str, str] = {}
    if len(tokens) == 2 + 2 * len(OPTIONS) and tokens[:2] == ["python3", VERIFIER_PATH]:
        declared = dict(zip(tokens[2::2], tokens[3::2]))
    if set(declared) != set(OPTIONS) or declared.get("--repository") != ".":
        raise Blocked(
            "CONTRACT_UNSUPPORTED",
            scenario["id"],
            "the declared scenario command is not this verifier's invocation",
        )
    for name in ("--contract", "--scenario", "--output"):
        if declared[name] != options[name]:
            raise Blocked(
                "ARGUMENT_INVALID",
                name,
                f"{name} differs from the contract's declared command for {scenario['id']}",
            )
    return scenario


def read_bodies(repository: Path) -> dict[str, bytes]:
    bodies: dict[str, bytes] = {}
    for relative in BODY_PATHS:
        path = contained_file(repository, relative)
        try:
            if path is None:
                raise OSError("not a regular file")
            content = path.read_bytes()
            content.decode("utf-8")
        except (OSError, UnicodeDecodeError):
            raise Blocked(
                "SURFACE_UNREADABLE", relative, "a governed workflow body is missing or unreadable"
            ) from None
        bodies[relative] = content
    return bodies


def load_renderer() -> Any:
    """Load the tooling's own `render_skill.py` without running it."""
    if str(TOOLING_DIRECTORY) not in sys.path:
        sys.path.insert(0, str(TOOLING_DIRECTORY))
    spec = importlib.util.spec_from_file_location("story_completion_render_skill", RENDERER_PATH)
    if spec is None or spec.loader is None:
        raise ImportError(f"cannot load {RENDERER_PATH}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def render_in_memory(module: Any, project_root: Path, skill_dir: Path) -> dict[str, str]:
    """Reproduce `render_skill.render()` up to, but excluding, its publication.

    The generation destination is computed exactly as the renderer computes it,
    so snapshot references resolve identically, but nothing is created or
    written: `_bmad/render/` is never touched.
    """
    project_root = project_root.resolve(strict=True)
    skill_dir = skill_dir.resolve(strict=True)
    skill_dir.relative_to(project_root)
    if not (project_root / "_bmad").is_dir():
        raise module.RenderError(f"project root does not contain _bmad/: {project_root}")
    sources = module._load_sources(skill_dir)
    central = module.load_central_config(project_root)
    has_customization = any(module._CUSTOM_TOKEN.search(content) for content in sources.values())
    defaults = (
        module.load_toml(skill_dir / "customize.toml", required=True) if has_customization else None
    )
    customization = module.load_customization(project_root, skill_dir) if has_customization else {}
    replacements, input_values = module._resolve_replacements(
        sources, central, customization, defaults, project_root
    )
    source_hashes = {
        name: module._hash_bytes(content.encode("utf-8")) for name, content in sources.items()
    }
    root_hash = module._hash_bytes(str(project_root).encode("utf-8"))[:12]
    slug = re.sub(r"[^a-z0-9]+", "-", project_root.name.lower()).strip("-") or "project"
    slug = slug[:80].rstrip("-") or "project"
    identity = {
        "project_root": str(project_root),
        "renderer_sha256": module._hash_bytes(Path(module.__file__).read_bytes()),
        "resolved_values": input_values,
        "source_sha256": source_hashes,
    }
    generation_hash = module._hash_bytes(module._canonical_json(identity))[:20]
    destination = (
        project_root / "_bmad" / "render" / skill_dir.name / f"{slug}-{root_hash}" / generation_hash
    )
    return module._render_sources(sources, replacements, destination)


def render_twins(repository: Path) -> dict[str, str]:
    """The in-memory render of every rendered governed route, keyed by twin label."""
    module = load_renderer()
    twins: dict[str, str] = {}
    rendered: dict[str, dict[str, str]] = {}
    for route in ROUTES:
        if route.rendered_skill is None:
            continue
        skill = route.rendered_skill
        if skill not in rendered:
            rendered[skill] = render_in_memory(module, repository, repository / TWIN_TREE / skill)
        name = route.path.split("/", 1)[1]
        twins[f"{TWIN_PREFIX}{TWIN_TREE}/{route.path}"] = rendered[skill][name]
    return twins


def collect_surfaces(repository: Path) -> tuple[list[Surface], dict[str, bytes]]:
    bodies = read_bodies(repository)
    routes = {route.path: route for route in ROUTES}
    surfaces = [
        Surface(path, content.decode("utf-8"), routes[path.split("/skills/", 1)[1]])
        for path, content in bodies.items()
    ]
    try:
        twins = render_twins(repository)
    except Exception as error:  # noqa: BLE001 - any render failure blocks, never passes
        raise Blocked(
            "RENDER_UNAVAILABLE",
            "render_skill.py",
            f"the in-memory render of a governed route failed ({type(error).__name__})",
        ) from None
    if sorted(twins) != list(TWIN_LABELS):
        raise Blocked("RENDER_UNAVAILABLE", "render_skill.py", "the render twins are incomplete")
    for label in TWIN_LABELS:
        twin = twins[label]
        if not isinstance(twin, str):
            raise Blocked("RENDER_UNAVAILABLE", label, "a render twin is not text")
        surfaces.append(Surface(label, twin, routes[label.split("/skills/", 1)[1]]))
    return surfaces, bodies


def occurrences(text: str, token: str) -> list[int]:
    found = []
    index = text.find(token)
    while index >= 0:
        found.append(index)
        index = text.find(token, index + len(token))
    return found


def locate_block(text: str) -> tuple[str, int, int, str]:
    """(state, start, end, detail); state is `present`, `missing`, or `displaced`."""
    begins = occurrences(text, BLOCK_BEGIN)
    ends = occurrences(text, BLOCK_END)
    if not begins:
        return "missing", -1, -1, "the surface carries no completion-gate block"
    if len(begins) != len(ends) or any(end < begin for begin, end in zip(begins, ends)):
        return "missing", -1, -1, "the completion-gate block is incomplete or out of order"
    if len(begins) > 1:
        return "displaced", -1, -1, f"{len(begins)} completion-gate blocks; exactly one is allowed"
    return "present", begins[0], ends[0] + len(BLOCK_END), ""


def placement_problem(text: str, route: Route, start: int, end: int) -> str | None:
    """Why the block does not gate the route's transition, or None when it does."""
    gate = text.find(route.gate)
    if gate < 0:
        return f"the gate span marker {route.gate!r} is absent"
    follower = text.find(route.follower, gate + len(route.gate))
    if follower < 0:
        return f"the gate span follower {route.follower!r} is absent"
    if not gate < start < end <= follower:
        return "the block lies outside the final-record gate span"
    transition = text.find(route.transition)
    if transition < 0:
        return f"the lifecycle transition {route.transition!r} is absent"
    if transition < follower:
        return "a lifecycle transition precedes the end of the gate span"
    return None


def presence_checks(surfaces: list[Surface]) -> tuple[list[dict[str, str]], list[tuple[str, str]]]:
    """AC-7.3-01: one complete in-span block carrying the generator invocation."""
    findings: list[dict[str, str]] = []
    rows: list[tuple[str, str]] = []
    for surface in surfaces:
        state, start, end, detail = locate_block(surface.text)
        present = state == "present"
        invokes = present and GENERATOR_COMMAND in surface.text[start:end]
        problem = placement_problem(surface.text, surface.route, start, end) if present else None
        if state == "missing":
            findings.append(finding("WORKFLOW_INTEGRATION_MISSING", surface.label, detail))
        elif state == "displaced":
            findings.append(finding("WORKFLOW_INTEGRATION_DISPLACED", surface.label, detail))
        if present and not invokes:
            findings.append(
                finding(
                    "WORKFLOW_INTEGRATION_MISSING",
                    surface.label,
                    "the completion-gate block does not invoke the v2 generator",
                )
            )
        if problem is not None:
            findings.append(finding("WORKFLOW_INTEGRATION_DISPLACED", surface.label, problem))
        for name, passed in (
            ("completion-gate-block-present", present),
            ("generator-invocation-in-block", invokes),
            ("block-in-gate-span-before-transition", present and problem is None),
        ):
            rows.append((f"{surface.label}::{name}", "PASS" if passed else "FAIL"))
    return findings, rows


def parity_checks(surfaces: list[Surface]) -> tuple[list[dict[str, str]], list[tuple[str, str]]]:
    """AC-7.3-02: byte-identical blocks that state the whole gate contract."""
    findings: list[dict[str, str]] = []
    rows: list[tuple[str, str]] = []
    blocks: dict[str, str | None] = {}
    for surface in surfaces:
        state, start, end, _ = locate_block(surface.text)
        blocks[surface.label] = surface.text[start:end] if state == "present" else None
    # The reference is the most common block, ties broken by surface order, so one
    # drifted surface is named rather than every surface that agrees with the rest.
    present = [block for block in blocks.values() if block is not None]
    reference = max(
        present, key=lambda block: (present.count(block), -present.index(block)), default=None
    )
    for surface in surfaces:
        block = blocks[surface.label]
        equal = block is not None and block == reference
        if block is None:
            findings.append(
                finding(
                    "SURFACE_PARITY_DRIFT",
                    surface.label,
                    "the surface has no single complete completion-gate block to compare",
                )
            )
        elif not equal:
            findings.append(
                finding(
                    "SURFACE_PARITY_DRIFT",
                    surface.label,
                    "the completion-gate block bytes differ from the other governed surfaces",
                )
            )
        clauses = (("generator-invocation", GENERATOR_COMMAND), *REQUIRED_CLAUSES)
        missing = [] if block is None else [name for name, clause in clauses if clause not in block]
        claims_ci = block is not None and CI_CLAIM.search(block) is not None
        if missing:
            findings.append(
                finding(
                    "SURFACE_PARITY_DRIFT",
                    surface.label,
                    "the completion-gate block lacks required clauses: " + ", ".join(missing),
                )
            )
        if claims_ci:
            findings.append(
                finding(
                    "SURFACE_PARITY_DRIFT",
                    surface.label,
                    "the completion-gate block claims CI enforcement that does not exist",
                )
            )
        states_contract = block is not None and not missing and not claims_ci
        for name, passed in (
            ("block-bytes-identical-across-surfaces", equal),
            ("block-states-gate-contract", states_contract),
        ):
            rows.append((f"{surface.label}::{name}", "PASS" if passed else "FAIL"))
    return findings, rows


def evaluate(
    repository: Path, candidate: str, scenario: dict[str, Any]
) -> tuple[list[dict[str, str]], list[tuple[str, str]], list[dict[str, str]]]:
    """(findings, ledger rows, body inputs) for one scenario against the working tree."""
    surfaces, bodies = collect_surfaces(repository)
    if scenario["id"] == PRESENCE_SCENARIO:
        findings, rows = presence_checks(surfaces)
    else:
        findings, rows = parity_checks(surfaces)
    inputs = [{"path": path, "sha256": sha256(content)} for path, content in sorted(bodies.items())]
    return findings, rows, inputs


def acceptance_result(
    scenario: dict[str, Any],
    candidate: str,
    findings: list[dict[str, str]],
    rows: list[tuple[str, str]],
    inputs: list[dict[str, str]],
) -> dict[str, Any]:
    codes = sorted({item["code"] for item in findings})
    blocked = any(code in BLOCKED_CODES for code in codes)
    result = "BLOCKED" if blocked else ("FAIL" if codes else "PASS")
    exit_code = {"PASS": 0, "FAIL": 1, "BLOCKED": 2}[result]
    return {
        "schemaVersion": ACCEPTANCE_SCHEMA_VERSION,
        "storyId": STORY_ID,
        "scenarioId": scenario["id"],
        "command": scenario["command"],
        "exitCode": exit_code,
        "result": result,
        "blockers": codes,
        "candidate": candidate,
        # No partial evidence: a blocked run binds no input and asserts nothing.
        "inputs": [] if blocked else inputs,
        "outputs": [],
        "assertionLedger": [] if blocked else [
            {"id": f"{scenario['id']}#{ordinal:04d}", "subject": subject, "state": state}
            for ordinal, (subject, state) in enumerate(rows, start=1)
        ],
    }


def render_json(document: dict[str, Any]) -> bytes:
    return (json.dumps(document, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def write_output(repository: Path, relative: str, content: bytes) -> None:
    target = repository / relative
    try:
        parent = target.parent
        existing = parent
        while not existing.exists():
            existing = existing.parent
        existing.resolve(strict=True).relative_to(repository)
        if target.is_symlink() or (target.exists() and not target.is_file()):
            raise OSError("the output leaf is a symlink or not a regular file")
        parent.mkdir(parents=True, exist_ok=True)
        temporary = None
        descriptor = -1
        try:
            flags = os.O_WRONLY | os.O_CREAT | os.O_EXCL
            if hasattr(os, "O_NOFOLLOW"):
                flags |= os.O_NOFOLLOW
            for _ in range(128):
                proposed = target.with_name(f".{target.name}.{secrets.token_hex(16)}.tmp")
                try:
                    descriptor = os.open(proposed, flags, 0o600)
                    temporary = proposed
                    break
                except FileExistsError:
                    continue
            if descriptor < 0:
                raise OSError("no exclusive temporary output name is available")
            with os.fdopen(descriptor, "wb") as handle:
                descriptor = -1
                handle.write(content)
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temporary, target)
        finally:
            if descriptor >= 0:
                os.close(descriptor)
            if temporary is not None:
                temporary.unlink(missing_ok=True)
        if target.read_bytes() != content:
            raise OSError("the written result differs from the derived result")
    except (OSError, ValueError):
        raise Blocked(
            "OUTPUT_WRITE_FAILED", relative, "the acceptance result could not be written"
        ) from None


def report(findings: list[dict[str, str]]) -> None:
    for item in findings:
        sys.stderr.write(f"{item['code']} {item['subject']}: {item['message']}\n")


def emit(content: bytes) -> None:
    buffer = getattr(sys.stdout, "buffer", None)
    sys.stdout.flush()
    if buffer is None:  # pragma: no cover - exotic stdout
        sys.stdout.write(content.decode("utf-8"))
    else:
        buffer.write(content)
        buffer.flush()


def failure_document(findings: list[dict[str, str]]) -> dict[str, Any]:
    codes: list[str] = []
    for item in findings:
        if item["code"] not in codes:
            codes.append(item["code"])
    return {
        "schemaVersion": FAILURE_SCHEMA_VERSION,
        "result": "BLOCKED",
        "exitCode": 2,
        "blockers": codes,
        "diagnostics": findings,
    }


def main(arguments: Sequence[str] | None = None) -> int:
    raw = list(arguments) if arguments is not None else sys.argv[1:]
    context: tuple[Path, str, dict[str, Any], dict[str, str], Any] | None = None
    try:
        options = parse_arguments(raw)
        repository, candidate = resolve_repository(options["--repository"])
        contract_validator, acceptance_validator = load_validators()
        scenario = load_scenario(repository, options, contract_validator)
        context = (repository, candidate, scenario, options, acceptance_validator)
        findings, rows, inputs = evaluate(repository, candidate, scenario)
    except Blocked as stop:
        findings, rows, inputs = [stop.finding], [], []
    except Exception as error:  # noqa: BLE001 - never a traceback, never a partial PASS
        findings = [
            finding(
                "INTERNAL_ERROR", "verifier", f"unexpected internal error ({type(error).__name__})"
            )
        ]
        rows, inputs = [], []

    if context is None:
        # No contract-bound scenario or candidate: no acceptance result can be formed.
        report(findings)
        emit(render_json(failure_document(findings)))
        return 2

    repository, candidate, scenario, options, acceptance_validator = context
    document = acceptance_result(scenario, candidate, findings, rows, inputs)
    if not acceptance_validator.is_valid(document) or (
        document["result"] == "PASS" and not document["assertionLedger"]
    ):
        findings = [
            finding("INTERNAL_ERROR", "verifier", "the derived acceptance result is invalid")
        ]
        document = acceptance_result(scenario, candidate, findings, [], [])
    content = render_json(document)
    try:
        write_output(repository, options["--output"], content)
    except Blocked as stop:
        findings = [*findings, stop.finding]
        document = acceptance_result(scenario, candidate, findings, [], [])
        content = render_json(document)
    report(findings)
    emit(content)
    return document["exitCode"]


if __name__ == "__main__":
    sys.exit(main())
