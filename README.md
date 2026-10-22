# Final project - Team Satanical Babies

Working game title: Jamie. Dystopian psychological horror exploration; gameplay direction remains under development.

## Open the project

1. Clone the repository with GitHub and ensure Git LFS is enabled. In a terminal inside the clone, run `git lfs install --local`, then `git lfs pull`.
2. Install the exact Editor version recorded in `ProjectSettings/ProjectVersion.txt`. Proposed initial baseline: **6000.3.27f1 (Unity 6.3 LTS)** with the Universal 2D template. Update this paragraph if the team chooses a different initial baseline.
3. In Unity Hub, add the cloned project folder from disk. Let package resolution and asset import finish.
4. Open `Assets/_Game/Scenes/Showcase.unity` once that scene has been created, then enter Play mode.

Controls and build platform: record the implemented choices here before sharing the first playable.

## Team

| Member | Role |
| --- | --- |
| Josh | Tech lead and integration/programming |
| Mikkel | Programming and music/audio |
| Luis | Producer |
| Alexia | Design and 2D art |
| Jana | Design and 2D art |

## Working together

Use short branches and pull requests into `main`. Read [CONTRIBUTING.md](CONTRIBUTING.md) before the first change. Announce shared scene/prefab ownership before editing.

Commit `Assets/` with `.meta` files, `Packages/`, `ProjectSettings/`, useful editable originals, and shared docs. Local caches and build output are ignored. All teammates use the same Editor and package versions; coordinate upgrades with Josh and Mikkel.

Art originals live in `SourceAssets/Art/`; game exports in `Assets/_Game/Art/`. Audio equivalents are `SourceAssets/Audio/` and `Assets/_Game/Audio/`. Add third-party sources to [asset credits](Docs/ASSET_CREDITS.md).

## Milestones

- **November 2, 2026:** first playable showcase.
- **December 11, 2026, 16:00:** final submission.

Intermediate dates are flexible planning targets. Before the showcase, write the controls, scene to open, build target and any known blockers here. Keep builds under ignored `Builds/` and share a complete zipped build through the team's agreed delivery location.

## Agreed art/import settings

Fill in after the first scene sample: sprite scale/pixels per unit, filtering, pivot conventions, reference resolution, camera direction, and asset naming. Preserve existing import settings when updating an asset.
