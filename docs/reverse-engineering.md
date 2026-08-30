# Reverse Engineering — Target Installation

> Snapshot: 2026-08-30. Raw agent reports with the full dumps live in [discovery/](discovery/).

## Identification

| Item | Value |
|---|---|
| Game | Honey Select 2 DX — BetterRepack R16 (released 2024-08-28) |
| Studio | Studio NEO V2 (`StudioNEOV2.exe`) |
| Install path | `D:\[ScrewThisNoise] HoneySelect 2 DX BetterRepack R16(1)` |
| Unity | 2018.4.11f1 |
| Architecture | x86-64 (PE Machine = Amd64) |
| Scripting backend | Mono (`MonoBleedingEdge`), .NET 4.x profile — **not IL2CPP** |
| Plugin target framework | `net46` (mscorlib/System.Core 4.6.57.0, no `netstandard.dll`) |
| Loader | BepInEx 5.4.23 (DLL FileVersion 5.4.23.2) via Unity Doorstop (`winhttp.dll`) |
| Harmony | HarmonyX 2.9.0 (`0Harmony.dll`) |

**Path gotcha:** the install path contains `[ ] ( )`. Always `-LiteralPath` in PowerShell, never glob patterns against it, and quote it carefully in MSBuild props.

## Loader stack

- Doorstop → `BepInEx\core\BepInEx.Preloader.dll` → BepInEx 5.4.23.2. Entrypoint is the default (`UnityEngine.CoreModule` / `Application` / `.cctor`).
- Patchers: CultureFix 21.6, MagicCarrot 21.6, HS2_ExtensibleSaveFormat_Patcher, BepInEx.SplashScreen 2.2.
- **Console AND disk logging are both disabled** in `BepInEx\config\BepInEx.cfg` — plugin dev needs one of them enabled, or a self-provisioned log sink.
- Plugin dir: `BepInEx\Plugins` (capital P), 192 active DLLs (+2 disabled as `.dl_`).
- ScriptLoader compiles C# from the game-root `scripts\` folder at runtime (`AYAYA_v2.cs` present) — interaction surface to keep in mind.
- Doorstop exposes an optional Mono debug server (`127.0.0.1:10000`, currently disabled).

## Assemblies

- Reference dir: `StudioNEOV2_Data\Managed` (114 DLLs). Studio ships its **own** `Assembly-CSharp.dll` (5 001 728 B, differs from the HoneySelect2 copy) — always reference the Studio copy.
- All Studio + character code lives in `Assembly-CSharp.dll` (4660 TypeDefinitions); there are no Studio-only satellite assemblies.
- Notables: `IL.dll` (contains `NeckLookControllerVer2`), Sirenix/Odin 2.0.13, UniRx + UniRx.Async, modular `UnityEngine.*Module` DLLs.
- Avoid `System.Drawing`/`System.Windows.Forms` (ancient 2.0.50727 builds).

## Studio API surface (metadata dump)

Full type list and member dumps: [discovery/studio-classes.md](discovery/studio-classes.md). The Studio namespace has 184 top-level + 459 nested types. Load-bearing classes:

| Class | Role |
|---|---|
| `Studio.Studio` | Scene singleton / god-object — `dicObjectCtrl`, `sceneInfo`, `treeNodeCtrl`, `AddFemale/AddMale/AddItem/AddLight/AddFolder/AddRoute/AddCamera`, `GetCtrlInfo`, `LoadScene/SaveScene/ImportScene`, `Duplicate` |
| `Studio.ObjectCtrlInfo` | Base of every scene object — `objectInfo`, `treeNodeObject`, `guideObject` |
| `Studio.OCIChar` | Character in scene (99 methods) — `charInfo`/`charReference` → `AIChara.ChaControl`, `fkCtrl`, `ikCtrl`, `neckLookCtrl`, `finalIK`; `ActiveKinematicMode`, `ActiveIK/ActiveFK`, `LoadAnime`, `ChangeMouthPtn`, `ChangeEyesOpen`… |
| `Studio.OCIItem` / `OCILight` / `OCIFolder` / `OCICamera` / `OCIRoute` | Other scene object kinds; serializable twins are the `OI*Info` classes |
| `Studio.FKCtrl` / `IKCtrl` / `ItemFKCtrl` | FK/IK control; kinematic mode enum is `Studio.OICharInfo+KinematicMode` |
| `Studio.CameraControl` | The **viewport** camera (Import/Export CameraData) — distinct from the in-scene `OCICamera` object |
| `Studio.SceneInfo` | Serializable scene model incl. chara/map lights and post-processing (bloom/AO/DOF/fog/sunShafts/vignette), `Save/Load/Import` |
| `Studio.Info` | Asset catalogs — `dicItemLoadInfo`, `dicAnimeLoadInfo`, `dicLightLoadInfo`, `dicMapLoadInfo`: the enumeration source for spawnable content |
| `Studio.TreeNodeCtrl` | Scene hierarchy tree |
| `Studio.GuideObject` | Transform gizmo/handles |
| `Studio.UndoRedoManager` + `*Command` | Native undo/redo — route write operations through commands where possible |
| `AIChara.ChaControl` | Character body/face/clothes (426 methods; namespace `AIChara`, not root) |
| `NeckLookControllerVer2` (IL.dll) | Neck/eye look-at (`ptnNo`, `target`, `rate`) |

Caveats:
- Dumps are **method names only** — exact signatures via dnSpy (bundled in `[MODDING] Tools`) before writing Harmony patches or tricky calls.
- `LightCtrl` and `MoveAnimator` do **not** exist in this build; lights are `OCILight` + `SceneInfo.charaLight/mapLight`.

## Modding ecosystem (installed and relevant)

| Plugin | Version | Relevance for MiraStudio |
|---|---|---|
| HS2API (IllusionModdingAPI) | 1.41 | official modding API, XML docs beside the DLL |
| Timeline (joan6694) | 1.4.1.0 | keyframe animation — Phase 4 target; no reference DLL, integration is reflection-based |
| HS2_Screencap (BepisPlugins R20) | 20.0 | screenshot reference implementation |
| RuntimeUnityEditor | 5.4.0 | live inspection during development |
| NodesConstraints | 1.3.3.0 | constraints — Phase 5 |
| HS2PE (PoseEditor) | — | pose data reference |
| ConfigurationManager | 18.3.0 | config UI |

Bundled tools: dnSpy 6.1.5, SB3Utility 23.1.7, Hooh Modding Tools 0.7, ZipStudio 0.2.1 (in `[MODDING] Tools`).

## Content paths

| What | Where |
|---|---|
| Studio scenes | `UserData\Studio\scene` (107 files) |
| Poses | `UserData\Studio\pose` (3868 files) |
| Character cards | `UserData\chara\female` (1050), `UserData\chara\male` (30) |
| Coordinates | `UserData\coordinate\female` (1800), `UserData\coordinate\male` (166) |
| Screenshots | `UserData\cap` — **does not exist** until the first in-game capture |

## Dev machine

- dotnet SDK 8.0.424 / 9.0.313 (SDK-style csproj + `Microsoft.NETFramework.ReferenceAssemblies` for net46), bun 1.4.0, node 22.15.0, no MSBuild in PATH.
- Game/Studio processes were not running during discovery.

## Consequences for MiraStudio

1. Plugin = BepInEx 5 `BaseUnityPlugin`, `net46`, x64; HarmonyX 2.9 available if patching is ever needed (not for v0 read-only).
2. Studio/Unity APIs are main-thread only → the transport thread must marshal work into `Update()` and complete responses asynchronously.
3. Stable object IDs come from `Studio.Studio.dicObjectCtrl` keys.
4. The whole v0 read-only surface is reachable without patches: scene (`sceneInfo`), objects (`dicObjectCtrl`), camera (`CameraControl`), characters (`OCIChar` → `ChaControl`), screenshot (Unity `ScreenCapture`, main thread).
5. Logging must be self-provisioned (repack disables BepInEx console + disk logs).
