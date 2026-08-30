# Architecture

## Layers

```text
MCP client (Claude / ChatGPT)
  │  MCP over stdio
  ▼
MiraStudio MCP  (TypeScript / Bun)
  │  localhost JSON-RPC (transport TBD: HTTP vs WebSocket)
  ▼
MiraStudio.Plugin (BepInEx, C#)
  ├── Transport   — listens on localhost, deserializes requests, dispatches
  ├── Core        — MiraStudio API: game-agnostic contracts (IStudioScene, ICharacterController, IObjectController, ...)
  ├── Services    — implement Core contracts, orchestrate adapter calls, main-thread marshalling
  └── Adapters    — the ONLY layer referencing Unity / Studio types (Studio.Studio, OCIChar, ...)
  ▼
Studio NEO V2 (Unity / Mono)
```

## Rules

- **Unity types never cross the adapter boundary.** Core/Services/Transport speak DTOs only (plain serializable records mirrored by JSON schemas in `shared/schemas/`).
- **Main-thread discipline.** Studio/Unity APIs are main-thread only. The transport receives requests on a worker thread and marshals execution to the Unity main thread (queue drained in `Update()`); responses complete asynchronously.
- **Stable IDs.** Studio's own `ObjectCtrlInfo` dictionary keys are the source of truth for object identity; MiraStudio exposes them as opaque string IDs.
- **READ ONLY first.** The first shipped tool set is inspection + screenshot only.
- **Batch + idempotency.** `execute_batch([...])` executes a list of operations in one round-trip; write operations accept absolute values (not deltas) wherever possible.
- **Logging.** Tagged channels: `[MiraStudio]`, `[MCP]`, `[StudioAdapter]`, `[Animation]`, `[IK]`.

## Decisions log

| # | Decision | Status |
|---|----------|--------|
| 1 | Plugin targets BepInEx 5 x64 / net46 (Unity 2018.4.11f1 + Mono, BepInEx 5.4.23.2, HarmonyX 2.9) | confirmed by discovery |
| 2 | MCP in TypeScript on Bun, official MCP SDK, stdio transport | done |
| 3 | Plugin⇄MCP transport: HTTP JSON-RPC 2.0 via `HttpListener` (no dependency, curl-debuggable); push events deferred | done |
| 4 | No HS2API dependency — raw Studio classes only, re-evaluate for character creation / extended save | done |
| 5 | Screenshot: own `ScreenCapture.CaptureScreenshotAsTexture` after `WaitForEndOfFrame`, not the Screencap plugin | done |
| 6 | Write ops go through Studio's `UndoRedoManager` commands where they exist, so agent edits are undoable in the UI | done |
| 7 | Write ops take absolute values and partial patches (omitted field = unchanged) for idempotency | done |
