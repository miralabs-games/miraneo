# Modding APIs Inventory — HS2 DX BetterRepack R16

Game root: `D:\[ScrewThisNoise] HoneySelect 2 DX BetterRepack R16(1)`
Scanned: 2026-08-30 (read-only). 192 DLLs found under `BepInEx\plugins` (recursive).
Note: the actual folder on disk is `BepInEx\Plugins` (capital P); Windows is case-insensitive so `BepInEx\plugins` resolves fine.

## 1. Target DLL matches (name, FileVersion, ProductVersion)

Paths relative to `BepInEx\Plugins\`.

| DLL | FileVersion | ProductVersion | Size | Notes |
|---|---|---|---|---|
| `HS2API.dll` | 1.41 | 1.41 | 278 KB | IllusionModdingAPI (KKAPI family), Company=ManlyMarco. `HS2API.xml` doc file sits next to it. |
| `Timeline.dll` | 1.4.1.0 | 1.4.1.0 | 452 KB | joan6694 HSPlugins (Company=https://github.com/IllusionMods/HSPlugins) |
| `RuntimeUnityEditor\RuntimeUnityEditor.Core.dll` | 5.4.0.0 | 5.4.0.0 | 1632 KB | ManlyMarco RUE |
| `RuntimeUnityEditor\RuntimeUnityEditor.Bepin5.dll` | 5.4.0.0 | 5.4.0.0 | 23 KB | BepInEx 5 loader shim |
| `HS2_BepisPlugins\HS2_Screencap.dll` | 20.0 | 20.0 | 100 KB | BepisPlugins Screencap |
| `HS2_BepisPlugins\HS2_ExtensibleSaveFormat.dll` | 20.0 | 20.0 | 54 KB | ExtendedSave API |
| `HS2_BepisPlugins\HS2_Sideloader.dll` | 20.0 | 20.0 | 163 KB | zipmod loader |
| `HS2_BepisPlugins\HS2_ConfigurationManager.dll` | 20.0 | 20.0 | 22 KB | Bepis variant |
| `ConfigurationManager\ConfigurationManager.dll` | 18.3.0.0 | 18.3.0.0 | 60 KB | ManlyMarco ConfigurationManager |
| `NodesConstraints.dll` | 1.3.3.0 | 1.3.3.0 | 100 KB | joan6694 |
| `HS2_Plugins\HS2_PoseTools.dll` | 1.1.2 | 1.1.2 | 32 KB | |
| `HS2_Plugins\HS2_Subtitles.dll` | 2.3.1 | 2.3.1 | 35 KB | |
| `HS2_Plugins\HS2_TimelineFlowControl.dll` | 1.0.0.0 | 1.0.0.0 | 34 KB | Timeline companion |
| `HS2_CheatTools.dll` | 3.3.2.0 | 3.3.2.0 | 28 KB | |
| `GraphicsSettings.dll` | 1.3.1 | 1.0.0+b31aaa7... | 24 KB | |
| `BepInEx.MessageCenter.dll` | 1.1.1 | 1.1.1 | 10 KB | On-screen message API |

Not found as plugin DLLs (searched names with no match): `ModdingAPI`, `KKAPI` (HS2 build is `HS2API.dll`), `StudioSceneLoader`, `VNGE` (Python/scene-script based; ships uninstalled in `[OPTIONAL] Mods` as `[countd360] VNGE v43.0` and `[Keitaro] VNGE v42.5`), `UnityExplorer`.

## 2. Specific confirmations

- **IllusionModdingAPI for HS2**: CONFIRMED. `BepInEx\Plugins\HS2API.dll` v1.41, ManlyMarco. XML doc (`HS2API.xml`) present — IntelliSense available when referencing.
- **joan6694 Timeline**: CONFIRMED. `BepInEx\Plugins\Timeline.dll` v1.4.1.0 (IllusionMods/HSPlugins). Plus `HS2_TimelineFlowControl.dll` v1.0.0.0.
- **Screenshot plugin**: CONFIRMED. `HS2_BepisPlugins\HS2_Screencap.dll` v20.0 (BepisPlugins Screencap).
- **RuntimeUnityEditor**: CONFIRMED. v5.4.0.0 (`RuntimeUnityEditor.Core.dll` + `RuntimeUnityEditor.Bepin5.dll` in `BepInEx\Plugins\RuntimeUnityEditor\`).

## 3. Top-level inventories

### `[MODDING] Tools`
- dnSpy v6.1.5
- Hooh Modding Tools v0.7.0.0
- SB3Utility v23.1.7
- ZipStudio v0.2.1

### `[OPTIONAL] Mods` (not installed by default; 41 entries)
- [01010101lzy] HSLipsync v0.2.0
- [CalciumSocks] HS2_MultiAngleRotation v1.0.1
- [Calscks] HS2_MultiAngleRotation v1.0.2
- [countd360] MMDD v2.9.4
- [countd360] VNGE v43.0
- [Dainty] Deformers v0.5
- [essu] FemaleParameterInfo v1.0
- [essu] FHS2Ansel v1.0
- [FairBear] HS2 BoobSettings v1.1.0
- [Grey] MeshExporter v0.1.5.5
- [Hanmen] Graphics Plugin Lite v1.0.1
- [HijackHornet] HS2_UIScalerAndWidescreenSupport v1.0.2
- [Hooh] Heelz v1.14.3
- [Keitaro] VNGE v42.5
- [krypto] HS2_StudioNodeTweaks v2.0
- [Marco] OrthographicCamera v1.1.2
- [McHarny] HDSaveCard v1.0
- [Meina] HS2_PauseAnimationsToggle v1.2
- [meinabox] MeinaPlugin v2.3
- [Mike] Playboop v1.0
- [Mikke] Beaver v1.2.2
- [Mikke] CameraClipController v1.0
- [Mikke] Character Loader Plugin v1.4.2
- [Mikke] MapControllerPlugin v1.2
- [Mikke] MoveController v1.9
- [Mikke] PushUpPlugin for HoneySelect2 v2.1.4
- [Mikke] SilhouettePlugin v1.0
- [Mikke] Straight 2 Maker v1.1
- [Mlocke] VR Fluid Fix v1.0
- [OrangeSpork] DynamicBonesGravityPlugin v1.0.1
- [OrangeSpork] HS2VR v0.9.0
- [Prolo] Character Morpher v1.1
- [Prolo] FashionLine v0.3.3
- [Sauceke] Lovemachine v3.0.5
- [Sauceke] Lovemachine v3.19
- [Staksor] HS2_Jiggle v1.0
- [Unknown] HS2_GifPlane
- [Unknown] HS2_VMDPlayPlugin v0.3.4
- [Unknown] mp4Plane v1.1
- OptionalMods_ListDescriptions.txt (8 KB)

### `[OPTIONAL] BRExtras`
- 100% save (dir)
- BetterRepack Official uploader rip (dir)
- UserData stuffs from other games (dir)
- Vanilla DefaultData (dir)
- Vanilla Launcher (dir)
- Vanilla UserData (dir)
- Download VPN to access the uploader.url
- How to play with Linux (Wine-guide).desktop / .url

## 4. Reference assemblies for plugin development

### Game assemblies
- `StudioNEOV2_Data\Managed\` — 114 DLLs. The primary target for MiraStudio (Studio NEO V2). Key: `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll` (+ per-module UnityEngine.*.dll).
- `HoneySelect2_Data\Managed\` — 116 DLLs (main game; same Assembly-CSharp family, if the plugin also targets the main game).

### BepInEx core (`BepInEx\core\`)
- `BepInEx.dll` 5.4.23.2 (BepInEx 5, not 6)
- `0Harmony.dll` 2.9.0.0 (HarmonyX), `BepInEx.Harmony.dll`, `MonoMod.RuntimeDetour.dll` / `MonoMod.Utils.dll` 22.1.29.1, `Mono.Cecil.dll` 0.10.4.0

### Plugin DLLs worth referencing
- `BepInEx\Plugins\HS2API.dll` (v1.41, with `HS2API.xml` docs) — chara/studio/maker APIs, the main abstraction layer.
- `BepInEx\Plugins\Timeline.dll` (1.4.1.0) — animation timeline interop (single-file interop typically done via reflection; no public interop DLL shipped).
- `BepInEx\Plugins\HS2_BepisPlugins\HS2_ExtensibleSaveFormat.dll` (20.0) — ExtendedSave for scene/card extra data.
- `BepInEx\Plugins\HS2_BepisPlugins\HS2_Sideloader.dll` (20.0) — zipmod/GUID resolution.
- `BepInEx\Plugins\HS2_BepisPlugins\HS2_Screencap.dll` (20.0) — screenshot capture.
- `BepInEx\Plugins\NodesConstraints.dll` (1.3.3.0) — node constraint interop for posing.
- `BepInEx\Plugins\RuntimeUnityEditor\RuntimeUnityEditor.Core.dll` (5.4.0.0) — inspection/REPL, useful during development.
- `BepInEx\Plugins\BepInEx.MessageCenter.dll` (1.1.1) — on-screen notifications.
- Also present and potentially useful: `HS2PE.dll` (KKPE-family pose editor), `HS2ABMX.dll` (+xml), `MoreAccessories.dll`, `HS2_OverlayMods.dll` (+xml), `VideoExport\` folder, `ScriptLoader.dll`.

### Patchers (`BepInEx\patchers\`)
`CultureFix.dll` 21.6, `HS2_ExtensibleSaveFormat_Patcher.dll`, `MagicCarrot.dll` 21.6, BepInEx.SplashScreen patchers.

### Loader
`winhttp.dll` + `doorstop_config.ini` at game root (UnityDoorstop). Executables: `HoneySelect2.exe`, `HoneySelect2VR.exe`, `StudioNEOV2.exe`, `InitSetting.exe`.
