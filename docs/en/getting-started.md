# Getting started — build, deploy, connect

From a clean checkout to an agent driving Studio NEO V2: prerequisites, plugin build, MCP registration, health check, smoke test.

## Prerequisites

The exact target the plugin is built and tested against:

| Component | Version |
|---|---|
| Game | Honey Select 2 DX BetterRepack R16 |
| Studio | Studio NEO V2 (`StudioNEOV2.exe`, x64) |
| Engine | Unity 2018.4.11f1, x64, Mono |
| Mod loader | BepInEx 5.4.23.2 |
| Patching library | HarmonyX 2.9 |
| Plugin runtime | .NET Framework 4.6 (`net46`) |
| MCP runtime | Bun |

To build you also need a .NET SDK able to build SDK-style projects (the project pulls the `net46` reference assemblies from NuGet) and Bun.

Optional third-party plugins, tested versions: Timeline 1.4.1 and NodesConstraints 1.3.3. See [optional-plugins.md](./optional-plugins.md).

How these versions were established: [internals/reverse-engineering.md](./internals/reverse-engineering.md) and [internals/discovery/runtime.md](./internals/discovery/runtime.md).

## Build and deploy the plugin

The project needs to know where the game is installed. Copy the example props file and set `GameDir`:

```text
plugin/Miraneo.Plugin/GamePath.props.example  →  plugin/Miraneo.Plugin/GamePath.local.props
```

```xml
<Project>
  <PropertyGroup>
    <GameDir>C:\Path\To\HoneySelect2</GameDir>
  </PropertyGroup>
</Project>
```

`GamePath.local.props` is untracked. Then build:

```sh
dotnet build plugin/Miraneo.Plugin -c Release
```

The build compiles `Miraneo.dll` against the game's own assemblies (`StudioNEOV2_Data\Managed`, `BepInEx\core\BepInEx.dll`) and copies it into `BepInEx\Plugins\Miraneo` under the game folder. Pass `-p:SkipDeploy=true` to compile without copying.

## Plugin configuration

On first launch BepInEx writes `BepInEx\config\com.miraneo.plugin.cfg`:

| Section | Key | Default | Meaning |
|---|---|---|---|
| `Server` | `Port` | `6472` | TCP port of the local JSON-RPC server (`http://localhost:<port>`) |
| `Server` | `RunInBackground` | `true` | Keep Unity running while the Studio window is unfocused, so an agent can keep driving it |

If you change the port, tell the MCP server with the `MIRANEO_URL` environment variable (default `http://localhost:6472`).

## Register the MCP server

The repository ships a `.mcp.json`. Any MCP client that reads it spawns the server over stdio:

```json
{
  "mcpServers": {
    "miraneo": {
      "command": "bun",
      "args": ["run", "mcp/src/index.ts"]
    }
  }
}
```

Install the dependencies once:

```sh
cd mcp && bun install
```

The server can also be started by hand with `bun run start` from `mcp/`.

## Start Studio and check the plugin

1. Launch `StudioNEOV2.exe`.
2. Wait for the Studio scene to load.
3. Probe the plugin:

```sh
curl http://localhost:6472/health
```

```json
{"ok":true,"studio":true,"version":"0.1.0"}
```

`ok` means the plugin is listening. `studio` turns `true` once the Studio scene is ready. Tools that need the scene, such as `studio_get_scene` and `studio_screenshot`, fail with `-32000` (studio not ready) until then; `studio_get_info` works either way.

If the log says the plugin failed to bind the port with an access error, run once from an elevated prompt (replace the user name):

```text
netsh http add urlacl url=http://localhost:6472/ user=<your user>
```

## Smoke test

```sh
cd mcp && bun install && bun x tsc --noEmit && bun run test/smoke.ts
```

The typecheck must pass. The smoke test spawns the MCP server itself, prints the tool list (`tools (56): ...`) and calls `studio_get_info`. Without Studio running, the listing still works and the call reports that the plugin is unreachable at `http://localhost:6472`.

## First scene

Ids cannot be guessed; search before you create. Placeholders below stand for ids returned by the search:

```text
studio_get_info                                  # plugins, versions, studio_loaded
studio_search_catalog {"catalog":"item","query":"chair"}
studio_create_object {"group":<g>,"category":<c>,"no":<n>,"pos":[0,0,0]}
studio_set_camera {"target_pos":[0,1,0],"distance":4}
studio_screenshot {}
studio_save_scene {"name":"agent/first"}
```

Next: every tool in [tools.md](./tools.md), the wire format in [protocol.md](./protocol.md).
