# Architecture — layers, rules, decisions

How MiraStudio is split, the rules each layer follows, and why the main choices were made.

## Layers

```text
MCP client (Claude / ChatGPT)
  │  MCP over stdio
  ▼
MiraStudio MCP  (TypeScript / Bun)          mcp/
  │  HTTP JSON-RPC 2.0, localhost:6472
  ▼
MiraStudio.Plugin (BepInEx, C#, net46)      plugin/MiraStudio.Plugin/
  ├── Transport   — HttpListener on localhost, parses requests, dispatches, main-thread dispatcher
  ├── Core        — game-agnostic contracts: DTOs, JSON model, parameter parsing, API exceptions
  ├── Services    — implement the contracts, orchestrate adapter calls, main-thread coroutines
  └── Adapters    — the layer that references Unity / Studio types (StudioAdapter, TimelineAdapter, NodesConstraintsAdapter)
  ▼
Studio NEO V2 (Unity 2018.4.11f1 / Mono)
```

Shared by both sides: `shared/schemas/`, one JSON file per tool with its `description`, `input` and `output`. The MCP server imports the files as its tool definitions; the plugin implements them.

| Layer | Folder | Role |
|---|---|---|
| MCP server | `mcp/src/` | `index.ts` serves MCP over stdio; `tools.ts` lists the 56 tools with a timeout each; `bridge.ts` posts JSON-RPC to the plugin |
| Transport | `Transport/` | `RpcServer` (`POST /rpc`, `GET /health`, batch) and `MainThreadDispatcher` |
| Core | `Core/` | `IStudioApi`, `Dtos`, `Json`, `Params` |
| Services | `Services/` | `StudioService`, `SceneService`, `CameraService`, `MapService`, `KinematicsService`, `ScreenshotService`, `TimelineService`, `ConstraintService` |
| Adapters | `Adapters/` | `StudioAdapter`, plus the reflection bridges `TimelineAdapter` and `NodesConstraintsAdapter` |

## Rules

- **Unity types never cross the adapter boundary.** Core, Services and Transport speak DTOs only: plain serialisable records mirrored by the JSON schemas in `shared/schemas/`.
- **Main-thread discipline.** Studio and Unity APIs are main-thread only. The transport receives requests on worker threads and marshals execution to the Unity main thread (a queue drained in `Update()`); responses complete asynchronously. A worker-thread Unity call does not fail cleanly: it crashes Studio.
- **Stable ids.** Studio's own `ObjectCtrlInfo` dictionary keys are the source of truth for object identity. MiraStudio exposes them as integer ids.
- **Read-only first.** The first shipped tool set was inspection plus screenshot; writes came after.
- **Batch and idempotency.** `studio_execute_batch` executes a list of operations in one round-trip. Write operations accept absolute values, not deltas, and partial patches (an omitted field is unchanged).
- **Strict schemas.** Every tool has a JSON schema; the MCP server serves it as the tool's input schema.
- **Optional plugins are bridged by reflection.** No reference assembly, every member resolved in one all-or-nothing pass, tools registered whether or not the plugin is there. See [optional-plugins.md](./optional-plugins.md).

## Decisions log

| # | Decision | Status |
|---|---|---|
| 1 | Plugin targets BepInEx 5 x64 / net46 (Unity 2018.4.11f1 + Mono, BepInEx 5.4.23.2, HarmonyX 2.9) | confirmed by discovery |
| 2 | MCP in TypeScript on Bun, official MCP SDK, stdio transport | done |
| 3 | Plugin⇄MCP transport: HTTP JSON-RPC 2.0 via `HttpListener` (no dependency, curl-debuggable); push events deferred | done |
| 4 | No HS2API dependency: raw Studio classes only, to be re-evaluated for character creation and extended save | done |
| 5 | Screenshot: own `ScreenCapture.CaptureScreenshotAsTexture` after `WaitForEndOfFrame`, not the Screencap plugin | done |
| 6 | Write ops go through Studio's `UndoRedoManager` commands where they exist, so agent edits are undoable in the UI | done |
| 7 | Write ops take absolute values and partial patches (omitted field = unchanged) for idempotency | done |
| 8 | Third-party plugins (Timeline, NodesConstraints) are soft BepInEx dependencies bridged by reflection; their tools are always registered and fail with a readable `-32001` when the plugin is absent | done |
| 9 | `studio_execute_batch` validates every step up front, runs on the HTTP worker thread, and has no rollback | done |

## Where to go next

- Wire format: [protocol.md](./protocol.md).
- Every tool: [tools.md](./tools.md).
- The Studio API surface the adapter targets and the reverse-engineering method: [internals/studio-api.md](./internals/studio-api.md), [internals/reverse-engineering.md](./internals/reverse-engineering.md).
- Raw discovery notes: [assemblies](./internals/discovery/assemblies.md), [BepInEx](./internals/discovery/bepinex.md), [modding APIs](./internals/discovery/modding-apis.md), [runtime](./internals/discovery/runtime.md), [Studio classes](./internals/discovery/studio-classes.md), [UserData](./internals/discovery/userdata.md).
