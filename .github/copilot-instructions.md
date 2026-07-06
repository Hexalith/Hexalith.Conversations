## Git Submodules

- Initialize only root-declared submodules under `references/`; never initialize nested submodules.
- Do not use recursive submodule initialization commands, such as `git submodule update --init --recursive`, unless the user explicitly requests nested submodules.
