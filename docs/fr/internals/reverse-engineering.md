# Rétro-ingénierie — installation cible

Ce contre quoi Miraneo tourne : l'installation exacte de Honey Select 2 DX / Studio NEO V2, sa pile de chargement, ses assemblies, ses classes Studio et son écosystème de mods, telle que relevée en inspectant le repack et en décompilant ses assemblies (instantané du 2026-08-30).

Les rapports bruts avec les dumps complets sont dans [discovery/](discovery/) : [assemblies](discovery/assemblies.md), [bepinex](discovery/bepinex.md), [modding-apis](discovery/modding-apis.md), [runtime](discovery/runtime.md), [studio-classes](discovery/studio-classes.md), [userdata](discovery/userdata.md). Ce que le plugin fait de ces API est décrit dans [studio-api.md](studio-api.md).

## Identification

| Élément | Valeur |
|---|---|
| Jeu | Honey Select 2 DX — BetterRepack R16 (sorti le 2024-08-28) |
| Studio | Studio NEO V2 (`StudioNEOV2.exe`) |
| Racine d'installation | `<game>` — le dossier du repack ; son nom contient `[ ] ( )` |
| Unity | 2018.4.11f1 |
| Architecture | x86-64 (PE Machine = Amd64) |
| Backend de script | Mono (`MonoBleedingEdge`), profil .NET 4.x — **pas IL2CPP** |
| Framework cible du plugin | `net46` (mscorlib/System.Core 4.6.57.0, pas de `netstandard.dll`) |
| Loader | BepInEx 5.4.23 (FileVersion de la DLL 5.4.23.2) via Unity Doorstop (`winhttp.dll`) |
| Harmony | HarmonyX 2.9.0 (`0Harmony.dll`) |

**Piège du chemin :** le chemin d'installation contient `[ ] ( )`. Toujours `-LiteralPath` en PowerShell, jamais de motifs glob dessus, et le citer avec soin dans les props MSBuild.

## Pile de chargement

- Doorstop → `BepInEx\core\BepInEx.Preloader.dll` → BepInEx 5.4.23.2. Le point d'entrée est celui par défaut (`UnityEngine.CoreModule` / `Application` / `.cctor`).
- Patchers : CultureFix 21.6, MagicCarrot 21.6, HS2_ExtensibleSaveFormat_Patcher, BepInEx.SplashScreen 2.2.
- **Les logs console ET disque sont tous deux désactivés** dans `BepInEx\config\BepInEx.cfg` — le développement de plugin exige d'en activer un, ou de fournir son propre puits de logs.
- Dossier des plugins : `BepInEx\Plugins` (P majuscule), 192 DLL actives (+2 désactivées en `.dl_`).
- ScriptLoader compile du C# depuis le dossier `scripts\` à la racine du jeu à l'exécution (`AYAYA_v2.cs` présent) — une surface d'interaction à garder en tête.
- Doorstop expose un serveur de debug Mono optionnel (`127.0.0.1:10000`, actuellement désactivé).

## Assemblies

- Dossier de référence : `StudioNEOV2_Data\Managed` (114 DLL). Studio embarque **sa propre** `Assembly-CSharp.dll` (5 001 728 o, différente de la copie HoneySelect2) — toujours référencer la copie de Studio.
- Tout le code Studio et personnage vit dans `Assembly-CSharp.dll` (4660 TypeDefinitions) ; il n'y a pas d'assembly satellite propre à Studio.
- Notables : `IL.dll` (contient `NeckLookControllerVer2`, `Singleton<T>`, `UserData`, `PngFile`), Sirenix/Odin 2.0.13, UniRx + UniRx.Async, DLL `UnityEngine.*Module` modulaires.
- Éviter `System.Drawing` et `System.Windows.Forms` (builds anciens 2.0.50727).

## Surface de l'API Studio

Le dump de métadonnées liste 184 types de premier niveau + 459 types imbriqués dans le namespace `Studio`. Liste complète des types et dumps des membres : [discovery/studio-classes.md](discovery/studio-classes.md). Classes porteuses :

| Classe | Rôle |
|---|---|
| `Studio.Studio` | Singleton de scène / god-object — `dicObjectCtrl`, `sceneInfo`, `treeNodeCtrl`, `AddFemale/AddMale/AddItem/AddLight/AddFolder/AddRoute/AddCamera`, `GetCtrlInfo`, `LoadScene/SaveScene/ImportScene`, `Duplicate` |
| `Studio.ObjectCtrlInfo` | Base de tout objet de scène — `objectInfo`, `treeNodeObject`, `guideObject` |
| `Studio.OCIChar` | Personnage dans la scène (99 méthodes) — `charInfo`/`charReference` → `AIChara.ChaControl`, `fkCtrl`, `ikCtrl`, `neckLookCtrl`, `finalIK` ; `ActiveKinematicMode`, `ActiveIK/ActiveFK`, `LoadAnime`, `ChangeMouthPtn`, `ChangeEyesOpen`… |
| `Studio.OCIItem` / `OCILight` / `OCIFolder` / `OCICamera` / `OCIRoute` | Autres genres d'objets de scène ; les jumeaux sérialisables sont les classes `OI*Info` |
| `Studio.FKCtrl` / `IKCtrl` / `ItemFKCtrl` | Contrôle FK/IK ; l'enum du mode cinématique est `Studio.OICharInfo+KinematicMode` |
| `Studio.CameraControl` | La caméra du **viewport** (Import/Export CameraData) — distincte de l'objet `OCICamera` dans la scène |
| `Studio.SceneInfo` | Modèle de scène sérialisable, avec lumières chara/map et post-traitement (bloom/AO/DOF/fog/sunShafts/vignette) ; `Save/Load/Import` |
| `Studio.Info` | Catalogues d'assets — `dicItemLoadInfo`, `dicAnimeLoadInfo`, `dicLightLoadInfo`, `dicMapLoadInfo` : la source d'énumération du contenu instanciable |
| `Studio.TreeNodeCtrl` | Arbre de hiérarchie de la scène |
| `Studio.GuideObject` | Gizmo / poignées de transformation |
| `Studio.UndoRedoManager` + `*Command` | Annuler/rétablir natif — faire passer les écritures par des commandes quand c'est possible |
| `AIChara.ChaControl` | Corps, visage et vêtements du personnage (426 méthodes ; namespace `AIChara`, pas la racine) |
| `NeckLookControllerVer2` (`IL.dll`) | Regard cou/yeux (`ptnNo`, `target`, `rate`) |

Réserves :

- Les dumps ne listent que les **noms de méthodes** — obtenir les signatures exactes avec dnSpy (fourni dans `[MODDING] Tools`) ou `ilspycmd` (voir [studio-api.md](studio-api.md#méthode-de-rétro-ingénierie)) avant d'écrire des patchs Harmony ou des appels délicats.
- `LightCtrl` et `MoveAnimator` n'existent **pas** dans ce build ; les lumières sont `OCILight` plus `SceneInfo.charaLight` / `mapLight`.

## Écosystème de mods

Plugins installés qui comptent pour Miraneo :

| Plugin | Version | Pertinence pour Miraneo |
|---|---|---|
| HS2API (IllusionModdingAPI) | 1.41 | API de modding officielle, docs XML à côté de la DLL |
| Timeline (joan6694) | 1.4.1.0 | Animation par keyframes. Pas de DLL de référence — l'intégration passe par réflexion ([optional-plugins](../optional-plugins.md)) |
| HS2_Screencap (BepisPlugins R20) | 20.0 | Implémentation de référence des captures d'écran |
| RuntimeUnityEditor | 5.4.0 | Inspection en direct pendant le développement |
| NodesConstraints | 1.3.3.0 | Contraintes de nœuds. Pas de DLL de référence, et rien de public du tout ([studio-api.md](studio-api.md#pont-nodesconstraints-plugin-optionnel-réflexion-uniquement)) |
| HS2_ExtensibleSaveFormat (BepisPlugins) | R20 | Transpile `SceneInfo.Save`/`Load`/`Import` : les données de scène tierces voyagent donc avec le chemin de sauvegarde/chargement de Miraneo |
| HS2PE (PoseEditor) | — | Référence pour les données de pose |
| ConfigurationManager | 18.3.0 | Interface de configuration |

Outils fournis (dans `[MODDING] Tools`) : dnSpy 6.1.5, SB3Utility 23.1.7, Hooh Modding Tools 0.7, ZipStudio 0.2.1.

## Chemins de contenu

Tous les chemins sont relatifs à `<game>`.

| Quoi | Où |
|---|---|
| Scènes Studio | `UserData\Studio\scene` (107 fichiers) |
| Poses | `UserData\Studio\pose` (3868 fichiers) |
| Cartes de personnage | `UserData\chara\female` (1050), `UserData\chara\male` (30) |
| Coordonnées (tenues) | `UserData\coordinate\female` (1800), `UserData\coordinate\male` (166) |
| Captures d'écran | `UserData\cap` — **n'existe pas** avant la première capture en jeu |

## Chaîne d'outils de développement

- SDK dotnet 8.0.424 / 9.0.313 (csproj de style SDK + `Microsoft.NETFramework.ReferenceAssemblies` pour `net46`), bun 1.4.0, node 22.15.0.
- Pas de MSBuild dans le `PATH`.

## Conséquences pour Miraneo

1. Le plugin est un `BaseUnityPlugin` BepInEx 5, `net46`, x64. HarmonyX 2.9 est disponible si un patch devient nécessaire ; le plugin ne patche rien.
2. Les API Studio et Unity sont réservées au thread principal → le thread de transport fait transiter le travail vers `Update()` et termine les réponses de façon asynchrone ([règles de threading](studio-api.md#règles-de-threading)).
3. Les identifiants d'objets stables sont les clés de `Studio.Studio.dicObjectCtrl`.
4. La surface de lecture — scène (`sceneInfo`), objets (`dicObjectCtrl`), caméra (`CameraControl`), personnages (`OCIChar` → `ChaControl`), capture d'écran (Unity `ScreenCapture`, thread principal) — est accessible sans patch.
5. Les logs sont à fournir soi-même, car le repack désactive les logs console et disque de BepInEx.

Voir aussi : [architecture](../architecture.md), [protocol](../protocol.md), [tools](../tools.md).
