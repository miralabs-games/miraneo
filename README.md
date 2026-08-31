# MiraStudio

Semantic Studio NEO V2 API for AI agents. MiraStudio lets an MCP-compatible agent (Claude, ChatGPT, ...) build, inspect and animate scenes in Honey Select 2 / Studio NEO V2 from natural language.

```text
Claude / ChatGPT
       |
       | MCP (stdio)
       v
+----------------------+
|   MiraStudio MCP     |  TypeScript / Bun
+----------+-----------+
           | localhost JSON-RPC
           v
+----------------------+
|  MiraStudio.Plugin   |  BepInEx / C#
+----------+-----------+
           | Studio Adapter
           v
+----------------------+
|   Studio NEO V2      |  Honey Select 2 DX
+----------------------+
```

## Target installation

Honey Select 2 DX — BetterRepack R16 (`StudioNEOV2.exe`, x64, Unity + Mono, BepInEx). Exact versions and the reverse-engineered Studio API surface are documented in [docs/reverse-engineering.md](docs/reverse-engineering.md).

## Repository layout

```text
MiraStudio/
├── docs/                     # architecture, reverse engineering, API references
├── plugin/
│   └── MiraStudio.Plugin/    # BepInEx plugin (C#)
│       ├── Core/             # MiraStudio API (game-agnostic contracts)
│       ├── Adapters/         # Studio NEO V2 adapter (the only layer touching Unity/Studio types)
│       ├── Services/         # scene, character, object, camera, screenshot services
│       └── Transport/        # localhost JSON-RPC endpoint
├── mcp/                      # MCP server (TypeScript / Bun)
├── shared/
│   └── schemas/              # JSON schemas shared by plugin and MCP
└── tools/
    └── reverse-engineering/  # assembly dump / diagnostic scripts
```

## Principles

1. Reverse-engineer first — never code against an imagined API.
2. READ ONLY tools before WRITE tools.
3. Unity/Studio types never leak past the adapter layer; the MCP only sees the MiraStudio API.
4. Every MCP tool has a strict JSON schema.
5. Idempotent operations, stable IDs, batch support, dry-run where possible.

## Status

Phase 4 (partial) — 25 MCP tools live against Studio NEO V2: full read-only inspection + screenshot, asset/scene discovery, loading and saving scenes, camera, create/transform/delete items and lights, characters (create, pose, animation and playback, expression, hands) and attachment to bones, items or folders. See [docs/mcp-api.md](docs/mcp-api.md) for the tool list and [docs/reverse-engineering.md](docs/reverse-engineering.md) for the target analysis.

Not yet implemented: kinematics (IK/FK), outfits, timeline, batching.
