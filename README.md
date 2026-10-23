# Final project - Team Satanical Babies

Working game title: Jamie. Dystopian psychological horror exploration. The team has confirmed a top-down view, with Darkwood as a camera/control reference; story and gameplay details remain under development.

## Open the project

1. Clone the repository with GitHub and ensure Git LFS is enabled. In a terminal inside the clone, run `git lfs install --local`, then `git lfs pull`.
2. Install the exact Editor version recorded in `ProjectSettings/ProjectVersion.txt`. Proposed initial baseline: **6000.3.27f1 (Unity 6.3 LTS)** with the Universal 2D template. Update this paragraph if the team chooses a different initial baseline.
3. In Unity Hub, add the cloned project folder from disk. Let package resolution and asset import finish.
4. Open `Assets/_Game/Scenes/Playfield.unity`, then enter Play mode. Click the Game view to give it keyboard/mouse focus.

## Placeholder playfield

The first blockout has five connected rooms, open doorways, colliding walls/furniture, a mouse-aimed player, a following camera, and a toggleable 2D flashlight with shadows. All scene objects and simple placeholder sprites are editable in Unity. There are no enemies, story progression, or interaction systems yet.

| Input | Action |
| --- | --- |
| WASD or arrow keys | Move |
| Mouse | Aim / face the cursor |
| Hold Shift | Sprint |
| F | Toggle flashlight |
| R | Return to the starting position |

The current build scene is `Playfield.unity`; the prototype build target is Windows x64. Local exported builds belong under ignored `Builds/`.

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


