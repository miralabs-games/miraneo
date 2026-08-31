# MiraStudio Core API (plugin internals)

Layering inside `plugin/MiraStudio.Plugin/` (see [architecture.md](architecture.md)):

```text
Transport/RpcServer            HTTP JSON-RPC endpoint (worker threads)
Transport/MainThreadDispatcher queue drained in Update() — all Studio access goes through it
Services/StudioService         implements Core/IStudioApi (single-frame operations)
Services/ScreenshotService     WaitForEndOfFrame coroutine → ScreenCapture → PNG/JPG
Services/SceneService          scene loading (waits for Studio's map coroutine)
Services/CameraService         camera writes (reads back after LateUpdate)
Adapters/StudioAdapter         the ONLY file referencing Studio/Unity types
Core/                          DTOs + JSON + Params + IStudioApi (no Unity types)
```

## Core contract (v0)

```csharp
public interface IStudioApi
{
    // Read
    RuntimeInfoDto  GetRuntimeInfo();
    SceneSummaryDto GetScene();
    List<CharacterDto>    ListCharacters();
    List<ObjectDto>       ListObjects();
    CameraDto             GetCamera();
    List<CatalogEntryDto> SearchCatalog(string catalog, string query, int limit);
    List<SceneFileDto>       ListScenes(string query, int limit);
    List<CharacterCardDto>   ListCharacterCards(string sex, string query, int limit);
    List<PoseFileDto>        ListPoses(string sex, string query, int limit);

    // Write — patches carry only the fields the caller supplied, always absolute
    CameraDto SetCamera(CameraPatch patch);
    ObjectDto CreateItem(int group, int category, int no, TransformPatch transform);
    ObjectDto CreateLight(int no, TransformPatch transform, LightPatch light);
    ObjectDto SetLight(int id, LightPatch light);
    ObjectDto SetObjectTransform(int id, TransformPatch transform);
    int       DeleteObject(int id);
    ObjectDto    CreateCharacter(string card, string sex, TransformPatch transform);
    CharacterDto SetCharacterPose(int id, string path);
    CharacterDto SetCharacterAnime(int id, int group, int category, int no, float? normalizedTime);
    CharacterDto SetCharacterExpression(int id, ExpressionPatch patch);
}
```

`studio_load_scene`, `studio_save_scene` and `studio_set_camera` are not on this interface: all three need to span frames and live in `Services/SceneService` and `Services/CameraService` as coroutine-backed `Task<JsonNode>`.

DTOs serialize themselves to the wire format via `ToJson()` (`Core/Json.cs` is a dependency-free JSON reader/writer — the game ships no Newtonsoft).

## Studio access map (what the adapter actually calls)

| Need | Studio API used |
|---|---|
| Ready check | `Singleton<Studio.Studio>.IsInstance()` (`Singleton<T>` lives in IL.dll) |
| All scene objects | `Studio.Studio.dicObjectCtrl` (`Dictionary<int, ObjectCtrlInfo>`; key = stable id) |
| Object kind | C# type tests: `OCIChar`/`OCIItem`/`OCILight`/`OCIFolder`/`OCIRoute`/`OCICamera` |
| Display name / visibility | `ObjectCtrlInfo.treeNodeObject.textName` / `.visible` |
| Transform | `ObjectCtrlInfo.objectInfo.changeAmount.pos/rot/scale` |
| Attachment parent | `ObjectCtrlInfo.parentInfo.objectInfo.dicKey` |
| Character card name / sex | `OCIChar.oiCharInfo.charFile.parameter.fullname`, `OCIChar.sex` |
| Scene summary | `Studio.Studio.sceneInfo` (`background`, `frame`) |
| Camera | `Studio.Studio.cameraCtrl` (`targetPos`, `cameraAngle`, `fieldOfView`) + `Camera.main` |
| Screenshot | `ScreenCapture.CaptureScreenshotAsTexture()` after `WaitForEndOfFrame` |

## Threading rules

- Studio/Unity APIs are main-thread only. `RpcServer` receives requests on worker threads and executes every handler through `MainThreadDispatcher.Run()` (queue drained in `Update()`), with per-method timeouts.
- Violations do NOT throw — they crash the process natively, misleadingly attributed to the GPU driver (`nvwgf2umx.dll`, 0xc0000409/0xC0000005). Confirmed twice during v0 bring-up when `/health` called `Singleton<Studio.Studio>.Instance` off-thread (the getter can create GameObjects via `CheckInstance`). Off-thread code may only read fields cached by the main thread; `Singleton<T>.IsInstance()` is the only acceptable static probe.
- `studio_screenshot` is asynchronous: the main thread only starts a coroutine; the worker awaits its `TaskCompletionSource`.
- `Application.runInBackground` is forced on (configurable) so the agent can drive Studio while the window is unfocused.

## Write path (implemented)

| Operation | Studio API used |
|---|---|
| Spawn item | `Studio.AddObjectItem.Add(group, category, no)` → `OCIItem`. Prefer it over `Studio.Studio.AddItem`, which returns void and wipes the undo stack. Internally does `UndoRedoManager.Do(AddObjectCommand.AddItemCommand(...))`, so the add is undoable. |
| Spawn light | `Studio.AddObjectLight.Add(no)` → `OCILight` (same undoable pattern via `AddLightCommand`) |
| Validate ids | `Singleton<Studio.Info>.Instance.dicItemLoadInfo[group][category][no]`, `dicLightLoadInfo[no]`; names for search from `dicItemGroupCategory` (`GroupInfo.name`, `CategoryInfo.name`) |
| Set transform | `UndoRedoManager.Do(GuideCommand.MoveEqualsCommand / RotationEqualsCommand / ScaleEqualsCommand)` with `EqualsInfo{dicKey, oldValue, newValue}`. Those commands mutate `Studio.GetChangeAmount(dicKey)`, so the adapter first checks it is the same instance as `objectInfo.changeAmount` and otherwise falls back to a direct (non-undoable) assignment. |
| Light settings | `OCILight.SetColor/SetIntensity/SetRange/SetSpotAngle/SetEnable/SetShadow` (pass `_force: true` — they no-op when the value is unchanged) |
| Camera | `CameraControl.targetPos / cameraAngle / fieldOfView` properties; distance via `Export()` → `data.distance = (0,0,-d)` → `Import(data)` (`cameraData` itself is `protected`) |
| Load scene | `Studio.Studio.LoadSceneCoroutine(path)` — **not** `LoadScene`, which returns before the map coroutine finishes |
| Save scene | The three steps of `Studio.Studio.SaveScene()`: `OnSavePreprocessing()` over `dicObjectCtrl`, then `sceneInfo.cameraSaveData = cameraCtrl.Export()`, then `SceneInfo.Save(path)`. `SaveScene()` itself takes no path and stamps its own name, so it cannot be reused as-is. The thumbnail is produced inside `SceneInfo.Save` by `gameScreenShot.CreatePngScreen(320, 180)`, which re-renders `GameScreenShot.renderCam[]` into a temporary `RenderTexture` — no backbuffer grab, so no `WaitForEndOfFrame` is technically required, but the adapter waits for one anyway (see below) |
| Delete | `treeNodeCtrl.DeleteNode(treeNodeObject)` (respects `enableDelete`, recurses into children, `Studio.OnDeleteNode` → `OCI.OnDelete` → `DeleteInfo` removes it from `dicObjectCtrl`) |
| Scene folder | `UserData.Path` (root namespace, IL.dll) + `Studio\scene` |
| Spawn character | `Studio.AddObjectFemale.Add(cardPath)` / `AddObjectMale.Add(cardPath)` → `OCICharFemale`/`OCICharMale`. Synchronous (`ChaControl.Load`), does the whole setup: `GetNewIndex`, `AddInfo`, `AddNode`, bones/IK/look-at, initial `LoadAnime`, expressions. |
| List cards | `Directory.GetFiles(UserData.Path + "chara/{female,male}", "*.png")` + `AIChara.ChaFileControl.LoadCharaFile(path, sex, noLoadPng: true)` → `parameter.fullname`, exactly as `Studio.CharaList.InitFemaleList`. **Not** `LoadFileLimited`: it always returns `false` (missing `return true`) and mutates `Character.isMod`. |
| Apply pose file | `Studio.PauseCtrl.Load(ociChar, path)`; list with `Directory.GetFiles(UserData.Create(PauseCtrl.savePath), "*.dat")`, name via `PauseCtrl.LoadName`, sex filter via `PauseCtrl.CheckIdentifyingCode(path, sex)` |
| Set animation | `OCIChar.LoadAnime(group, category, no, normalizedTime)`; catalog `Info.dicAnimeLoadInfo` (g→c→n→`AnimeLoadInfo.name`), names in `Info.dicAGroupCategory` |
| Anime playback | `UndoRedoManager.Do(AnimeCommand.SpeedCommand / PatternCommand / OptionParamCommand)` over `int[] dicKey` + old-value arrays, exactly as `Studio.AnimeControl` does. Ranges are that panel's own clamps: speed 0..3, pattern and both option params 0..1 |
| Hand shapes | `OCIChar.ChangeHandAnime(lr, ptn)` (`lr` 0 = left, 1 = right), bound `OCIChar.HandAnimeNum`, read back from `oiCharInfo.handPtn[lr]` |
| Attach / detach | `treeNodeCtrl.SetParent(childNode, parentNodeOrNull)`. The tree change is what triggers the real work: `TreeNodeCtrl.SetParent` fires `onParentage` → `Studio.OnParentage` → `FindLoop(parent).OnAttach(parent, dicInfo[child])`, or `child.OnDetach()` when the parent is null |
| Attach points | `OCIChar.dicAccessoryPoint` (`Dictionary<TreeNodeObject,int>`, built by `AddObjectAssist.InitAccessoryPoint`), bone lookup `ChaInfo.GetAccessoryParentTransform(index)`, names from the `ChaAccessoryDefine.AccessoryParentKey` enum, current occupants in `oiCharInfo.child[index]` |
| Face | `OCIChar.ChangeEyesOpen/ChangeBlink/ChangeMouthPtn/ChangeMouthOpen/ChangeLipSync/SetTears/SetHohoAkaRate/ChangeLookEyesPtn/ChangeLookNeckPtn/EnableExpressionCategory`; eye and eyebrow patterns go through `ChaControl.ChangeEyesPtn/ChangeEyebrowPtn` |
| Eye/eyebrow pattern list | `Singleton<Manager.Character>.Instance.chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.custom_eye{,brow}_{f,m}).Keys` |
| Character state read-back | `OCIChar.oiCharInfo` (`animeInfo`, `handPtn`, `enableIK/FK`, `activeIK/FK`, `expression`, `mouthOpen`, `lipSync`), `OCIChar.charFileStatus` (`eyesOpenMax`, `eyesBlink`, `eyesLookPtn`, `neckLookPtn`), `OCIChar.animeSpeed/animePattern/animeOptionParam1|2/isAnimeMotion/isHAnime`, `charInfo.GetEyesPtn/GetEyebrowPtn/GetMouthPtn`, `charAnimeCtrl.normalizedTime`. **Not** `oiCharInfo.kinematicMode` — see below |

Rules that follow from the above:

- Studio NEO V2 has **no delete command**: deletion is not undoable, and leaving the undo stack alive after one would replay commands against destroyed objects — so `DeleteObject` clears the stack.
- `AddObjectCommand` only defines `AddItemCommand` and `AddLightCommand` — there is **no add-character command**. `Studio.AddFemale`/`AddMale` call `UndoRedoManager.Clear()` right after adding, and `CreateCharacter` mirrors that.
- Eye and eyebrow patterns are asset ids, not contiguous indices (mods add ids far outside 0..N). `MPCharCtrl.EtcInfo` shows the *position* in the per-sex category list and maps it through the key array; the MCP surface uses that same index space so it agrees with what the user sees in Studio.
- `charAnimeCtrl.normalizedTime` is Unity's Animator normalized time: it keeps growing while a clip loops and is only sampled a frame behind a `LoadAnime`, so it is a live reading, not a round-trip value.
- `OICharInfo.kinematicMode` is **dead data**: `OICharInfo.Load` is its only writer in the whole assembly, and `ActiveKinematicMode` writes `enableIK`/`enableFK` instead. It keeps reporting whatever the scene file held, so `kinematic.mode` is derived from `enableIK`/`enableFK` (which are mutually exclusive by construction) rather than read from it.
- A *pose file* is not an *animation*: `PauseCtrl.FileInfo.Apply` calls `LoadAnime` **and** restores every IK/FK target plus the expression flags, and is sex-locked; `LoadAnime` alone is not. The anime catalog itself is not sex-filtered — `MPCharCtrl.LoadAnime(sex, ...)` ignores its `sex` argument.
- **Hand shapes are orthogonal to everything else.** `PauseCtrl.FileInfo` has no hand field at all and `LoadAnime` never touches `handPtn`, so neither a pose nor a clip ever restores the hands — verified live: a character keeps `handPtn` across both. Only `ChangeHandAnime` and a scene load (`OICharInfo.Load`) write it.
- `ChangeHandAnime` writes `oiCharInfo.handPtn[lr]` **before** it can fail, and both its failure modes are invisible to the caller. `ptn` maps to shape key `ptn - 1` (0 means "no shape", `SetEnableShapeHand(lr, false)`), and `AnimationKeyInfo.GetInfo` only guards `Count <= key`: a `ptn` above the range returns `false` up a chain that `ChaControl.SetShapeHandValue` discards (it returns `true` unconditionally), while a negative one throws `ArgumentOutOfRangeException` out of the middle of the write. Hence the valid space is `0..HandAnimeNum` inclusive — same as `MPCharCtrl.HandInfo`, which sets `PatternInfo.num = HandAnimeNum + 1` — and both hands are validated before either is written, so a rejected value cannot leave the other hand applied.
- `OCIChar.HandAnimeNum` dereferences `ChaControl.sibHand` with no guard, so it throws until the shape bundle is initialised; the read path reports `count: -1` there and the write path refuses.
- Studio has no readable names for hand shapes — `MPCharCtrl.PatternInfo` renders a bare `$"{m_Ptn:0}"` — so the MCP surface exposes the index and nothing else, and agents identify shapes by screenshot.
- **`TreeNodeCtrl.SetParent` returns `void` and skips silently.** It drops the whole parentage — `OnAttach` included — when the node refuses to change parent, when the target refuses children (`enableAddChild`, false on every accessory *group* node), or when the move would close a loop, and says nothing. The only reliable signal is re-reading `parentInfo` afterwards.
- **A missing bone does not stop an attach.** `ChaInfo.InitializeAccessoryParent` fills `dictAccessoryParent` with all 54 keys and lets the *values* be null, so `GetAccessoryParentTransform` returns a null `Transform` rather than failing. `OCIChar.OnAttach` then runs to the end: `transformTarget.SetParent(null)` reparents the object to the scene root, `changeAmount` is rebased against nothing, and `parentInfo` is still assigned — every read-back reports a successful attach on an orphaned object. Hence the hard gate on `GetAccessoryParentTransform(index) != null` before writing. (On this HS2 build all 54 points resolve on both sexes, male and female alike — the guard is for other rigs and modded bodies, not for `N_Dan` on a female as originally assumed.)
- Attaching to a character's **own** node rather than one of its accessory-point nodes is the same silent half-write from the other side: `OnAttach` looks the node up in `dicAccessoryPoint`, misses, and returns, leaving the object nested in the workspace tree with `parentInfo` still null. `attach_object` therefore requires an attach point whenever the parent is a character.
- `OnAttach`/`OnDetach` rewrite `objectInfo.changeAmount.pos/rot` between world and parent space. That silently changes the meaning of every `GuideCommand` already on the undo stack, and Studio has no parentage command to undo the attach itself — so `AttachObject` clears the stack, like `DeleteObject`.
- Detaching an object that has no parent is safe: `TreeNodeCtrl.SetParent(node, null)` short-circuits on `CheckNode(node)` (the node is already a root) and never reaches `OnDetach`, which would have dereferenced a null `parentInfo`.
- Anime playback settings are the **only** undoable character writes: `AnimeCommand` defines `SpeedCommand`/`PatternCommand`/`OptionParamCommand`, `ChangeHandAnime` has no command (neither do Studio's own hand buttons), and neither does `LoadAnime`. `animePattern` only reaches the animator when `isAnimeMotion`, `animeOptionParam1|2` only when `isHAnime` — otherwise the setters store the value and stop, so both flags are exposed on read.
- **Saving is not atomic in Studio and the destructive step comes first.** `SceneInfo.Save` opens the target with `FileMode.Create` — truncating it — and only *then* calls `CreatePngScreen`; a capture that throws leaves the previous scene destroyed. `SaveScene` therefore writes to a sibling `*.mirastudio-tmp` and `File.Move`s it over the target only once the bytes are known to start with the PNG signature. `SceneInfo.Save` returns `true` unconditionally (there is no `false` path), so inspecting the written file is the *only* failure signal.
- `SceneInfo.cameraSaveData` is `null` until the session's first save or load (`Init()` clears it, the constructor never sets it) and `SceneInfo.Save` dereferences it with no guard — assigning `cameraCtrl.Export()` before saving is mandatory, not cosmetic. `cameraData[10]` is pre-filled by the constructor but only refilled by `Init()` when Studio is instanced, so it is null-checked too.
- A save waits one `WaitForEndOfFrame` first. The thumbnail renders the live cameras, whose transform is only applied in `LateUpdate`, and `OCIChar.OnSavePreprocessing` dereferences `neckLookCtrl` / `charInfo.animBody` with no null guard — so a character added in the same dispatcher drain would NRE. One frame covers both.
- Never hand out Unity object references — ids only (`dicObjectCtrl` keys).
- The camera transform is recomputed in `LateUpdate` (`InternalUpdateCameraState`), so anything reading `Camera.main` back after a camera write must wait a frame.
- Scene scale varies per scene; never assume 1 unit = 1 m when choosing distances or light ranges.

## Reverse-engineering workflow

`ilspycmd` (dotnet global tool) decompiles the exact call sequences before anything is wired up:

```text
dotnet tool install --global ilspycmd --version 8.2.0.7535
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'      # the tool targets .NET 6; only 8/9 are installed
ilspycmd "<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -t Studio.AddObjectItem
ilspycmd "<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -t 'Studio.CameraControl+CameraData'
```

`tools/reverse-engineering/dump-studio-types.ps1` gives the faster name-only metadata dump when signatures are not needed. dnSpy 6.1.5 (GUI + `dnSpy.Console.exe`) ships with the repack under `[MODDING] Tools`.
