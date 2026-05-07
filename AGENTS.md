## Git Submodules

- Initialize only submodules at the root of the repository; never initialize nested submodules.
- Do not use recursive submodule initialization commands, such as `git submodule update --init --recursive`, unless the user explicitly requests nested submodules.
