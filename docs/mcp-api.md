# MCP API

The MCP server (`mcp/`, TypeScript/Bun, stdio transport) exposes Studio NEO V2 to any MCP client. Each tool forwards to the plugin's local JSON-RPC endpoint (`http://localhost:6472`, port configurable via `BepInEx\config\com.mirastudio.plugin.cfg` and `MIRASTUDIO_URL` on the MCP side).

Input/output contracts live in [shared/schemas/](../shared/schemas/) — one JSON file per tool (`description`, `input`, `output`). The MCP server imports them directly; the plugin implements them.

### Read

| Tool | Description | Notes |
|---|---|---|
| `studio_get_info` | Runtime info (versions, `studio_loaded`, object count) | works before the Studio scene is ready |
| `studio_get_scene` | Scene summary: object counts per kind, background/frame | errors with `-32000` until Studio is ready |
| `studio_list_characters` | All characters: id, name, card full name, sex, visible, transform, anime, kinematic, face, expression | the read-back tool for every character write |
| `studio_list_objects` | All scene objects: id, kind, name, visible, parent_id, transform, light state | `parent_id` set for attached objects |
| `studio_get_camera` | Viewport camera: target pos, angle, distance, fov, world pos/rot | first four round-trip into `studio_set_camera` |
| `studio_screenshot` | Screenshot of the Studio window, returned as MCP image content | `format` jpg (default) or png, `quality` 1–100 |
| `studio_search_catalog` | Search spawnable/loadable assets — `catalog: item\|light\|anime` | **required** before creating anything: ids cannot be guessed |
| `studio_list_scenes` | Saved scenes under `UserData/Studio/scene` | paths feed `studio_load_scene` |
| `studio_list_character_cards` | Character cards under `UserData/chara/{female,male}` | paths feed `studio_create_character`; first call reads every card, then cached |
| `studio_list_poses` | Pose files under `UserData/studio/pose` | paths feed `studio_set_character_pose`; each pose is sex-specific |

### Write

| Tool | Description | Notes |
|---|---|---|
| `studio_load_scene` | Load a saved scene, replacing the workspace | destructive + not undoable; waits for the map coroutine to finish |
| `studio_set_camera` | Move the viewport camera (target/angle/distance/fov) | partial + absolute; reads back after a frame so `world_pos` is fresh |
| `studio_create_object` | Spawn an item from `(group, category, no)` | optional pos/rot/scale; undoable in Studio |
| `studio_create_light` | Add a light from its catalog `no` | optional transform + colour/intensity/range/spot/shadow |
| `studio_set_light` | Change an existing light's settings | partial + absolute |
| `studio_set_object_transform` | Move/rotate/scale any object by id | partial + absolute → idempotent; undoable in Studio |
| `studio_delete_object` | Delete an object and its children | **not** undoable, and clears Studio's undo stack |
| `studio_create_character` | Add a character from a card path | **not** undoable, and clears the undo stack — same as Studio's own Add menu |
| `studio_set_character_pose` | Apply a pose file: anime + all IK/FK targets + expression flags | sex-checked against the character |
| `studio_set_character_anime` | Set only the base animation `(group, category, no[, normalized_time])` | ids from `studio_search_catalog` with `catalog='anime'` |
| `studio_set_character_expression` | Face: eyebrows/eyes/mouth patterns, openings, blink, tears, blush, look-at, expression categories | partial + absolute |

Every write tool takes **absolute** values and applies only the fields present, so repeating a call is idempotent. Item/light creation and transform edits are pushed onto Studio's native `UndoRedoManager`, so they can be undone from the Studio UI; deletion and character creation cannot (Studio NEO V2 has neither a delete command nor an add-character command, and clears the stack itself on the latter).

**Pose vs anime.** They are two different Studio concepts. An *anime* is a clip from the catalog (`OCIChar.LoadAnime`) and only sets the animation. A *pose file* (`PauseCtrl`, `UserData/studio/pose/*.dat`) additionally restores every IK and FK bone target and the expression category flags, and only applies to a character of the sex it was saved for. The anime catalog itself is **not** sex-filtered — Studio does not filter it either.

**Eye and eyebrow patterns are list positions, not asset ids.** Studio's own face panel shows the index inside the character's per-sex pattern list and maps it through the key array, because mods add non-contiguous ids. `studio_set_character_expression` uses that same index space; read `face.eyebrows_count` / `face.eyes_count` from `studio_list_characters` for the valid range.

**Scene scale is not fixed.** Studio scenes are frequently built at a scale other than 1 unit = 1 m — a real user scene tested here has a ~15-unit-tall character. Read an object's `transform` before choosing camera distances or light ranges instead of assuming metres.

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

`studio_attach_object` (`TreeNodeCtrl.SetParent`), `studio_save_scene` (`SceneInfo.Save`), `studio_set_character_hands` (`OCIChar.ChangeHandAnime`), `studio_set_character_kinematics` (`ActiveKinematicMode` / `ActiveIK` / `ActiveFK` + per-bone targets), `execute_batch`.
