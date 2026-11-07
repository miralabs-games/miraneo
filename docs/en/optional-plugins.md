# Optional plugins — Timeline and NodesConstraints

Two third-party Studio plugins are bridged when installed: Timeline (keyframe animation, nine tools) and NodesConstraints (four tools). Everything else works without them.

## Failure model

Both bridges follow the same rule: **unconditional registration, explicit failure.**

| Situation | What happens |
|---|---|
| Plugin installed | The tools work. |
| Plugin missing | Every tool of the group fails with `-32001` and the reason, except the group's read tool, which answers normally with `available: false`. |
| Plugin moved a member | The bridge resolves the plugin's members by reflection in one all-or-nothing pass, so a version that changed something reports itself unavailable instead of half-working. |

The MCP tool list is static, so a missing plugin has to answer with a readable error rather than `unknown method`. Check `studio_get_info` first: `plugins.timeline` and `plugins.nodes_constraints` hold the plugin version, or `null` when it is absent.

On the C# side both plugins are declared as BepInEx soft dependencies, so BepInEx loads them first when they are installed; the bridges bind by reflection either way.

| Plugin | Tested version | Read tool |
|---|---|---|
| Timeline (joan6694) | 1.4.1 | `studio_timeline_get_state` |
| NodesConstraints (joan6694) | 1.3.3 | `studio_constraint_list` |

How the reflection split works (public versus private members, the selection trick): [internals/studio-api.md](./internals/studio-api.md).

## Timeline

| Tool | Params | Notes |
|---|---|---|
| `studio_timeline_get_state` | `object_id`, `include_keyframes` (default `true`) | Transport plus every interpolable and its keyframes. The read-back for all the tools below; `available` says whether Timeline is there. |
| `studio_timeline_playback` | **`action`** `play`\|`pause`\|`stop`\|`seek`\|`next_frame`\|`previous_frame`, `time` | Seeking applies the pose immediately, so `seek` plus `studio_screenshot` renders a given moment. |
| `studio_timeline_set_settings` | `duration`, `frame_rate` (≥ 1) | Timeline length and the frame rate the steps use. Partial and absolute. |
| `studio_timeline_list_models` | `object_id`, `query`, `limit` (default 100) | The animatable properties, filtered to what a given object accepts. **Required** before adding: 95 models on a character here, only 38 of them Timeline's own. |
| `studio_timeline_add_interpolable` | **`object_id`**, **`model`**, `owner`, `name`, `target`, `alias` | Binds one property to one object, creating the track. Idempotent: re-adding returns the existing track. |
| `studio_timeline_remove_interpolable` | **`id`** | Drops one or more tracks, keyframes included. Every id is validated before anything is removed. |
| `studio_timeline_set_keyframe` | **`interpolable_id`**, **`time`**, `value`, `curve` | Puts or overwrites a keyframe, with an easing curve. Omit `value` to capture the scene as it stands. |
| `studio_timeline_delete_keyframe` | **`interpolable_id`**, **`time`** | Removes one keyframe by its exact time. Times are floats: read them back rather than recomputing. |
| `studio_timeline_single_file` | **`action`** `list`\|`load`\|`save`\|`delete`, `name`, `object_id`, `overwrite` | One object's animation as a reusable file. How an animation moves from one character to another. |

`play` is idempotent here: it never toggles back to pause, unlike Timeline's own button. `seek` needs a `time` between 0 and the timeline duration; `stop` rewinds to 0 and applies that pose. When pairing a seek with a screenshot, take the screenshot in its own call, not in the same batch, since the frame it captures is the one before.

### Pose, then record

Add the interpolable, seek to a time, pose the scene with the ordinary tools (`studio_set_character_bone`, `studio_set_character_ik`, `studio_set_object_transform`, ...), then call `studio_timeline_set_keyframe` **without** `value`. The keyframe captures the property exactly as it stands, which is what Timeline's own button does and the only option for value types with no JSON form.

```json
{"steps": [
  {"method": "studio_timeline_add_interpolable", "params": {"object_id": 4, "model": "guideObjectRot", "target": "cf_J_ArmUp00_L"}},
  {"method": "studio_timeline_playback", "params": {"action": "seek", "time": 0}},
  {"method": "studio_set_character_bone", "params": {"id": 4, "bones": {"cf_J_ArmUp00_L": [0, 0, 0]}}},
  {"method": "studio_timeline_set_keyframe", "params": {"interpolable_id": 0, "time": 0}}
]}
```

Ids and values above are placeholders: read the real ones from `studio_timeline_list_models`, `studio_list_bones` and the result of the add. Passing `value` explicitly works for the ordinary types (numbers, booleans, `[x,y,z]`, euler degrees, `[r,g,b,a]`) and is typed against the property's current value, so a wrong shape is refused rather than coerced.

`studio_execute_batch` is the right vehicle for this: pose and record pairs alternate correctly inside one batch, because every write lands on the change amounts synchronously and the capture that follows sees it.

### Animating a bone still needs FK or IK on

`guideObjectPos`, `guideObjectRot` and `guideObjectScale` are the per-target models, and they write the same change amounts `studio_set_character_bone` and `studio_set_character_ik` do. `target` takes `root` for the object itself or any bone or IK target name from `studio_list_bones`; the bridge resolves the guide object from Studio's own `BoneInfo` and `IKInfo`, so nothing touches the Studio selection. But `FKCtrl` still only applies a bone while its chain is enabled and the bone is weighted: with kinematics off, the animation is stored, read back correctly, and never reaches the mesh.

### Curves

Timeline's own five presets: `line`, `top` (fast then slow), `bottom` (slow then fast), `hermite` (eased both ends) and `stairs` (a hard step), with the exact tangents Timeline's keyframe window applies. Or explicit `[time, value, in_tangent, out_tangent]` points. An omitted curve inherits the preceding keyframe's, falling back to linear, the same inheritance Timeline's `AddKeyframe` applies. `stairs` carries an infinite tangent, which JSON cannot spell, so it round-trips through `null`.

### Ids that share a name

`charClothes` and `juice` register one model per slot under a single id, told apart only by their label (`Bra State`, `Chest Juice State`). `studio_timeline_add_interpolable` takes `name` for that, and the error lists the choices. `owner` separates models that different plugins publish under the same id: HS2PE, Flow Control and PregnancyPlus all publish their own here.

### Single files and history

Single files live outside `UserData`: they are written next to Timeline's own DLL, and carry one object's tracks plus, for a character, the clip it is playing. Loading merges into the current timeline rather than replacing it, and rebinds the tracks onto the object given. That is exactly how an animation built on one character is replayed on another.

Timeline keeps its own history. None of these tools touches Studio's `UndoRedoManager`, so a timeline edit is not undoable from the Studio UI and does not disturb the undo stack of the scene edits around it.

## NodesConstraints

| Tool | Params | Notes |
|---|---|---|
| `studio_constraint_list` | `object_id` | Every constraint in the scene, fully expanded. The read-back for the three below; `available` says whether NodesConstraints is there. |
| `studio_constraint_create` | **`parent`**, **`child`**, `enabled`, `alias`, `position`, `rotation`, `scale`, `look_at`, `mirror_*`, `*_offset`, `*_factor`, `*_damp`, `*_locks`, `reset_*`, `fix_dynamic_bone` | Makes one node follow another. Offsets left out are captured from where the nodes are now. |
| `studio_constraint_set` | **`id`**, the same options | Changes one constraint. Partial and absolute; the two nodes cannot be changed. |
| `studio_constraint_delete` | **`id`** (integer or array) | Drops one or more, restoring their children. Every id is validated before anything is removed. |

### A constraint is not an attachment

`studio_attach_object` reparents in Studio's own tree: one hard link, a fixed offset, and the object's transform switches to the parent's space. A constraint reparents nothing, so the workspace tree stays flat, and it can do what an attachment cannot:

- drive position, rotation and scale independently, or only some axes;
- mirror the parent's motion;
- turn the child to face the parent (`look_at`);
- scale or exaggerate the parent's movement (`*_factor`) or lag behind it (`*_damp`);
- link **two nodes of two different characters**.

The two tools do not conflict: an attached object can also be a constraint's parent or child.

### A node is `{object_id, target}`

`target` omitted or `"root"` is the object itself. Otherwise it is a bone or IK target name from `studio_list_bones`, an anchor name from `studio_list_attach_points`, or any transform name or slash path under the object's root: constraints reach transforms no other tool exposes. Resolution tries those in that order and a bare name takes the first match in the subtree, so `path` in the read-back is the unambiguous form and is accepted as `target` too. The two nodes are the constraint's identity: `studio_constraint_set` refuses to re-target, delete and create instead.

```json
{
  "parent": {"object_id": 4, "target": "N_Hand_L"},
  "child":  {"object_id": 9},
  "position_offset": [0, 0, 0]
}
```

### Creating a constraint does not move anything

Each offset that is omitted is captured from the nodes' current relation, the way NodesConstraints' own *Use current* buttons compute it, so a create freezes the pose and the child only starts moving when the parent does. That is the one deliberate deviation from the plugin's defaults, whose zero position offset and identity rotation offset would snap the child onto the parent. Send `position_offset: [0,0,0]` when that is what you want, for example to put a prop in a hand (as above). The position offset is in the parent's local space, the rotation offset is applied after the parent's rotation, and the scale offset multiplies the parent's world scale. `studio_constraint_set` does not re-capture: read the current values and send them.

### `*_locks` say which axes are driven

`[true, true, true]`, the default, means the constraint owns all three. `false` on an axis leaves the child's own value there, which is how you get "follow horizontally but keep your own height". The field keeps NodesConstraints' own name so a saved scene reads the same way.

### `enabled` is not a flag

Turning a constraint off restores the child to the transform captured when the constraint was created (per `reset_position`, `reset_rotation`, `reset_scale`); turning it on re-captures the child's current transform as the new resting state. `studio_constraint_set` therefore applies `enabled` after every other field in the same call, so the restore follows the constraint as that call leaves it. Deleting restores the same way.

### One constraint per pair

NodesConstraints scans its list before adding and silently returns nothing on a duplicate, so the pair is checked here first, in either direction, and the error names the existing id.

### Which frame applies a constraint depends on Timeline

With Timeline installed the pass runs from a Harmony postfix on its `Update`; without it, from NodesConstraints' own. The two are not the same pass: the vanilla one skips a channel whose guide object has it disabled and ignores `fix_dynamic_bone`, the Timeline one honours `fix_dynamic_bone` and ignores the guide flags. `fix_dynamic_bone` is therefore only meaningful while Timeline is there, which is also the only configuration tested here.

### Constraints live in the scene file

`ExtendedSave` transpiles `SceneInfo.Save`, `Load` and `Import`, which is exactly what `studio_save_scene` and `studio_load_scene` go through, so constraints round-trip with the scene (verified live). They are restored about eight frames after the load, so a `studio_constraint_list` issued immediately after `studio_load_scene` can still come back empty: read it again. Like the timeline tools, nothing here touches `UndoRedoManager`: constraint edits are not undoable from the Studio UI and leave the scene's undo stack intact.
