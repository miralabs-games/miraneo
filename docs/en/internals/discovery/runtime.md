# Runtime — engine, loader and version facts of HS2 DX BetterRepack R16

Which Unity, Mono and BepInEx versions the install runs on, how the loader hooks in, and what that means for the MiraStudio plugin.

Game root: `<game>`. Inventory date: 2026-08-30.

## Version files

- `version` file content: `BetterRepack R16`
- `[BR] README.txt`: `Release 16 // 28.08.2024`, base game up to date as of 30.10.2020.

### Components bundled in R16

Versions as listed in the README changelog.

| Component | Version |
|---|---|
| BepInEx | 5.4.23.1 |
| BepisPlugins | R20.0 |
| BepInEx.ConfigurationManager | 18.3 |
| IllusionFixes HS2 | 21.6 |
| HSPlugins | R2.13.1 |
| HS2API (ModdingAPI) | 1.41 |
| HS2ABMX | 5.2.2 |
| HS2_MaterialEditor | 3.9.1 |
| RuntimeUnityEditor | 5.4.0 |
| HS2_CheatTools | 3.3.2 |
| VNGE | 43.0 (optional) |
| XUnity.AutoTranslator | 5.3.1 |
| KKManager | 1.3.1 (utility) |

The installed `BepInEx.dll` reports 5.4.23.2 (see [BepInEx](bepinex.md)); the changelog says 5.4.23.1.

## Engine and executables

`FileVersionInfo` of the main binaries:

| File | FileVersion | ProductVersion |
|---|---|---|
| HoneySelect2.exe | 2018.4.11.7379119 | 2018.4.11.7379119 |
| StudioNEOV2.exe | 2018.4.11.7379119 | 2018.4.11.7379119 |
| UnityPlayer.dll | 2018.4.11.7379119 | 2018.4.11.7379119 |

The engine is **Unity 2018.4.11f1** (LTS). The FileDescription, ProductName and CompanyName fields are empty.

## Architecture and scripting backend

- `StudioNEOV2.exe` PE machine type: **Amd64** (x86-64, 64-bit).
- Scripting backend: **Mono** (`MonoBleedingEdge` present), not IL2CPP.
- `MonoBleedingEdge\EmbedRuntime` contents:
  - `mono-2.0-bdwgc.dll` (4,946,376 bytes)
  - `MonoPosixHelper.dll` (781,312 bytes)

## Loader

### `doorstop_config.ini`

- `enabled = true`
- `target_assembly = BepInEx\core\BepInEx.Preloader.dll` (the BepInEx 5 preloader entrypoint)
- `redirect_output_log = false`
- `[UnityMono]` section present (Doorstop 4.x style): `debug_enabled = false`, `debug_address = 127.0.0.1:10000`, `debug_suspend = false`
- The Mono debugger server can be enabled from this config for development.

### `InitSetting.exe.config`

- .NET Framework launcher: `supportedRuntime v4.0` (fallback v2.0.50727).
- Assembly probing path: `BepInEx\LauncherEN`.

## Process check

`Get-Process HoneySelect2, StudioNEOV2, HoneySelect2VR` returned nothing: no game process was running when the inventory was taken.

## Implications for the MiraStudio plugin

- Target framework: .NET Framework 4.x, net35 to net46 compatible (the Unity 2018.4 Mono runtime matches the .NET 4.x API profile; C# 7.3 compatibility via Mono). See [Assemblies](assemblies.md) for the net46 choice.
- Plugin API: BepInEx 5.4.23.x (`BaseUnityPlugin`, BepInEx 5 namespace), not BepInEx 6.
- The process is 64-bit and the Mono backend gives full reflection and Harmony support, plus the Mono debug server through the Doorstop config.

## See also

- [Assemblies](assemblies.md)
- [BepInEx](bepinex.md)
- [Modding APIs](modding-apis.md)
