# MCP API

The MCP server (`mcp/`, TypeScript/Bun, stdio transport) exposes Studio NEO V2 to any MCP client. Each tool forwards to the plugin's local JSON-RPC endpoint (`http://localhost:6472`, port configurable via `BepInEx\config\com.mirastudio.plugin.cfg` and `MIRASTUDIO_URL` on the MCP side).

Input/output contracts live in [shared/schemas/](../shared/schemas/) — one JSON file per tool (`description`, `input`, `output`). The MCP server imports them directly; the plugin implements them.

### Read

| Tool | Description | Notes |
|---|---|---|
| `studio_get_info` | Runtime info (versions, `studio_loaded`, object count, `plugins`) | works before the Studio scene is ready; `plugins.timeline` says whether the timeline tools will work |
| `studio_get_scene` | Scene summary: object counts per kind, background/frame, plus the map, the scene light and the full post-processing state | errors with `-32000` until Studio is ready; the read-back for all four environment write tools |
| `studio_list_characters` | All characters: id, name, card full name, sex, visible, transform, anime, kinematic, face, hands, expression | the read-back tool for every character write |
| `studio_list_objects` | All scene objects: id, kind, name, visible, parent_id, transform, light state | `parent_id` set for attached objects; the read-back for the four workspace write tools |
| `studio_get_camera` | Viewport camera: target pos, angle, distance, fov, world pos/rot | first four round-trip into `studio_set_camera` |
| `studio_screenshot` | Screenshot of the Studio window, returned as MCP image content | `format` jpg (default) or png, `quality` 1–100 |
| `studio_search_catalog` | Search spawnable/loadable assets — `catalog: item\|light\|anime` | **required** before creating anything: ids cannot be guessed |
| `studio_list_scenes` | Saved scenes under `UserData/Studio/scene` | paths feed `studio_load_scene` |
| `studio_list_character_cards` | Character cards under `UserData/chara/{female,male}` | paths feed `studio_create_character`; first call reads every card, then cached |
| `studio_list_poses` | Pose files under `UserData/studio/pose` | paths feed `studio_set_character_pose`; each pose is sex-specific |
| `studio_list_coordinates` | Outfit files under `UserData/coordinate/{female,male}` | paths feed `studio_set_character_outfit`; header-only read, so the ~2000-file sweep is sub-second |
| `studio_list_outfit` | A character's eight clothes slots (item worn, state, states accepted, coupling) and twenty accessory slots | **required** before `studio_set_character_outfit`: the states a slot takes depend on the garment |
| `studio_list_attach_points` | A character's 54 bone anchors: index, name, `available`, ids already attached | **required** before `studio_attach_object`: only `available` points can be used |
| `studio_list_bones` | A character's posable skeleton: every FK bone (name, group, `weighted`, rot) and the 13 IK targets (name, group, `rotatable`, pos/rot) | **required** before posing: the bone set depends on the rig, hair and clothes |
| `studio_list_overlays` | The 2D background and frame images on disk | paths feed `studio_set_overlay` |

### Write

| Tool | Description | Notes |
|---|---|---|
| `studio_load_scene` | Load a saved scene, replacing the workspace | destructive + not undoable; waits for the map coroutine to finish |
| `studio_save_scene` | Save the workspace under `UserData/Studio/scene`, thumbnail included | the only way to persist agent work; refuses to replace a file unless `overwrite` |
| `studio_set_camera` | Move the viewport camera (target/angle/distance/fov) | partial + absolute; reads back after a frame so `world_pos` is fresh |
| `studio_create_object` | Spawn an item from `(group, category, no)` | optional pos/rot/scale; undoable in Studio |
| `studio_create_light` | Add a light from its catalog `no` | optional transform + colour/intensity/range/spot/shadow |
| `studio_set_light` | Change an existing light's settings | partial + absolute |
| `studio_set_object_transform` | Move/rotate/scale any object by id | partial + absolute → idempotent; undoable in Studio |
| `studio_delete_object` | Delete an object and its children | **not** undoable, and clears Studio's undo stack |
| `studio_set_object_visible` | Show or hide an object and its branch | refused on lights and route points; not undoable but leaves the undo stack intact |
| `studio_rename_object` | Rename a folder, an in-scene camera or a route | refused on every other kind — their label is not saved |
| `studio_create_folder` | Add an empty folder to group objects | optional name + transform; **not** undoable, and clears the undo stack |
| `studio_duplicate_object` | Copy one or more objects with everything under them | returns each copy with the `source_id` it came from; **not** undoable |
| `studio_create_character` | Add a character from a card path | **not** undoable, and clears the undo stack — same as Studio's own Add menu |
| `studio_set_character_pose` | Apply a pose file: anime + all IK/FK targets + expression flags | sex-checked against the character |
| `studio_set_character_anime` | Base animation `(group, category, no[, normalized_time])` and/or playback: `speed`, `pattern`, `option_param1|2` | ids from `studio_search_catalog` with `catalog='anime'`; the playback fields are undoable |
| `studio_set_character_expression` | Face: eyebrows/eyes/mouth patterns, openings, blink, tears, blush, look-at, expression categories | partial + absolute |
| `studio_set_character_hands` | Hand shapes, `left` and `right` independently | partial + absolute; `0` disables the shape, `1..hands.count` selects one |
| `studio_set_character_outfit` | Load an outfit, dress/undress the eight slots, show or hide accessories | partial + absolute; `coordinate` is applied first and the rest is validated against it |
| `studio_set_character_state` | The rest of Studio's state panel: skin gloss, wetness, flat silhouette + colour, son + length, nipple and liquids (both female-only) | partial + absolute; read back from `state` in `studio_list_characters` |
| `studio_set_character_kinematics` | Switch posing mode (`fk`/`ik`/`none`), toggle bone groups, and `reflect` the live pose into the change amounts | gates the two tools below; not undoable but leaves the stack intact |
| `studio_set_character_bone` | Rotate FK bones, several at a time | absolute euler degrees; one call = one undo step |
| `studio_set_character_ik` | Move (and rotate, on hands and feet) IK targets, several at a time | absolute offsets; one call = one undo step |
| `studio_attach_object` | Attach an object to a character bone, an item or a folder — or detach it | omit `parent_id` to detach; **not** undoable and clears the undo stack |
| `studio_set_map` | Choose the 3D set, move it, toggle its optional objects and its own lights | a full scene load; **not** undoable and clears the undo stack when `no` is given |
| `studio_set_scene_lighting` | The scene's main character light: colour, intensity, direction, shadows | partial + absolute; ranges are Studio's own clamps |
| `studio_set_overlay` | The 2D `background` and `frame` images | `""` clears one; an unreadable image clears it too and reports an error |
| `studio_set_post_processing` | The twelve screen effects: colour grading, AO, bloom, DOF, vignette, SSR, reflection probe, fog, sun shafts, self shadow, environment lighting, sky | partial + absolute per section; not undoable |
| `studio_execute_batch` | Run up to 32 tools in order in one call | validated as a whole before anything runs, but **no rollback** once it starts |

Every write tool takes **absolute** values and applies only the fields present, so repeating a call is idempotent. Item/light creation, transform edits and the anime playback settings are pushed onto Studio's native `UndoRedoManager`, so they can be undone from the Studio UI; deletion and character creation cannot (Studio NEO V2 has neither a delete command nor an add-character command, and clears the stack itself on the latter). The remaining character writes — pose, clip load, face, hands — have no command in Studio either, but unlike deletion they leave the stack usable, so they do not clear it.

**Pose vs anime.** They are two different Studio concepts. An *anime* is a clip from the catalog (`OCIChar.LoadAnime`) and only sets the animation. A *pose file* (`PauseCtrl`, `UserData/studio/pose/*.dat`) additionally restores every IK and FK bone target and the expression category flags, and only applies to a character of the sex it was saved for. The anime catalog itself is **not** sex-filtered — Studio does not filter it either.

**Hands are set separately from everything else.** Neither a pose file nor a clip touches the hand shapes, so `studio_set_character_hands` is the only way to change them and its effect survives a later pose or anime. The shapes have no names anywhere in Studio — its own panel shows a bare number — so the tool takes an index and the way to identify a shape is `studio_screenshot`. `0` means "no shape", i.e. the animation drives the fingers; `1..hands.count` select one, `hands.count` coming from `studio_list_characters` (it depends on installed mods, and is `-1` while the shape list is still loading).

**Dressing is per slot, and Studio never says no.** `studio_set_character_outfit` covers the three things
Studio's costume and state tabs do: load a coordinate, set how each of the eight slots is worn (`worn`, `half`,
`off`) and toggle the twenty accessory slots. Its ordering inside one call is fixed — `coordinate`, then
`clothes_all`, then `clothes`, then `accessories` — because a coordinate replaces both the clothes and the
accessories and therefore what the rest of the patch means. Which states a slot accepts depends on the garment
worn there: `ChaControl.SetClothesState` walks `(state + 1) % 3` until it finds one the slot does accept and
applies *that* without a word, so an unsupported state is refused here instead — `states` in
`studio_list_outfit` is the authority, and an empty list means the slot has no dressing states at all. The same
goes for an accessory slot with nothing in it: the flag would be stored and nothing would appear.
`clothes_all` is the exception and is deliberately not validated, since it is Studio's own three dressing
buttons and falls back per slot exactly as they do.

**One garment can own two slots.** A full-body top leaves `bot` empty and a one-piece leaves `inner_b` empty; in
that state Studio mirrors `off` between the two, so writing one slot moves the other. `linked_to` in
`studio_list_outfit` names the sibling, and both the read tools return every slot rather than the ones that were
asked for. Two more consequences of changing an outfit: the FK skirt bones are re-weighted against the new
garment (re-read `studio_list_bones` — with a skirt on, 6 of the 48 are weighted here; with shorts, none are),
and the dressing states are *not* reset, so a slot left `off` by the previous outfit stays `off` under the new
one unless the same call passes `clothes_all`.

**The state panel is split in two.** Studio's per-character state tab holds the face sliders — tears and blush,
which belong to `studio_set_character_expression` — next to the body ones, which are
`studio_set_character_state`: skin gloss, wetness, the flat single-colour silhouette and its colour, the male
part and its length, plus `nipple` and `siru` which are refused on a male because their appliers do nothing
there. `siru` levels are capped at 2 for a reason that is not cosmetic: the level indexes a three-entry table
inside Studio's own update loop on the next frame, where an out-of-range value takes the process down instead of
returning an error.

**Attaching changes what a transform means.** While an object is attached, its `transform` — both what `studio_list_objects` reports and what `studio_set_object_transform` writes — is in the parent's local space, not world space. `pos: [0,0,0]` puts the object exactly on the bone. Detaching converts back to world space. Because the conversion happens on the object's stored coordinates, every transform still on Studio's undo stack would be replayed in the wrong space, so `studio_attach_object` clears the stack.

Attach points are the same 54 anchors for every character, but the bone behind one depends on the rig, so an unusable point looks identical to a usable one until the object silently ends up at the scene root. `studio_list_attach_points` reports `available` per point and `studio_attach_object` refuses the rest. A point takes either its index or its name (`44` or `"N_Hand_L"`). Objects can also be parented to items and folders, with no attach point.

**The workspace tree is writable, within Studio's own limits.** `studio_list_objects` reports `name` and
`visible` for every object, and the four workspace tools are what write them back. Hiding is per branch: hiding a
parent hides everything under it while each child keeps its own flag, so re-showing the parent restores the branch
as it was. The flag only persists because every save path resyncs it from the tree node first — the value stored on
the object itself is stale between saves, which is why `visible` is read from the node and not from the save data.
Lights are refused rather than silently ignored: Studio never gives a light a visibility toggle and its handler is
empty, so the flag would be stored and inert (use `enabled` in `studio_set_light`). Route points are refused for the
same reason — theirs follows their route.

Renaming is narrower still: only folders, in-scene cameras and routes store a name of their own in the save file.
Every other kind takes its workspace label from its card or from the asset catalog on load, so writing it would look
right until the next reload. Duplicating goes through Studio's own scene serialiser, which means a copy carries the
pose, outfit, colours **and** third-party plugin data of its source, lands under the same parent, and comes back
paired with the `source_id` it was made from — Studio allocates fresh ids, so nothing else ties the two together.
Restored plugin data (node constraints in particular) arrives a few frames later, so re-read it rather than
querying it in the same batch. Adding a folder clears the undo stack, exactly as Studio's own button does; hiding
and renaming touch nothing that the undo stack replays and leave it alone.

**Anime playback.** `speed`, `pattern` and `option_param1|2` adjust the clip that is already loaded and can be sent without `(group, category, no)`. `pattern` and the option params reach the animator only for clips that declare them — `anime.is_anime_motion` and `anime.is_h_anime` say whether they will, and otherwise the value is stored but changes nothing. The ranges are Studio's own panel clamps (`speed` 0..3, the rest 0..1) so that a saved scene stays reproducible from the UI.

**Eye and eyebrow patterns are list positions, not asset ids.** Studio's own face panel shows the index inside the character's per-sex pattern list and maps it through the key array, because mods add non-contiguous ids. `studio_set_character_expression` uses that same index space; read `face.eyebrows_count` / `face.eyes_count` from `studio_list_characters` for the valid range.

**Posing: mode first, then bones.** FK and IK are mutually exclusive chains and both are off on a fresh character — the animation drives the skeleton. `studio_set_character_bone` and `studio_set_character_ik` refuse to write while their chain is off, because Studio would happily store the value and never apply it: `ActiveFK` only reaches the bones when `enableFK` is already true, and `FKCtrl` only writes a bone while `enable & boneWeight`. The same silent no-op exists one level down, per group and per bone, so both tools also refuse a bone whose FK group is off, an IK target whose group is off, and an unweighted bone (the normal state of the 48 skirt bones when no skirt is worn — `weighted: false` in `studio_list_bones`). Group flags themselves are stored regardless of the mode, so pre-configuring `fk` groups while in IK is allowed and simply invisible until the switch.

Bone names are the rig's own transform names (`cf_J_ArmUp00_L`, `cf_J_Hand_Thumb01_R`) and are unique per character; the `label` field is Studio's Japanese caption and is shared by several bones, so it is display-only. The FK `body` group covers the whole skeleton except the parts listed separately — arms, legs and spine are all body bones, because Studio stores a bone's group as a mask where every limb also carries the `Body` bit. Bones rotate only (Studio gives their guides no position or scale handle); IK targets move, and only the four hand and foot targets also rotate. Both tools take a map of several entries and push a single `GuideCommand` for the batch, so an agent's edit is one undo step in the Studio UI.

**`reflect` captures the pose the character is actually showing.** Enabling FK or IK snaps the skeleton to whatever change amounts were last stored, which after an animation change is stale. `reflect: "fk" | "ik" | "both"` copies the live skeleton into those change amounts one frame after the call and **before** `mode` is applied — that ordering is the whole point: `{"mode": "fk", "reflect": "fk"}` freezes the running animation instead of jumping back to an older pose. The wait is required because the values only become correct once the Animator has written the frame, which is why Studio defers its own reflect buttons by one `LateUpdate`. Capturing into a chain that is already enabled is a no-op, since that chain is what is driving the bones. FK capture covers body, neck and both hands (hair, breast and skirt are driven by dynamic bones); IK capture covers all five groups. The capture itself is not undoable.

**Scene scale is not fixed.** Studio scenes are frequently built at a scale other than 1 unit = 1 m — a real user scene tested here has a ~15-unit-tall character. Read an object's `transform` before choosing camera distances or light ranges instead of assuming metres.

**Saving does not disturb the live scene.** `studio_save_scene` is the only write tool that changes nothing in the workspace: no object is touched, the undo stack is left intact, and the viewport is unchanged. Its thumbnail is rendered from the cameras as they are at save time, so anything visible in `studio_screenshot` — including selection gizmos and IK/FK handles — lands in the preview. The tool waits for the end of the frame first, so a save issued right after `studio_set_camera` or `studio_create_character` still captures the new state.

**The set, the light and the grade are three different things.** `studio_set_map` loads the 3D set the scene is built in — a whole Unity scene, so the call takes a moment and, because Studio resets the map transform on every map change with no undo command, it clears the undo stack whenever `no` is given. It resets that transform even when the requested map is the one already loaded, so `pos`/`rot` are applied after the load has settled rather than before. `option` and `light` show the map's own optional objects and lights and are refused on a map that has neither: `map.option_available` and `map.light_available` say which. `studio_set_overlay` sets flat 2D images instead — `background` behind everything, `frame` on top — and neither replaces the map.

**There is one scene light, not two.** `studio_set_scene_lighting` drives the character light Studio's light panel controls, which is separate from the `OCILight` objects `studio_create_light` spawns. Its sibling `mapLight` exists in the scene file and is deliberately absent from the API: nothing in Studio ever applies it. Loading a scene does not apply the character light either — Studio only reflects it when its panel is opened — so `studio_load_scene` reflects it itself and the viewport matches the scene that was just read.

**Post-processing is one call for twelve effects.** Every section and field of `studio_set_post_processing` is optional and absolute, and the whole effect state is re-applied in one pass afterwards, so a rejected field means nothing was written at all. Numeric ranges are the panel's slider bounds, which live in Studio's UI prefab rather than in its code: they are documented per field but not enforced, and the returned state is what the scene actually holds. Three fields are ids that must exist — `color_grading.lookup_texture`, `reflection_probe.cubemap`, `sky.pattern`, all from `studio_search_catalog` — and two are scene object ids that must be **items**: `depth_of_field.focus_object_id` and `sun_shafts.caster_object_id` (`-1` for none). Studio's own setters accept anything and quietly fall back to `-1`, so both are refused instead. One field is not scene state at all: `self_shadow` moves the global quality level a step and outlives the scene it was set in.

**Batching saves turns, not milliseconds.** A call costs about a millisecond of loopback plus one frame, so `studio_execute_batch` is worth reaching for when it removes LLM round-trips — a scene to build, a limb to pose, props to spawn and place — not when it removes latency. It buys no atomicity: every step is validated before any of them runs (unknown method, malformed step, nesting, `studio_screenshot`) and an unusable batch is refused whole, but once execution starts a failing step stops the batch and everything before it stays applied. Studio has no transaction, `UndoRedoManager.Do` pushes its command *before* executing it, and several tools clear the stack outright, so there is nothing to roll back to. The tool therefore always returns a result rather than an RPC error: `ok`, `executed`, `stopped_at` and `error` say exactly how far it got, and `steps` carries each result as the tool would have returned it alone.

Two tools already batch internally and should be preferred over a batch of single writes: `studio_set_character_bone` and `studio_set_character_ik` take several entries and push one `GuideCommand` for the lot, so they apply as a single undo step. A batch of individual calls does not.

### Timeline (optional third-party plugin)

| Tool | Description | Notes |
|---|---|---|
| `studio_timeline_get_state` | Transport plus every interpolable and its keyframes | the read-back for all the tools below; `available` says whether Timeline is there |
| `studio_timeline_playback` | `play`, `pause`, `stop`, `seek`, `next_frame`, `previous_frame` | seeking applies the pose immediately, so `seek` + `studio_screenshot` renders a given moment |
| `studio_timeline_set_settings` | Timeline length and the frame rate the steps use | partial + absolute |
| `studio_timeline_list_models` | The animatable properties, filtered to what a given object accepts | **required** before adding: 95 models on a character here, only 38 of them Timeline's own |
| `studio_timeline_add_interpolable` | Bind one property to one object, creating the track | idempotent — re-adding returns the existing track |
| `studio_timeline_remove_interpolable` | Drop one or more tracks, keyframes included | every id is validated before anything is removed |
| `studio_timeline_set_keyframe` | Put or overwrite a keyframe, with an easing curve | omit `value` to capture the scene as it stands |
| `studio_timeline_delete_keyframe` | Remove one keyframe by its exact time | times are floats: read them back rather than recomputing |
| `studio_timeline_single_file` | `list`, `load`, `save`, `delete` one object's animation as a reusable file | how an animation moves from one character to another |

**Timeline is optional, but its tools are always registered.** The MCP tool list is static, so a missing plugin has to answer with a readable error rather than `unknown method`: every timeline tool fails with `-32001` and the reason, while `studio_timeline_get_state` answers normally with `available: false`. `studio_get_info` reports `plugins.timeline` as the version or `null`. The bridge resolves Timeline's members by reflection in one all-or-nothing pass, the way Timeline's own `TimelineCompatibility.Init` does, so a version that moved something reports itself unavailable instead of half-working. Tested against Timeline 1.4.1.

**The workflow is pose-then-record.** Add the interpolable, seek to a time, pose the scene with the ordinary tools (`studio_set_character_bone`, `studio_set_character_ik`, `studio_set_object_transform`, …), then call `studio_timeline_set_keyframe` **without** `value`: the keyframe captures the property exactly as it stands, which is what Timeline's own button does and the only option for value types with no JSON form. Passing `value` explicitly works for the ordinary ones — numbers, booleans, `[x,y,z]`, euler degrees, `[r,g,b,a]` — and is typed against the property's current value, so a wrong shape is refused rather than coerced. `studio_execute_batch` is the right vehicle for this: pose/record pairs alternate correctly inside one batch, because every write lands on the change amounts synchronously and the capture that follows sees it.

**Animating a bone still needs FK or IK on.** `guideObjectPos|Rot|Scale` are the per-target models, and they write the same change amounts `studio_set_character_bone` and `studio_set_character_ik` do. `target` takes `root` for the object itself or any bone or IK target name from `studio_list_bones` — the bridge resolves the guide object from Studio's own `BoneInfo`/`IKInfo`, so nothing touches the Studio selection. But `FKCtrl` still only applies a bone while its chain is enabled and the bone is weighted: with kinematics off, the animation is stored, read back correctly, and never reaches the mesh.

**Curves are Timeline's own five presets.** `line`, `top` (fast then slow), `bottom` (slow then fast), `hermite` (eased both ends) and `stairs` (a hard step), with the exact tangents Timeline's keyframe window applies, or explicit `[time, value, in_tangent, out_tangent]` points. An omitted curve inherits the preceding keyframe's, falling back to linear — the same inheritance Timeline's `AddKeyframe` applies. `stairs` carries an infinite tangent, which JSON cannot spell, so it round-trips through `null`.

**Two ids share a name, and one of them needs disambiguating.** `charClothes` and `juice` register one model per slot under a single id, told apart only by their label (`Bra State`, `Chest Juice State`); `add_interpolable` takes `name` for that and the error lists the choices. `owner` separates models that different plugins publish under the same id — HS2PE, Flow Control and PregnancyPlus all publish their own here.

**Single files live outside UserData.** They are written next to Timeline's own DLL, not under `UserData`, and carry one object's tracks plus, for a character, the clip it is playing. Loading merges into the current timeline rather than replacing it, and rebinds the tracks onto the object given — which is exactly how an animation built on one character is replayed on another.

**Timeline keeps its own history.** None of these tools touches Studio's `UndoRedoManager`, so a timeline edit is not undoable from the Studio UI and does not disturb the undo stack of the scene edits around it.

### Node constraints (optional third-party plugin)

| Tool | Description | Notes |
|---|---|---|
| `studio_constraint_list` | Every constraint in the scene, fully expanded | the read-back for the three below; `available` says whether NodesConstraints is there |
| `studio_constraint_create` | Make one node follow another | offsets left out are captured from where the nodes are now |
| `studio_constraint_set` | Change one constraint | partial + absolute; the two nodes cannot be changed |
| `studio_constraint_delete` | Drop one or more, restoring their children | every id is validated before anything is removed |

**A constraint is not an attachment.** `studio_attach_object` reparents in Studio's own tree: one hard link, a fixed
offset, and the object's transform switches to the parent's space. A constraint reparents nothing — the workspace
tree stays flat — and can do what an attachment cannot: drive position, rotation and scale independently, drive
only some axes, mirror the parent's motion, turn the child to face the parent (`look_at`), scale or exaggerate the
parent's movement (`*_factor`), lag behind it (`*_damp`), and link **two nodes of two different characters**. The
two tools do not conflict; an attached object can also be a constraint's parent or child.

**A node is `{object_id, target}`.** `target` omitted or `"root"` is the object itself; otherwise it is a bone or
IK target name from `studio_list_bones`, an anchor name from `studio_list_attach_points`, or any transform name or
slash path under the object's root — constraints reach transforms no other tool exposes. Resolution tries those in
that order and a bare name takes the first match in the subtree, so `path` in the read-back is the unambiguous
form and is accepted as `target` too. The two nodes are the constraint's identity: `studio_constraint_set` refuses
to re-target, delete and create instead.

**Creating a constraint does not move anything.** Each offset that is omitted is captured from the nodes' current
relation, the way NodesConstraints' own *Use current* buttons compute it, so a create freezes the pose and the
child only starts moving when the parent does. That is the one deliberate deviation from the plugin's defaults,
whose zero position offset and identity rotation offset would snap the child onto the parent — send
`position_offset: [0,0,0]` when that is what you want, e.g. to put a prop in a hand. The position offset is in the
parent's local space, the rotation offset is applied after the parent's rotation, and the scale offset multiplies
the parent's world scale. `studio_constraint_set` does not re-capture: read the current values and send them.

**`*_locks` say which axes are driven, not which are frozen.** `[true, true, true]` — the default — means the
constraint owns all three; `false` on an axis leaves the child's own value there, which is how you get "follow
horizontally but keep your own height". The field keeps NodesConstraints' own name so a saved scene reads the same
way.

**`enabled` is not a flag.** Turning a constraint off restores the child to the transform captured when the
constraint was created (per `reset_position` / `reset_rotation` / `reset_scale`); turning it on re-captures the
child's current transform as the new resting state. `studio_constraint_set` therefore applies `enabled` after
every other field in the same call, so the restore follows the constraint as that call leaves it. Deleting
restores the same way.

**One constraint per pair, in either direction.** NodesConstraints scans its list before adding and silently
returns nothing on a duplicate, so the pair is checked here first and the error names the existing id.

**Which frame applies a constraint depends on Timeline.** With Timeline installed the pass runs from a Harmony
postfix on its `Update`; without it, from NodesConstraints' own. The two are not the same pass: the vanilla one
skips a channel whose guide object has it disabled and ignores `fix_dynamic_bone`, the Timeline one honours
`fix_dynamic_bone` and ignores the guide flags. `fix_dynamic_bone` is therefore only meaningful while Timeline is
there, which is also the only configuration tested here.

**Constraints live in the scene file.** `ExtendedSave` transpiles `SceneInfo.Save`/`Load`/`Import`, which is
exactly what `studio_save_scene` and `studio_load_scene` go through, so constraints round-trip with the scene —
verified live. They are restored about eight frames after the load, so a `studio_constraint_list` issued
immediately after `studio_load_scene` can still come back empty; read it again. Like the timeline tools, nothing
here touches `UndoRedoManager`: constraint edits are not undoable from the Studio UI and leave the scene's undo
stack intact.

**Its tools are always registered, like Timeline's.** A missing plugin answers `-32001` with the reason, while
`studio_constraint_list` answers normally with `available: false`; `studio_get_info` reports
`plugins.nodes_constraints` as the version or `null`. Tested against NodesConstraints 1.3.3.

## Wire protocol (plugin side)

- `POST /rpc` — JSON-RPC 2.0, method names identical to the MCP tool names, `params` as object.
- `GET /health` — `{"ok":true,"studio":<bool>,"version":"0.1.0"}` — cheap liveness probe.

Error codes: `-32700` parse, `-32600` invalid request, `-32601` unknown method, `-32602` invalid params, `-32000` studio not ready, `-32001` operation failed, `-32002` main-thread timeout, `-32003` not found, `-32603` internal.

## Running

```text
# plugin: built + auto-deployed by
dotnet build plugin/MiraStudio.Plugin -c Release

# MCP server (spawned by the MCP client via .mcp.json)
cd mcp && bun install && bun run start

# smoke test (spawns the server itself)
cd mcp && bun run smoke
```

## Planned next

Studio's own workspace operations, none of which have a tool yet: object visibility (`studio_list_objects` reports
it but nothing writes it), folders (`AddObjectFolder.Add`), renaming — which only persists on folders, since
`OIFolderInfo.name` is serialised while `TreeNodeObject.textName` is the UI label and nothing else — duplication
(`Studio.Duplicate`, driven by the selection), in-scene cameras and routes (`OCIRoute` is a sub-API of its own).
Not covered at all: voices and the `bgmCtrl`/`envCtrl`/`outsideSoundCtrl` settings already present in `SceneInfo`.

Left aside inside the outfit surface: changing the *item* in a slot (`ChangeClothes(kind, id)`, `ChangeAccessory`) and its colours — that is the character maker's job and needs the whole per-sex clothes catalog, which Studio's own Studio-side panels do not expose either. Not covered at all: voices, routes, and the `bgmCtrl`/`envCtrl`/`outsideSoundCtrl` settings already present in `SceneInfo`.
