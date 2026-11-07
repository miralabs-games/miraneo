# API Studio — internes du plugin

Comment le plugin atteint Studio NEO V2 : les couches de `plugin/MiraStudio.Plugin/`, le contrat Core, les appels Studio exacts derrière chaque lecture et chaque écriture, et les pièges trouvés dans le code décompilé.

Contexte : [architecture](../architecture.md) (couches et décisions), [protocol](../protocol.md) (format sur le fil), [tools](../tools.md) (la surface MCP), [optional-plugins](../optional-plugins.md) (Timeline et NodesConstraints vus par l'utilisateur), [reverse-engineering](reverse-engineering.md) (l'installation cible).

## Couches

```text
Transport/RpcServer            HTTP JSON-RPC endpoint (worker threads)
Transport/MainThreadDispatcher queue drained in Update() — all Studio access goes through it
Services/StudioService         implements Core/IStudioApi (single-frame operations)
Services/ScreenshotService     WaitForEndOfFrame coroutine → ScreenCapture → PNG/JPG
Services/SceneService          scene loading and saving (waits for Studio's map coroutine)
Services/MapService            map changes (waits for Studio.onChangeMap)
Services/CameraService         camera writes (reads back after LateUpdate)
Services/KinematicsService     FK/IK mode and group switching (captures the live pose first)
Services/TimelineService       Timeline tools, forwards to Adapters/TimelineAdapter
Services/ConstraintService     NodesConstraints tools, forwards to Adapters/NodesConstraintsAdapter
Adapters/StudioAdapter         the ONLY file referencing Studio types
Adapters/TimelineAdapter       the ONLY file touching Timeline (reflection)
Adapters/NodesConstraintsAdapter  the ONLY file touching NodesConstraints (reflection)
Core/                          DTOs + JSON + Params + IStudioApi (no Unity types)
```

## Contrat Core

```csharp
public interface IStudioApi
{
    // Read
    RuntimeInfoDto  GetRuntimeInfo();
    SceneSummaryDto GetScene();
    List<CharacterDto>       ListCharacters();
    List<ObjectDto>          ListObjects();
    CameraDto                GetCamera();
    List<CatalogEntryDto>    SearchCatalog(string catalog, string query, int limit);
    List<SceneFileDto>       ListScenes(string query, int limit);
    List<CharacterCardDto>   ListCharacterCards(string sex, string query, int limit);
    List<PoseFileDto>        ListPoses(string sex, string query, int limit);
    List<CoordinateFileDto>  ListCoordinates(string sex, string query, int limit);
    List<AttachPointDto>     ListAttachPoints(int id);
    SkeletonDto              ListBones(int id);
    OutfitDto                ListOutfit(int id);
    List<OverlayFileDto>     ListOverlays(string kind, string query, int limit);

    // Write — patches carry only the fields the caller supplied, always absolute
    CameraDto    SetCamera(CameraPatch patch);
    ObjectDto    CreateItem(int group, int category, int no, TransformPatch transform);
    ObjectDto    CreateLight(int no, TransformPatch transform, LightPatch light);
    ObjectDto    SetLight(int id, LightPatch light);
    ObjectDto    SetObjectTransform(int id, TransformPatch transform);
    int          DeleteObject(int id);
    ObjectDto    SetObjectVisible(int id, bool visible);
    ObjectDto    RenameObject(int id, string name);
    ObjectDto    CreateFolder(string name, TransformPatch transform);
    DuplicateDto DuplicateObject(List<int> ids);
    ObjectDto    CreateCharacter(string card, string sex, TransformPatch transform);
    CharacterDto SetCharacterPose(int id, string path);
    CharacterDto SetCharacterAnime(int id, AnimePatch patch);
    CharacterDto SetCharacterExpression(int id, ExpressionPatch patch);
    CharacterDto SetCharacterHands(int id, HandsPatch patch);
    OutfitDto    SetCharacterOutfit(int id, OutfitPatch patch);
    CharacterDto SetCharacterState(int id, CharacterStatePatch patch);
    ObjectDto    AttachObject(int id, int? parentId, int? attachPoint, string attachPointName);
    SkeletonDto  SetCharacterBone(int id, List<KeyValuePair<string, Vec3>> rotations);
    SkeletonDto  SetCharacterIk(int id, List<IkTargetPatch> targets);
    SceneLightingDto SetSceneLighting(SceneLightingPatch patch);
    OverlaysDto  SetOverlay(OverlayPatch patch);
    PostProcessingDto SetPostProcessing(PostProcessingDto patch);
}
```

Les échecs sont des `StudioApiException(code, message)` :

| Constante | Code |
|---|---|
| `InvalidParams` | -32602 |
| `StudioNotReady` | -32000 |
| `OperationFailed` | -32001 |
| `NotFound` | -32003 |

Les opérations qui doivent s'étaler sur plusieurs frames ne sont pas sur cette interface. Elles vivent dans leurs propres services sous forme de `Task<JsonNode>` adossées à une coroutine, et sont enregistrées avec `RpcServer.RegisterAsync` :

| Tool | Service | Timeout |
|---|---|---|
| `studio_screenshot` | `ScreenshotService.Capture` | 60 s |
| `studio_set_camera` | `CameraService.SetCamera` | 30 s |
| `studio_set_character_kinematics` | `KinematicsService.SetKinematics` | 30 s |
| `studio_set_map` | `MapService.SetMap` | 180 s |
| `studio_load_scene` | `SceneService.LoadScene` | 180 s |
| `studio_save_scene` | `SceneService.SaveScene` | 60 s |

Chaque starter s'exécute sur le thread principal (15 s pour rendre sa task) ; le timeout du tableau couvre l'achèvement de la task. Les handlers simples enregistrés avec `RpcServer.Register` passent par `MainThreadDispatcher.Run` avec un timeout par défaut de 15 s (`studio_set_overlay` et `studio_set_post_processing` utilisent 60 s). `studio_execute_batch` est enregistré avec `RegisterBatch` (32 étapes au maximum, timeout de 300 s).

Les DTO se sérialisent eux-mêmes au format sur le fil via `ToJson()`. `Core/Json.cs` est un lecteur/écrivain JSON sans dépendance — le jeu n'embarque pas Newtonsoft.

## Carte d'accès à Studio

Ce que l'adapter appelle pour les lectures :

| Besoin | API Studio utilisée |
|---|---|
| Vérification de disponibilité | `Singleton<Studio.Studio>.IsInstance()` (`Singleton<T>` vit dans `IL.dll`) |
| Tous les objets de scène | `Studio.Studio.dicObjectCtrl` (`Dictionary<int, ObjectCtrlInfo>` ; clé = id stable) |
| Genre d'objet | Tests de type C# : `OCIChar`/`OCIItem`/`OCILight`/`OCIFolder`/`OCIRoute`/`OCIRoutePoint`/`OCICamera` (les waypoints d'une route sont enregistrés avec `Studio.AddCtrlInfo` comme le reste, donc ils apparaissent dans `dicObjectCtrl`) |
| Nom affiché / visibilité | `ObjectCtrlInfo.treeNodeObject.textName` / `.visible` |
| Transform | `ObjectCtrlInfo.objectInfo.changeAmount.pos/rot/scale` |
| Parent d'attache | `ObjectCtrlInfo.parentInfo.objectInfo.dicKey` |
| Nom de carte / sexe du personnage | `OCIChar.oiCharInfo.charFile.parameter.fullname`, `OCIChar.sex` |
| Résumé de scène | `Studio.Studio.sceneInfo` (`background`, `frame`, `mapInfo`, `charaLight`, et les champs d'effets d'écran à plat) |
| Caméra | `Studio.Studio.cameraCtrl` (`targetPos`, `cameraAngle`, `fieldOfView`) + `Camera.main` |
| Capture d'écran | `ScreenCapture.CaptureScreenshotAsTexture()` après `WaitForEndOfFrame` |

## Règles de threading

- Les API Studio et Unity sont réservées au thread principal. `RpcServer` reçoit les requêtes sur des threads de travail et exécute chaque handler via `MainThreadDispatcher.Run()` (file vidée dans `Update()`), avec des timeouts par méthode.
- Les violations ne lèvent PAS d'exception — elles font planter le processus nativement, à tort attribuées au pilote GPU (`nvwgf2umx.dll`, 0xc0000409/0xC0000005). C'est arrivé deux fois pendant la mise en route, quand `/health` appelait `Singleton<Studio.Studio>.Instance` hors du thread principal (le getter peut créer des GameObjects via `CheckInstance`). Le code hors thread principal ne peut lire que des champs mis en cache par le thread principal ; `Singleton<T>.IsInstance()` est la seule sonde statique acceptable.
- `studio_screenshot` est asynchrone : le thread principal ne fait que démarrer une coroutine ; le worker attend son `TaskCompletionSource`.
- `Application.runInBackground` est forcé à vrai (entrée de configuration `Server.RunInBackground`, `true` par défaut) pour que l'agent puisse piloter Studio fenêtre non focalisée.
- Un batch n'est jamais dispatché comme une méthode normale — voir [Batchs](#batchs).
- Ne jamais distribuer de références d'objets Unity — uniquement des ids (clés de `dicObjectCtrl`).
- Le transform de la caméra est recalculé dans `LateUpdate` (`InternalUpdateCameraState`) : tout ce qui relit `Camera.main` après une écriture caméra doit attendre une frame.
- L'échelle de la scène varie d'une scène à l'autre ; ne jamais supposer 1 unité = 1 m pour choisir des distances ou des portées de lumière.

## Chemin d'écriture

Chaque opération d'écriture et les appels Studio qui la sous-tendent, regroupés par sujet.

### Objets de scène

| Opération | API Studio utilisée |
|---|---|
| Instancier un item | `Studio.AddObjectItem.Add(group, category, no)` → `OCIItem`. À préférer à `Studio.Studio.AddItem`, qui renvoie void et vide la pile d'annulation. Fait en interne `UndoRedoManager.Do(AddObjectCommand.AddItemCommand(...))`, donc l'ajout est annulable. |
| Instancier une lumière | `Studio.AddObjectLight.Add(no)` → `OCILight` (même schéma annulable via `AddLightCommand`) |
| Valider les ids | `Singleton<Studio.Info>.Instance.dicItemLoadInfo[group][category][no]`, `dicLightLoadInfo[no]` ; noms pour la recherche depuis `dicItemGroupCategory` (`GroupInfo.name`, `CategoryInfo.name`) |
| Définir le transform | `UndoRedoManager.Do(GuideCommand.MoveEqualsCommand / RotationEqualsCommand / ScaleEqualsCommand)` avec `EqualsInfo{dicKey, oldValue, newValue}`. Ces commandes modifient `Studio.GetChangeAmount(dicKey)` ; l'adapter vérifie donc d'abord que c'est la même instance que `objectInfo.changeAmount`, sinon il retombe sur une affectation directe (non annulable). |
| Réglages de lumière | `OCILight.SetColor/SetIntensity/SetRange/SetSpotAngle/SetEnable/SetShadow` (passer `_force: true` — ils ne font rien quand la valeur est inchangée) |
| Supprimer | `treeNodeCtrl.DeleteNode(treeNodeObject)` (respecte `enableDelete`, descend dans les enfants, `Studio.OnDeleteNode` → `OCI.OnDelete` → `DeleteInfo` le retire de `dicObjectCtrl`) |
| Afficher / masquer | `ObjectCtrlInfo.treeNodeObject.visible = b` → `TreeNodeObject.SetVisible`, qui déclenche le `onVisible` du nœud (le `OnVisible` propre à l'OCI) et parcourt les enfants via `SetVisibleChild` |
| Renommer | `OCIFolder.name` / `OCICamera.name` / `OCIRoute.name` — les setters écrivent à la fois `OI*Info.name` (sérialisé) et `treeNodeObject.textName` (le libellé de l'UI). Après le renommage d'une caméra, `Studio.cameraSelector.Init()` reconstruit les libellés du menu déroulant et `Studio.ChangeCamera(cam, true)` ré-estampille l'en-tête pour la caméra active ; après le renommage d'une route, `Studio.routeControl.Init()` quand le panneau est affiché, exactement comme `Studio.AddRoute` |
| Ajouter un dossier | `Studio.AddObjectFolder.Add()` → `OCIFolder`. Contrairement aux items et lumières, il n'existe pas de commande pour cela, c'est donc ici que `Studio.AddFolder` appelle `UndoRedoManager.Clear()` |
| Dupliquer | `Studio.Studio.Duplicate()`, piloté en empruntant le `protected TreeNodeCtrl.hashSelectNode` le temps de l'appel (sauvegarder l'ensemble, en installer un contenant les nœuds cibles, restaurer dans un `finally`). Les résultats viennent des `sceneInfo.dicImport` (nouvelle clé → nouvel `ObjectInfo`) et `sceneInfo.dicChangeKey` (nouvelle clé → clé source), publics |
| Attacher / détacher | `treeNodeCtrl.SetParent(childNode, parentNodeOrNull)`. C'est le changement d'arbre qui déclenche le vrai travail : `TreeNodeCtrl.SetParent` déclenche `onParentage` → `Studio.OnParentage` → `FindLoop(parent).OnAttach(parent, dicInfo[child])`, ou `child.OnDetach()` quand le parent est null |
| Points d'attache | `OCIChar.dicAccessoryPoint` (`Dictionary<TreeNodeObject,int>`, construit par `AddObjectAssist.InitAccessoryPoint`), recherche de l'os via `ChaInfo.GetAccessoryParentTransform(index)`, noms depuis l'enum `ChaAccessoryDefine.AccessoryParentKey`, occupants actuels dans `oiCharInfo.child[index]` |

### Caméra, scènes, map et environnement

| Opération | API Studio utilisée |
|---|---|
| Caméra | Propriétés `CameraControl.targetPos / cameraAngle / fieldOfView` ; distance via `Export()` → `data.distance = (0,0,-d)` → `Import(data)` (`cameraData` lui-même est `protected`) |
| Charger une scène | `Studio.Studio.LoadSceneCoroutine(path)` — **pas** `LoadScene`, qui rend la main avant la fin de la coroutine de map |
| Sauvegarder une scène | Les trois étapes de `Studio.Studio.SaveScene()` : `OnSavePreprocessing()` sur `dicObjectCtrl`, puis `sceneInfo.cameraSaveData = cameraCtrl.Export()`, puis `SceneInfo.Save(path)`. `SaveScene()` lui-même ne prend pas de chemin et estampille son propre nom, donc il ne peut pas être réutilisé tel quel. La vignette est produite dans `SceneInfo.Save` par `gameScreenShot.CreatePngScreen(320, 180)`, qui refait le rendu de `GameScreenShot.renderCam[]` dans une `RenderTexture` temporaire — pas de lecture du backbuffer, donc techniquement aucun `WaitForEndOfFrame` n'est requis, mais l'adapter en attend un quand même (voir [Scènes](#scènes)) |
| Dossier des scènes | `UserData.Path` (namespace racine, `IL.dll`) + `Studio\scene` |
| Changer la map | `Studio.Studio.AddMap(no, _close, _wait, _coroutine: true)` → `Map.LoadMapCoroutine` (un `Scene.LoadBaseScene` complet) puis le `SetupMap` privé. Catalogue `Info.dicMapLoadInfo` (`MapLoadInfo.name`) ; `-1` libère la map |
| Transform / visibilité de la map | `sceneInfo.mapInfo.ca.pos/rot` + `Singleton<MapCtrl>.Instance.Reflect()`, annulable via `UndoRedoManager.Push(MapCommand.MoveEqualsCommand / RotationEqualsCommand)` ; `Map.VisibleOption` / `VisibleLight` (ils écrivent eux-mêmes dans `sceneInfo`), conditionnés par `Map.IsOption` / `IsLight` |
| Lumière de scène (chara) | `sceneInfo.charaLight` (`CameraLightCtrl.LightInfo` : `color`, `intensity`, `rot[2]`, `shadow`) + `Studio.cameraLightCtrl.Reflect()` |
| Overlays 2D | `BackgroundCtrl.Load(path)` et `Studio.frameCtrl.Load(name)` ; listes construites comme `BackgroundList`/`FrameList`, depuis `DefaultData.Create("bg")` + `UserData.Create("bg")` et `UserData.Create("frame")` |
| Post-traitement | Écrire les champs de `sceneInfo`, puis `Studio.systemButtonCtrl.UpdateInfo()`. `SystemButtonCtrl.Apply()` pousse les douze `EffectInfo` de `sceneInfo` vers leurs composants ; `UpdateInfo()` fait cela *et* rafraîchit les widgets du panneau, ce que Studio appelle lui-même après un chargement de scène. Catalogues `Info.dicColorGradingLoadInfo`, `dicReflectionProbeLoadInfo`, `dicSkyPatternInfo` |
| Cibles objet du post-traitement | `Studio.SetDepthOfFieldForcus(key)` et `Studio.SetSunCaster(key)` (tous deux transmettent à `SystemButtonCtrl`) |

### Personnages

| Opération | API Studio utilisée |
|---|---|
| Instancier un personnage | `Studio.AddObjectFemale.Add(cardPath)` / `AddObjectMale.Add(cardPath)` → `OCICharFemale`/`OCICharMale`. Synchrone (`ChaControl.Load`), fait toute la mise en place : `GetNewIndex`, `AddInfo`, `AddNode`, os/IK/regard, `LoadAnime` initial, expressions. |
| Lister les cartes | `Directory.GetFiles(UserData.Path + "chara/{female,male}", "*.png")` + `AIChara.ChaFileControl.LoadCharaFile(path, sex, noLoadPng: true)` → `parameter.fullname`, exactement comme `Studio.CharaList.InitFemaleList`. **Pas** `LoadFileLimited` : il renvoie toujours `false` (`return true` manquant) et modifie `Character.isMod`. |
| Appliquer un fichier de pose | `Studio.PauseCtrl.Load(ociChar, path)` ; liste avec `Directory.GetFiles(UserData.Create(PauseCtrl.savePath), "*.dat")`, nom via `PauseCtrl.LoadName`, filtre de sexe via `PauseCtrl.CheckIdentifyingCode(path, sex)` |
| Définir l'animation | `OCIChar.LoadAnime(group, category, no, normalizedTime)` ; catalogue `Info.dicAnimeLoadInfo` (g→c→n→`AnimeLoadInfo.name`), noms dans `Info.dicAGroupCategory` |
| Lecture de l'animation | `UndoRedoManager.Do(AnimeCommand.SpeedCommand / PatternCommand / OptionParamCommand)` sur `int[] dicKey` + tableaux d'anciennes valeurs, exactement comme `Studio.AnimeControl`. Les plages sont les bornes propres à ce panneau : vitesse 0..3, motif et les deux paramètres d'option 0..1 |
| Formes de mains | `OCIChar.ChangeHandAnime(lr, ptn)` (`lr` 0 = gauche, 1 = droite), borne `OCIChar.HandAnimeNum`, relu depuis `oiCharInfo.handPtn[lr]` |
| Visage | `OCIChar.ChangeEyesOpen/ChangeBlink/ChangeMouthPtn/ChangeMouthOpen/ChangeLipSync/SetTears/SetHohoAkaRate/ChangeLookEyesPtn/ChangeLookNeckPtn/EnableExpressionCategory` ; les motifs d'yeux et de sourcils passent par `ChaControl.ChangeEyesPtn/ChangeEyebrowPtn` |
| Liste des motifs yeux/sourcils | `Singleton<Manager.Character>.Instance.chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.custom_eye{,brow}_{f,m}).Keys` |
| Mode cinématique | `OCIChar.ActiveKinematicMode(OICharInfo.KinematicMode.FK\|IK, active, _force: false)` ; `none` = les deux désactivés. Par groupe : `OCIChar.ActiveFK(FKCtrl.parts[i], active)` et `OCIChar.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), active)` |
| Découverte des os / cibles IK | `OCIChar.listBones` (`BoneInfo.boneID`, `.boneGroup`, `.boneWeight`, `.boneInfo.changeAmount.rot`) et `OCIChar.listIKTarget` (`IKInfo.targetInfo`, `.boneObject`, `.guideObject.enableRot`) ; noms depuis `Singleton<Info>.Instance.dicBoneInfo[boneID].bone` (le nom du transform du rig) et `Info.BoneInfo.name` (légende japonaise) |
| Poser un os / une cible IK | `UndoRedoManager.Do(GuideCommand.RotationEqualsCommand / MoveEqualsCommand)` sur `OIBoneInfo.dicKey` / `OIIKTargetInfo.dicKey` — même chemin que les transforms d'objets, puisque `ObjectInfo..ctor` enregistre chaque os dans `Studio.dicChangeAmount` |
| Refléter la pose en direct | `OCIChar.fkCtrl.CopyBone((OIBoneInfo.BoneGroup)353)` / `ikCtrl.CopyBone((OIBoneInfo.BoneGroup)31)` — les masques que `MPCharCtrl` passe lui-même |
| Panneau d'état du personnage | `OCIChar.SetNipStand` / `SetTuyaRate` / `SetWetRate` / `SetVisibleSimple` / `SetSimpleColor` / `SetVisibleSon` / `SetSonLength` / `SetSiruFlags`, relu depuis `oiCharInfo` (`nipple`, `SkinTuyaRate`, `WetRate`, `visibleSimple`, `simpleColor`, `visibleSon`, `sonLength`, `siru`) |
| Relecture de l'état du personnage | `OCIChar.oiCharInfo` (`animeInfo`, `handPtn`, `enableIK/FK`, `activeIK/FK`, `expression`, `mouthOpen`, `lipSync`), `OCIChar.charFileStatus` (`eyesOpenMax`, `eyesBlink`, `eyesLookPtn`, `neckLookPtn`), `OCIChar.animeSpeed/animePattern/animeOptionParam1/animeOptionParam2/isAnimeMotion/isHAnime`, `charInfo.GetEyesPtn/GetEyebrowPtn/GetMouthPtn`, `charAnimeCtrl.normalizedTime`. **Pas** `oiCharInfo.kinematicMode` — voir [Cinématique](#cinématique-fk-et-ik) |

### Tenues et accessoires

| Opération | API Studio utilisée |
|---|---|
| Charger une tenue | `OCIChar.LoadClothesFile(path)` — toujours le virtual : `OCICharFemale` rafraîchit aussi la souplesse de la poitrine, récupère à nouveau les dynamic bones de la jupe et fait cycler le groupe FK de la jupe via `FKCtrl.ResetUsedBone`. Il transmet à `ChaControl.ChangeNowCoordinate(path, reload: true)` + `AssignCoordinate()` |
| Lister les tenues | `Directory.GetFiles(UserData.Path + "coordinate/{female,male}", "*.png")`, nom lu dans l'en-tête du fichier (`PngFile.SkipPng` dans `IL.dll`, puis numéro de produit, `【AIS_Clothes】`, version, langue, nom) — le même préfixe que `ChaFileCoordinate.LoadFile` lit avant sa charge MessagePack |
| État d'habillage | `OCIChar.SetClothesState(kind, state)` / `SetClothesStateAll(state)` ; ce qu'un emplacement accepte vient de `ChaControl.GetClothesStateKind(kind)` (`IsClothesStateType` pour une valeur), relu depuis `charFileStatus.clothesState[kind]` |
| Ce qui est porté | `ChaControl.infoClothes[kind].Name` et `infoAccessory[slot].Name` (`ListInfoBase.Name` choisit la langue du jeu), présence via `objClothes[kind]` / `objAccessory[slot]`, couplage via `ChaControl.notBot` / `notInnerB` |
| Visibilité des accessoires | `OCIChar.ShowAccessory(slot, flag)`, relu depuis `charFileStatus.showAccessory[slot]` ; os parent dans `nowCoordinate.accessory.parts[slot].parentKey` |

## Règles de comportement et pièges

Chaque règle ci-dessous vient du code décompilé, et la plupart ont été confirmées en direct. L'adapter les applique.

### Pile d'annulation

| Opération | Pile d'annulation |
|---|---|
| `CreateItem`, `CreateLight` | Annulable (`AddItemCommand`, `AddLightCommand`) |
| `SetObjectTransform`, rotations d'os et de cibles IK | Annulable (`GuideCommand`) |
| Réglages de lecture de l'animation | Annulable (`AnimeCommand`) |
| `SetMap`, transform seul | Annulable (`MapCommand`, poussé après application) |
| `DeleteObject` | Vidée |
| `CreateCharacter` | Vidée |
| `CreateFolder` | Vidée |
| `AttachObject` | Vidée |
| `SetMap` avec `no` fourni | Vidée |
| `DuplicateObject`, mode cinématique et bascules de groupes, éditions Timeline, éditions NodesConstraints | Laissée valide, rien n'est poussé |

- Studio NEO V2 n'a **aucune commande de suppression** : la suppression n'est pas annulable, et laisser la pile d'annulation en vie après coup rejouerait des commandes sur des objets détruits — donc `DeleteObject` vide la pile.
- `AddObjectCommand` ne définit que `AddItemCommand` et `AddLightCommand` — il n'y a **aucune commande d'ajout de personnage**. `Studio.AddFemale`/`AddMale` appellent `UndoRedoManager.Clear()` juste après l'ajout, et `CreateCharacter` fait de même.
- `OnAttach`/`OnDetach` réécrivent `objectInfo.changeAmount.pos/rot` entre l'espace monde et l'espace parent. Cela change silencieusement le sens de tout `GuideCommand` déjà dans la pile d'annulation, et Studio n'a aucune commande de parentage pour annuler l'attache elle-même — donc `AttachObject` vide la pile, comme `DeleteObject`.
- Le changement de map n'est pas annulable, pas plus que les deux bascules de map, et `SetupMap` réinitialise `changeAmount` sans commande, donc `SetMap` vide la pile dès que `no` est fourni. Un appel limité au transform pousse à la place un `MapCommand`, comme le font les champs de saisie de `MapCtrl` (appliquer d'abord, `Push` plutôt que `Do`).
- Le mode cinématique et les bascules de groupes ne sont pas annulables (Studio n'a aucune commande pour eux), mais contrairement à la suppression et à l'attache ils ne détruisent aucun objet et ne rebasent aucun `changeAmount`, donc la pile d'annulation reste valide — `SetKinematics` ne la vide pas. Annuler une rotation d'os pendant que sa chaîne est désactivée écrit simplement une valeur qui est réappliquée au retour de la chaîne.
- `Duplicate` alloue de nouvelles clés mais n'en invalide aucune ancienne et ne pousse aucune commande, donc il laisse la pile d'annulation valide.
- Parmi les écritures de personnage, les réglages de lecture de l'animation sont les seuls à avoir une commande, avec les rotations d'os et de cibles IK : `AnimeCommand` définit `SpeedCommand`/`PatternCommand`/`OptionParamCommand`, tandis que `ChangeHandAnime` et `LoadAnime` n'en ont aucune (les boutons de mains de Studio non plus). `animePattern` n'atteint l'animator que si `isAnimeMotion`, `animeOptionParam1`/`animeOptionParam2` que si `isHAnime` — sinon les setters stockent la valeur et s'arrêtent, donc les deux drapeaux sont exposés à la lecture.
- Timeline tient son propre historique et NodesConstraints n'a aucune commande ; les deux laissent intacte la pile d'annulation de la scène.

### Visage et animation

- Les motifs d'yeux et de sourcils sont des ids d'assets, pas des indices contigus (les mods ajoutent des ids très en dehors de 0..N). `MPCharCtrl.EtcInfo` affiche la *position* dans la liste de catégorie par sexe et la fait passer par le tableau de clés ; la surface MCP utilise ce même espace d'indices pour concorder avec ce que l'utilisateur voit dans Studio.
- `charAnimeCtrl.normalizedTime` est le temps normalisé de l'Animator Unity : il continue de croître pendant qu'un clip boucle et n'est échantillonné qu'une frame après un `LoadAnime`, c'est donc une lecture en direct, pas une valeur d'aller-retour.
- Un *fichier de pose* n'est pas une *animation* : `PauseCtrl.FileInfo.Apply` appelle `LoadAnime` **et** restaure chaque cible IK/FK ainsi que les drapeaux d'expression, et il est verrouillé par sexe ; `LoadAnime` seul ne l'est pas. Le catalogue d'animations lui-même n'est pas filtré par sexe — `MPCharCtrl.LoadAnime(sex, ...)` ignore son argument `sex`.

### Formes de mains

- **Les formes de mains sont orthogonales à tout le reste.** `PauseCtrl.FileInfo` n'a aucun champ de main et `LoadAnime` ne touche jamais `handPtn`, donc ni une pose ni un clip ne restaurent jamais les mains — vérifié en direct : un personnage conserve `handPtn` à travers les deux. Seuls `ChangeHandAnime` et un chargement de scène (`OICharInfo.Load`) l'écrivent.
- `ChangeHandAnime` écrit `oiCharInfo.handPtn[lr]` **avant** de pouvoir échouer, et ses deux modes d'échec sont invisibles pour l'appelant. `ptn` correspond à la clé de forme `ptn - 1` (0 signifie « pas de forme », `SetEnableShapeHand(lr, false)`), et `AnimationKeyInfo.GetInfo` ne garde que `Count <= key` : un `ptn` au-dessus de la plage renvoie `false` dans une chaîne que `ChaControl.SetShapeHandValue` écarte (il renvoie `true` inconditionnellement), tandis qu'un `ptn` négatif lève `ArgumentOutOfRangeException` au milieu de l'écriture. L'espace valide est donc `0..HandAnimeNum` inclus — comme `MPCharCtrl.HandInfo`, qui fixe `PatternInfo.num = HandAnimeNum + 1` — et les deux mains sont validées avant que l'une soit écrite, de sorte qu'une valeur rejetée ne peut pas laisser l'autre main appliquée.
- `OCIChar.HandAnimeNum` déréférence `ChaControl.sibHand` sans garde, donc il lève une exception tant que le bundle de formes n'est pas initialisé ; le chemin de lecture rapporte `count: -1` dans ce cas et le chemin d'écriture refuse.
- Studio n'a aucun nom lisible pour les formes de mains — `MPCharCtrl.PatternInfo` affiche un simple `$"{m_Ptn:0}"` — donc la surface MCP expose l'indice et rien d'autre, et les agents identifient les formes par capture d'écran.

### Cinématique (FK et IK)

- **`OICharInfo.kinematicMode` est une donnée morte.** `OICharInfo.Load` en est le seul écrivain dans tout l'assembly, et `ActiveKinematicMode` écrit `enableIK`/`enableFK` à la place. Il continue de rapporter ce que contenait le fichier de scène, donc `kinematic.mode` est dérivé de `enableIK`/`enableFK` (mutuellement exclusifs par construction) au lieu d'être lu depuis lui.
- **Un drapeau de groupe sans son interrupteur général est stocké et inerte, et FK et IK ne sont pas symétriques sur ce point.** `ActiveFK` garde le travail réel avec `_force || (Utility.SetStruct(ref activeFK[i], _active) && oiCharInfo.enableFK)` : FK désactivé, il écrit le drapeau et s'arrête. `ActiveIK` n'a pas `enableIK` dans sa garde : il atteint bien `ActiveIKGroup`, qui écrit les poids d'effecteurs FinalIK — inertes seulement parce que `finalIK.enabled` est faux. Dans les deux cas rien ne bouge, et `ActiveKinematicMode` re-force chaque groupe depuis les drapeaux stockés quand la chaîne est activée ; un patch doit donc appliquer `mode` **avant** ses drapeaux de groupes, sous peine de les perdre.
- **Le reflect doit s'exécuter après l'Animator et avant l'activation de la chaîne.** `MPCharCtrl` diffère ses boutons « reflect from animation » via `LateUpdateAsObservable().Take(1)` parce que les `localEulerAngles` lus plus tôt dans la frame sont périmés ; ses boutons de reflect FK↔IK sont appelés de façon synchrone, car ils lisent des valeurs que l'autre chaîne a déjà stabilisées. La contrainte d'ordre côté écriture est la nôtre : activer FK fait que `FKCtrl.LateUpdate` écrase les os animés avec les change amounts stockés dans cette même frame, donc une capture faite après la bascule relirait ce qu'elle devait remplacer. `SetKinematics` attend donc `WaitForEndOfFrame`, capture, puis applique le mode.
- **Changer de tenue repondère les os de la jupe.** `FKCtrl.ResetUsedBone` recalcule `boneWeight` pour les 48 os de jupe selon les nouveaux vêtements sans toucher aucun `dicKey`, donc la pile d'annulation reste valide mais `studio_list_bones` devient périmé — en direct, 6 pondérés avec une jupe, 0 avec un short.

### Os et cibles IK

- **Les os et les cibles IK sont des `ObjectInfo` ordinaires.** `ObjectInfo..ctor` appelle `Studio.AddChangeAmount(dicKey, changeAmount)` pour chaque `dicKey != -1`, donc `OIBoneInfo` et `OIIKTargetInfo` sont dans `dicChangeAmount` et le chemin `GuideCommand` existant s'applique tel quel — vérifié en direct : 149/149 os et 13/13 cibles enregistrés sur une femme, 135 os sur un homme. Comme `EqualsInfo[]` est un tableau, une commande couvre tout un lot d'os, ce qui fait d'une édition multi-os d'un agent une seule étape d'annulation.
- **`OIBoneInfo.group` est un masque, pas un id de groupe.** `AddObjectAssist.InitBone` stocke `(1 << group) | 1` pour les cinq groupes du corps, donc chaque os de bras, de jambe et de colonne porte aussi le bit `Body` ; `ActiveFK` compare avec `(boneGroup & parts[i]) != 0`. Le groupe FK `body` couvre donc tout le squelette sauf cheveux, cou, poitrine, mains et jupe, et le groupe auquel répond un os est la première entrée de `FKCtrl.parts` que son masque intersecte.
- **`FKCtrl` ignore les os non pondérés.** `TargetInfo.Update` n'applique un os que tant que `enable & boneWeight`, et `boneWeight` est calculé selon que les vêtements actuels sont réellement skinnés sur lui — en direct, 42 des 48 os de jupe d'une femme sont non pondérés sans jupe portée. En faire pivoter un stocke la valeur et ne déplace rien, donc le chemin d'écriture le refuse plutôt que de mentir.
- Les 13 cibles IK sont ajoutées par `AddObjectAssist.InitIKTarget` exactement dans l'ordre de `OICharInfo.IKTargetEN`, donc l'indice est le nom. Seuls les quatre effecteurs terminaux (`LeftHand`, `RightHand`, `LeftFoot`, `RightFoot`) sont créés avec `_usedRot: true` ; sur les neuf autres, `IKInfo.CopyBone` écrit `Vector3.zero` dans `changeAmount.rot`, donc une rotation envoyée là est écartée au reflect suivant — le chemin d'écriture la rejette.
- **Le nom d'os Excel est l'identifiant ; la légende non.** `Info.BoneInfo.bone` est le nom du transform Unity, il était unique sur les 149/135 os en direct, et correspondait à chaque fois à `guideObject.transformTarget.name`. `Info.BoneInfo.name` est une légende japonaise partagée par des familles entières (les cinq articulations du pouce droit se lisent toutes 親指), et `AddObjectAssist.AddBoneGuide` ne l'attache même pas au guide.

### Attacher et détacher

- **`TreeNodeCtrl.SetParent` renvoie `void` et ignore en silence.** Il abandonne tout le parentage — `OnAttach` compris — quand le nœud refuse de changer de parent, quand la cible refuse les enfants (`enableAddChild`, faux sur chaque nœud *groupe* d'accessoires), ou quand le déplacement fermerait une boucle, et ne dit rien. Le seul signal fiable est de relire `parentInfo` ensuite.
- **Un os manquant n'arrête pas une attache.** `ChaInfo.InitializeAccessoryParent` remplit `dictAccessoryParent` avec les 54 clés et laisse les *valeurs* nulles, donc `GetAccessoryParentTransform` renvoie un `Transform` null au lieu d'échouer. `OCIChar.OnAttach` va alors jusqu'au bout : `transformTarget.SetParent(null)` reparente l'objet à la racine de la scène, `changeAmount` est rebasé contre rien, et `parentInfo` est quand même affecté — chaque relecture rapporte une attache réussie sur un objet orphelin. D'où la garde stricte sur `GetAccessoryParentTransform(index) != null` avant d'écrire. (Sur ce build HS2, les 54 points se résolvent sur les deux sexes, homme comme femme — la garde vaut pour d'autres rigs et corps moddés, pas pour `N_Dan` sur une femme comme supposé au départ.)
- Attacher à **son propre** nœud de personnage plutôt qu'à l'un de ses nœuds de points d'accessoire est la même demi-écriture silencieuse vue de l'autre côté : `OnAttach` cherche le nœud dans `dicAccessoryPoint`, ne le trouve pas et retourne, laissant l'objet imbriqué dans l'arbre du workspace avec `parentInfo` toujours null. `attach_object` exige donc un point d'attache dès que le parent est un personnage.
- Détacher un objet sans parent est sans danger : `TreeNodeCtrl.SetParent(node, null)` court-circuite sur `CheckNode(node)` (le nœud est déjà une racine) et n'atteint jamais `OnDetach`, qui aurait déréférencé un `parentInfo` null.

### Visibilité et renommage

- **`ObjectInfo.visible` n'est écrit que par `OnSavePreprocessing`.** La valeur vivante est `treeNodeObject.visible` ; la copie sur l'objet est resynchronisée depuis elle juste avant chaque sauvegarde (c'est pourquoi le drapeau persiste) et est périmée entre-temps. La visibilité est donc lue et écrite sur le nœud, jamais sur les données de sauvegarde.
- **La visibilité n'est pas uniforme selon les genres, et Studio ne le dit que par une propriété en écriture seule.** `OCILight.OnVisible` est vide et `AddObjectLight` est le seul loader qui ne définit jamais `treeNodeObject.enableVisible`, donc une lumière n'a pas de bascule dans le workspace et le drapeau serait stocké et inerte ; `AddObjectRoute.LoadPoint` définit `enableVisible = false` sur les waypoints, dont la visibilité est pilotée par `OCIRoute.SetVisible` à la place. `TreeNodeObject.enableVisible` a un setter et pas de getter, donc la garde ne peut pas relire l'état et refuse par genre. `SetVisible` lui-même n'est pas gardé et écrirait volontiers l'un comme l'autre.
- **Le masquage se fait par branche et n'est pas destructif.** `SetVisibleChild` calcule l'état affiché de chaque enfant comme `parentVisible && child.visible` sans toucher `child.visible`, donc masquer un parent laisse intacts les drapeaux propres des enfants et le ré-afficher restaure la branche. Vérifié en direct sur un aller-retour sauvegarde/chargement.
- **Trois genres possèdent un nom ; les autres ne possèdent qu'un libellé.** `OIFolderInfo`, `OICameraInfo` et `OIRouteInfo` font chacun `Write`/`ReadString` d'un `name`, et `OCIFolder.name`/`OCICamera.name`/`OCIRoute.name` écrivent à la fois celui-ci et `treeNodeObject.textName`. Pour tout autre genre, `textName` est écrit une fois au chargement (depuis la carte, ou `Info.*LoadInfo.name`) et n'est lu par rien de ce qui sauvegarde — `OnSavePreprocessing` copie `treeState` et `visible` et aucun nom. Renommer autre chose ne survivrait que jusqu'au prochain chargement, donc `RenameObject` le refuse.
- Le renommage ne rafraîchit pas ce qui met le nom en cache : le menu déroulant des caméras (`CameraSelector.Init` reconstruit ses options depuis `OCICamera.name` et restaure la sélection en sortant), le texte d'en-tête (`textCamera` privé, écrit uniquement par `ChangeCamera` — qui est idempotent sur la caméra déjà active, `SetActive(true)` retournant tôt sur un abonnement vivant), et le panneau de route (`RouteControl.Init`, dont les nœuds prennent `OIRouteInfo.name`). Les trois sont relancés après un renommage, comme le font `AddCamera`/`AddRoute`.

### Duplication

- **`Studio.Duplicate` est piloté par la sélection, et il n'existe qu'une seule façon sûre de la définir.** Il lit `treeNodeCtrl.selectNodes`, soit `hashSelectNode.ToArray()`. `SelectSingle` déplacerait les gizmos (`GuideObjectManager.selectObject`), repeindrait les couleurs des nœuds et désélectionnerait un nœud déjà sélectionné ; le setter `selectNode` est pire, puisque `SetSelectNode` lit `Input.GetKey(P/O/X)` plus Ctrl et peut reparenter, détacher ou copier un transform au lieu de sélectionner. Échanger le champ `protected` pour la durée de l'appel et le restaurer dans un `finally` ne touche rien de tout cela. Tout ce qui suit est synchrone — `AddObjectAssist.LoadChild` n'a pas de branche coroutine, même pour les personnages — donc l'emprunt dure un appel.
- `Duplicate` fait passer la sélection par `sceneInfo.Save(writer, dict)` et `sceneInfo.Import`, ce qui explique qu'une copie porte tout ce que porte le format de sauvegarde, données `ExtendedSave` tierces comprises, et que sa restauration soit soumise au même délai `ExecuteDelayed` de 8 frames qu'un chargement de scène. `dicImport` ne contient que les racines (les enfants voyagent dans l'`ObjectInfo` de leur parent) et `dicChangeKey` relie chaque nouvelle clé à sa source, si bien que le tool peut nommer les copies au lieu de laisser l'appelant comparer la liste des objets.
- `enableCopy` est ce qui conditionne le bouton Dupliquer du workspace dans `WorkspaceCtrl`, et il est faux sur les waypoints de route, sur la racine enfant `子接続先` d'une route et sur les nœuds *groupe* d'accessoires d'un personnage. `Duplicate` ne le vérifie pas lui-même, donc `DuplicateObject` le fait.

### Scènes

- **La sauvegarde n'est pas atomique dans Studio et l'étape destructive vient en premier.** `SceneInfo.Save` ouvre la cible avec `FileMode.Create` — la tronquant — et n'appelle `CreatePngScreen` qu'*ensuite* ; une capture qui lève une exception laisse la scène précédente détruite. `SaveScene` écrit donc dans un voisin `*.mirastudio-tmp` et le `File.Move` sur la cible seulement une fois qu'on sait que les octets commencent par la signature PNG. `SceneInfo.Save` renvoie `true` inconditionnellement (il n'y a pas de chemin `false`), donc inspecter le fichier écrit est le *seul* signal d'échec.
- `SceneInfo.cameraSaveData` vaut `null` jusqu'à la première sauvegarde ou au premier chargement de la session (`Init()` l'efface, le constructeur ne le définit jamais) et `SceneInfo.Save` le déréférence sans garde — affecter `cameraCtrl.Export()` avant de sauvegarder est obligatoire, pas cosmétique. `cameraData[10]` est prérempli par le constructeur mais seulement rempli de nouveau par `Init()` quand Studio est instancié, donc il est lui aussi testé contre null.
- Une sauvegarde attend d'abord un `WaitForEndOfFrame`. La vignette rend les caméras en direct, dont le transform n'est appliqué que dans `LateUpdate`, et `OCIChar.OnSavePreprocessing` déréférence `neckLookCtrl` / `charInfo.animBody` sans garde null — un personnage ajouté dans le même drain du dispatcher provoquerait donc une NRE. Une frame couvre les deux.
- **Rien ne reflète `charaLight` quand une scène est chargée.** `Studio.OnLoadAfter` recharge la map, les deux overlays et `systemButtonCtrl.UpdateInfo()`, mais ne touche jamais `m_CameraLightCtrl`, dont les seuls appelants sont le `Start`/`OnEnable` de son propre panneau. Une scène chargée dans Studio standard garde donc l'éclairage précédent jusqu'à l'ouverture du panneau de lumière. `LoadScene` appelle lui-même `cameraLightCtrl.Reflect()` pour que le viewport corresponde à la scène qu'il vient de lire.

### Map

- **`SetupMap` réinitialise `mapInfo.ca` et s'exécute même quand la map ne change pas.** `Map.LoadMapCoroutine` court-circuite sur `no == Map.no`, mais `AddMapCoroutine` appelle `SetupMap` quoi qu'il arrive, donc re-sélectionner la map courante la recentre en silence. Et `mapInfo.no` est écrit par `SetupMap` avant de pouvoir servir de signal d'achèvement — le signal fiable est `Studio.onChangeMap`, l'`Action` publique que `SetupMap` déclenche en dernier. Un `no` inconnu est pire qu'une erreur : `LoadMapCoroutine` libère la map et `SetupMap` stocke quand même l'id, laissant une scène qui prétend avoir une map qu'elle n'a pas.
- **`SceneInfo.mapLight` est une donnée morte.** `SceneInfo.Init`, `Save` et `Load` en sont les seuls lecteurs dans tout l'assembly — rien ne pousse jamais `CameraLightCtrl.MapLightInfo` dans une `Light`. Seule `charaLight` est vivante, via `CameraLightCtrl`, donc la surface MCP expose une lumière de scène et non deux. Même forme de piège que `OICharInfo.kinematicMode`.

### Overlays, éclairage et post-traitement

- **L'arrière-plan et le cadre sont stockés de façon asymétrique.** `sceneInfo.background` garde un chemin relatif à la racine (`DefaultData/bg/x.png`, résolu contre `Studio.ApplicationPath`) parce que les arrière-plans existent sous les deux racines ; `sceneInfo.frame` garde un simple nom de fichier parce que `FrameCtrl.Load` préfixe lui-même `UserData/frame/`. Les deux méthodes `Load` renvoient `false` *et* effacent leur champ `sceneInfo` quand le fichier est absent, ce qui fait de `Load("")` la façon documentée d'effacer un overlay et de la valeur de retour le seul signal d'échec.
- **`SystemButtonCtrl` est l'applicateur du post-traitement, pas `ScreenEffect`.** Ses douze `EffectInfo` lisent chacun `sceneInfo` et écrivent un composant, et `Apply()`/`UpdateInfo()` les exécutent tous les douze — un patch partiel ne peut donc pas être appliqué partiellement : l'état complet des effets repasse à l'écran en une passe. C'est pourquoi chaque champ refusable est vérifié avant qu'un seul soit écrit.
- **Deux champs de post-traitement sont des ids d'objets, et ils échouent en silence.** `SetDepthOfFieldForcus` et `SetSunCaster` acceptent n'importe quelle clé et stockent discrètement `-1` pour tout ce dont `kind != 1`, donc les deux sont validés contre `OCIItem` en amont. Ils ne sont pas non plus symétriques à la restauration : `DOFInfo.Apply` re-résout `sceneInfo.depthForcus` à chaque application, tandis que `SunShaftsInfo.Apply` ne touche jamais `sunShafts.sunTransform` — seul `SetSunCaster` l'écrit, donc un sun caster doit être renvoyé explicitement.
- **`selfShadow` n'est pas un réglage de scène.** `SelfShadowInfo.Set` fait `QualitySettings.SetQualityLevel(level / 2 * 2 + (enable ? 0 : 1))` : il déplace d'un cran le niveau de qualité global et survit à la scène qui l'a défini.
- Les bornes numériques du panneau d'effets d'écran vivent dans son prefab (`InputCombination.Min/Max` lisent `slider.minValue/maxValue`), pas dans le code, donc elles sont documentées et non appliquées — contrairement à celles de la lumière chara, que `CameraLightCtrl` borne dans le code à `0.1..2` pour l'intensité et `0..359` par axe de rotation.

### Tenues et accessoires

- **`ChaControl.SetClothesState` ne peut pas échouer, alors il ment.** Quand l'état demandé n'est pas dans `dictStateType[kind]`, il boucle `state = (state + 1) % 3` jusqu'à en trouver un qui y soit (ou revenir à l'état courant) et applique *celui-là*. `SetClothesState` est aussi le seul écrivain, donc rien en aval ne voit jamais la différence — le chemin d'écriture vérifie lui-même `IsClothesStateType` et refuse. `AddClothesStateKindSub` n'enregistre qu'un emplacement dont le vêtement déclare `StateType` 0 (porté/mi-ouvert/retiré) ou 1 (porté/retiré) ; sans vêtement le type vaut 3 par défaut et l'emplacement n'a **aucune entrée**, auquel cas `SetClothesState` stocke l'octet brut sans validation et sans effet.
- **Le couplage entre emplacements voisins vit dans le moteur, pas dans l'API.** La fin de `SetClothesState` reflète `off` entre `top` et `bot` tant que `notBot`, et entre `inner_t` et `inner_b` tant que `notInnerB` (un vêtement couvre les deux emplacements), donc une écriture sur un seul emplacement peut en déplacer deux — d'où des relectures qui renvoient les huit. En direct : une tenue de ninja intégrale rapporte `bot` sans item mais avec une liste d'états complète, et remettre `bot` à `worn` a entraîné `top` avec lui.
- **`OCIChar.ShowAccessory` n'a aucun contrôle de bornes** — contrairement à `ChaControl.SetAccessoryState`, qui en a un — donc un emplacement ≥ 20 lève une exception au milieu de l'écriture, et sur un emplacement vide le drapeau est stocké et inerte : `ChaControl.UpdateVisible` ne parcourt que les emplacements dont `objAccessory` est non null. Les deux cas sont refusés en amont. `equipped` n'est pas la même chose que « porter quelque chose » : l'entrée explicite *None* du catalogue est un vrai objet, donc un emplacement peut être équipé, nommé « None », et n'avoir aucun état d'habillage.
- **Les écritures de vêtements et d'accessoires ne touchent que les données ; l'écran rattrape dans `LateUpdate`.** `Manager.Character` appelle `ChaControl.LateUpdateForce` → `UpdateVisible` à chaque frame, donc `clothesState` et `showAccessory` se relisent correctement tout de suite, mais une capture prise dans le même batch peut encore montrer l'état précédent.
- **`LoadClothesFile` est synchrone et avale ses deux valeurs de retour.** `Reload` démarre `ReloadAsync(asyncFlags: false)`, dont les branches synchrones ne cèdent jamais la main, donc tout le rechargement s'exécute en ligne — mais `OCIChar.LoadClothesFile` renvoie `void` et abandonne le `bool` de `ChangeNowCoordinate`, qui vaut `false` sur un fichier absent ou illisible. Le chemin d'écriture valide donc d'abord le fichier avec `ChaFileCoordinate.LoadFile`, le même appel que `ChangeNowCoordinate` s'apprête à faire.
- **Une coordinate ne porte aucun sexe et son nom ne survit pas au port.** Contrairement à un fichier de pose il n'y a pas de code d'identification — `ChaFileControl.ConvertCoordinateFilePath` préfixe simplement `coordinate/{male,female}/` pour un nom nu (et résout tout ce qui contient un composant de dossier contre le CWD du processus, donc l'adapter passe toujours un chemin absolu). Et `ChangeNowCoordinate` passe par `LoadBytes`, qui ne restaure que `clothes` et `accessory` : `nowCoordinate.coordinateName` garde ce qu'il contenait, donc ce qu'un personnage porte ne peut être relu qu'item par item, jamais comme un nom de tenue.

### Panneau d'état du personnage

- **`siru` est une écriture différée avec un plantage au bout.** `SetSiruFlag` ne fait que stocker dans `siruNewLv` ; `UpdateSiru`, une frame plus tard dans la mise à jour de `Manager.Character`, le copie dans `fileStatus.siruLv` et s'en sert pour indexer un `float[3]` — un niveau supérieur à 2 fait donc tomber le processus à cet endroit au lieu de produire une erreur dans l'appel qui l'a défini. Il est aussi réservé aux femmes (`UpdateSiru` retourne immédiatement à `sex == 0`, et `OCIChar.GetSiruFlags` est un 0 constant sur la classe de base), tout comme `SetNipStand`, dont l'implémentation de base est vide. La relecture utilise `oiCharInfo.siru`, écrit de façon synchrone et que `AddObjectAssist.UpdateState` repousse dans le personnage lors d'un chargement de scène.
- `wetRate` et `skinGlossRate` se bornent eux-mêmes à 0..1 dans leurs setters, donc la relecture montre la valeur bornée ; `ChangeNipRate` et `SetSonLength` ne bornent rien, et leurs bornes sont celles du prefab — documentées, non appliquées, comme celles du panneau d'effets d'écran.

### Batchs

- **Un batch ne doit pas être dispatché comme une méthode normale.** `RpcServer.Register` enveloppe son handler dans `MainThreadDispatcher.Run` ; un batch enregistré ainsi bouclerait *dans* `Drain()` pendant que chacune de ses étapes attend que le thread principal vide une file que seul ce même thread bloqué peut vider — un gel complet sur le chemin ordinaire, pas seulement sur un cas exotique. `RegisterBatch` garde donc la boucle sur le thread de travail HTTP (`Task.Run`) et laisse chaque étape se marshaller comme d'habitude, ce qui fait aussi fonctionner telles quelles les étapes asynchrones (`set_camera`, `set_character_kinematics`, `save_scene`).
- Un batch ne peut pas annuler et ne doit pas faire semblant. `UndoRedoManager.Do` pousse une commande *avant* de l'exécuter, donc une création dont le chargement d'asset échoue laisse une commande orpheline dans la pile et décale tous les indices suivants ; `DeleteObject`, `CreateCharacter` et `AttachObject` vident entièrement la pile. Ce qui est abordable, c'est de refuser le batch avant qu'il démarre : chaque étape est donc vérifiée en amont pour méthode connue, forme d'objet, imbrication et `studio_screenshot`.

## Pont Timeline (plugin optionnel, réflexion uniquement)

`Adapters/TimelineAdapter.cs` est le seul fichier qui touche Timeline (joan6694, `com.joan6694.illusionplugins.timeline`, testé en 1.4.1). Il n'y a pas d'assembly de référence : chaque membre est donc résolu par réflexion dans un unique `Bind()` paresseux tout-ou-rien ; un membre manquant renseigne `_error` et tout le pont se déclare indisponible. `Plugin.cs` déclare une dépendance souple `BepInDependency` pour que BepInEx charge Timeline en premier quand il est installé, et enregistre les tools dans les deux cas.

**Ce que Timeline expose publiquement est exactement la moitié de ce qu'il faut.**

| Besoin | API Timeline utilisée | Accès |
|---|---|---|
| Transport | `Timeline.playbackTime` / `duration` / `isPlaying`, `Play/Pause/Stop/Seek/NextFrame/PreviousFrame` | public static |
| Liste des pistes, keyframes | `Interpolable.keyframes` (`SortedList<float, Keyframe>`), `.enabled`, `.alias`, `.oci`, `.GetValue()` | public |
| Créer une piste | `new Interpolable(oci, model)` / `(oci, parameter, model)`, `Tree<,>.AddLeaf/RemoveLeaf` | public |
| Créer une keyframe | `new Keyframe(value, parent, curve)`, `.value`, `.curve` | public |
| Catalogue de modèles, compatibilité | `InterpolableModel.owner/id/parameter`, `.name`, `.IsCompatibleWithTarget(oci)` | public |
| Atteindre tout cela | `_self`, `_interpolables`, `_interpolablesTree`, `_interpolableModelsList`, `_selectedOCI`, `_duration`, `_desiredFrameRate`, `_singleFilesFolder` | champs private/internal |
| Redessiner, supprimer, fichiers uniques | `UpdateGrid()`, `UpdateInterpolablesView()`, `RemoveInterpolable(i)`, `LoadSingle(path)`, `SaveSingle(path)` | méthodes private |

`ToolBox.TimelineCompatibility` — embarqué dans Timeline et dans chaque plugin qui s'intègre avec lui — n'est **pas** le point d'entrée ici : il sert à *publier* de nouvelles propriétés animables (`AddInterpolableModelStatic/Dynamic`), pas à piloter la timeline.

- **`_selectedOCI` peut être emprunté sans risque le temps d'un appel.** `LoadSingle` et `SaveSingle` ne prennent aucune cible et lisent ce champ à la place. Le `Update` de Timeline ne le réécrit que lorsque `_selectedGuideObjects.FirstOrDefault()` diffère du guide object qu'il a vu en dernier, donc le définir, appeler et le restaurer au sein d'un même appel du thread principal n'entre jamais en collision avec le plugin — et, contrairement à piloter `TreeNodeCtrl.SelectSingle`, ne déplace aucun gizmo et ne touche aucune pile d'annulation.
- **La création de piste n'a jamais besoin de la sélection.** `Timeline.AddInterpolable(model)` construit son `Interpolable` depuis `_selectedOCI`, mais les constructeurs `Interpolable` à deux et trois arguments sont publics, donc le pont construit le sien contre l'`oci` cible et l'insère dans `_interpolables` (indexé par `Interpolable.GetHashCode()`, qui est aussi l'id que l'API distribue) plus `_interpolablesTree`. Pour les modèles dynamiques `guideObjectPos|Rot|Scale`, dont le `getParameter` renvoie `GuideObjectManager.Instance.selectObject`, le constructeur à trois arguments prend le guide object directement depuis `OCIChar.BoneInfo.guideObject`, `OCIChar.IKInfo.guideObject` ou `ObjectCtrlInfo.guideObject`.
- **Un id de modèle n'est pas unique.** `charClothes` et `juice` enregistrent un `InterpolableModel` par emplacement sous le même id, ne différant que par `parameter` et `name` ; d'autres plugins (HS2PE, Flow Control, PregnancyPlus — 95 modèles compatibles sur un personnage sur cette installation contre 38 pour Timeline seul) publient sous leur propre `owner`. `AddInterpolable` fait donc correspondre `(id, owner?, name?)` et rapporte les candidats quand le triplet reste ambigu.
- **Les valeurs de keyframe sont boxées et polymorphes.** `Keyframe.value` est un `object` dont seul son modèle connaît le type ; chaque modèle le sérialise via ses propres délégués XML. Plutôt que de les reproduire, le pont convertit selon le type à l'exécution (`Vector2/3/4`, `Quaternion` en degrés d'euler, `Color`, les types numériques, `bool`, `string`, `OICharInfo.AnimeInfo`) et retombe sur `ToString()`, ce qui se dégrade aussi proprement pour les modèles tiers. Les écritures sont typées contre la valeur actuellement renvoyée par `GetValue()`, donc une forme incompatible est refusée au lieu d'être forcée — et omettre la valeur capture la valeur vivante, ce qui est à la fois le flux naturel et la seule voie pour les types sans forme JSON.
- **Les presets de courbes sont copiés depuis Timeline, tangentes comprises** (`_linePreset`, `_topPreset`, `_bottomPreset`, `_hermitePreset`, `_stairsPreset`), donc une courbe définie via l'API est indiscernable de celle choisie dans la fenêtre de keyframe de Timeline. `stairs` porte une tangente `float.PositiveInfinity`, que `JsonNumber` écrit en `null` ; le lecteur accepte `null` en retour comme infini.
- **Rien ici ne passe par `UndoRedoManager`.** Timeline tient son propre historique, donc les éditions de timeline ne sont pas annulables depuis l'UI de Studio — et, symétriquement, elles laissent intacte la pile d'annulation de la scène.

## Pont NodesConstraints (plugin optionnel, réflexion uniquement)

`Adapters/NodesConstraintsAdapter.cs` est le seul fichier qui touche NodesConstraints (joan6694, `com.joan6694.illusionplugins.nodesconstraints`, testé en 1.3.3). Même forme que le pont Timeline — pas d'assembly de référence, un `Bind()` paresseux tout-ou-rien, un `BepInDependency` souple, tools enregistrés dans les deux cas — mais un niveau plus profond : **rien n'est public**, le type imbriqué `Constraint` compris, donc le type lui-même est résolu par réflexion avant qu'aucun de ses membres puisse l'être.

| Besoin | API NodesConstraints utilisée | Accès |
|---|---|---|
| Instance du plugin, liste des contraintes | `_self`, `_constraints` (`List<Constraint>`) | private static / private |
| Créer | `AddConstraint(...)`, la surcharge à 26 arguments | private |
| Supprimer | `RemoveConstraintAt(int)` | private |
| Activer / désactiver | `SetConstraintEnabled(Constraint, bool)` | private |
| Tout ce que contient une contrainte | ~30 champs sur `Constraint` | champs publics sur un **type imbriqué privé** |
| Masques par axe | `TransformLock(bool, bool, bool)` et ses `x`/`y`/`z` | type imbriqué public |

Règles qui découlent de la décompilation :

- **`AddConstraint` refuse les doublons en renvoyant `null`.** Il parcourt `_constraints` à la recherche de la même paire de transforms *dans l'un ou l'autre sens* et ne rend rien — ni exception, ni message. Le pont vérifie d'abord lui-même la paire pour que l'erreur puisse nommer la contrainte existante, et traite quand même un `null` comme un échec.
- **`SetConstraintEnabled` est une transaction, pas un setter.** Passer de `true` à `false` réécrit `originalChildPosition`/`Rotation`/`Scale` dans `childTransform` et dans le `changeAmount` du `GuideObject` enfant, conditionné par `position`/`rotation`/`scale` et `resetOriginal*` ; passer de `false` à `true` recapture `childTransform.localPosition`/`localRotation` comme nouveaux originaux. Écrire `.enabled` directement sauterait les deux et laisserait l'enfant là où la contrainte l'avait traîné, donc `Set` applique `enabled` en dernier et seulement via la méthode. `RemoveConstraintAt` effectue la même restauration avant de retirer l'entrée.
- **Il y a deux applicateurs et ils ne sont pas équivalents.** `Update` n'appelle `ApplyNodesConstraints()` que si `!_hasTimeline` ; avec Timeline installé, le travail est fait par `Timeline_Update_Postfix` → `ApplyConstraints()`. Le chemin standard exige `child != null || parent != null` et respecte `child.enablePos/enableRot/enableScale`, en ignorant `fixDynamicBone` ; le chemin Timeline saute les contraintes avec `fixDynamicBone` et ignore les drapeaux du guide. `_hasTimeline` est défini 10 frames après `Awake` par `TimelineCompatibility.Init()`.
- **Les offsets sont trois espaces différents.** `UpdatePosition` calcule `parentTransform.TransformPoint(movement * (factor - 1) + positionOffset)`, donc l'offset de position est local au parent et `Vector3.zero` place l'enfant sur l'origine du parent — c'est pourquoi un offset omis est capturé (`parentTransform.InverseTransformPoint(childTransform.position)`, la formule *Use current* du plugin) au lieu d'être mis à une valeur par défaut. `rotationOffset` est post-multiplié sur la rotation monde du parent (`Inverse(parent.rotation) * child.rotation` à la capture, ou `Inverse(lookRotation) * child.rotation` en mode `lookAt`, quaternion miroir compris) ; `scaleOffset` multiplie `originalParentScale` et est capturé comme `child.lossyScale / parent.lossyScale`.
- **`TransformLock` est un masque des axes pilotés, pas des axes figés.** `UpdatePosition` écrase la valeur calculée avec celle de l'enfant sur chaque axe dont le verrou est `false`. La valeur par défaut du ctor est `(true, true, true)`.
- **`GetPositionMovement` vaut `parentTransform.position - originalParentPosition`**, et `originalParent*` est capturé dans `AddConstraint` : une contrainte suit le mouvement du parent *depuis sa création*, pas le transform absolu du parent. C'est la même capture que la désactivation et la suppression restaurent.
- **`UpdateScale` divise par `childTransform.parent.lossyScale` sans garde null**, dans une boucle qui s'exécute à chaque frame hors de tout appel d'API. Un lien d'échelle sur un enfant à la racine de la scène est donc refusé en amont.
- **L'identité d'une contrainte est son instance.** `Constraint` ne surcharge ni `Equals` ni `GetHashCode`, donc le hash d'identité est stable pendant toute sa vie et c'est lui que l'API distribue — le même choix que le pont timeline pour les interpolables. Son indice dans `_constraints` est inutilisable : `RemoveConstraintAt` décale tout ce qui suit, donc une suppression multi-ids retire dans l'ordre décroissant des indices.
- **La persistance est `ExtendedSave`, pas le plugin.** NodesConstraints s'abonne à `ExtendedSave.SceneBeingSaved/Loaded/Imported`, et `HS2_ExtensibleSaveFormat` transpile `SceneInfo.Save`/`Load`/`Import` — les méthodes exactes par lesquelles passent `SaveScene` et `LoadSceneCoroutine`, donc le chemin de sauvegarde écrit à la main transporte les données de scène tierces sans modification. La restauration est différée par `ExecuteDelayed(..., 8)`, d'où l'écart de huit frames après un chargement.
- **`PopulateTimeline` publie exactement un modèle, `constraintEnabled`**, dont `getParameter` et `isCompatibleWithTarget` lisent tous deux le `_selectedConstraint` privé. Animer une contrainte depuis les tools timeline reviendrait à emprunter ce champ comme `LoadSingle`/`SaveSingle` empruntent `_selectedOCI` ; volontairement non fait.
- Rien ici ne passe par `UndoRedoManager` — aucune commande n'existe — et rien ici ne détruit d'objet ni ne rebase un `changeAmount` appartenant à Studio, donc la pile d'annulation reste utilisable au lieu d'être vidée.

## Méthode de rétro-ingénierie

`ilspycmd` (outil global dotnet) décompile les séquences d'appels exactes avant tout branchement :

```text
dotnet tool install --global ilspycmd --version 8.2.0.7535
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'      # the tool targets .NET 6; only 8/9 are installed
ilspycmd "<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -t Studio.AddObjectItem
ilspycmd "<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -t 'Studio.CameraControl+CameraData'
```

Un nom de type imbriqué est accepté mais pas respecté : `-t 'Studio.MPCharCtrl+FKInfo'` décompile tout `MPCharCtrl`. Rediriger vers un fichier et faire un grep plutôt que lire la sortie — les classes porteuses font des milliers de lignes.

[`tools/reverse-engineering/dump-studio-types.ps1`](../../../tools/reverse-engineering/dump-studio-types.ps1) donne le dump de métadonnées par noms seuls, plus rapide, quand les signatures ne sont pas nécessaires. dnSpy 6.1.5 (GUI + `dnSpy.Console.exe`) est livré avec le repack dans `[MODDING] Tools`.
