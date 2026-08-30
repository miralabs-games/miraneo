# Runtime Discovery — HS2 DX BetterRepack R16

Game root: `D:\[ScrewThisNoise] HoneySelect 2 DX BetterRepack R16(1)`
Inventory date: 2026-08-30

## Version files

- `version` file content: `BetterRepack R16`
- `[BR] README.txt`: `Release 16 // 28.08.2024` — base game up2date as of 30.10.2020.

### Key plugin versions bundled in R16 (from README changelog)

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

## doorstop_config.ini

- `enabled = true`
- `target_assembly = BepInEx\core\BepInEx.Preloader.dll` (BepInEx 5 preloader entrypoint)
- `redirect_output_log = false`
- `[UnityMono]` section present (Doorstop 4.x style): `debug_enabled = false`, `debug_address = 127.0.0.1:10000`, `debug_suspend = false`
- Mono debugger server can be enabled via this config if needed for development.

## InitSetting.exe.config

- .NET Framework launcher: `supportedRuntime v4.0` (fallback v2.0.50727)
- Assembly probing path: `BepInEx\LauncherEN`

## Executable / engine versions (FileVersionInfo)

| File | FileVersion | ProductVersion |
|---|---|---|
| HoneySelect2.exe | 2018.4.11.7379119 | 2018.4.11.7379119 |
| StudioNEOV2.exe | 2018.4.11.7379119 | 2018.4.11.7379119 |
| UnityPlayer.dll | 2018.4.11.7379119 | 2018.4.11.7379119 |

Unity engine: **Unity 2018.4.11f1** (LTS). FileDescription/ProductName/CompanyName fields are empty.

## Architecture and scripting backend

- StudioNEOV2.exe PE machine type: **Amd64** (x86-64, 64-bit)
- Scripting backend: **Mono** (MonoBleedingEdge present — not IL2CPP)
- `MonoBleedingEdge\EmbedRuntime` contents:
  - `mono-2.0-bdwgc.dll` (4,946,376 bytes)
  - `MonoPosixHelper.dll` (781,312 bytes)

## Process check

`Get-Process HoneySelect2, StudioNEOV2, HoneySelect2VR` → none running at inventory time.

## Implications for MiraStudio plugin

- Target framework for plugin: .NET Framework 4.x / net35-net46 compatible (Unity 2018.4 Mono ≈ .NET 4.x equivalent profile, C# 7.3 compat via Mono).
- BepInEx 5.4.23.1 plugin API (`BaseUnityPlugin`, BepInEx 5 namespace) — not BepInEx 6.
- 64-bit process; Mono backend means full reflection/Harmony support, and Mono debug server available via doorstop config.
