# Studio API — plugin internals

How the plugin reaches Studio NEO V2: the layering inside `plugin/MiraStudio.Plugin/`, the Core contract, the exact Studio calls behind every read and write, and the traps found in the decompiled code.

Context: [architecture](../architecture.md) (layers and decisions), [protocol](../protocol.md) (wire format), [tools](../tools.md) (the MCP surface), [optional-plugins](../optional-plugins.md) (Timeline and NodesConstraints as seen by the user), [reverse-engineering](reverse-engineering.md) (the target install).

## Layering

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

## Core contract

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

Failures are `StudioApiException(code, message)`:

| Constant | Code |
|---|---|
| `InvalidParams` | -32602 |
| `StudioNotReady` | -32000 |
| `OperationFailed` | -32001 |
| `NotFound` | -32003 |

Operations that must span frames are not on this interface. They live in their own services as coroutine-backed `Task<JsonNode>` and are registered with `RpcServer.RegisterAsync`:

| Tool | Service | Timeout |
|---|---|---|
| `studio_screenshot` | `ScreenshotService.Capture` | 60 s |
| `studio_set_camera` | `CameraService.SetCamera` | 30 s |
| `studio_set_character_kinematics` | `KinematicsService.SetKinematics` | 30 s |
| `studio_set_map` | `MapService.SetMap` | 180 s |
| `studio_load_scene` | `SceneService.LoadScene` | 180 s |
| `studio_save_scene` | `SceneService.SaveScene` | 60 s |

Each starter runs on the main thread (15 s to return its task); the timeout in the table covers the task's completion. Plain handlers registered with `RpcServer.Register` run through `MainThreadDispatcher.Run` with a 15 s default timeout (`studio_set_overlay` and `studio_set_post_processing` use 60 s). `studio_execute_batch` is registered with `RegisterBatch` (32 steps maximum, 300 s timeout).

DTOs serialize themselves to the wire format via `ToJson()`. `Core/Json.cs` is a dependency-free JSON reader/writer — the game ships no Newtonsoft.

## Studio access map

What the adapter calls for reads:

| Need | Studio API used |
|---|---|
| Ready check | `Singleton<Studio.Studio>.IsInstance()` (`Singleton<T>` lives in `IL.dll`) |
| All scene objects | `Studio.Studio.dicObjectCtrl` (`Dictionary<int, ObjectCtrlInfo>`; key = stable id) |
| Object kind | C# type tests: `OCIChar`/`OCIItem`/`OCILight`/`OCIFolder`/`OCIRoute`/`OCIRoutePoint`/`OCICamera` (a route's waypoints are registered with `Studio.AddCtrlInfo` like anything else, so they show up in `dicObjectCtrl`) |
| Display name / visibility | `ObjectCtrlInfo.treeNodeObject.textName` / `.visible` |
| Transform | `ObjectCtrlInfo.objectInfo.changeAmount.pos/rot/scale` |
| Attachment parent | `ObjectCtrlInfo.parentInfo.objectInfo.dicKey` |
| Character card name / sex | `OCIChar.oiCharInfo.charFile.parameter.fullname`, `OCIChar.sex` |
| Scene summary | `Studio.Studio.sceneInfo` (`background`, `frame`, `mapInfo`, `charaLight`, and the flat screen-effect fields) |
| Camera | `Studio.Studio.cameraCtrl` (`targetPos`, `cameraAngle`, `fieldOfView`) + `Camera.main` |
| Screenshot | `ScreenCapture.CaptureScreenshotAsTexture()` after `WaitForEndOfFrame` |

## Threading rules

- Studio and Unity APIs are main-thread only. `RpcServer` receives requests on worker threads and executes every handler through `MainThreadDispatcher.Run()` (queue drained in `Update()`), with per-method timeouts.
- Violations do NOT throw — they crash the process natively, misleadingly attributed to the GPU driver (`nvwgf2umx.dll`, 0xc0000409/0xC0000005). This happened twice during bring-up, when `/health` called `Singleton<Studio.Studio>.Instance` off-thread (the getter can create GameObjects via `CheckInstance`). Off-thread code may only read fields cached by the main thread; `Singleton<T>.IsInstance()` is the only acceptable static probe.
- `studio_screenshot` is asynchronous: the main thread only starts a coroutine; the worker awaits its `TaskCompletionSource`.
- `Application.runInBackground` is forced on (configuration entry `Server.RunInBackground`, default `true`) so the agent can drive Studio while the window is unfocused.
- A batch is never dispatched like a normal method — see [Batches](#batches).
- Never hand out Unity object references — ids only (`dicObjectCtrl` keys).
- The camera transform is recomputed in `LateUpdate` (`InternalUpdateCameraState`), so anything reading `Camera.main` back after a camera write must wait a frame.
- Scene scale varies per scene; never assume 1 unit = 1 m when choosing distances or light ranges.

## Write path

Every write operation and the Studio calls behind it, grouped by subject.

### Scene objects

| Operation | Studio API used |
|---|---|
| Spawn item | `Studio.AddObjectItem.Add(group, category, no)` → `OCIItem`. Prefer it over `Studio.Studio.AddItem`, which returns void and wipes the undo stack. Internally does `UndoRedoManager.Do(AddObjectCommand.AddItemCommand(...))`, so the add is undoable. |
| Spawn light | `Studio.AddObjectLight.Add(no)` → `OCILight` (same undoable pattern via `AddLightCommand`) |
| Validate ids | `Singleton<Studio.Info>.Instance.dicItemLoadInfo[group][category][no]`, `dicLightLoadInfo[no]`; names for search from `dicItemGroupCategory` (`GroupInfo.name`, `CategoryInfo.name`) |
| Set transform | `UndoRedoManager.Do(GuideCommand.MoveEqualsCommand / RotationEqualsCommand / ScaleEqualsCommand)` with `EqualsInfo{dicKey, oldValue, newValue}`. Those commands mutate `Studio.GetChangeAmount(dicKey)`, so the adapter first checks it is the same instance as `objectInfo.changeAmount` and otherwise falls back to a direct (non-undoable) assignment. |
| Light settings | `OCILight.SetColor/SetIntensity/SetRange/SetSpotAngle/SetEnable/SetShadow` (pass `_force: true` — they no-op when the value is unchanged) |
| Delete | `treeNodeCtrl.DeleteNode(treeNodeObject)` (respects `enableDelete`, recurses into children, `Studio.OnDeleteNode` → `OCI.OnDelete` → `DeleteInfo` removes it from `dicObjectCtrl`) |
| Show / hide | `ObjectCtrlInfo.treeNodeObject.visible = b` → `TreeNodeObject.SetVisible`, which fires the node's `onVisible` (the OCI's own `OnVisible`) and walks the children through `SetVisibleChild` |
| Rename | `OCIFolder.name` / `OCICamera.name` / `OCIRoute.name` — the setters write both `OI*Info.name` (serialised) and `treeNodeObject.textName` (the UI label). After a camera rename, `Studio.cameraSelector.Init()` rebuilds the dropdown labels and `Studio.ChangeCamera(cam, true)` re-stamps the header for the active one; after a route rename, `Studio.routeControl.Init()` when the panel is up, exactly as `Studio.AddRoute` does |
| Add folder | `Studio.AddObjectFolder.Add()` → `OCIFolder`. Unlike items and lights there is no command for it, so this is where `Studio.AddFolder` calls `UndoRedoManager.Clear()` |
| Duplicate | `Studio.Studio.Duplicate()`, driven by borrowing the `protected TreeNodeCtrl.hashSelectNode` for the call (save the set, install one holding the target nodes, restore in a `finally`). Results come from the public `sceneInfo.dicImport` (new key → new `ObjectInfo`) and `sceneInfo.dicChangeKey` (new key → source key) |
| Attach / detach | `treeNodeCtrl.SetParent(childNode, parentNodeOrNull)`. The tree change is what triggers the real work: `TreeNodeCtrl.SetParent` fires `onParentage` → `Studio.OnParentage` → `FindLoop(parent).OnAttach(parent, dicInfo[child])`, or `child.OnDetach()` when the parent is null |
| Attach points | `OCIChar.dicAccessoryPoint` (`Dictionary<TreeNodeObject,int>`, built by `AddObjectAssist.InitAccessoryPoint`), bone lookup `ChaInfo.GetAccessoryParentTransform(index)`, names from the `ChaAccessoryDefine.AccessoryParentKey` enum, current occupants in `oiCharInfo.child[index]` |

### Camera, scenes, map and environment

| Operation | Studio API used |
|---|---|
| Camera | `CameraControl.targetPos / cameraAngle / fieldOfView` properties; distance via `Export()` → `data.distance = (0,0,-d)` → `Import(data)` (`cameraData` itself is `protected`) |
| Load scene | `Studio.Studio.LoadSceneCoroutine(path)` — **not** `LoadScene`, which returns before the map coroutine finishes |
| Save scene | The three steps of `Studio.Studio.SaveScene()`: `OnSavePreprocessing()` over `dicObjectCtrl`, then `sceneInfo.cameraSaveData = cameraCtrl.Export()`, then `SceneInfo.Save(path)`. `SaveScene()` itself takes no path and stamps its own name, so it cannot be reused as-is. The thumbnail is produced inside `SceneInfo.Save` by `gameScreenShot.CreatePngScreen(320, 180)`, which re-renders `GameScreenShot.renderCam[]` into a temporary `RenderTexture` — no backbuffer grab, so no `WaitForEndOfFrame` is technically required, but the adapter waits for one anyway (see [Scenes](#scenes)) |
| Scene folder | `UserData.Path` (root namespace, `IL.dll`) + `Studio\scene` |
| Change the map | `Studio.Studio.AddMap(no, _close, _wait, _coroutine: true)` → `Map.LoadMapCoroutine` (a full `Scene.LoadBaseScene`) then the private `SetupMap`. Catalog `Info.dicMapLoadInfo` (`MapLoadInfo.name`); `-1` releases the map |
| Map transform / visibility | `sceneInfo.mapInfo.ca.pos/rot` + `Singleton<MapCtrl>.Instance.Reflect()`, undoable via `UndoRedoManager.Push(MapCommand.MoveEqualsCommand / RotationEqualsCommand)`; `Map.VisibleOption` / `VisibleLight` (they write `sceneInfo` themselves), gated on `Map.IsOption` / `IsLight` |
| Scene (chara) light | `sceneInfo.charaLight` (`CameraLightCtrl.LightInfo`: `color`, `intensity`, `rot[2]`, `shadow`) + `Studio.cameraLightCtrl.Reflect()` |
| 2D overlays | `BackgroundCtrl.Load(path)` and `Studio.frameCtrl.Load(name)`; lists as `BackgroundList`/`FrameList` build them, from `DefaultData.Create("bg")` + `UserData.Create("bg")` and `UserData.Create("frame")` |
| Post-processing | Write the `sceneInfo` fields, then `Studio.systemButtonCtrl.UpdateInfo()`. `SystemButtonCtrl.Apply()` pushes all twelve `EffectInfo`s from `sceneInfo` into their components; `UpdateInfo()` does that *and* refreshes the panel's widgets, which is what Studio itself calls after a scene load. Catalogs `Info.dicColorGradingLoadInfo`, `dicReflectionProbeLoadInfo`, `dicSkyPatternInfo` |
| Post-processing object targets | `Studio.SetDepthOfFieldForcus(key)` and `Studio.SetSunCaster(key)` (both forward to `SystemButtonCtrl`) |

### Characters

| Operation | Studio API used |
|---|---|
| Spawn character | `Studio.AddObjectFemale.Add(cardPath)` / `AddObjectMale.Add(cardPath)` → `OCICharFemale`/`OCICharMale`. Synchronous (`ChaControl.Load`), does the whole setup: `GetNewIndex`, `AddInfo`, `AddNode`, bones/IK/look-at, initial `LoadAnime`, expressions. |
| List cards | `Directory.GetFiles(UserData.Path + "chara/{female,male}", "*.png")` + `AIChara.ChaFileControl.LoadCharaFile(path, sex, noLoadPng: true)` → `parameter.fullname`, exactly as `Studio.CharaList.InitFemaleList`. **Not** `LoadFileLimited`: it always returns `false` (missing `return true`) and mutates `Character.isMod`. |
| Apply pose file | `Studio.PauseCtrl.Load(ociChar, path)`; list with `Directory.GetFiles(UserData.Create(PauseCtrl.savePath), "*.dat")`, name via `PauseCtrl.LoadName`, sex filter via `PauseCtrl.CheckIdentifyingCode(path, sex)` |
| Set animation | `OCIChar.LoadAnime(group, category, no, normalizedTime)`; catalog `Info.dicAnimeLoadInfo` (g→c→n→`AnimeLoadInfo.name`), names in `Info.dicAGroupCategory` |
| Anime playback | `UndoRedoManager.Do(AnimeCommand.SpeedCommand / PatternCommand / OptionParamCommand)` over `int[] dicKey` + old-value arrays, exactly as `Studio.AnimeControl` does. Ranges are that panel's own clamps: speed 0..3, pattern and both option params 0..1 |
| Hand shapes | `OCIChar.ChangeHandAnime(lr, ptn)` (`lr` 0 = left, 1 = right), bound `OCIChar.HandAnimeNum`, read back from `oiCharInfo.handPtn[lr]` |
| Face | `OCIChar.ChangeEyesOpen/ChangeBlink/ChangeMouthPtn/ChangeMouthOpen/ChangeLipSync/SetTears/SetHohoAkaRate/ChangeLookEyesPtn/ChangeLookNeckPtn/EnableExpressionCategory`; eye and eyebrow patterns go through `ChaControl.ChangeEyesPtn/ChangeEyebrowPtn` |
| Eye/eyebrow pattern list | `Singleton<Manager.Character>.Instance.chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.custom_eye{,brow}_{f,m}).Keys` |
| Kinematic mode | `OCIChar.ActiveKinematicMode(OICharInfo.KinematicMode.FK\|IK, active, _force: false)`; `none` = both turned off. Per group: `OCIChar.ActiveFK(FKCtrl.parts[i], active)` and `OCIChar.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), active)` |
| Bone / IK target discovery | `OCIChar.listBones` (`BoneInfo.boneID`, `.boneGroup`, `.boneWeight`, `.boneInfo.changeAmount.rot`) and `OCIChar.listIKTarget` (`IKInfo.targetInfo`, `.boneObject`, `.guideObject.enableRot`); names from `Singleton<Info>.Instance.dicBoneInfo[boneID].bone` (the rig's transform name) and `Info.BoneInfo.name` (Japanese caption) |
| Pose a bone / IK target | `UndoRedoManager.Do(GuideCommand.RotationEqualsCommand / MoveEqualsCommand)` on `OIBoneInfo.dicKey` / `OIIKTargetInfo.dicKey` — same path as object transforms, since `ObjectInfo..ctor` registers every bone in `Studio.dicChangeAmount` |
| Reflect the live pose | `OCIChar.fkCtrl.CopyBone((OIBoneInfo.BoneGroup)353)` / `ikCtrl.CopyBone((OIBoneInfo.BoneGroup)31)` — the masks `MPCharCtrl` itself passes |
| Character state panel | `OCIChar.SetNipStand` / `SetTuyaRate` / `SetWetRate` / `SetVisibleSimple` / `SetSimpleColor` / `SetVisibleSon` / `SetSonLength` / `SetSiruFlags`, read back from `oiCharInfo` (`nipple`, `SkinTuyaRate`, `WetRate`, `visibleSimple`, `simpleColor`, `visibleSon`, `sonLength`, `siru`) |
| Character state read-back | `OCIChar.oiCharInfo` (`animeInfo`, `handPtn`, `enableIK/FK`, `activeIK/FK`, `expression`, `mouthOpen`, `lipSync`), `OCIChar.charFileStatus` (`eyesOpenMax`, `eyesBlink`, `eyesLookPtn`, `neckLookPtn`), `OCIChar.animeSpeed/animePattern/animeOptionParam1/animeOptionParam2/isAnimeMotion/isHAnime`, `charInfo.GetEyesPtn/GetEyebrowPtn/GetMouthPtn`, `charAnimeCtrl.normalizedTime`. **Not** `oiCharInfo.kinematicMode` — see [Kinematics](#kinematics-fk-and-ik) |

### Outfits and accessories

| Operation | Studio API used |
|---|---|
| Load an outfit | `OCIChar.LoadClothesFile(path)` — always the virtual: `OCICharFemale` also refreshes bust softness, re-fetches the skirt dynamic bones and cycles the FK skirt group through `FKCtrl.ResetUsedBone`. It forwards to `ChaControl.ChangeNowCoordinate(path, reload: true)` + `AssignCoordinate()` |
| List outfits | `Directory.GetFiles(UserData.Path + "coordinate/{female,male}", "*.png")`, name from the file header (`PngFile.SkipPng` in `IL.dll`, then product no, `【AIS_Clothes】`, version, language, name) — the same prefix `ChaFileCoordinate.LoadFile` reads before its MessagePack payload |
| Dressing state | `OCIChar.SetClothesState(kind, state)` / `SetClothesStateAll(state)`; what a slot accepts is `ChaControl.GetClothesStateKind(kind)` (`IsClothesStateType` for one value), read back from `charFileStatus.clothesState[kind]` |
| What is worn | `ChaControl.infoClothes[kind].Name` and `infoAccessory[slot].Name` (`ListInfoBase.Name` picks the game's language), presence from `objClothes[kind]` / `objAccessory[slot]`, coupling from `ChaControl.notBot` / `notInnerB` |
| Accessory visibility | `OCIChar.ShowAccessory(slot, flag)`, read back from `charFileStatus.showAccessory[slot]`; parent bone in `nowCoordinate.accessory.parts[slot].parentKey` |

## Behaviour rules and traps

Each rule below comes from decompiled code, and most were confirmed live. The adapter enforces them.

### Undo stack

| Operation | Undo stack |
|---|---|
| `CreateItem`, `CreateLight` | Undoable (`AddItemCommand`, `AddLightCommand`) |
| `SetObjectTransform`, bone and IK-target rotations | Undoable (`GuideCommand`) |
| Anime playback settings | Undoable (`AnimeCommand`) |
| `SetMap`, transform only | Undoable (`MapCommand`, pushed after applying) |
| `DeleteObject` | Cleared |
| `CreateCharacter` | Cleared |
| `CreateFolder` | Cleared |
| `AttachObject` | Cleared |
| `SetMap` with `no` supplied | Cleared |
| `DuplicateObject`, kinematic mode and group toggles, Timeline edits, NodesConstraints edits | Left valid, nothing pushed |

- Studio NEO V2 has **no delete command**: deletion is not undoable, and leaving the undo stack alive after one would replay commands against destroyed objects — so `DeleteObject` clears the stack.
- `AddObjectCommand` only defines `AddItemCommand` and `AddLightCommand` — there is **no add-character command**. `Studio.AddFemale`/`AddMale` call `UndoRedoManager.Clear()` right after adding, and `CreateCharacter` mirrors that.
- `OnAttach`/`OnDetach` rewrite `objectInfo.changeAmount.pos/rot` between world and parent space. That silently changes the meaning of every `GuideCommand` already on the undo stack, and Studio has no parentage command to undo the attach itself — so `AttachObject` clears the stack, like `DeleteObject`.
- The map change is not undoable, nor are the two map toggles, and `SetupMap` resets `changeAmount` with no command, so `SetMap` clears the stack whenever `no` is supplied. A transform-only call instead pushes `MapCommand`, the same way `MapCtrl`'s own input fields do (apply first, `Push` rather than `Do`).
- The kinematic mode and the group toggles are not undoable (Studio has no command for them), but unlike delete and attach they destroy no object and rebase no `changeAmount`, so the undo stack stays valid — `SetKinematics` does not clear it. Undoing a bone rotation while its chain is off simply writes a value that is re-applied when the chain comes back.
- `Duplicate` allocates new keys but invalidates none of the old ones and pushes no command, so it leaves the undo stack valid.
- Among character writes, the anime playback settings are the only ones with a command besides bone and IK-target rotations: `AnimeCommand` defines `SpeedCommand`/`PatternCommand`/`OptionParamCommand`, while `ChangeHandAnime` and `LoadAnime` have none (neither do Studio's own hand buttons). `animePattern` only reaches the animator when `isAnimeMotion`, `animeOptionParam1`/`animeOptionParam2` only when `isHAnime` — otherwise the setters store the value and stop, so both flags are exposed on read.
- Timeline keeps its own history and NodesConstraints has no command; both leave the scene's undo stack untouched.

### Face and animation

- Eye and eyebrow patterns are asset ids, not contiguous indices (mods add ids far outside 0..N). `MPCharCtrl.EtcInfo` shows the *position* in the per-sex category list and maps it through the key array; the MCP surface uses that same index space so it agrees with what the user sees in Studio.
- `charAnimeCtrl.normalizedTime` is Unity's Animator normalized time: it keeps growing while a clip loops and is only sampled a frame behind a `LoadAnime`, so it is a live reading, not a round-trip value.
- A *pose file* is not an *animation*: `PauseCtrl.FileInfo.Apply` calls `LoadAnime` **and** restores every IK/FK target plus the expression flags, and is sex-locked; `LoadAnime` alone is not. The anime catalog itself is not sex-filtered — `MPCharCtrl.LoadAnime(sex, ...)` ignores its `sex` argument.

### Hand shapes

- **Hand shapes are orthogonal to everything else.** `PauseCtrl.FileInfo` has no hand field at all and `LoadAnime` never touches `handPtn`, so neither a pose nor a clip ever restores the hands — verified live: a character keeps `handPtn` across both. Only `ChangeHandAnime` and a scene load (`OICharInfo.Load`) write it.
- `ChangeHandAnime` writes `oiCharInfo.handPtn[lr]` **before** it can fail, and both its failure modes are invisible to the caller. `ptn` maps to shape key `ptn - 1` (0 means "no shape", `SetEnableShapeHand(lr, false)`), and `AnimationKeyInfo.GetInfo` only guards `Count <= key`: a `ptn` above the range returns `false` up a chain that `ChaControl.SetShapeHandValue` discards (it returns `true` unconditionally), while a negative one throws `ArgumentOutOfRangeException` out of the middle of the write. Hence the valid space is `0..HandAnimeNum` inclusive — same as `MPCharCtrl.HandInfo`, which sets `PatternInfo.num = HandAnimeNum + 1` — and both hands are validated before either is written, so a rejected value cannot leave the other hand applied.
- `OCIChar.HandAnimeNum` dereferences `ChaControl.sibHand` with no guard, so it throws until the shape bundle is initialised; the read path reports `count: -1` there and the write path refuses.
- Studio has no readable names for hand shapes — `MPCharCtrl.PatternInfo` renders a bare `$"{m_Ptn:0}"` — so the MCP surface exposes the index and nothing else, and agents identify shapes by screenshot.

### Kinematics (FK and IK)

- **`OICharInfo.kinematicMode` is dead data.** `OICharInfo.Load` is its only writer in the whole assembly, and `ActiveKinematicMode` writes `enableIK`/`enableFK` instead. It keeps reporting whatever the scene file held, so `kinematic.mode` is derived from `enableIK`/`enableFK` (which are mutually exclusive by construction) rather than read from it.
- **A group flag without its master switch is stored and inert, and FK and IK are not symmetric about it.** `ActiveFK` guards the actual work with `_force || (Utility.SetStruct(ref activeFK[i], _active) && oiCharInfo.enableFK)`, so with FK off it writes the flag and stops. `ActiveIK` has no `enableIK` in its guard: it does reach `ActiveIKGroup`, which writes FinalIK effector weights — inert only because `finalIK.enabled` is false. Either way nothing moves, and `ActiveKinematicMode` re-forces every group from the stored flags when the chain is switched on, so a patch must apply `mode` **before** its group flags or lose them.
- **Reflect must run after the Animator and before the chain is enabled.** `MPCharCtrl` defers its "reflect from animation" buttons through `LateUpdateAsObservable().Take(1)` because `localEulerAngles` read earlier in the frame is stale; its FK↔IK reflect buttons are called synchronously, since those read values the other chain already settled. The ordering constraint on the write side is ours: enabling FK makes `FKCtrl.LateUpdate` overwrite the animated bones from the stored change amounts in that very frame, so a capture done after the switch would read back what it was meant to replace. `SetKinematics` therefore waits `WaitForEndOfFrame`, captures, then applies the mode.
- **Changing an outfit re-weights the skirt bones.** `FKCtrl.ResetUsedBone` recomputes `boneWeight` for the 48 skirt bones against the new clothes without touching any `dicKey`, so the undo stack stays valid but `studio_list_bones` goes stale — live, 6 weighted with a skirt on, 0 with shorts.

### Bones and IK targets

- **Bones and IK targets are ordinary `ObjectInfo`s.** `ObjectInfo..ctor` calls `Studio.AddChangeAmount(dicKey, changeAmount)` for every `dicKey != -1`, so `OIBoneInfo` and `OIIKTargetInfo` are in `dicChangeAmount` and the existing `GuideCommand` path applies verbatim — verified live: 149/149 bones and 13/13 targets registered on a female, 135 bones on a male. Since `EqualsInfo[]` is an array, one command covers a whole batch of bones, which is what makes an agent's multi-bone edit a single undo step.
- **`OIBoneInfo.group` is a mask, not a group id.** `AddObjectAssist.InitBone` stores `(1 << group) | 1` for the five body groups, so every arm, leg and spine bone also carries the `Body` bit; `ActiveFK` matches with `(boneGroup & parts[i]) != 0`. Hence the FK `body` group covers the whole skeleton except hair, neck, breast, hands and skirt, and the group a bone answers to is the first entry of `FKCtrl.parts` its mask intersects.
- **`FKCtrl` skips unweighted bones.** `TargetInfo.Update` applies a bone only while `enable & boneWeight`, and `boneWeight` is computed from whether the current clothes actually skin to it — live, 42 of a female's 48 skirt bones are unweighted with no skirt worn. Rotating one stores the value and moves nothing, so the write path refuses it rather than lying.
- The 13 IK targets are appended by `AddObjectAssist.InitIKTarget` in exactly `OICharInfo.IKTargetEN` order, so the index is the name. Only the four end effectors (`LeftHand`, `RightHand`, `LeftFoot`, `RightFoot`) are created with `_usedRot: true`; on the other nine `IKInfo.CopyBone` writes `Vector3.zero` into `changeAmount.rot`, so a rotation sent there is discarded on the next reflect — the write path rejects it.
- **The Excel bone name is the identifier; the caption is not.** `Info.BoneInfo.bone` is the Unity transform name and was unique across all 149/135 live bones, and matched `guideObject.transformTarget.name` every time. `Info.BoneInfo.name` is a Japanese caption shared by whole families (all five right-hand thumb joints read 親指), and `AddObjectAssist.AddBoneGuide` does not even attach it to the guide.

### Attach and detach

- **`TreeNodeCtrl.SetParent` returns `void` and skips silently.** It drops the whole parentage — `OnAttach` included — when the node refuses to change parent, when the target refuses children (`enableAddChild`, false on every accessory *group* node), or when the move would close a loop, and says nothing. The only reliable signal is re-reading `parentInfo` afterwards.
- **A missing bone does not stop an attach.** `ChaInfo.InitializeAccessoryParent` fills `dictAccessoryParent` with all 54 keys and lets the *values* be null, so `GetAccessoryParentTransform` returns a null `Transform` rather than failing. `OCIChar.OnAttach` then runs to the end: `transformTarget.SetParent(null)` reparents the object to the scene root, `changeAmount` is rebased against nothing, and `parentInfo` is still assigned — every read-back reports a successful attach on an orphaned object. Hence the hard gate on `GetAccessoryParentTransform(index) != null` before writing. (On this HS2 build all 54 points resolve on both sexes, male and female alike — the guard is for other rigs and modded bodies, not for `N_Dan` on a female as originally assumed.)
- Attaching to a character's **own** node rather than one of its accessory-point nodes is the same silent half-write from the other side: `OnAttach` looks the node up in `dicAccessoryPoint`, misses, and returns, leaving the object nested in the workspace tree with `parentInfo` still null. `attach_object` therefore requires an attach point whenever the parent is a character.
- Detaching an object that has no parent is safe: `TreeNodeCtrl.SetParent(node, null)` short-circuits on `CheckNode(node)` (the node is already a root) and never reaches `OnDetach`, which would have dereferenced a null `parentInfo`.

### Visibility and rename

- **`ObjectInfo.visible` is only written by `OnSavePreprocessing`.** The live value is `treeNodeObject.visible`; the copy on the object is resynced from it right before every save (which is why the flag persists at all) and is stale in between. So visibility is both read and written on the node, never on the save data.
- **Visibility is not uniform across kinds, and Studio says so only through a set-only property.** `OCILight.OnVisible` is empty and `AddObjectLight` is the one loader that never sets `treeNodeObject.enableVisible`, so a light has no toggle in the workspace and the flag would be stored and inert; `AddObjectRoute.LoadPoint` sets `enableVisible = false` on waypoints, whose visibility is driven by `OCIRoute.SetVisible` instead. `TreeNodeObject.enableVisible` has a setter and no getter, so the guard cannot read the state back and refuses by kind. `SetVisible` itself is unguarded and would happily write either.
- **Hiding is per branch and non-destructive.** `SetVisibleChild` computes each child's displayed state as `parentVisible && child.visible` without touching `child.visible`, so hiding a parent leaves the children's own flags intact and re-showing it restores the branch. Verified live across a save/load round trip.
- **Three kinds own a name; the rest only own a label.** `OIFolderInfo`, `OICameraInfo` and `OIRouteInfo` each `Write`/`ReadString` a `name`, and `OCIFolder.name`/`OCICamera.name`/`OCIRoute.name` write both it and `treeNodeObject.textName`. For every other kind `textName` is written once at load (from the card, or `Info.*LoadInfo.name`) and read by nothing that saves — `OnSavePreprocessing` copies `treeState` and `visible` and no name at all. Renaming anything else would survive exactly until the next load, so `RenameObject` refuses it.
- Renaming does not refresh what caches the name: the camera dropdown (`CameraSelector.Init` rebuilds its options from `OCICamera.name` and restores the selection on the way out), the header text (private `textCamera`, only written by `ChangeCamera` — which is idempotent on the already-active camera, `SetActive(true)` returning early on a live subscription), and the route panel (`RouteControl.Init`, whose nodes take `OIRouteInfo.name`). All three are re-run after a rename, mirroring what `AddCamera`/`AddRoute` do.

### Duplicate

- **`Studio.Duplicate` is driven by the selection, and there is exactly one safe way to set it.** It reads `treeNodeCtrl.selectNodes`, i.e. `hashSelectNode.ToArray()`. `SelectSingle` would move gizmos (`GuideObjectManager.selectObject`), repaint node colours and toggle off a node that was already selected; the `selectNode` setter is worse, since `SetSelectNode` reads `Input.GetKey(P/O/X)` plus Ctrl and may reparent, detach or copy a transform instead of selecting. Swapping the `protected` field for the call and restoring it in a `finally` touches none of that. Everything downstream is synchronous — `AddObjectAssist.LoadChild` has no coroutine branch even for characters — so the borrow lasts one call.
- `Duplicate` round-trips the selection through `sceneInfo.Save(writer, dict)` and `sceneInfo.Import`, which is why a copy carries everything the save format carries, third-party `ExtendedSave` data included, and why its restoration is subject to the same 8-frame `ExecuteDelayed` delay as a scene load. `dicImport` holds only the roots (children ride inside their parent's `ObjectInfo`) and `dicChangeKey` maps each new key back to its source, so the tool can name the copies instead of leaving the caller to diff the object list.
- `enableCopy` is what gates the workspace's own Duplicate button in `WorkspaceCtrl`, and it is false on route waypoints, on a route's `子接続先` child root and on a character's accessory *group* nodes. `Duplicate` itself does not check it, so `DuplicateObject` does.

### Scenes

- **Saving is not atomic in Studio and the destructive step comes first.** `SceneInfo.Save` opens the target with `FileMode.Create` — truncating it — and only *then* calls `CreatePngScreen`; a capture that throws leaves the previous scene destroyed. `SaveScene` therefore writes to a sibling `*.mirastudio-tmp` and `File.Move`s it over the target only once the bytes are known to start with the PNG signature. `SceneInfo.Save` returns `true` unconditionally (there is no `false` path), so inspecting the written file is the *only* failure signal.
- `SceneInfo.cameraSaveData` is `null` until the session's first save or load (`Init()` clears it, the constructor never sets it) and `SceneInfo.Save` dereferences it with no guard — assigning `cameraCtrl.Export()` before saving is mandatory, not cosmetic. `cameraData[10]` is pre-filled by the constructor but only refilled by `Init()` when Studio is instanced, so it is null-checked too.
- A save waits one `WaitForEndOfFrame` first. The thumbnail renders the live cameras, whose transform is only applied in `LateUpdate`, and `OCIChar.OnSavePreprocessing` dereferences `neckLookCtrl` / `charInfo.animBody` with no null guard — so a character added in the same dispatcher drain would NRE. One frame covers both.
- **Nothing reflects `charaLight` when a scene is loaded.** `Studio.OnLoadAfter` reloads the map, both overlays and `systemButtonCtrl.UpdateInfo()`, but never touches `m_CameraLightCtrl`, whose only callers are its own panel's `Start`/`OnEnable`. A scene loaded in vanilla Studio therefore keeps the previous lighting until the light panel is opened. `LoadScene` calls `cameraLightCtrl.Reflect()` itself so the viewport matches the scene it just read.

### Map

- **`SetupMap` resets `mapInfo.ca` and runs even when the map does not change.** `Map.LoadMapCoroutine` short-circuits on `no == Map.no`, but `AddMapCoroutine` calls `SetupMap` regardless, so re-selecting the current map silently recentres it. And `mapInfo.no` is written by `SetupMap` before it can be trusted as a completion signal — the reliable one is `Studio.onChangeMap`, the public `Action` `SetupMap` fires last. An unknown `no` is worse than an error: `LoadMapCoroutine` releases the map and `SetupMap` stores the id anyway, leaving a scene that claims a map it does not have.
- **`SceneInfo.mapLight` is dead data.** `SceneInfo.Init`, `Save` and `Load` are its only readers in the whole assembly — nothing ever pushes `CameraLightCtrl.MapLightInfo` into a `Light`. Only `charaLight` is live, through `CameraLightCtrl`, so the MCP surface exposes one scene light and not two. Same shape of trap as `OICharInfo.kinematicMode`.

### Overlays, lighting and post-processing

- **Background and frame are stored asymmetrically.** `sceneInfo.background` keeps a root-relative path (`DefaultData/bg/x.png`, resolved against `Studio.ApplicationPath`) because backgrounds exist under both roots; `sceneInfo.frame` keeps a bare file name because `FrameCtrl.Load` prefixes `UserData/frame/` itself. Both `Load` methods return `false` *and* clear their `sceneInfo` field when the file is missing, which makes `Load("")` the documented way to clear an overlay and the return value the only failure signal.
- **`SystemButtonCtrl` is the post-processing applier, not `ScreenEffect`.** Its twelve `EffectInfo`s each read `sceneInfo` and write one component, and `Apply()`/`UpdateInfo()` run all twelve — so a partial patch cannot be applied partially: the whole effect state goes back on screen in one pass. That is why every refusable field is checked before a single one is written.
- **Two post-processing fields are object ids, and they fail silently.** `SetDepthOfFieldForcus` and `SetSunCaster` accept any key and quietly store `-1` for anything whose `kind != 1`, so both are validated against `OCIItem` up front. They are not symmetric on restore either: `DOFInfo.Apply` re-resolves `sceneInfo.depthForcus` on every apply, while `SunShaftsInfo.Apply` never touches `sunShafts.sunTransform` — only `SetSunCaster` writes it, so a sun caster has to be re-sent explicitly.
- **`selfShadow` is not a scene setting.** `SelfShadowInfo.Set` does `QualitySettings.SetQualityLevel(level / 2 * 2 + (enable ? 0 : 1))`: it moves the global quality level by one step and outlives the scene that set it.
- The numeric bounds of the screen-effect panel live in its prefab (`InputCombination.Min/Max` read `slider.minValue/maxValue`), not in code, so they are documented and not enforced — unlike the chara light's, which `CameraLightCtrl` clamps in code to `0.1..2` for intensity and `0..359` per rotation axis.

### Outfits and accessories

- **`ChaControl.SetClothesState` cannot fail, so it lies instead.** When the requested state is not in `dictStateType[kind]` it loops `state = (state + 1) % 3` until it finds one that is (or wraps back to the current one) and applies *that*. `SetClothesState` is also the only writer, so nothing downstream ever sees the difference — the write path checks `IsClothesStateType` itself and refuses. `AddClothesStateKindSub` only registers a slot whose garment declares `StateType` 0 (worn/half/off) or 1 (worn/off); with no garment the type defaults to 3 and the slot gets **no entry at all**, at which point `SetClothesState` stores the raw byte with no validation and no effect.
- **The sibling coupling lives in the engine, not in the API.** `SetClothesState`'s tail mirrors `off` between `top` and `bot` while `notBot`, and between `inner_t` and `inner_b` while `notInnerB` (one garment covers both slots), so a single-slot write can move two slots — hence read-backs return all eight. Live: a full-body ninja outfit reports `bot` with no item but a full state list, and setting `bot` back to `worn` pulled `top` with it.
- **`OCIChar.ShowAccessory` has no bounds check** — unlike `ChaControl.SetAccessoryState`, which does — so a slot ≥ 20 throws out of the middle of the write, and on an empty slot the flag is stored and inert: `ChaControl.UpdateVisible` only walks slots whose `objAccessory` is non-null. Both are refused up front. `equipped` is not the same as "wearing something": the catalog's explicit *None* entry is a real object, so a slot can be equipped, named "None", and have no dressing states.
- **Clothes and accessory writes are data-only; the screen catches up in `LateUpdate`.** `Manager.Character` calls `ChaControl.LateUpdateForce` → `UpdateVisible` every frame, so `clothesState` and `showAccessory` are correct to read back immediately but a screenshot taken in the same batch can still show the previous state.
- **`LoadClothesFile` is synchronous and swallows both of its return values.** `Reload` starts `ReloadAsync(asyncFlags: false)`, whose synchronous branches never yield, so the whole reload runs inline — but `OCIChar.LoadClothesFile` returns `void` and drops the `bool` from `ChangeNowCoordinate`, which is `false` on a missing or unreadable file. The write path therefore validates the file with `ChaFileCoordinate.LoadFile` first, the same call `ChangeNowCoordinate` is about to make.
- **A coordinate carries no sex and its name does not survive being worn.** Unlike a pose file there is no identifying code — `ChaFileControl.ConvertCoordinateFilePath` just prefixes `coordinate/{male,female}/` for a bare name (and resolves anything with a directory component against the process CWD, so the adapter always passes an absolute path). And `ChangeNowCoordinate` goes through `LoadBytes`, which restores only `clothes` and `accessory`: `nowCoordinate.coordinateName` keeps whatever it held, so what a character is wearing can only be read back item by item, never as an outfit name.

### Character state panel

- **`siru` is a deferred write with a crash at the end of it.** `SetSiruFlag` only stores into `siruNewLv`; `UpdateSiru`, one frame later inside `Manager.Character`'s update, copies it into `fileStatus.siruLv` and uses it to index a `float[3]` — so a level above 2 takes the process down there rather than erroring in the call that set it. It is also female-only (`UpdateSiru` returns immediately at `sex == 0`, and `OCIChar.GetSiruFlags` is a constant 0 on the base class), as is `SetNipStand`, whose base implementation is empty. Read-back uses `oiCharInfo.siru`, which is written synchronously and is what `AddObjectAssist.UpdateState` pushes back into the character on a scene load.
- `wetRate` and `skinGlossRate` clamp themselves to 0..1 in their setters, so the read-back shows the clamped value; `ChangeNipRate` and `SetSonLength` clamp nothing, and their bounds are the prefab's — documented, not enforced, like the screen-effect panel's.

### Batches

- **A batch must not be dispatched like a normal method.** `RpcServer.Register` wraps its handler in `MainThreadDispatcher.Run`, so a batch registered that way would loop *inside* `Drain()` while each of its steps waits for the main thread to drain a queue only that same blocked thread can drain — a hard freeze on the ordinary path, not just an exotic one. `RegisterBatch` therefore keeps the loop on the HTTP worker thread (`Task.Run`) and lets every step marshal itself as usual, which also makes async steps (`set_camera`, `set_character_kinematics`, `save_scene`) work unchanged.
- A batch cannot roll back and must not pretend to. `UndoRedoManager.Do` pushes a command *before* executing it, so a create whose asset load fails leaves an orphan command on the stack and shifts every index after it; `DeleteObject`, `CreateCharacter` and `AttachObject` clear the stack entirely. What is affordable is refusing the batch before it starts, so every step is checked for a known method, an object shape, nesting and `studio_screenshot` up front.

## Timeline bridge (optional plugin, reflection only)

`Adapters/TimelineAdapter.cs` is the only file that touches Timeline (joan6694, `com.joan6694.illusionplugins.timeline`, tested at 1.4.1). There is no reference assembly, so every member is resolved by reflection in a single lazy all-or-nothing `Bind()`; one missing member sets `_error` and the whole bridge reports itself unavailable. `Plugin.cs` declares a `BepInDependency` soft dependency so BepInEx loads Timeline first when it is installed, and registers the tools either way.

**What Timeline exposes publicly is exactly half of what is needed.**

| Need | Timeline API used | Access |
|---|---|---|
| Transport | `Timeline.playbackTime` / `duration` / `isPlaying`, `Play/Pause/Stop/Seek/NextFrame/PreviousFrame` | public static |
| Track list, keyframes | `Interpolable.keyframes` (`SortedList<float, Keyframe>`), `.enabled`, `.alias`, `.oci`, `.GetValue()` | public |
| Create a track | `new Interpolable(oci, model)` / `(oci, parameter, model)`, `Tree<,>.AddLeaf/RemoveLeaf` | public |
| Create a keyframe | `new Keyframe(value, parent, curve)`, `.value`, `.curve` | public |
| Model catalog, compatibility | `InterpolableModel.owner/id/parameter`, `.name`, `.IsCompatibleWithTarget(oci)` | public |
| Reaching any of it | `_self`, `_interpolables`, `_interpolablesTree`, `_interpolableModelsList`, `_selectedOCI`, `_duration`, `_desiredFrameRate`, `_singleFilesFolder` | private/internal fields |
| Redraw, remove, single files | `UpdateGrid()`, `UpdateInterpolablesView()`, `RemoveInterpolable(i)`, `LoadSingle(path)`, `SaveSingle(path)` | private methods |

`ToolBox.TimelineCompatibility` — embedded in Timeline and in every plugin that integrates with it — is **not** the entry point here: it exists to *publish* new animatable properties (`AddInterpolableModelStatic/Dynamic`), not to drive the timeline.

- **`_selectedOCI` is safe to borrow for one call.** `LoadSingle` and `SaveSingle` take no target and read that field instead. Timeline's `Update` only rewrites it when `_selectedGuideObjects.FirstOrDefault()` differs from the guide object it last saw, so setting it, calling, and restoring it within one main-thread call never collides with the plugin — and, unlike driving `TreeNodeCtrl.SelectSingle`, moves no gizmo and touches no undo stack.
- **Track creation never needs the selection at all.** `Timeline.AddInterpolable(model)` builds its `Interpolable` from `_selectedOCI`, but the two-arg and three-arg `Interpolable` constructors are public, so the bridge builds its own against the target `oci` and inserts it into `_interpolables` (keyed by `Interpolable.GetHashCode()`, which is also the id the API hands out) plus `_interpolablesTree`. For the dynamic `guideObjectPos|Rot|Scale` models, whose `getParameter` returns `GuideObjectManager.Instance.selectObject`, the three-arg constructor takes the guide object straight from `OCIChar.BoneInfo.guideObject`, `OCIChar.IKInfo.guideObject` or `ObjectCtrlInfo.guideObject`.
- **A model id is not unique.** `charClothes` and `juice` register one `InterpolableModel` per slot under the same id, differing only by `parameter` and `name`; other plugins (HS2PE, Flow Control, PregnancyPlus — 95 compatible models on a character on this install against Timeline's own 38) publish under their own `owner`. `AddInterpolable` therefore matches on `(id, owner?, name?)` and reports the candidates when the triple is still ambiguous.
- **Keyframe values are boxed and polymorphic.** `Keyframe.value` is an `object` whose type only its model knows; each model serialises it through its own XML delegates. Rather than reproduce those, the bridge converts by runtime type (`Vector2/3/4`, `Quaternion` as euler degrees, `Color`, the numeric types, `bool`, `string`, `OICharInfo.AnimeInfo`) and falls back to `ToString()`, which also degrades cleanly for third-party models. Writes are typed against the value currently returned by `GetValue()`, so a mismatched shape is refused instead of coerced — and omitting the value captures the live one, which is both the natural workflow and the only route for types with no JSON form.
- **Curve presets are copied from Timeline, tangents included** (`_linePreset`, `_topPreset`, `_bottomPreset`, `_hermitePreset`, `_stairsPreset`), so a curve set through the API is indistinguishable from one picked in Timeline's own keyframe window. `stairs` carries a `float.PositiveInfinity` tangent, which `JsonNumber` writes as `null`; the reader accepts `null` back as infinity.
- **Nothing here goes through `UndoRedoManager`.** Timeline keeps its own history, so timeline edits are not undoable from the Studio UI — and, symmetrically, they leave the scene's undo stack untouched.

## NodesConstraints bridge (optional plugin, reflection only)

`Adapters/NodesConstraintsAdapter.cs` is the only file that touches NodesConstraints (joan6694, `com.joan6694.illusionplugins.nodesconstraints`, tested at 1.3.3). Same shape as the Timeline bridge — no reference assembly, one lazy all-or-nothing `Bind()`, a soft `BepInDependency`, tools registered either way — but a level deeper: **nothing is public**, the nested `Constraint` type included, so the type itself is resolved by reflection before any of its members can be.

| Need | NodesConstraints API used | Access |
|---|---|---|
| Plugin instance, constraint list | `_self`, `_constraints` (`List<Constraint>`) | private static / private |
| Create | `AddConstraint(...)`, the 26-argument overload | private |
| Delete | `RemoveConstraintAt(int)` | private |
| Enable / disable | `SetConstraintEnabled(Constraint, bool)` | private |
| Everything a constraint holds | ~30 fields on `Constraint` | public fields on a **private nested type** |
| Per-axis masks | `TransformLock(bool, bool, bool)` and its `x`/`y`/`z` | public nested type |

Rules that follow from the decompilation:

- **`AddConstraint` refuses duplicates by returning `null`.** It scans `_constraints` for the same pair of transforms *in either direction* and gives back nothing — no exception, no message. The bridge checks the pair itself first so the error can name the existing constraint, and still treats a `null` as a failure.
- **`SetConstraintEnabled` is a transaction, not a setter.** Going `true → false` writes `originalChildPosition`/`Rotation`/`Scale` back into both `childTransform` and the child `GuideObject`'s `changeAmount`, gated on `position`/`rotation`/`scale` and `resetOriginal*`; going `false → true` re-captures `childTransform.localPosition`/`localRotation` as the new originals. Writing `.enabled` directly would skip both and leave the child wherever the constraint had dragged it, so `Set` applies `enabled` last and only through the method. `RemoveConstraintAt` performs the same restore before dropping the entry.
- **There are two appliers and they are not equivalent.** `Update` calls `ApplyNodesConstraints()` only when `!_hasTimeline`; with Timeline installed the work is done by `Timeline_Update_Postfix` → `ApplyConstraints()`. The vanilla path requires `child != null || parent != null` and honours `child.enablePos/enableRot/enableScale`, ignoring `fixDynamicBone`; the Timeline path skips constraints with `fixDynamicBone` and ignores the guide flags. `_hasTimeline` is set 10 frames after `Awake` by `TimelineCompatibility.Init()`.
- **The offsets are three different spaces.** `UpdatePosition` computes `parentTransform.TransformPoint(movement * (factor - 1) + positionOffset)`, so the position offset is parent-local and `Vector3.zero` puts the child on the parent's origin — which is why an omitted offset is captured (`parentTransform.InverseTransformPoint(childTransform.position)`, the plugin's own *Use current* formula) rather than defaulted. `rotationOffset` is post-multiplied onto the parent's world rotation (`Inverse(parent.rotation) * child.rotation` when captured, or `Inverse(lookRotation) * child.rotation` in `lookAt` mode, mirrored quaternion included); `scaleOffset` multiplies `originalParentScale` and is captured as `child.lossyScale / parent.lossyScale`.
- **`TransformLock` is a mask of driven axes, not of frozen ones.** `UpdatePosition` overwrites the computed value with the child's own on every axis whose lock is `false`. The ctor default is `(true, true, true)`.
- **`GetPositionMovement` is `parentTransform.position - originalParentPosition`**, and `originalParent*` is captured inside `AddConstraint`: a constraint tracks the parent's movement *since it was created*, not the parent's absolute transform. The same capture is what disabling and deleting restore against.
- **`UpdateScale` divides by `childTransform.parent.lossyScale` with no null guard**, in a loop that runs every frame outside any API call. A scale link on a child at the scene root is refused up front for that reason.
- **A constraint's identity is its instance.** `Constraint` overrides neither `Equals` nor `GetHashCode`, so the identity hash is stable for its lifetime and is what the API hands out — the same choice the timeline bridge makes for interpolables. Its index in `_constraints` is not usable: `RemoveConstraintAt` shifts everything after it, so a multi-id delete removes in descending index order.
- **Persistence is `ExtendedSave`, not the plugin.** NodesConstraints subscribes to `ExtendedSave.SceneBeingSaved/Loaded/Imported`, and `HS2_ExtensibleSaveFormat` transpiles `SceneInfo.Save`/`Load`/`Import` — the exact methods `SaveScene` and `LoadSceneCoroutine` go through, so the hand-rolled save path carries third-party scene data unchanged. Restoration is deferred by `ExecuteDelayed(..., 8)`, hence the eight-frame gap after a load.
- **`PopulateTimeline` publishes exactly one model, `constraintEnabled`**, whose `getParameter` and `isCompatibleWithTarget` both read the private `_selectedConstraint`. Animating a constraint from the timeline tools would mean borrowing that field the way `LoadSingle`/`SaveSingle` borrow `_selectedOCI`; deliberately not done.
- Nothing here goes through `UndoRedoManager` — no command exists — and nothing here destroys an object or rebases a `changeAmount` owned by Studio, so the undo stack is left usable rather than cleared.

## Reverse-engineering workflow

`ilspycmd` (dotnet global tool) decompiles the exact call sequences before anything is wired up:

```text
dotnet tool install --global ilspycmd --version 8.2.0.7535
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'      # the tool targets .NET 6; only 8/9 are installed
ilspycmd "<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -t Studio.AddObjectItem
ilspycmd "<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -t 'Studio.CameraControl+CameraData'
```

A nested type name is accepted but not honoured: `-t 'Studio.MPCharCtrl+FKInfo'` decompiles the whole of `MPCharCtrl`. Redirect to a file and grep rather than reading the output — the load-bearing classes run to thousands of lines.

[`tools/reverse-engineering/dump-studio-types.ps1`](../../../tools/reverse-engineering/dump-studio-types.ps1) gives the faster name-only metadata dump when signatures are not needed. dnSpy 6.1.5 (GUI + `dnSpy.Console.exe`) ships with the repack under `[MODDING] Tools`.
