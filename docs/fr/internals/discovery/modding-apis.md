# Modding APIs — inventaire des plugins et outils de HS2 DX BetterRepack R16

Les DLL de plugin, bibliothèques d'API, outils et mods optionnels trouvés dans l'installation, et ceux que MiraStudio peut référencer.

Racine du jeu : `<game>`. Scan : 2026-08-30 (lecture seule). 192 DLL trouvées sous `BepInEx\Plugins` (récursif) ; la liste complète est dans [BepInEx](bepinex.md). Le dossier sur disque est `BepInEx\Plugins` (P majuscule) ; Windows ne distingue pas la casse, donc `BepInEx\plugins` résout aussi.

## DLL cibles

Les chemins sont relatifs à `BepInEx\Plugins\`.

| DLL | FileVersion | ProductVersion | Taille | Notes |
|---|---|---|---|---|
| `HS2API.dll` | 1.41 | 1.41 | 278 KB | IllusionModdingAPI (famille KKAPI), Company=ManlyMarco. Le fichier de doc `HS2API.xml` est à côté |
| `Timeline.dll` | 1.4.1.0 | 1.4.1.0 | 452 KB | HSPlugins de joan6694 (Company=https://github.com/IllusionMods/HSPlugins) |
| `RuntimeUnityEditor\RuntimeUnityEditor.Core.dll` | 5.4.0.0 | 5.4.0.0 | 1632 KB | RUE de ManlyMarco |
| `RuntimeUnityEditor\RuntimeUnityEditor.Bepin5.dll` | 5.4.0.0 | 5.4.0.0 | 23 KB | shim de chargement BepInEx 5 |
| `HS2_BepisPlugins\HS2_Screencap.dll` | 20.0 | 20.0 | 100 KB | Screencap de BepisPlugins |
| `HS2_BepisPlugins\HS2_ExtensibleSaveFormat.dll` | 20.0 | 20.0 | 54 KB | API ExtendedSave |
| `HS2_BepisPlugins\HS2_Sideloader.dll` | 20.0 | 20.0 | 163 KB | chargeur de zipmod |
| `HS2_BepisPlugins\HS2_ConfigurationManager.dll` | 20.0 | 20.0 | 22 KB | variante Bepis |
| `ConfigurationManager\ConfigurationManager.dll` | 18.3.0.0 | 18.3.0.0 | 60 KB | ConfigurationManager de ManlyMarco |
| `NodesConstraints.dll` | 1.3.3.0 | 1.3.3.0 | 100 KB | joan6694 |
| `HS2_Plugins\HS2_PoseTools.dll` | 1.1.2 | 1.1.2 | 32 KB | — |
| `HS2_Plugins\HS2_Subtitles.dll` | 2.3.1 | 2.3.1 | 35 KB | — |
| `HS2_Plugins\HS2_TimelineFlowControl.dll` | 1.0.0.0 | 1.0.0.0 | 34 KB | compagnon de Timeline |
| `HS2_CheatTools.dll` | 3.3.2.0 | 3.3.2.0 | 28 KB | — |
| `GraphicsSettings.dll` | 1.3.1 | 1.0.0+b31aaa7... | 24 KB | — |
| `BepInEx.MessageCenter.dll` | 1.1.1 | 1.1.1 | 10 KB | API de messages à l'écran |

Cherchés par nom sans correspondance parmi les DLL de plugin : `ModdingAPI`, `KKAPI` (le build HS2 est `HS2API.dll`), `StudioSceneLoader`, `VNGE` et `UnityExplorer`. VNGE est basé sur Python / scripts de scène et est livré non installé dans `[OPTIONAL] Mods` sous les noms `[countd360] VNGE v43.0` et `[Keitaro] VNGE v42.5`.

## Confirmations

- **IllusionModdingAPI pour HS2** : confirmé. `BepInEx\Plugins\HS2API.dll` v1.41, ManlyMarco. La doc XML (`HS2API.xml`) est présente : l'IntelliSense est disponible en référençant la DLL.
- **Timeline de joan6694** : confirmé. `BepInEx\Plugins\Timeline.dll` v1.4.1.0 (IllusionMods/HSPlugins), plus `HS2_TimelineFlowControl.dll` v1.0.0.0.
- **Plugin de capture d'écran** : confirmé. `HS2_BepisPlugins\HS2_Screencap.dll` v20.0 (Screencap de BepisPlugins).
- **RuntimeUnityEditor** : confirmé. v5.4.0.0 (`RuntimeUnityEditor.Core.dll` et `RuntimeUnityEditor.Bepin5.dll` dans `BepInEx\Plugins\RuntimeUnityEditor\`).

## Dossiers de premier niveau

### `[MODDING] Tools`

- dnSpy v6.1.5
- Hooh Modding Tools v0.7.0.0
- SB3Utility v23.1.7
- ZipStudio v0.2.1

### `[OPTIONAL] Mods`

Non installés par défaut. 40 entrées sont listées : 39 mods et `OptionalMods_ListDescriptions.txt` (le scan d'origine comptait 41 entrées).

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

- 100% save (dossier)
- BetterRepack Official uploader rip (dossier)
- UserData stuffs from other games (dossier)
- Vanilla DefaultData (dossier)
- Vanilla Launcher (dossier)
- Vanilla UserData (dossier)
- Download VPN to access the uploader.url
- How to play with Linux (Wine-guide).desktop / .url

## Assemblies de référence pour le développement de plugin

### Assemblies du jeu

| Dossier | DLL | Rôle |
|---|---|---|
| `StudioNEOV2_Data\Managed\` | 114 | La cible principale de MiraStudio (Studio NEO V2). Clés : `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll` (+ les `UnityEngine.*.dll` par module) |
| `HoneySelect2_Data\Managed\` | 116 | Jeu principal ; même famille `Assembly-CSharp`, si le plugin cible aussi le jeu principal |

Voir [Assemblies](assemblies.md) pour les listes complètes.

### Core de BepInEx

`BepInEx\core\` :

- `BepInEx.dll` 5.4.23.2 (BepInEx 5, pas 6)
- `0Harmony.dll` 2.9.0.0 (HarmonyX), `BepInEx.Harmony.dll`
- `MonoMod.RuntimeDetour.dll` / `MonoMod.Utils.dll` 22.1.29.1
- `Mono.Cecil.dll` 0.10.4.0

### DLL de plugin à référencer

| DLL | Version | Usage |
|---|---|---|
| `BepInEx\Plugins\HS2API.dll` | 1.41 (avec la doc `HS2API.xml`) | API chara / studio / maker, la principale couche d'abstraction |
| `BepInEx\Plugins\Timeline.dll` | 1.4.1.0 | interop avec la timeline d'animation ; l'interop se fait en général par réflexion, aucune DLL d'interop publique n'est livrée |
| `BepInEx\Plugins\HS2_BepisPlugins\HS2_ExtensibleSaveFormat.dll` | 20.0 | ExtendedSave pour les données supplémentaires de scène / carte |
| `BepInEx\Plugins\HS2_BepisPlugins\HS2_Sideloader.dll` | 20.0 | résolution des zipmods / GUID |
| `BepInEx\Plugins\HS2_BepisPlugins\HS2_Screencap.dll` | 20.0 | capture d'écran |
| `BepInEx\Plugins\NodesConstraints.dll` | 1.3.3.0 | interop des contraintes de nœuds pour le posage |
| `BepInEx\Plugins\RuntimeUnityEditor\RuntimeUnityEditor.Core.dll` | 5.4.0.0 | inspection / REPL, utile pendant le développement |
| `BepInEx\Plugins\BepInEx.MessageCenter.dll` | 1.1.1 | notifications à l'écran |

Aussi présents et potentiellement utiles : `HS2PE.dll` (éditeur de poses de la famille KKPE), `HS2ABMX.dll` (+xml), `MoreAccessories.dll`, `HS2_OverlayMods.dll` (+xml), le dossier `VideoExport\`, `ScriptLoader.dll`.

### Patchers

`BepInEx\patchers\` : `CultureFix.dll` 21.6, `HS2_ExtensibleSaveFormat_Patcher.dll`, `MagicCarrot.dll` 21.6 et les patchers BepInEx.SplashScreen. Les versions et sous-fichiers sont dans [BepInEx](bepinex.md).

### Loader

`winhttp.dll` et `doorstop_config.ini` à la racine du jeu (UnityDoorstop) ; voir [Runtime](runtime.md). Exécutables : `HoneySelect2.exe`, `HoneySelect2VR.exe`, `StudioNEOV2.exe`, `InitSetting.exe`.

## Voir aussi

- [BepInEx](bepinex.md)
- [Assemblies](assemblies.md)
- [Runtime](runtime.md)
