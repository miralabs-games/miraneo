# Protocol — JSON-RPC over localhost

The plugin serves JSON-RPC 2.0 over HTTP on `localhost:6472`. The MCP server is one client of it; `curl` is another.

## Endpoints

| Method and path | Purpose |
|---|---|
| `POST /rpc` | JSON-RPC 2.0 call. Method names are the MCP tool names; `params` is an object. |
| `GET /health` | Cheap liveness probe. |

Any other method or path answers HTTP 404 with a `-32600` error body (`use POST /rpc or GET /health`).

The server is an `HttpListener` bound to `http://localhost:<port>/`. The port is `Server.Port` in `BepInEx\config\com.miraneo.plugin.cfg` (default `6472`); the MCP side reads `MIRANEO_URL` (default `http://localhost:6472`).

## Health

```sh
curl http://localhost:6472/health
```

```json
{"ok":true,"studio":true,"version":"0.1.0"}
```

`studio` is `false` until the Studio scene is ready. The flag is refreshed from Unity's `Update()`, so `/health` never touches Unity from a worker thread.

## A call

```sh
curl -s http://localhost:6472/rpc -d '{"jsonrpc":"2.0","id":1,"method":"studio_get_camera","params":{}}'
```

```json
{"jsonrpc":"2.0","id":1,"result":{ "...": "same shape as the tool output" }}
```

The result is the tool's `output` from [`shared/schemas/`](../../shared/schemas/). Errors, including every failure of a tool, come back with HTTP 200 and an `error` object:

```json
{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"..."}}
```

On the MCP side an error becomes a tool result with `isError: true` and the text `Studio error <code>: <message>`. If the plugin cannot be reached at all, the message says so and names the URL.

## Error codes

| Code | Meaning |
|---|---|
| `-32700` | Parse error: the body is not valid JSON. |
| `-32600` | Invalid request: not a JSON object, no `method`, or wrong path. |
| `-32601` | Unknown method. |
| `-32602` | Invalid params. |
| `-32000` | Studio not ready: the Studio scene is not loaded yet. |
| `-32001` | Operation failed. Also what a tool of a missing optional plugin answers, with the reason. |
| `-32002` | Main-thread timeout: the operation did not finish in time. |
| `-32003` | Not found: the id, path or name does not exist. |
| `-32603` | Internal error. |

## Threading and timeouts

Studio and Unity APIs are main-thread only. The HTTP listener receives each request on a worker thread; the handler is queued and runs on the Unity main thread, drained in `Update()`. Tools that wait on a coroutine (screenshot, scene load and save, map load, camera, kinematics) start on the main thread and complete later.

Each method has its own timeout, and the MCP side has its own too:

| MCP-side timeout | Tools |
|---|---|
| 10 s | `studio_get_info` |
| 15 s | `studio_get_scene`, `studio_list_characters`, `studio_list_objects`, `studio_get_camera`, `studio_list_scenes`, `studio_list_outfit`, `studio_list_attach_points`, `studio_list_bones`, `studio_list_overlays`, `studio_set_light`, `studio_set_object_transform`, `studio_delete_object`, `studio_set_object_visible`, `studio_rename_object`, `studio_create_folder`, `studio_set_camera`, expression, hands, state, bone, IK, `studio_attach_object`, scene lighting, and the Timeline and NodesConstraints tools except `studio_timeline_single_file` |
| 30 s | `studio_search_catalog`, `studio_set_character_kinematics` |
| 90 s | `studio_screenshot`, `studio_save_scene`, `studio_create_object`, `studio_create_light`, `studio_duplicate_object`, pose, anime, overlay, post-processing, `studio_timeline_single_file` |
| 180 s | `studio_create_character`, `studio_set_character_outfit` |
| 240 s | `studio_list_character_cards`, `studio_list_poses`, `studio_list_coordinates`, `studio_load_scene`, `studio_set_map` |
| 300 s | `studio_execute_batch` |

## execute_batch

`studio_execute_batch` runs up to 32 steps in order in one call. Each step is `{method, params}`; `method` is any Miraneo tool except `studio_screenshot` and `studio_execute_batch` itself.

```json
{"jsonrpc":"2.0","id":2,"method":"studio_execute_batch","params":{"steps":[
  {"method":"studio_create_object","params":{"group":1,"category":1,"no":1,"pos":[0,0,0]}},
  {"method":"studio_set_camera","params":{"distance":5}}
]}}
```

Group, category and `no` above are placeholders: read real ones from `studio_search_catalog`.

**Validated up front.** Before anything runs, every step is checked: `steps` must be a non-empty array of at most 32 objects, each with a `method`, never the batch itself (no nesting), never `studio_screenshot` (its image would come back as base64 text no client can render), the method must exist, and `params` must be an object. One bad step refuses the whole batch with `-32602`, and nothing was applied.

**No rollback.** Once execution starts, a failing step stops the batch and everything before it stays applied. Studio has no transaction, `UndoRedoManager.Do` pushes its command *before* executing it, and several tools clear the stack outright, so there is nothing to roll back to.

**Always a result.** A started batch returns a result rather than an RPC error:

```json
{
  "ok": false,
  "executed": 1,
  "stopped_at": 1,
  "error": {"step": 1, "method": "studio_set_camera", "code": -32602, "message": "..."},
  "steps": [{"step": 0, "method": "studio_create_object", "result": {"...": "..."}}]
}
```

| Field | Meaning |
|---|---|
| `ok` | Every step ran. |
| `executed` | How many steps ran; on failure it is also the index of the failing step. |
| `stopped_at` | Index of the failing step, `null` when `ok`. |
| `error` | `null` when `ok`, else `{step, method, code, message}` with the same codes as a direct call. |
| `steps` | Each step's `result` exactly as the tool would have returned it alone. |

**Batching saves turns, not milliseconds.** A call costs about a millisecond of loopback plus one frame, so a batch is worth it when it removes LLM round-trips: a scene to build, a limb to pose, props to spawn and place. Prefer the multi-entry tools (`studio_set_character_bone`, `studio_set_character_ik`) over a batch of single writes: they apply as one undo step, a batch does not.

The batch loop runs on the HTTP worker thread, not on the main thread. Every step marshals itself, so running the loop inside the main-thread queue would have the main thread waiting on a queue only it can drain.
