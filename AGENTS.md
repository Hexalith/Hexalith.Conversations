## Shared Hexalith LLM Instructions

Before starting any work in this repository, read and follow
[`references/Hexalith.AI.Tools\hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md).

Before working on any module user interface or UX, also read and follow
[`references/Hexalith.AI.Tools/hexalith-ux-instructions.md`](./references/Hexalith.AI.Tools/hexalith-ux-instructions.md).

## Git Submodules

- These rules apply to all LLM/agent tools operating in this repository.
- Initialize only root-declared submodules under `references/`; never initialize nested submodules.
- Do not use recursive submodule initialization commands, such as `git submodule update --init --recursive`, unless the user explicitly requests nested submodules.
