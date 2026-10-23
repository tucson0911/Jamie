# Top-down placeholder playfield

Open `Assets/_Game/Scenes/Playfield.unity`, enter Play mode, and click the Game view. Move with WASD/arrows, aim with the mouse, hold Shift to sprint, press F to toggle the flashlight, and R to reset your position. Keyboard/mouse is the supported input for this prototype.

This is a movement, collision, camera and lighting blockout. Reception, ward, store, records and treatment are layout labels rather than settled story content. Doorways are open gaps. There are no door interactions, enemies, inventory, oxygen or narrative/reset progression yet. Ambient light deliberately leaves navigation visible when the flashlight is off; definitely not a full Darkwood visibility/fog system yet LOL.

## Edit the scene

- `Floors and markings`: floor colours, tile seams and doorway strips.
- `Walls - colliders and shadows`: move/resize wall objects to change the layout. Each has a BoxCollider2D and ShadowCaster2D.
- `Furniture - colliders and shadows`: placeholder beds, desks, shelves and a crate. They block movement and cast flashlight shadows.
- `Player`: Rigidbody2D with zero gravity, a circular collider, and PlayerController. Walk/sprint speeds are exposed in the Inspector. The child `Facing - mouse aim` rotates independently of movement and carries the player visuals/flashlight.
- `Main Camera`: orthographic camera with CameraFollow. Its smoothing and map bounds are exposed in the Inspector. Adjust bounds if the playfield changes size.
- `Ambient light`, `Nearby visibility` and `Flashlight cone`: adjust Light2D intensity, colour, range and cone angle in the Inspector.
- `Controls overlay`: a small runtime HUD for the prototype controls and flashlight state.

The scene contains ordinary saved Unity objects; no level-generation code runs during gameplay. Assets under `Assets/_Game/Art/Placeholders/` are tiny locally generated shapes/materials and need no external downloads. They can be replaced with final sprites later; keep matching `.meta` files.


## Check after changes

1. Move in all directions; diagonal movement should have the same speed as straight movement.
2. Sprint, walk into walls and furniture, and slide along their edges.
3. Walk through all five open doorways and back into the corridor.
4. Aim around the player and toggle the flashlight. Walls/furniture should cast shadows; basic navigation remains visible with the light off.
5. Check that the camera follows without exposing space beyond the map at the intended window sizes.
6. Reset with R and check for Console errors.
7. Build Windows x64 from the active build profile, with `Playfield.unity` enabled, into `Builds/JamiePlayfield/`. Test the complete build folder, including its Data folder and UnityPlayer.dll.

Save the scene/project and close Unity before switching branches or merging. Commit the scene, scripts and art with their `.meta` files together. Build output is ignored.

