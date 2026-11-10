# Reverse Engineering — target installation

What Miraneo runs against: the exact Honey Select 2 DX / Studio NEO V2 install, its loader stack, assemblies, Studio classes and modding ecosystem, as found by inspecting the repack and decompiling its assemblies (snapshot 2026-08-30).

Raw reports with the full dumps live in [discovery/](discovery/): [assemblies](discovery/assemblies.md), [bepinex](discovery/bepinex.md), [modding-apis](discovery/modding-apis.md), [runtime](discovery/runtime.md), [studio-classes](discovery/studio-classes.md), [userdata](discovery/userdata.md). What the plugin does with these APIs is in [studio-api.md](studio-api.md).

## Identification

| Item | Value |
|---|---|
| Game | Honey Select 2 DX — BetterRepack R16 (released 2024-08-28) |
| Studio | Studio NEO V2 (`StudioNEOV2.exe`) |
| Install root | `<game>` — the repack folder; its name contains `[ ] ( )` |
| Unity | 2018.4.11f1 |
| Architecture | x86-64 (PE Machine = Amd64) |
| Scripting backend | Mono (`MonoBleedingEdge`), .NET 4.x profile — **not IL2CPP** |
| Plugin target framework | `net46` (mscorlib/System.Core 4.6.57.0, no `netstandard.dll`) |
| Loader | BepInEx 5.4.23 (DLL FileVersion 5.4.23.2) via Unity Doorstop (`winhttp.dll`) |
| Harmony | HarmonyX 2.9.0 (`0Harmony.dll`) |

**Path gotcha:** the install path contains `[ ] ( )`. Always use `-LiteralPath` in PowerShell, never glob patterns against it, and quote it carefully in MSBuild props.

## Loader stack

- Doorstop → `BepInEx\core\BepInEx.Preloader.dll` → BepInEx 5.4.23.2. The entrypoint is the default (`UnityEngine.CoreModule` / `Application` / `.cctor`).
- Patchers: CultureFix 21.6, MagicCarrot 21.6, HS2_ExtensibleSaveFormat_Patcher, BepInEx.SplashScreen 2.2.
- **Console AND disk logging are both disabled** in `BepInEx\config\BepInEx.cfg` — plugin development needs one of them enabled, or a self-provisioned log sink.
- Plugin directory: `BepInEx\Plugins` (capital P), 192 active DLLs (+2 disabled as `.dl_`).
- ScriptLoader compiles C# from the game-root `scripts\` folder at runtime (`AYAYA_v2.cs` present) — an interaction surface to keep in mind.
- Doorstop exposes an optional Mono debug server (`127.0.0.1:10000`, currently disabled).

## Assemblies

- Reference directory: `StudioNEOV2_Data\Managed` (114 DLLs). Studio ships its **own** `Assembly-CSharp.dll` (5 001 728 B, differs from the HoneySelect2 copy) — always reference the Studio copy.
- All Studio and character code lives in `Assembly-CSharp.dll` (4660 TypeDefinitions); there are no Studio-only satellite assemblies.
- Notables: `IL.dll` (contains `NeckLookControllerVer2`, `Singleton<T>`, `UserData`, `PngFile`), Sirenix/Odin 2.0.13, UniRx + UniRx.Async, modular `UnityEngine.*Module` DLLs.
- Avoid `System.Drawing` and `System.Windows.Forms` (ancient 2.0.50727 builds).

## Studio API surface

The metadata dump lists 184 top-level + 459 nested types in the `Studio` namespace. Full type list and member dumps: [discovery/studio-classes.md](discovery/studio-classes.md). Load-bearing classes:

| Class | Role |
|---|---|
| `Studio.Studio` | Scene singleton / god-object — `dicObjectCtrl`, `sceneInfo`, `treeNodeCtrl`, `AddFemale/AddMale/AddItem/AddLight/AddFolder/AddRoute/AddCamera`, `GetCtrlInfo`, `LoadScene/SaveScene/ImportScene`, `Duplicate` |
| `Studio.ObjectCtrlInfo` | Base of every scene object — `objectInfo`, `treeNodeObject`, `guideObject` |
| `Studio.OCIChar` | Character in scene (99 methods) — `charInfo`/`charReference` → `AIChara.ChaControl`, `fkCtrl`, `ikCtrl`, `neckLookCtrl`, `finalIK`; `ActiveKinematicMode`, `ActiveIK/ActiveFK`, `LoadAnime`, `ChangeMouthPtn`, `ChangeEyesOpen`… |
| `Studio.OCIItem` / `OCILight` / `OCIFolder` / `OCICamera` / `OCIRoute` | Other scene object kinds; the serializable twins are the `OI*Info` classes |
| `Studio.FKCtrl` / `IKCtrl` / `ItemFKCtrl` | FK/IK control; the kinematic mode enum is `Studio.OICharInfo+KinematicMode` |
| `Studio.CameraControl` | The **viewport** camera (Import/Export CameraData) — distinct from the in-scene `OCICamera` object |
| `Studio.SceneInfo` | Serializable scene model, including chara/map lights and post-processing (bloom/AO/DOF/fog/sunShafts/vignette); `Save/Load/Import` |
| `Studio.Info` | Asset catalogs — `dicItemLoadInfo`, `dicAnimeLoadInfo`, `dicLightLoadInfo`, `dicMapLoadInfo`: the enumeration source for spawnable content |
| `Studio.TreeNodeCtrl` | Scene hierarchy tree |
| `Studio.GuideObject` | Transform gizmo / handles |
| `Studio.UndoRedoManager` + `*Command` | Native undo/redo — route write operations through commands where possible |
| `AIChara.ChaControl` | Character body, face and clothes (426 methods; namespace `AIChara`, not root) |
| `NeckLookControllerVer2` (`IL.dll`) | Neck/eye look-at (`ptnNo`, `target`, `rate`) |

Caveats:

- The dumps list **method names only** — get exact signatures with dnSpy (bundled in `[MODDING] Tools`) or `ilspycmd` (see [studio-api.md](studio-api.md#reverse-engineering-workflow)) before writing Harmony patches or tricky calls.
- `LightCtrl` and `MoveAnimator` do **not** exist in this build; lights are `OCILight` plus `SceneInfo.charaLight` / `mapLight`.

## Modding ecosystem

Installed plugins that matter for Miraneo:

| Plugin | Version | Relevance for Miraneo |
|---|---|---|
| HS2API (IllusionModdingAPI) | 1.41 | Official modding API, XML docs beside the DLL |
| Timeline (joan6694) | 1.4.1.0 | Keyframe animation. No reference DLL — integration is reflection-based ([optional-plugins](../optional-plugins.md)) |
| HS2_Screencap (BepisPlugins R20) | 20.0 | Screenshot reference implementation |
| RuntimeUnityEditor | 5.4.0 | Live inspection during development |
| NodesConstraints | 1.3.3.0 | Node constraints. No reference DLL, and nothing public at all ([studio-api.md](studio-api.md#nodesconstraints-bridge-optional-plugin-reflection-only)) |
| HS2_ExtensibleSaveFormat (BepisPlugins) | R20 | Transpiles `SceneInfo.Save`/`Load`/`Import`, so third-party scene data rides along with Miraneo's own save/load path |
| HS2PE (PoseEditor) | — | Pose data reference |
| ConfigurationManager | 18.3.0 | Config UI |

Bundled tools (in `[MODDING] Tools`): dnSpy 6.1.5, SB3Utility 23.1.7, Hooh Modding Tools 0.7, ZipStudio 0.2.1.

## Content paths

All paths are relative to `<game>`.

| What | Where |
|---|---|
| Studio scenes | `UserData\Studio\scene` (107 files) |
| Poses | `UserData\Studio\pose` (3868 files) |
| Character cards | `UserData\chara\female` (1050), `UserData\chara\male` (30) |
| Coordinates | `UserData\coordinate\female` (1800), `UserData\coordinate\male` (166) |
| Screenshots | `UserData\cap` — **does not exist** until the first in-game capture |

## Development toolchain

- dotnet SDK 8.0.424 / 9.0.313 (SDK-style csproj + `Microsoft.NETFramework.ReferenceAssemblies` for `net46`), bun 1.4.0, node 22.15.0.
- No MSBuild in `PATH`.

## Consequences for Miraneo

1. The plugin is a BepInEx 5 `BaseUnityPlugin`, `net46`, x64. HarmonyX 2.9 is available if patching is ever needed; the plugin does not patch.
2. Studio and Unity APIs are main-thread only → the transport thread marshals work into `Update()` and completes responses asynchronously ([threading rules](studio-api.md#threading-rules)).
3. Stable object IDs are the keys of `Studio.Studio.dicObjectCtrl`.
4. The read surface — scene (`sceneInfo`), objects (`dicObjectCtrl`), camera (`CameraControl`), characters (`OCIChar` → `ChaControl`), screenshot (Unity `ScreenCapture`, main thread) — is reachable without patches.
5. Logging is self-provisioned, because the repack disables BepInEx console and disk logs.

See also: [architecture](../architecture.md), [protocol](../protocol.md), [tools](../tools.md).
