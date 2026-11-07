# Tools — the 56 MCP tools

Every tool, grouped: what it takes, what it does, and which read tool confirms it.

Input and output contracts live in [`shared/schemas/`](../../shared/schemas/), one JSON file per tool (`description`, `input`, `output`). The MCP server imports them directly and the plugin implements them. Required parameters are in **bold**.

## Conventions

| Rule | Meaning |
|---|---|
| Absolute values | Writes take absolute values, never deltas. |
| Partial patch | Only the fields present are applied; an omitted field keeps its value. Repeating a call is idempotent. |
| Ids | Object and character `id` are integers read from `studio_list_objects` / `studio_list_characters`. Asset ids come from `studio_search_catalog`; file paths from the `studio_list_*` tools. |
| Read before write | Several tools refuse values the read tools do not advertise (`available`, `states`, `weighted`, ...). Read first. |
| Read-back | Every write has a read tool that shows what Studio now holds; it is named in the notes. |
| Errors | JSON-RPC codes, see [protocol.md](./protocol.md). A batch never raises an error, see [Batch](#batch). |
| Scene scale | Studio scenes are not always 1 unit = 1 m. Read an object's `transform` before choosing camera distances or light ranges. |

## Read

| Tool | Params | Notes |
|---|---|---|
| `studio_get_info` | none | Runtime info: product, Unity, game, plugin and BepInEx versions, active scene, `studio_loaded`, object count, `plugins`. Works before the scene is ready. `plugins.timeline` and `plugins.nodes_constraints` say whether those tools will work. |
| `studio_get_scene` | none | Object counts per kind, background/frame, the map, the scene light and the full post-processing state. Errors with `-32000` until Studio is ready. Read-back for the four environment write tools. |
| `studio_list_characters` | none | All characters: id, name, card full name, sex, visible, transform, anime, kinematic, face, hands, expression. Read-back for every character write. |
| `studio_list_objects` | none | All scene objects: id, kind, name, visible, `parent_id` (set for attached objects), transform, light state. Read-back for the workspace tools. |
| `studio_get_camera` | none | Viewport camera: target position, angle, distance, fov, world position and rotation. The first four round-trip into `studio_set_camera`. |
| `studio_screenshot` | `format` `jpg`\|`png` (default `jpg`), `quality` 1–100 (default 85) | Screenshot of the Studio window, UI included, returned as MCP image content. |
| `studio_search_catalog` | `catalog` `item`\|`light`\|`anime`\|`map`\|`color_grading`\|`reflection_probe`\|`sky` (default `item`), `query`, `limit` 1–500 (default 50) | **Required** before creating anything: ids cannot be guessed. `query` matches the name and, for items and animations, the group and category names. |
| `studio_list_scenes` | `query`, `limit` 1–1000 (default 100) | Saved scenes under `UserData/Studio/scene`. Paths feed `studio_load_scene`. |
| `studio_list_character_cards` | `sex` `female`\|`male`, `query`, `limit` 1–1000 (default 100) | Cards under `UserData/chara/{female,male}`. Paths feed `studio_create_character`. The first call reads every card, then it is cached. |
| `studio_list_poses` | `sex`, `query`, `limit` 1–1000 (default 100) | Pose files under `UserData/studio/pose`. Paths feed `studio_set_character_pose`. Each pose is sex-specific. |
| `studio_list_coordinates` | `sex`, `query`, `limit` ≥ 1 (default 100) | Outfit files under `UserData/coordinate/{female,male}`. Paths feed `studio_set_character_outfit`. Header-only read, so the ~2000-file sweep is sub-second. |
| `studio_list_outfit` | **`id`** | A character's eight clothes slots (item worn, state, states accepted, coupling) and twenty accessory slots. **Required** before `studio_set_character_outfit`: the states a slot takes depend on the garment. |
| `studio_list_attach_points` | **`id`** | A character's 54 bone anchors: index, name, `available`, ids already attached. **Required** before `studio_attach_object`. |
| `studio_list_bones` | **`id`** | A character's posable skeleton: every FK bone (name, group, `weighted`, rotation) and the 13 IK targets (name, group, `rotatable`, position/rotation). **Required** before posing: the bone set depends on the rig, hair and clothes. |
| `studio_list_overlays` | `kind` `background`\|`frame`, `query`, `limit` 1–500 (default 100) | The 2D background and frame images on disk. Paths feed `studio_set_overlay`. |

## Scene and objects

| Tool | Params | Notes |
|---|---|---|
| `studio_load_scene` | **`path`** | Loads a saved scene, replacing the workspace. Destructive and not undoable. Waits for the map coroutine to finish and reflects the scene light itself, so the viewport matches the scene just read. |
| `studio_save_scene` | `name`, `overwrite` | Saves the workspace under `UserData/Studio/scene`, thumbnail included. The only way to persist agent work. Refuses to replace a file unless `overwrite`. `name` may be a forward-slash relative path (`agent/run17`); omitted, Studio's timestamped name is used. Returns `relative_path`, the value to hand to `studio_load_scene`. |
| `studio_set_camera` | `target_pos`, `angle`, `distance`, `fov` (1–160) | Moves the viewport camera. The camera orbits a target point. Partial and absolute; reads back after a frame so `world_pos` is fresh. |
| `studio_create_object` | **`group`**, **`category`**, **`no`**, `pos`, `rot`, `scale` | Spawns an item from the catalog triple. Undoable in Studio. |
| `studio_create_light` | **`no`**, `pos`, `rot`, `color`, `intensity`, `range`, `spot_angle` (0–180), `enabled`, `shadow` | Adds a light from its catalog `no`. Undoable in Studio. |
| `studio_set_light` | **`id`**, `color`, `intensity`, `range`, `spot_angle`, `enabled`, `shadow` | Changes an existing light. Partial and absolute. |
| `studio_set_object_transform` | **`id`**, `pos`, `rot`, `scale` | Moves, rotates or scales any object. Idempotent; undoable in Studio. |
| `studio_delete_object` | **`id`** | Deletes an object and its children. **Not** undoable, and clears Studio's undo stack. |
| `studio_attach_object` | **`id`**, `parent_id`, `attach_point` (index or name) | Attaches an object to a character bone, an item or a folder, or detaches it (omit `parent_id`). **Not** undoable and clears the undo stack. |

**Saving does not disturb the live scene.** `studio_save_scene` is the only write tool that changes nothing in the workspace: no object is touched, the undo stack is left intact and the viewport is unchanged. Its thumbnail is rendered from the cameras as they are at save time, so anything visible in `studio_screenshot`, selection gizmos and IK/FK handles included, lands in the preview. The tool waits for the end of the frame first, so a save issued right after `studio_set_camera` or `studio_create_character` still captures the new state.

**Attaching changes what a transform means.** While an object is attached, its `transform`, both what `studio_list_objects` reports and what `studio_set_object_transform` writes, is in the parent's local space, not world space. `pos: [0,0,0]` puts the object exactly on the bone. Detaching converts back to world space. The conversion happens on the object's stored coordinates, so every transform still on Studio's undo stack would be replayed in the wrong space; that is why `studio_attach_object` clears the stack.

Attach points are the same 54 anchors for every character, but the bone behind one depends on the rig, so an unusable point looks identical to a usable one until the object silently ends up at the scene root. `studio_list_attach_points` reports `available` per point and `studio_attach_object` refuses the rest. A point takes either its index or its name (`44` or `"N_Hand_L"`). Objects can also be parented to items and folders, with no attach point.

## Workspace

The workspace tree is writable, within Studio's own limits. `studio_list_objects` is the read-back for all four.

| Tool | Params | Notes |
|---|---|---|
| `studio_set_object_visible` | **`id`**, **`visible`** | Shows or hides an object and its branch. Refused on lights and route points. Not undoable but leaves the undo stack intact. |
| `studio_rename_object` | **`id`**, **`name`** | Renames a folder, an in-scene camera or a route. Refused on every other kind: their label is not saved. |
| `studio_create_folder` | `name`, `pos`, `rot`, `scale` | Adds an empty folder to group objects. **Not** undoable, and clears the undo stack, exactly as Studio's own button does. |
| `studio_duplicate_object` | **`id`** (integer or array) | Copies one or more objects with everything under them. Returns each copy with the `source_id` it came from. **Not** undoable. |

Hiding is per branch: hiding a parent hides everything under it while each child keeps its own flag, so re-showing the parent restores the branch as it was. The flag only persists because every save path resyncs it from the tree node first; the value stored on the object itself is stale between saves, which is why `visible` is read from the node. Lights are refused rather than silently ignored: Studio never gives a light a visibility toggle and its handler is empty, so the flag would be stored and inert (use `enabled` in `studio_set_light`). Route points are refused for the same reason, since theirs follows their route.

Renaming is narrower: only folders, in-scene cameras and routes store a name of their own in the save file. Every other kind takes its workspace label from its card or from the asset catalog on load, so writing it would look right until the next reload.

Duplicating goes through Studio's own scene serialiser. A copy carries the pose, outfit, colours **and** third-party plugin data of its source, lands under the same parent, and comes back paired with its `source_id`: Studio allocates fresh ids, so nothing else ties the two together. Restored plugin data (node constraints in particular) arrives a few frames later, so re-read it rather than querying it in the same batch.

## Characters

| Tool | Params | Notes |
|---|---|---|
| `studio_create_character` | **`card`**, `sex` `female`\|`male`, `pos`, `rot`, `scale` | Adds a character from a card path. **Not** undoable, and clears the undo stack, same as Studio's own Add menu. |
| `studio_set_character_pose` | **`id`**, **`path`** | Applies a pose file: anime, all IK/FK targets and expression flags. Sex-checked against the character. |
| `studio_set_character_anime` | **`id`**, `group`, `category`, `no`, `normalized_time`, `speed` (0–3), `pattern` (0–1), `option_param1`, `option_param2` (0–1) | Base animation `(group, category, no)` and/or playback settings. Ids from `studio_search_catalog` with `catalog: "anime"`. The playback fields are undoable. |
| `studio_set_character_expression` | **`id`**, `eyebrows`, `eyes`, `eyes_open`, `blink`, `mouth`, `mouth_open`, `lip_sync`, `tears`, `blush`, `look_eyes`, `look_neck`, `expression_categories` | Face patterns, openings, blink, tears, blush, look-at, expression categories. Partial and absolute. |
| `studio_set_character_hands` | **`id`**, `left`, `right` | Hand shapes, each side independently. `0` disables the shape, `1..hands.count` selects one. |
| `studio_set_character_outfit` | **`id`**, `coordinate`, `clothes_all` `worn`\|`half`\|`off`, `clothes`, `accessories` | Loads an outfit, dresses or undresses the eight slots, shows or hides accessories. Partial and absolute. |
| `studio_set_character_state` | **`id`**, `skin_gloss`, `wet`, `simple`, `simple_color`, `son`, `son_length`, `nipple`, `siru` | The rest of Studio's state panel. Read back from `state` in `studio_list_characters`. |
| `studio_set_character_kinematics` | **`id`**, `mode` `fk`\|`ik`\|`none`, `fk`, `ik`, `reflect` `fk`\|`ik`\|`both` | Switches posing mode, toggles bone groups, and `reflect`s the live pose into the change amounts. Gates the two tools below. Not undoable but leaves the stack intact. |
| `studio_set_character_bone` | **`id`**, **`bones`** | Rotates FK bones, several at a time, in absolute euler degrees. One call is one undo step. |
| `studio_set_character_ik` | **`id`**, **`targets`** | Moves, and on hands and feet rotates, IK targets, several at a time, as absolute offsets. One call is one undo step. |

`studio_list_characters` is the read-back for all of them.

**Pose versus anime.** They are two different Studio concepts. An *anime* is a clip from the catalog (`OCIChar.LoadAnime`) and only sets the animation. A *pose file* (`PauseCtrl`, `UserData/studio/pose/*.dat`) additionally restores every IK and FK bone target and the expression category flags, and only applies to a character of the sex it was saved for. The anime catalog itself is **not** sex-filtered; Studio does not filter it either.

**Anime playback.** `speed`, `pattern` and `option_param1|2` adjust the clip that is already loaded and can be sent without `(group, category, no)`. `pattern` and the option params reach the animator only for clips that declare them: `anime.is_anime_motion` and `anime.is_h_anime` say whether they will, and otherwise the value is stored but changes nothing. The ranges are Studio's own panel clamps so that a saved scene stays reproducible from the UI.

**Hands are set separately from everything else.** Neither a pose file nor a clip touches the hand shapes, so `studio_set_character_hands` is the only way to change them and its effect survives a later pose or anime. The shapes have no names anywhere in Studio, its own panel shows a bare number, so the tool takes an index and the way to identify a shape is `studio_screenshot`. `0` means "no shape", i.e. the animation drives the fingers; `1..hands.count` select one, `hands.count` coming from `studio_list_characters` (it depends on installed mods, and is `-1` while the shape list is still loading).

**Eye and eyebrow patterns are list positions, not asset ids.** Studio's own face panel shows the index inside the character's per-sex pattern list and maps it through the key array, because mods add non-contiguous ids. `studio_set_character_expression` uses that same index space; read `face.eyebrows_count` and `face.eyes_count` from `studio_list_characters` for the valid range.

### Outfits

**Dressing is per slot, and Studio never says no.** `studio_set_character_outfit` covers the three things Studio's costume and state tabs do: load a coordinate, set how each of the eight slots is worn (`worn`, `half`, `off`) and toggle the twenty accessory slots. Its ordering inside one call is fixed (`coordinate`, then `clothes_all`, then `clothes`, then `accessories`) because a coordinate replaces both the clothes and the accessories and therefore what the rest of the patch means.

Which states a slot accepts depends on the garment worn there. `ChaControl.SetClothesState` walks `(state + 1) % 3` until it finds one the slot does accept and applies *that* without a word, so an unsupported state is refused here instead. `states` in `studio_list_outfit` is the authority, and an empty list means the slot has no dressing states at all. The same goes for an accessory slot with nothing in it: the flag would be stored and nothing would appear. `clothes_all` is the exception and is deliberately not validated, since it is Studio's own three dressing buttons and falls back per slot exactly as they do.

**One garment can own two slots.** A full-body top leaves `bot` empty and a one-piece leaves `inner_b` empty; in that state Studio mirrors `off` between the two, so writing one slot moves the other. `linked_to` in `studio_list_outfit` names the sibling, and the read tools return every slot rather than the ones that were asked for. Two more consequences of changing an outfit: the FK skirt bones are re-weighted against the new garment (re-read `studio_list_bones`; with a skirt on, 6 of the 48 are weighted here, with shorts none are), and the dressing states are *not* reset, so a slot left `off` by the previous outfit stays `off` under the new one unless the same call passes `clothes_all`.

**The state panel is split in two.** Studio's per-character state tab holds the face sliders, tears and blush, which belong to `studio_set_character_expression`, next to the body ones, which are `studio_set_character_state`: skin gloss, wetness, the flat single-colour silhouette and its colour, the male part and its length, plus `nipple` and `siru`, which are refused on a male because their appliers do nothing there. `siru` levels are capped at 2 for a reason that is not cosmetic: the level indexes a three-entry table inside Studio's own update loop on the next frame, where an out-of-range value takes the process down instead of returning an error.

### Posing

**Mode first, then bones.** FK and IK are mutually exclusive chains and both are off on a fresh character: the animation drives the skeleton. `studio_set_character_bone` and `studio_set_character_ik` refuse to write while their chain is off, because Studio would happily store the value and never apply it: `ActiveFK` only reaches the bones when `enableFK` is already true, and `FKCtrl` only writes a bone while `enable & boneWeight`. The same silent no-op exists one level down, per group and per bone, so both tools also refuse a bone whose FK group is off, an IK target whose group is off, and an unweighted bone (the normal state of the 48 skirt bones when no skirt is worn: `weighted: false` in `studio_list_bones`). Group flags themselves are stored regardless of the mode, so pre-configuring `fk` groups while in IK is allowed and simply invisible until the switch.

```json
{"id": 4, "mode": "fk", "reflect": "fk"}
```

Bone names are the rig's own transform names (`cf_J_ArmUp00_L`, `cf_J_Hand_Thumb01_R`) and are unique per character; the `label` field is Studio's Japanese caption and is shared by several bones, so it is display-only. The FK `body` group covers the whole skeleton except the parts listed separately: arms, legs and spine are all body bones, because Studio stores a bone's group as a mask where every limb also carries the `Body` bit. Bones rotate only (Studio gives their guides no position or scale handle); IK targets move, and only the four hand and foot targets also rotate. Both tools take a map of several entries and push a single `GuideCommand` for the batch, so an agent's edit is one undo step in the Studio UI.

**`reflect` captures the pose the character is actually showing.** Enabling FK or IK snaps the skeleton to whatever change amounts were last stored, which after an animation change is stale. `reflect: "fk" | "ik" | "both"` copies the live skeleton into those change amounts one frame after the call and **before** `mode` is applied. That ordering is the whole point: the call above freezes the running animation instead of jumping back to an older pose. The wait is required because the values only become correct once the Animator has written the frame, which is why Studio defers its own reflect buttons by one `LateUpdate`. Capturing into a chain that is already enabled is a no-op, since that chain is what is driving the bones. FK capture covers body, neck and both hands (hair, breast and skirt are driven by dynamic bones); IK capture covers all five groups. The capture itself is not undoable.

## Environment

`studio_get_scene` is the read-back for all four.

| Tool | Params | Notes |
|---|---|---|
| `studio_set_map` | `no`, `pos`, `rot`, `option`, `light` | Chooses the 3D set, moves it, toggles its optional objects (`option`) and its own lights (`light`). A full scene load: **not** undoable, and clears the undo stack when `no` is given. |
| `studio_set_scene_lighting` | `color`, `intensity` (0.1–2), `rot`, `shadow` | The scene's main character light. Partial and absolute; the ranges are Studio's own clamps. |
| `studio_set_overlay` | `background`, `frame` | The 2D images behind and over the scene. `""` clears one; an unreadable image clears it too and reports an error. |
| `studio_set_post_processing` | `color_grading`, `ambient_occlusion`, `bloom`, `depth_of_field`, `vignette`, `screen_space_reflections`, `reflection_probe`, `fog`, `sun_shafts`, `self_shadow`, `environment_lighting`, `sky` | The twelve screen effects. Partial and absolute per section; not undoable. |

**The set, the light and the grade are three different things.** `studio_set_map` loads the 3D set the scene is built in, a whole Unity scene, so the call takes a moment and, because Studio resets the map transform on every map change with no undo command, it clears the undo stack whenever `no` is given. It resets that transform even when the requested map is the one already loaded, so `pos` and `rot` are applied after the load has settled rather than before. `option` and `light` show the map's own optional objects and lights and are refused on a map that has neither: `map.option_available` and `map.light_available` say which. `studio_set_overlay` sets flat 2D images instead, `background` behind everything and `frame` on top, and neither replaces the map.

**There is one scene light, not two.** `studio_set_scene_lighting` drives the character light Studio's light panel controls, which is separate from the `OCILight` objects `studio_create_light` spawns. Its sibling `mapLight` exists in the scene file and is deliberately absent from the API: nothing in Studio ever applies it. Loading a scene does not apply the character light either, since Studio only reflects it when its panel is opened, so `studio_load_scene` reflects it itself.

**Post-processing is one call for twelve effects.** Every section and field of `studio_set_post_processing` is optional and absolute, and the whole effect state is re-applied in one pass afterwards, so a rejected field means nothing was written at all. Numeric ranges are the panel's slider bounds, which live in Studio's UI prefab rather than in its code: they are documented per field but not enforced, and the returned state is what the scene actually holds. Three fields are ids that must exist, `color_grading.lookup_texture`, `reflection_probe.cubemap` and `sky.pattern`, all from `studio_search_catalog` (`color_grading`, `reflection_probe`, `sky`). Two are scene object ids that must be **items**: `depth_of_field.focus_object_id` and `sun_shafts.caster_object_id` (`-1` for none). Studio's own setters accept anything and quietly fall back to `-1`, so both are refused instead. One field is not scene state at all: `self_shadow` moves the global quality level a step and outlives the scene it was set in.

## Batch

| Tool | Params | Notes |
|---|---|---|
| `studio_execute_batch` | **`steps`** (1–32 of `{method, params}`) | Runs tools in order in one call. Validated as a whole before anything runs, but **no rollback** once it starts. |

Batching saves turns, not milliseconds. Two tools already batch internally and should be preferred over a batch of single writes: `studio_set_character_bone` and `studio_set_character_ik` apply as a single undo step, a batch of individual calls does not. Rules, result shape and failure behaviour: [protocol.md](./protocol.md#execute_batch).

## Optional plugins

Thirteen tools bridge to third-party plugins. They are always registered; the details, the workflows and the failure model are in [optional-plugins.md](./optional-plugins.md).

| Tool | Params | Notes |
|---|---|---|
| `studio_timeline_get_state` | `object_id`, `include_keyframes` (default `true`) | Transport plus every interpolable and its keyframes. `available` says whether Timeline is there. |
| `studio_timeline_playback` | **`action`** `play`\|`pause`\|`stop`\|`seek`\|`next_frame`\|`previous_frame`, `time` | Drives the transport. |
| `studio_timeline_set_settings` | `duration`, `frame_rate` | Timeline length and the frame rate the steps use. |
| `studio_timeline_list_models` | `object_id`, `query`, `limit` (default 100) | The animatable properties a given object accepts. |
| `studio_timeline_add_interpolable` | **`object_id`**, **`model`**, `owner`, `name`, `target`, `alias` | Binds one property to one object, creating the track. |
| `studio_timeline_remove_interpolable` | **`id`** | Drops one or more tracks, keyframes included. |
| `studio_timeline_set_keyframe` | **`interpolable_id`**, **`time`**, `value`, `curve` | Puts or overwrites a keyframe. |
| `studio_timeline_delete_keyframe` | **`interpolable_id`**, **`time`** | Removes one keyframe by its exact time. |
| `studio_timeline_single_file` | **`action`** `list`\|`load`\|`save`\|`delete`, `name`, `object_id`, `overwrite` | One object's animation as a reusable file. |
| `studio_constraint_list` | `object_id` | Every constraint in the scene, fully expanded. `available` says whether NodesConstraints is there. |
| `studio_constraint_create` | **`parent`**, **`child`** (`{object_id, target}`), plus per-channel options | Makes one node follow another. |
| `studio_constraint_set` | **`id`**, plus the same options | Changes one constraint. The two nodes cannot be changed. |
| `studio_constraint_delete` | **`id`** (integer or array) | Drops one or more constraints, restoring their children. |

## Undo behaviour

| Behaviour | Tools |
|---|---|
| Undoable from Studio | `studio_create_object`, `studio_create_light`, `studio_set_object_transform`, the anime playback fields of `studio_set_character_anime`; `studio_set_character_bone` and `studio_set_character_ik` as one step per call |
| Not undoable, and **clears** the undo stack | `studio_delete_object`, `studio_create_folder`, `studio_create_character`, `studio_attach_object`, `studio_set_map` (when `no` is given) |
| Not undoable, stack intact | `studio_set_object_visible`, `studio_rename_object`, `studio_set_character_kinematics`, `studio_save_scene`, and the rest of the character writes (pose, clip load, face, hands), which Studio has no command for |
| Own history | The Timeline and NodesConstraints tools never touch Studio's `UndoRedoManager` |

Deletion and character creation cannot be undone because Studio NEO V2 has neither a delete command nor an add-character command, and clears the stack itself on the latter.

## Known limits

- No routes and no in-scene cameras.
- No sound: voices and the `bgmCtrl`, `envCtrl` and `outsideSoundCtrl` settings in the scene file are not covered.
- Changing the clothing item of a slot (`ChangeClothes`, `ChangeAccessory`) and its colours is out of scope: that is the character maker's job and needs the whole per-sex clothes catalog, which Studio's own Studio-side panels do not expose either.
