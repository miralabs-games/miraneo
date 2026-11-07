# Managed Assemblies — HS2 DX BetterRepack R16 inventory

The DLLs in the two `Managed` folders of the install, their sizes and versions, and what they imply for the .NET target.

Game root: `<game>`. Inventory date: 2026-08-30.

## Summary

- `StudioNEOV2_Data\Managed`: **114 DLLs**.
- `HoneySelect2_Data\Managed`: **116 DLLs**.
- Studio-only assemblies: **none**.
- HS2-only assemblies: `QHierarchyNullable.dll` (3,584 B), `QHierarchyRuntime.dll` (7,168 B).
- The two sets are otherwise identical by name. `Assembly-CSharp` differs in size (Studio 5,001,728 B, HS2 4,978,688 B), so Studio code lives in Studio's own `Assembly-CSharp.dll`.

## .NET profile

| Check | Value |
|---|---|
| mscorlib.dll FileVersion | 4.6.57.0 (ProductName: "Mono Common Language Infrastructure") |
| netstandard.dll present | **No** |
| System.Core.dll FileVersion | 4.6.57.0 |
| System.Runtime.dll | 4.0.0.0 (facade, 12,800 B) |

The runtime is a Unity 2018.4-era Mono with the .NET 4.x equivalent API profile and no netstandard facade. The conventional BepInEx plugin `TargetFramework` is **`net46`** (BepInEx 5.x, x64 Mono build). Reference the game's own Managed DLLs, or matching NuGet reference assemblies, when compiling.

## Key gameplay assemblies

Assemblies of `StudioNEOV2_Data\Managed`:

| Assembly | Size (bytes) | FileVersion | Notes |
|---|---|---|---|
| Assembly-CSharp.dll | 5,001,728 | 0.0.0.0 | Main game/Studio code (Studio, ChaControl, etc.) |
| Assembly-CSharp-firstpass.dll | 896,000 | 0.0.0.0 | Shared/plugin-level game code |
| IL.dll | 158,720 | 0.0.0.0 | Illusion shared library (AssetBundle/manager helpers) |
| Sirenix.Serialization.dll | 297,984 | 2.0.13 | Odin serialization |
| Sirenix.Utilities.dll | 159,232 | 2.0.13 | — |
| Sirenix.OdinInspector.Attributes.dll | 30,208 | 2.0.13 | — |
| Sirenix.Serialization.Config.dll | 15,872 | 2.0.13 | — |
| UniRx.dll | 419,840 | 0.0.0.0 | Reactive extensions, heavily used by Illusion code |
| UniRx.Async.dll | 242,688 | 0.0.0.0 | UniTask |
| Unity.TextMeshPro.dll | 331,776 | 0.0.0.0 | Studio UI text |
| UnityEngine.CoreModule.dll | 848,896 | 0.0.0.0 | Unity 2018.4 modular engine |

No assembly named `*Chara*` or `Vectrosity` exists in either Managed directory; the character classes are inside `Assembly-CSharp`.

## Full list: `StudioNEOV2_Data\Managed`

All 114 DLLs with size in bytes and FileVersion.

| Name | Bytes | FileVersion |
|---|---|---|
| Assembly-CSharp-firstpass.dll | 896000 | 0.0.0.0 |
| Assembly-CSharp.dll | 5001728 | 0.0.0.0 |
| Cinemachine.dll | 177664 | 0.0.0.0 |
| DOTween.dll | 139264 | 1.0.0.0 |
| DOTween43.dll | 9728 | 1.0.0.0 |
| DOTween46.dll | 20480 | 1.0.0.0 |
| DOTween50.dll | 5632 | 1.0.0.0 |
| FileBrowserWin.dll | 7168 | 2019.2.5 |
| I18N.CJK.dll | 680960 | 2.0.0.0 |
| I18N.dll | 32256 | 2.0.0.0 |
| I18N.MidEast.dll | 29696 | 2.0.0.0 |
| I18N.Other.dll | 32768 | 2.0.0.0 |
| I18N.Rare.dll | 171520 | 2.0.0.0 |
| I18N.West.dll | 66048 | 2.0.0.0 |
| IL.dll | 158720 | 0.0.0.0 |
| Ionic.Zip.dll | 462336 | 1.9.1.8 |
| Mono.Posix.dll | 212480 | 4.0.0.0 |
| Mono.Security.dll | 309248 | 4.0.0.0 |
| mscorlib.dll | 4069888 | 4.6.57.0 |
| Obi.dll | 160256 | 0.0.0.0 |
| Ookii.Dialogs.dll | 137680 | 1.0.0.0 |
| Sirenix.OdinInspector.Attributes.dll | 30208 | 2.0.13 |
| Sirenix.Serialization.Config.dll | 15872 | 2.0.13 |
| Sirenix.Serialization.dll | 297984 | 2.0.13 |
| Sirenix.Utilities.dll | 159232 | 2.0.13 |
| System.ComponentModel.Composition.dll | 256512 | 4.6.57.0 |
| System.Configuration.dll | 124928 | 4.6.57.0 |
| System.Core.dll | 1046016 | 4.6.57.0 |
| System.dll | 2715136 | 4.6.57.0 |
| System.Drawing.dll | 630784 | 2.0.50727.8015 |
| System.Runtime.dll | 12800 | 4.0.0.0 |
| System.Runtime.Serialization.dll | 928256 | 4.6.57.0 |
| System.Security.dll | 184320 | 4.6.57.0 |
| System.ServiceModel.Internals.dll | 214528 | 4.6.57.0 |
| System.Windows.Forms.dll | 3053568 | 2.0.50727.1433 |
| System.Xml.dll | 3138048 | 4.6.57.0 |
| System.Xml.Linq.dll | 119296 | 4.6.57.0 |
| uAudio.dll | 490496 | 0.0.0.0 |
| UniRx.Async.dll | 242688 | 0.0.0.0 |
| UniRx.dll | 419840 | 0.0.0.0 |
| Unity.Addressables.dll | 52224 | 0.0.0.0 |
| Unity.Analytics.DataPrivacy.dll | 7680 | 0.0.0.0 |
| Unity.Burst.dll | 16896 | 0.0.0.0 |
| Unity.Burst.Unsafe.dll | 5632 | (none) |
| Unity.Mathematics.dll | 709632 | 0.0.0.0 |
| Unity.Postprocessing.Runtime.dll | 145920 | 0.0.0.0 |
| Unity.ResourceManager.dll | 69120 | 0.0.0.0 |
| Unity.ScriptableBuildPipeline.dll | 7680 | 0.0.0.0 |
| Unity.TextMeshPro.dll | 331776 | 0.0.0.0 |
| UnityEngine.AccessibilityModule.dll | 10752 | 0.0.0.0 |
| UnityEngine.AIModule.dll | 42496 | 0.0.0.0 |
| UnityEngine.AnimationModule.dll | 137216 | 0.0.0.0 |
| UnityEngine.ARModule.dll | 12288 | 0.0.0.0 |
| UnityEngine.AssetBundleModule.dll | 20992 | 0.0.0.0 |
| UnityEngine.AudioModule.dll | 58368 | 0.0.0.0 |
| UnityEngine.BaselibModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.ClothModule.dll | 14336 | 0.0.0.0 |
| UnityEngine.ClusterInputModule.dll | 9728 | 0.0.0.0 |
| UnityEngine.ClusterRendererModule.dll | 8704 | 0.0.0.0 |
| UnityEngine.CoreModule.dll | 848896 | 0.0.0.0 |
| UnityEngine.CrashReportingModule.dll | 9216 | 0.0.0.0 |
| UnityEngine.DirectorModule.dll | 12800 | 0.0.0.0 |
| UnityEngine.dll | 72192 | 0.0.0.0 |
| UnityEngine.FileSystemHttpModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.GameCenterModule.dll | 26112 | 0.0.0.0 |
| UnityEngine.GridModule.dll | 13312 | 0.0.0.0 |
| UnityEngine.HotReloadModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.ImageConversionModule.dll | 9728 | 0.0.0.0 |
| UnityEngine.IMGUIModule.dll | 146944 | 0.0.0.0 |
| UnityEngine.InputModule.dll | 11264 | 0.0.0.0 |
| UnityEngine.JSONSerializeModule.dll | 10240 | 0.0.0.0 |
| UnityEngine.LocalizationModule.dll | 9216 | 0.0.0.0 |
| UnityEngine.Networking.dll | 260608 | 1.0.0.0 |
| UnityEngine.ParticleSystemModule.dll | 127488 | 0.0.0.0 |
| UnityEngine.PerformanceReportingModule.dll | 8704 | 0.0.0.0 |
| UnityEngine.Physics2DModule.dll | 93184 | 0.0.0.0 |
| UnityEngine.PhysicsModule.dll | 84992 | 0.0.0.0 |
| UnityEngine.ProfilerModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.Purchasing.dll | 27648 | 0.0.0.0 |
| UnityEngine.ScreenCaptureModule.dll | 9216 | 0.0.0.0 |
| UnityEngine.SharedInternalsModule.dll | 19456 | 0.0.0.0 |
| UnityEngine.SpatialTracking.dll | 11264 | 0.0.0.0 |
| UnityEngine.SpriteMaskModule.dll | 9728 | 0.0.0.0 |
| UnityEngine.SpriteShapeModule.dll | 9728 | 0.0.0.0 |
| UnityEngine.StreamingModule.dll | 8704 | 0.0.0.0 |
| UnityEngine.StyleSheetsModule.dll | 16384 | 0.0.0.0 |
| UnityEngine.SubstanceModule.dll | 12800 | 0.0.0.0 |
| UnityEngine.TerrainModule.dll | 62976 | 0.0.0.0 |
| UnityEngine.TerrainPhysicsModule.dll | 8704 | 0.0.0.0 |
| UnityEngine.TextCoreModule.dll | 32256 | 0.0.0.0 |
| UnityEngine.TextRenderingModule.dll | 26624 | 0.0.0.0 |
| UnityEngine.TilemapModule.dll | 24064 | 0.0.0.0 |
| UnityEngine.Timeline.dll | 98304 | 1.0.0.0 |
| UnityEngine.TimelineModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.TLSModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.UI.dll | 252928 | 1.0.0.0 |
| UnityEngine.UIElementsModule.dll | 351744 | 0.0.0.0 |
| UnityEngine.UIModule.dll | 22016 | 0.0.0.0 |
| UnityEngine.UmbraModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.UNETModule.dll | 76800 | 0.0.0.0 |
| UnityEngine.UnityAnalyticsModule.dll | 24064 | 0.0.0.0 |
| UnityEngine.UnityConnectModule.dll | 10240 | 0.0.0.0 |
| UnityEngine.UnityTestProtocolModule.dll | 8192 | 0.0.0.0 |
| UnityEngine.UnityWebRequestAssetBundleModule.dll | 11264 | 0.0.0.0 |
| UnityEngine.UnityWebRequestAudioModule.dll | 10752 | 0.0.0.0 |
| UnityEngine.UnityWebRequestModule.dll | 41984 | 0.0.0.0 |
| UnityEngine.UnityWebRequestTextureModule.dll | 10240 | 0.0.0.0 |
| UnityEngine.UnityWebRequestWWWModule.dll | 19968 | 0.0.0.0 |
| UnityEngine.VehiclesModule.dll | 11776 | 0.0.0.0 |
| UnityEngine.VFXModule.dll | 27136 | 0.0.0.0 |
| UnityEngine.VideoModule.dll | 26624 | 0.0.0.0 |
| UnityEngine.VRModule.dll | 29184 | 0.0.0.0 |
| UnityEngine.WindModule.dll | 9216 | 0.0.0.0 |
| UnityEngine.XRModule.dll | 44544 | 0.0.0.0 |

## Differences with `HoneySelect2_Data\Managed`

`HoneySelect2_Data\Managed` holds 116 DLLs: the same name set as `StudioNEOV2_Data\Managed` plus the two HS2-only DLLs.

| Type | Assemblies |
|---|---|
| Studio-only | none |
| HS2-only | `QHierarchyNullable.dll` (3,584 B), `QHierarchyRuntime.dll` (7,168 B) |

Notable size differences against Studio:

| Assembly | HS2 size (bytes) | Studio size (bytes) |
|---|---|---|
| Assembly-CSharp.dll | 4,978,688 | 5,001,728 |
| Assembly-CSharp-firstpass.dll | 897,024 | 896,000 |

## See also

- [Runtime](runtime.md)
- [Modding APIs](modding-apis.md)
- [Studio classes](studio-classes.md)
