# Team workflow

## Start a task

1. Save scenes/project and close Unity before switching branches or pulling/merging substantial changes.
2. Commit existing work, fetch updates, and start a short task branch from current `main`.
3. Announce ownership before editing a shared scene, prefab, input/settings asset, or binary art source.
4. Open Unity and work on one reviewable task. Prefer separate development scenes and small prefabs for parallel work.

Suggested names: `feature/inspection`, `fix/clue-reset`, `art/ward-tiles`, `audio/hospital-ambience`. Keep `main` working and use pull requests after the initial repository bootstrap.

## Before a commit

- Stop Play mode, save the scene/project, close Unity and inspect the changed-file list.
- Include matching asset and folder `.meta` files, including import-setting changes.
- Move or rename imported assets through Unity so their identity stays attached.
- Preserve the existing asset and its `.meta` when replacing artwork in place.
- Avoid unrelated package, Editor-version and project-setting changes. Discuss intentional changes with Josh and Mikkel.
- Keep generated caches, local IDE files, build output and temporary exports out of commits.
- If you encounter unfamiliar changes, inspect them or ask before discarding anything.

Every teammate needs Git LFS support. The committed `.gitattributes` handles the selected layered art and audio formats. Small PNGs and Unity text files stay in normal Git. Agree new large-file types before committing them; do not change LFS policy casually after assets have accumulated history.

## Asset contributions

Keep useful editable originals under `SourceAssets/` and import game-ready files into `Assets/_Game/Art/` or `Audio/`. Native imports can live directly under `Assets/` when that workflow is intentional; keep a single editable source of truth.

Allow Unity to finish importing. Check appearance, scale, pivot, slicing/animation and relevant settings in a development scene. Include matching `.meta` files and any prefab that intentionally uses the new asset. Add source/credit information for external assets in `Docs/ASSET_CREDITS.md`.

Alexia and Jana agree their division of art/level tasks. Mikkel schedules audio alongside programming; audio time is part of project capacity.

## Pull requests

1. Commit and push the task branch.
2. Explain what changed and how you checked it. Include an image/clip for visible changes and a short listening note for audio.
3. Ask for one relevant review. Josh and Mikkel review code; asset changes need an import/appearance check by a teammate.
4. Resolve feedback, verify the merged result opens/runs, then remove the finished branch once no work depends on it.

Before accepting changes to the showcase scene, open it and play through the affected interaction. A screenshot alone does not verify that scene references and progression still work.

## Conflicts and shared files

Do not accept an entire scene/prefab version blindly to make a conflict disappear. Ask both editors to agree on the intended result. Keep one current editor per shared scene/prefab; ownership is a team convention, not a Git lock.

Unity text serialization makes changes inspectable but does not guarantee safe merges. A developer can configure UnityYAMLMerge when needed; this repository does not register an unconfigured custom merge driver.

## Builds and upgrades

Agree the showcase platform and verify a small exported build early. Put local exports in `Builds/`. Share the complete build package, not just the executable. Tag the source commit for the actual November 2 showcase build.

Everyone uses the exact Editor version in `ProjectSettings/ProjectVersion.txt`. Coordinate Editor/package upgrades and commit their resulting shared files together. No unilateral upgrades during parallel work.
