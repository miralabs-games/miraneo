# BepInEx — inventaire du loader, de la config et des plugins de HS2 DX BetterRepack R16

La version et les dossiers de BepInEx, les réglages clés de `BepInEx.cfg`, les fichiers de config livrés et toutes les DLL de plugin de l'installation.

Racine du jeu : `<game>`.

## Versions du core

`BepInEx\core` :

| DLL | FileVersion | ProductVersion |
|---|---|---|
| BepInEx.dll | 5.4.23.2 | 5.4.23.2 |
| BepInEx.Preloader.dll | 5.4.23.2 | 5.4.23.2 |
| 0Harmony.dll | 2.9.0.0 | 2.9.0 |

L'installation tourne sous **BepInEx 5.4.23.2** avec **HarmonyX 2.9.0**.

Contenu complet de `BepInEx\core` :

```text
0Harmony.dll
0Harmony.xml
0Harmony20.dll
BepInEx.dll
BepInEx.Harmony.dll
BepInEx.Harmony.xml
BepInEx.Preloader.dll
BepInEx.Preloader.xml
BepInEx.xml
HarmonyXInterop.dll
Mono.Cecil.dll
Mono.Cecil.Mdb.dll
Mono.Cecil.Pdb.dll
Mono.Cecil.Rocks.dll
MonoMod.RuntimeDetour.dll (+xml)
MonoMod.Utils.dll (+xml)
XUnity.Common.dll
```

## Dossiers

Dossiers de premier niveau de `BepInEx` : `BRInfo`, `cache`, `config`, `core`, `LauncherEN`, `patchers`, **`Plugins`** (P majuscule ; Windows ne distingue pas la casse, donc `BepInEx\plugins` résout aussi), `Translation`.

### Patchers

`BepInEx\patchers`, récursif :

| Chemin | FileVersion |
|---|---|
| CultureFix.dll | 21.6 |
| HS2_ExtensibleSaveFormat_Patcher.dll | 0.0.0.0 (+ .pdb) |
| MagicCarrot.dll | 21.6 |
| BepInEx.SplashScreen\BepInEx.SplashScreen.GUI.exe | 2.2.0.0 (+ .config, LICENSE, README.md) |
| BepInEx.SplashScreen\BepInEx.SplashScreen.Patcher.BepInEx5.dll | 2.2.0.0 |
| BepInEx.SplashScreen\BepInEx.SplashScreen.Patcher.dll | 1.0.0.0 |

## Réglages clés de `BepInEx.cfg`

`BepInEx\config\BepInEx.cfg` :

| Domaine | Réglages |
|---|---|
| Entrypoint | Assembly = `UnityEngine.CoreModule.dll`, Type = `Application`, Method = `.cctor` (valeurs par défaut) |
| Console | `[Logging.Console] Enabled = false` ; LogLevels = Fatal, Error, Warning, Message, Info ; DisplayedLogLevel = Info ; UTF-8 ; StandardOutType = Auto |
| Log disque | `[Logging.Disk] Enabled = false` (log sur disque désactivé) ; WriteUnityLog = false ; AppendLog = false |
| Log Unity | UnityLogListening = true ; PreloaderConsoleOutRedirection = true |
| Log Harmony | canaux Warn, Error (HarmonyX : Warn/Error true, IL/Info false) |
| Preloader | ApplyRuntimePatches = true, HarmonyBackend = auto, DumpAssemblies = false, ShimHarmonySupport = false |
| Cache | EnableAssemblyCache = true |
| Chainloader | HideManagerGameObject = false |
| SplashScreen | Enabled = true, OnlyNoConsole = true |

Pour Miraneo : les logs console et disque sont tous deux désactivés d'origine. Le plugin doit s'appuyer sur son propre puits de log, ou la config doit être ajustée.

## Autres fichiers de config

Une centaine de fichiers `.cfg` / `.ini` se trouvent dans `BepInEx\config` :

```text
animal42069.HS2betterpenetration.cfg
Autostart.cfg
AutoTranslatorConfig.ini
BepInEx.cfg
BepInEx.EnableResize.cfg
BepInEx.MuteInBackground.cfg
Bulge.cfg
certain.lsp.togglefkik.cfg
com.2155x.fairbair.hs2_povx.cfg
com.bepis.bepinex.configurationmanager.cfg
com.bepis.bepinex.screenshotmanager.cfg
com.bepis.bepinex.sideloader.cfg
com.bepis.bepinex.sliderunlocker.cfg
com.bepis.messagecenter.cfg
com.deathweasel.bepinex.animationcontroller.cfg
com.deathweasel.bepinex.autosave.cfg
com.deathweasel.bepinex.characterexport.cfg
com.deathweasel.bepinex.colliders.cfg
com.deathweasel.bepinex.demosaic.cfg
com.deathweasel.bepinex.makerdefaults.cfg
com.deathweasel.bepinex.materialeditor.cfg
com.deathweasel.bepinex.posefolders.cfg
com.deathweasel.bepinex.posequickload.cfg
com.deathweasel.bepinex.studiocustommasking.cfg
com.deathweasel.bepinex.studioimageembed.cfg
com.deathweasel.bepinex.studioobjectmovehotkeys.cfg
com.deathweasel.bepinex.studiosceneloadedsound.cfg
com.deathweasel.bepinex.subtitles.cfg
com.deathweasel.bepinex.textresourceredirector.cfg
com.deathweasel.bepinex.uncensorselector.cfg
com.gebo.BepInEx.GameWhoIsThere.cfg
com.gebo.BepInEx.GeBoAPI.cfg
com.gebo.BepInEx.StudioMultiSelectChara.cfg
com.gebo.BepInEx.StudioSceneInitialCamera.cfg
com.gebo.BepInEx.StudioSceneNavigation.cfg
com.gebo.BepInEx.TranslationCacheCleaner.cfg
com.gebo.BepInEx.TranslationHelper.cfg
com.hooh.hs2.heelz.cfg
com.joan6694.illusionplugins.hs2us.cfg
com.joan6694.illusionplugins.nodesconstraints.cfg
com.joan6694.illusionplugins.poseeditor.cfg
com.joan6694.illusionplugins.timeline.cfg
com.joan6694.illusionplugins.videoexport.cfg
com.rikkibalboa.bepinex.projectorutils.cfg
com.rskoi.particleeditor.cfg
com.toppac.PoseLoader.cfg
com.yy.poop.cfg
Countd360.StudioCharaEditor.HS2.cfg
CultureFix.cfg
GarryWu.HS2WearCustom.cfg
gravydevsupreme.xunity.resourceredirector.cfg
HeightBar.cfg
HS2_BetterHScenes.cfg
HS2_ExtraGroups.cfg
HS2_Fix_MakerOptimizations.cfg
HS2_Fix_ResourceUnloadOptimizations.cfg
HS2_HLightControl.cfg
HS2_MakerSearch.cfg
HS2_QuickAccessBox.cfg
HS2_StudioPOV.cfg
HS2_UnlockPlayerHeight.cfg
IllusionMods.CharacterReplacer.cfg
keelhauled.defaultstudioscene.cfg
keelhauled.draganddrop.cfg
keelhauled.graphicssettings.cfg
keelhauled.hideallui.cfg
keelhauled.itemlayeredit.cfg
keelhauled.titleshortcuts.cfg
KK_PregnancyPlus.cfg
KKABMX.Core.cfg
kky.aihs2.studiocharasort.cfg
kky.bepinex.plugins.colorpanelx.cfg
kky.hs2.StudioTransformOrientation.cfg
kky.hs2.StudioVoiceX.cfg
KSOX_GUI.cfg
KSOX.cfg
MarC0.FPSCounter.cfg
marco.authordata.cfg
marco.FolderBrowser.cfg
marco.kkapi.cfg
mikke.gravureAI.cfg
orange.spork.additionalaccessorycontrolsplugin.cfg
orange.spork.advikplugin.cfg
orange.spork.chainlighting.cfg
orange.spork.characterrandomizer.cfg
orange.spork.eyelookadjuster.cfg
orange.spork.fkheightadjustuiplugin.cfg
orange.spork.hs2chicksdigmapmoddersplugin.cfg
orange.spork.hs2takechargeplugin.cfg
orange.spork.makersorticonreplacer.cfg
orange.spork.moarcamzplugin.cfg
orange.spork.studiocharmon.cfg
RendererEditor.cfg
RuntimeUnityEditor.cfg
sakacheggs.measurements.cfg
spockbauru.hs2.girlsentrance.cfg
StudioCameraTweaks.cfg
StudioExtraMoveAxis.cfg
StudioWindowResize.cfg
```

## Divers

- **`BepInEx_Shim_Backup`** (premier niveau) : contient un unique dossier `BepInEx`, sauvegarde de la disposition d'avant le shim. Il contient `core\BepInEx.Harmony.dll` (2.0.0.0) et une arborescence `plugins` avec d'anciennes copies d'environ 30 DLL de plugin (sous-dossiers `2155X` et `HS2_Plugins` compris).
- **`scripts`** (racine du jeu, premier niveau) : `AYAYA_v2.cs` et `scriptignores`, consommés par `ScriptLoader.dll` (présent dans `Plugins`), qui compile et charge des scripts C# à l'exécution.

## DLL de plugin

Les 192 DLL de `BepInEx\Plugins`, récursif, avec leur FileVersion. Les chemins sont relatifs à `BepInEx\Plugins`.

| Chemin relatif | FileVersion |
|---|---|
| Aura2_Core.dll | 0.0.0.0 |
| BepInEx.CatchUnityEventExceptions.dll | 1.0 |
| BepInEx.EnableFullScreenToggle.dll | 1.0 |
| BepInEx.EnableResize.dll | 3.0 |
| BepInEx.InputHotkeyBlock.dll | 1.4 |
| BepInEx.MessageCenter.dll | 1.1.1 |
| BepInEx.MuteInBackground.dll | 1.1 |
| BepInEx.OptimizeIMGUI.dll | 1.0 |
| BepisFixHS2.dll | 0.0.0.0 |
| BonesFramework.dll | 1.0.0.0 |
| DefaultStudioScene.HoneySelect2.dll | 1.0.4.0 |
| Deformers.dll | 1.0.0.0 |
| DragAndDrop.HoneySelect2.dll | 1.2.5 |
| FPSCounter.dll | 3.1 |
| GraphicsSettings.dll | 1.3.1 |
| GravureAI.dll | 1.0.0.0 |
| HairMeshforAll.dll | 1.0.0 |
| HeadBundleRedirect.dll | 0.0.0.0 |
| HideAllUI.HoneySelect2.dll | 2.3.2 |
| HS2_AdditionalAccessoryControls.dll | 1.0.0.0 |
| HS2_AdditionalFKNodes.dll | 1.0.0.0 |
| HS2_AdvIKPlugin.dll | 1.0.0.0 |
| HS2_AnimationPairing.dll | 1.0.0.0 |
| HS2_Autostart.dll | 1.0.0.0 |
| HS2_BetterPenetration.dll | 5.0.1.1 |
| HS2_BrowserFolders.dll | 3.2.3.0 |
| HS2_Bulge.dll | 1.0.2.0 |
| HS2_ChainLighting.dll | 1.0.0.0 |
| HS2_CharacterRandomizer.dll | 1.0.0.0 |
| HS2_CharacterReplacer.dll | 1.6.3 |
| HS2_CheatTools.dll | 3.3.2.0 |
| HS2_ChicksDigMapModdersPlugin.dll | 1.0.0.0 |
| HS2_ClothColliders.dll | 1.1.0.0 |
| HS2_ColorPanelX.dll | 1.4.1 |
| HS2_CopyChaBodyMat.dll | 0.0.0.0 |
| HS2_DependencyLoader.dll | 1.0.0.0 |
| HS2_EarWiggle.dll | 1.0.0.0 |
| HS2_EyeLookAdjuster.dll | 1.0.0.0 |
| HS2_FKHeightAdjustUI.dll | 1.0.0.0 |
| HS2_GirlsEntrance.dll | 1.1 |
| HS2_Heelz.dll | 1.15.4.0 |
| HS2_HeightBar.dll | 3.4.0.0 |
| HS2_Hooah.dll | 2.2.2 |
| HS2_HooahUtility.dll | 1.2.1.0 |
| HS2_JumpToSelectionPlugin.dll | 1.0.0.0 |
| HS2_MakerSortIconReplacer.dll | 1.0.0.0 |
| HS2_Measurements.dll | 1.0.0.0 |
| HS2_MoarCamz.dll | 1.0.0.0 |
| HS2_OutfitPainter.dll | 1.0.0.0 |
| HS2_OverlayMods.dll | 6.1.5.0 |
| HS2_ParticleEditor.dll | 1.0.0 |
| HS2_PoseLoader.dll | 0.1.6.5 |
| HS2_PovX.dll | 1.0.0.0 |
| HS2_PregnancyPlus.dll | 7.8.0.0 |
| HS2_ProjectorUtils.dll | 1.0 |
| HS2_QuickAccessBox.dll | 3.2.0.0 |
| HS2_RemoveToRecycleBin.dll | 2.0.0.0 |
| HS2_Studio_BetterPenetration.dll | 5.0.1.1 |
| HS2_StudioCameraObjectTweaks.dll | 1.0.0.0 |
| HS2_StudioCharaRefresh.dll | 1.0.0.0 |
| HS2_StudioCharMonitor.dll | 1.0.0.0 |
| HS2_StudioExtraMoveAxis.dll | 2.0.0.0 |
| HS2_StudioTransformOrientation.dll | 1.1.1 |
| HS2_StudioVoiceX.dll | 1.0.0 |
| HS2ABMX.dll | 5.2.2.0 |
| HS2API.dll | 1.41 |
| HS2PE.dll | 2.17.2.0 |
| HS2StudioCharaEditor.dll | 2.1.0.0 |
| HS2TakeChargePlugin.dll | 1.0.0.0 |
| HS2US.dll | 1.14.2.0 |
| HS2VR_CustomMapsFix.dll | 1.1.0 |
| HS2WearCustom.dll | 0.3.0.0 |
| ItemLayerEdit.HoneySelect2.dll | 1.1.2.0 |
| MapController.dll | 1.0.0.0 |
| MdgqBodyChange.dll | 1.0.0.0 |
| ModBoneImplantor.dll | 1.1.1.0 |
| MoreAccessories.dll | 1.0.0.0 |
| NodesConstraints.dll | 1.3.3.0 |
| playboop.dll | 1.12.0.0 |
| RendererEditor.dll | 1.0.0.0 |
| SayNoToModChecker.dll | 1.0.0.0 |
| ScriptLoader.dll | 1.0.0.0 |
| StudioCharaSort.dll | 1.0.1 |
| Timeline.dll | 1.4.1.0 |
| TitleShortcuts.HoneySelect2.dll | 1.3.1.0 |
| ToggleFKIK.dll | 1.0.0.0 |
| VideoExport.dll | 1.3.0.0 |
| 2155X\HS2_BetterHScenes.dll | 2.6.5 |
| 2155X\HS2_ExtraGroups.dll | 1.0.1 |
| 2155X\HS2_HCharaSwitcher.dll | 1.2.1 |
| 2155X\HS2_HLightControl.dll | 1.2.5 |
| 2155X\HS2_LightprobesReset.dll | 1.1.0 |
| 2155X\HS2_MakerAdditions.dll | 1.0.0 |
| 2155X\HS2_MakerRandomPicker.dll | 1.2.0 |
| 2155X\HS2_MakerSearch.dll | 1.5.1 |
| 2155X\HS2_RandomHPicker.dll | 1.0.0 |
| 2155X\HS2_StudioAccessoryNames.dll | 1.1.0 |
| 2155X\HS2_StudioMiscSearch.dll | 1.0.0 |
| 2155X\HS2_StudioPOV.dll | 1.1.1 |
| 2155X\HS2_UnlockPlayerHClothes.dll | 1.4.4 |
| 2155X\HS2_UnlockPlayerHeight.dll | 1.4.3 |
| ConfigurationManager\ConfigurationManager.dll | 18.3.0.0 |
| GeBo_Plugins\HS2_GameWhoIsThere.dll | 1.0.1.4 |
| GeBo_Plugins\HS2_GeBoCommon.dll | 1.2.0.1 |
| GeBo_Plugins\HS2_StudioMultiselectChara.dll | 1.0.0.3 |
| GeBo_Plugins\HS2_StudioSceneInitialCamera.dll | 0.7.0.1 |
| GeBo_Plugins\HS2_StudioSceneNavigation.dll | 1.0.3.0 |
| GeBo_Plugins\HS2_TranslationCacheCleaner.dll | 0.6.0.3 |
| GeBo_Plugins\HS2_TranslationHelper.dll | 1.1.1.0 |
| HS2_BepisPlugins\HS2_BGMLoader.dll | 20.0 |
| HS2_BepisPlugins\HS2_ConfigurationManager.dll | 20.0 |
| HS2_BepisPlugins\HS2_ExtensibleSaveFormat.dll | 20.0 |
| HS2_BepisPlugins\HS2_InputUnlocker.dll | 20.0 |
| HS2_BepisPlugins\HS2_Screencap.dll | 20.0 |
| HS2_BepisPlugins\HS2_Sideloader.dll | 20.0 |
| HS2_BepisPlugins\HS2_SliderUnlocker.dll | 20.0 |
| HS2_BepisPlugins\ICSharpCode.SharpZipLib.dll | 1.3.3.11 |
| HS2_BepisPlugins\Unity.InputSystem.dll | 1.5.0.0 |
| HS2_Plugins\HS2_AccessoriesToStudioItems.dll | 1.0.1 |
| HS2_Plugins\HS2_AnimationController.dll | 2.2 |
| HS2_Plugins\HS2_AnimationOverdrive.dll | 1.1 |
| HS2_Plugins\HS2_Autosave.dll | 1.1.1 |
| HS2_Plugins\HS2_CharacterExport.dll | 1.0 |
| HS2_Plugins\HS2_Colliders.dll | 1.3 |
| HS2_Plugins\HS2_Demosaic.dll | 1.1 |
| HS2_Plugins\HS2_DynamicBoneEditor.dll | 1.0.5 |
| HS2_Plugins\HS2_EyeControl.dll | 1.0.1 |
| HS2_Plugins\HS2_FKIK.dll | 1.1.3 |
| HS2_Plugins\HS2_InvisibleBody.dll | 1.4 |
| HS2_Plugins\HS2_MakerDefaults.dll | 1.1 |
| HS2_Plugins\HS2_MaleJuice.dll | 1.3 |
| HS2_Plugins\HS2_MaterialEditor.dll | 3.9.1 |
| HS2_Plugins\HS2_PoseFolders.dll | 1.0 |
| HS2_Plugins\HS2_PoseQuickLoad.dll | 1.1 |
| HS2_Plugins\HS2_PoseTools.dll | 1.1.2 |
| HS2_Plugins\HS2_PoseUnlocker.dll | 1.0 |
| HS2_Plugins\HS2_StudioCustomMasking.dll | 1.1.1 |
| HS2_Plugins\HS2_StudioImageEmbed.dll | 1.0.3 |
| HS2_Plugins\HS2_StudioObjectMoveHotkeys.dll | 1.0 |
| HS2_Plugins\HS2_StudioSceneLoadedSound.dll | 1.1 |
| HS2_Plugins\HS2_StudioSceneSettings.dll | 1.3.2 |
| HS2_Plugins\HS2_StudioWindowResize.dll | 1.1.1 |
| HS2_Plugins\HS2_Subtitles.dll | 2.3.1 |
| HS2_Plugins\HS2_TimelineFlowControl.dll | 1.0.0.0 |
| HS2_Plugins\HS2_UncensorSelector.dll | 3.12.1 |
| IllusionFixes\HS2_Fix_CameraTarget.dll | 21.6 |
| IllusionFixes\HS2_Fix_DataCorruptionFixes.dll | 21.6 |
| IllusionFixes\HS2_Fix_DynamicBones.dll | 21.6 |
| IllusionFixes\HS2_Fix_ExpandShaderDropdown.dll | 21.6 |
| IllusionFixes\HS2_Fix_GarbageTruck.dll | 21.6 |
| IllusionFixes\HS2_Fix_GravityAdjust.dll | 21.6 |
| IllusionFixes\HS2_Fix_InvalidSceneFileProtection.dll | 21.6 |
| IllusionFixes\HS2_Fix_MakerMaleFaceTypes.dll | 21.6 |
| IllusionFixes\HS2_Fix_MakerOptimizations.dll | 21.6 |
| IllusionFixes\HS2_Fix_ManifestCorrector.dll | 21.6 |
| IllusionFixes\HS2_Fix_NullChecks.dll | 21.6 |
| IllusionFixes\HS2_Fix_PersonalityCorrector.dll | 21.6 |
| IllusionFixes\HS2_Fix_PoseLoad.dll | 21.6 |
| IllusionFixes\HS2_Fix_ResourceUnloadOptimizations.dll | 21.6 |
| IllusionFixes\HS2_Fix_SettingsVerifier.dll | 21.6 |
| IllusionFixes\HS2_Fix_StudioOptimizations.dll | 21.6 |
| IllusionFixes\HS2_Fix_UpdateWet.dll | 21.6 |
| IllusionFixes\HS2_Fix_WetAccessories.dll | 21.6 |
| RuntimeUnityEditor\RuntimeUnityEditor.Bepin5.dll | 5.4.0.0 |
| RuntimeUnityEditor\RuntimeUnityEditor.Core.dll | 5.4.0.0 |
| TranslationTools\HS2_TextResourceRedirector.dll | 1.4.4.3 |
| XUnity.AutoTranslator\ExIni.dll | 1.0.2.1 |
| XUnity.AutoTranslator\XUnity.AutoTranslator.Plugin.BepInEx.dll | 5.3.1.0 |
| XUnity.AutoTranslator\XUnity.AutoTranslator.Plugin.Core.dll | 5.3.1.0 |
| XUnity.AutoTranslator\XUnity.AutoTranslator.Plugin.ExtProtocol.dll | 1.0.1.0 |
| XUnity.AutoTranslator\Translators\BaiduTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\BingTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\BingTranslateLegitimate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\CustomTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\DeepLTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\ezTransXP.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\GoogleTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\GoogleTranslateCompat.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\GoogleTranslateLegitimate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\LecPowerTranslator15.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\LingoCloudTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\PapagoTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\WatsonTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\YandexTranslate.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\FullNET\Common.ExtProtocol.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\FullNET\DeepLTranslate.ExtProtocol.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\FullNET\GoogleTranslateCompat.ExtProtocol.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\FullNET\Http.ExtProtocol.dll | 1.0.0.0 |
| XUnity.AutoTranslator\Translators\FullNET\Newtonsoft.Json.dll | 13.0.1.25517 |
| XUnity.AutoTranslator\Translators\FullNET\XUnity.AutoTranslator.Plugin.ExtProtocol.dll | 1.0.1.0 |
| XUnity.ResourceRedirector\XUnity.ResourceRedirector.BepInEx.dll | 2.1.0.0 |
| XUnity.ResourceRedirector\XUnity.ResourceRedirector.dll | 2.1.0.0 |

## Voir aussi

- [Modding APIs](modding-apis.md)
- [Runtime](runtime.md)
- [Assemblies](assemblies.md)
