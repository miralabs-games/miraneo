using System;
using System.Collections.Generic;
using System.IO;
using MiraStudio.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using SNeo = global::Studio;

namespace MiraStudio.Adapters
{
    // The only layer allowed to touch Unity / Studio NEO types. Everything it
    // returns is a MiraStudio.Core DTO. All members must be called on the Unity
    // main thread (except IsStudioReady, which only reads a static reference).
    internal static class StudioAdapter
    {
        // Safe from any thread: IsInstance only reads the static field and never
        // creates the singleton (unlike the Instance getter, which may call
        // CheckInstance and touch native Unity off the main thread).
        public static bool IsStudioReady()
        {
            try
            {
                return Singleton<SNeo.Studio>.IsInstance();
            }
            catch
            {
                return false;
            }
        }

        private static SNeo.Studio Require()
        {
            if (!IsStudioReady())
                throw new StudioApiException(StudioApiException.StudioNotReady, "Studio NEO V2 scene is not loaded yet");
            return Singleton<SNeo.Studio>.Instance;
        }

        public static RuntimeInfoDto GetRuntimeInfo(string pluginVersion)
        {
            var dto = new RuntimeInfoDto
            {
                ProductName = Application.productName,
                UnityVersion = Application.unityVersion,
                GameVersion = ReadGameVersionFile(),
                PluginVersion = pluginVersion,
                BepInExVersion = typeof(BepInEx.BaseUnityPlugin).Assembly.GetName().Version.ToString(),
                ActiveScene = SceneManager.GetActiveScene().name,
                StudioLoaded = IsStudioReady(),
            };
            if (dto.StudioLoaded)
            {
                try { dto.SceneObjectCount = Singleton<SNeo.Studio>.Instance.dicObjectCtrl.Count; }
                catch { dto.SceneObjectCount = 0; }
            }
            return dto;
        }

        public static SceneSummaryDto GetScene() => GetScene(true);

        // withPostProcessing is off for the summary embedded in the load and save
        // results: those describe what happened to the workspace, not its grading.
        public static SceneSummaryDto GetScene(bool withPostProcessing)
        {
            var studio = Require();
            var dto = new SceneSummaryDto();
            foreach (var kv in studio.dicObjectCtrl)
            {
                dto.Total++;
                if (kv.Value is SNeo.OCIChar) dto.Characters++;
                else if (kv.Value is SNeo.OCIItem) dto.Items++;
                else if (kv.Value is SNeo.OCILight) dto.Lights++;
                else if (kv.Value is SNeo.OCIFolder) dto.Folders++;
                else if (kv.Value is SNeo.OCIRoute) dto.Routes++;
                else if (kv.Value is SNeo.OCICamera) dto.Cameras++;
            }
            try
            {
                if (studio.sceneInfo != null)
                {
                    dto.Background = Convert.ToString(studio.sceneInfo.background);
                    dto.Frame = Convert.ToString(studio.sceneInfo.frame);
                }
            }
            catch { }
            dto.Map = GetMap();
            dto.Lighting = GetSceneLighting();
            if (withPostProcessing) dto.PostProcessing = GetPostProcessing();
            return dto;
        }

        public static List<CharacterDto> ListCharacters()
        {
            var studio = Require();
            var list = new List<CharacterDto>();
            foreach (var kv in studio.dicObjectCtrl)
            {
                var chr = kv.Value as SNeo.OCIChar;
                if (chr == null) continue;
                list.Add(ToCharacterDto(kv.Key, chr));
            }
            return list;
        }

        public static List<ObjectDto> ListObjects()
        {
            var studio = Require();
            var list = new List<ObjectDto>();
            foreach (var kv in studio.dicObjectCtrl)
                list.Add(ToDto(kv.Key, kv.Value));
            return list;
        }

        public static CameraDto GetCamera()
        {
            var studio = Require();
            var dto = new CameraDto();
            var cc = studio.cameraCtrl;
            if (cc != null)
            {
                dto.TargetPos = V(cc.targetPos);
                dto.Angle = V(cc.cameraAngle);
                dto.Fov = cc.fieldOfView;
                dto.Distance = -cc.Export().distance.z;
            }
            var cam = Camera.main;
            if (cam != null)
            {
                dto.WorldPos = V(cam.transform.position);
                dto.WorldRot = V(cam.transform.eulerAngles);
            }
            return dto;
        }

        public static List<CatalogEntryDto> SearchCatalog(string catalog, string query, int limit)
        {
            Require();
            var info = Singleton<SNeo.Info>.Instance;
            if (info == null)
                throw new StudioApiException(StudioApiException.StudioNotReady, "Studio asset catalogs are not loaded yet");

            var list = new List<CatalogEntryDto>();
            if (catalog == "item")
            {
                foreach (var group in info.dicItemLoadInfo)
                {
                    var groupName = GroupName(info, group.Key);
                    foreach (var category in group.Value)
                    {
                        var categoryName = CategoryName(info, group.Key, category.Key);
                        foreach (var item in category.Value)
                        {
                            var name = item.Value != null ? item.Value.name : null;
                            if (!Matches(query, name, groupName, categoryName)) continue;
                            list.Add(new CatalogEntryDto
                            {
                                Catalog = "item",
                                Group = group.Key,
                                Category = category.Key,
                                No = item.Key,
                                Name = name,
                                GroupName = groupName,
                                CategoryName = categoryName,
                            });
                            if (list.Count >= limit) return list;
                        }
                    }
                }
            }
            else if (catalog == "anime")
            {
                foreach (var group in info.dicAnimeLoadInfo)
                {
                    var groupName = AnimeGroupName(info, group.Key);
                    foreach (var category in group.Value)
                    {
                        var categoryName = AnimeCategoryName(info, group.Key, category.Key);
                        foreach (var anime in category.Value)
                        {
                            var name = anime.Value != null ? anime.Value.name : null;
                            if (!Matches(query, name, groupName, categoryName)) continue;
                            list.Add(new CatalogEntryDto
                            {
                                Catalog = "anime",
                                Group = group.Key,
                                Category = category.Key,
                                No = anime.Key,
                                Name = name,
                                GroupName = groupName,
                                CategoryName = categoryName,
                            });
                            if (list.Count >= limit) return list;
                        }
                    }
                }
            }
            else if (catalog == "light")
            {
                foreach (var light in info.dicLightLoadInfo)
                {
                    var name = light.Value != null ? light.Value.name : null;
                    if (!Matches(query, name, null, null)) continue;
                    list.Add(new CatalogEntryDto
                    {
                        Catalog = "light",
                        No = light.Key,
                        Name = name,
                        Target = light.Value != null ? light.Value.target.ToString() : null,
                    });
                    if (list.Count >= limit) return list;
                }
            }
            else if (catalog == "map")
            {
                foreach (var map in info.dicMapLoadInfo)
                {
                    var name = map.Value != null ? map.Value.name : null;
                    if (!Matches(query, name, null, null)) continue;
                    list.Add(new CatalogEntryDto { Catalog = "map", No = map.Key, Name = name });
                    if (list.Count >= limit) return list;
                }
            }
            // The three catalogs the screen-effect panel picks from. They are plain
            // no → name lists like the lights, with no group or category.
            else if (catalog == "color_grading")
            {
                foreach (var lut in info.dicColorGradingLoadInfo)
                {
                    var name = lut.Value != null ? lut.Value.name : null;
                    if (!Matches(query, name, null, null)) continue;
                    list.Add(new CatalogEntryDto { Catalog = "color_grading", No = lut.Key, Name = name });
                    if (list.Count >= limit) return list;
                }
            }
            else if (catalog == "reflection_probe")
            {
                foreach (var probe in info.dicReflectionProbeLoadInfo)
                {
                    var name = probe.Value != null ? probe.Value.name : null;
                    if (!Matches(query, name, null, null)) continue;
                    list.Add(new CatalogEntryDto { Catalog = "reflection_probe", No = probe.Key, Name = name });
                    if (list.Count >= limit) return list;
                }
            }
            else if (catalog == "sky")
            {
                foreach (var sky in info.dicSkyPatternInfo)
                {
                    var name = sky.Value != null ? sky.Value.name : null;
                    if (!Matches(query, name, null, null)) continue;
                    list.Add(new CatalogEntryDto { Catalog = "sky", No = sky.Key, Name = name });
                    if (list.Count >= limit) return list;
                }
            }
            else
            {
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "unknown catalog: " + catalog + " (expected 'item', 'light', 'anime', 'map', 'color_grading', 'reflection_probe' or 'sky')");
            }
            return list;
        }

        public static List<SceneFileDto> ListScenes(string query, int limit)
        {
            var root = SceneRoot();
            var list = new List<SceneFileDto>();
            if (!Directory.Exists(root)) return list;
            foreach (var file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                var relative = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                if (!Matches(query, relative, null, null)) continue;
                var slash = relative.LastIndexOf('/');
                list.Add(new SceneFileDto
                {
                    Path = relative,
                    Folder = slash < 0 ? "" : relative.Substring(0, slash),
                    Size = new FileInfo(file).Length,
                });
                if (list.Count >= limit) break;
            }
            return list;
        }

        public static string ResolveScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'path' must be a non-empty string");
            var candidate = Path.IsPathRooted(path) ? path : Path.Combine(SceneRoot(), path.Replace('/', '\\'));
            if (!File.Exists(candidate) && !candidate.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                candidate += ".png";
            if (!File.Exists(candidate))
                throw new StudioApiException(StudioApiException.NotFound, "scene file not found: " + candidate + " (use studio_list_scenes to see available scenes)");
            return candidate;
        }

        // Unlike ResolveScenePath this one feeds a write, so the result is confined
        // to the scene folder: SceneInfo.Save opens with FileMode.Create and would
        // truncate whatever it is pointed at.
        public static string ResolveSaveScenePath(string name, bool overwrite, out bool overwriting)
        {
            if (string.IsNullOrEmpty(name)) name = DefaultSceneName();
            if (Path.IsPathRooted(name) || name.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'name' must be a relative file name under UserData/Studio/scene");
            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) name += ".png";

            var full = Path.GetFullPath(Path.Combine(SceneRoot(), name.Replace('/', '\\')));
            if (!full.StartsWith(SceneRootPrefix(), StringComparison.OrdinalIgnoreCase))
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'name' must stay inside UserData/Studio/scene");

            overwriting = File.Exists(full);
            if (overwriting && !overwrite)
                throw new StudioApiException(StudioApiException.OperationFailed, "scene already exists: " + SceneRelative(full) + " (pass overwrite=true to replace it)");

            var folder = Path.GetDirectoryName(full);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return full;
        }

        // Mirrors Studio.SaveScene, except for where the bytes land: Studio writes
        // straight into the target with FileMode.Create and only then asks
        // GameScreenShot for the thumbnail, so a failed capture leaves the previous
        // scene truncated. Staging into a sibling file and moving it over the target
        // makes a failed save a no-op instead.
        public static SceneSaveDto SaveScene(string target, bool overwriting)
        {
            var studio = Require();
            if (SingletonInitializer<Manager.Scene>.initialized && Manager.Scene.IsNowLoadingFade)
                throw new StudioApiException(StudioApiException.OperationFailed, "a scene transition is in progress; retry once the fade finishes");

            foreach (var pair in studio.dicObjectCtrl)
                pair.Value.OnSavePreprocessing();

            var info = studio.sceneInfo;
            // SceneInfo.Save dereferences both without a null check, and Init()
            // leaves cameraSaveData null until the session's first save or load.
            info.cameraSaveData = studio.cameraCtrl.Export();
            if (info.cameraData == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "the scene's camera slots are not initialised yet");

            var staging = target + ".mirastudio-tmp";
            try
            {
                info.Save(staging);
                var written = new FileInfo(staging);
                if (!written.Exists || written.Length == 0)
                    throw new StudioApiException(StudioApiException.OperationFailed, "scene save produced no data");
                // SceneInfo.Save always returns true, so the file itself is the only
                // failure signal: the thumbnail is its first bytes and the loader
                // (PngFile.GetPngSize) skips a well-formed PNG prefix to find the rest.
                if (!HasPngHeader(staging))
                    throw new StudioApiException(StudioApiException.OperationFailed, "thumbnail capture failed: the saved file has no PNG header and would not load back");
                if (File.Exists(target)) File.Delete(target);
                File.Move(staging, target);
            }
            catch
            {
                try { if (File.Exists(staging)) File.Delete(staging); }
                catch { }
                throw;
            }

            return new SceneSaveDto
            {
                Path = target,
                RelativePath = SceneRelative(target),
                Bytes = new FileInfo(target).Length,
                Objects = info.dicObject.Count,
                Overwritten = overwriting,
                ThumbnailWidth = ThumbnailWidth,
                ThumbnailHeight = ThumbnailHeight,
                Scene = GetScene(false),
            };
        }

        // Studio itself builds this list by fully loading every card at startup
        // (CharaList.InitFemaleList). LoadFileLimited reads the parameter block
        // only, and the result is cached per file so the cost is paid once.
        public static List<CharacterCardDto> ListCharacterCards(string sex, string query, int limit)
        {
            var list = new List<CharacterCardDto>();
            foreach (var folder in new[] { "female", "male" })
            {
                if (sex != null && sex != folder) continue;
                var root = Path.Combine(CharaRoot(), folder);
                if (!Directory.Exists(root)) continue;
                foreach (var file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    var card = ReadCard(file, folder);
                    if (card == null) continue;
                    if (!Matches(query, card.Name, card.Path)) continue;
                    list.Add(card);
                    if (list.Count >= limit) return list;
                }
            }
            return list;
        }

        public static List<PoseFileDto> ListPoses(string sex, string query, int limit)
        {
            var root = PoseRoot();
            var list = new List<PoseFileDto>();
            if (!Directory.Exists(root)) return list;
            foreach (var file in Directory.GetFiles(root, "*" + SNeo.PauseCtrl.saveExtension, SearchOption.AllDirectories))
            {
                var pose = ReadPose(file, root);
                if (pose == null) continue;
                if (sex != null && pose.Sex != sex) continue;
                if (!Matches(query, pose.Name, pose.Path)) continue;
                list.Add(pose);
                if (list.Count >= limit) break;
            }
            return list;
        }

        public static ObjectDto CreateCharacter(string card, string sex, TransformPatch transform)
        {
            Require();
            int cardSex;
            var path = ResolveCardPath(card, sex, out cardSex);

            SNeo.OCIChar oci = cardSex == 0 ? (SNeo.OCIChar)SNeo.AddObjectMale.Add(path) : SNeo.AddObjectFemale.Add(path);
            if (oci == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "Studio failed to create the character from " + path);

            // AddObjectCommand has no add-character command, so this is not undoable.
            // Studio.AddFemale/AddMale clear the stack for the same reason: replaying
            // it would apply commands against a scene that no longer matches.
            Singleton<SNeo.UndoRedoManager>.Instance.Clear();
            ApplyTransform(oci, transform);
            return ToDto(oci.objectInfo.dicKey, oci);
        }

        public static CharacterDto SetCharacterPose(int id, string path)
        {
            var chr = RequireCharacter(id);
            var full = ResolvePosePath(path);
            var sex = chr.oiCharInfo.sex;
            if (!SNeo.PauseCtrl.CheckIdentifyingCode(full, sex))
            {
                var other = sex == 0 ? 1 : 0;
                throw new StudioApiException(StudioApiException.InvalidParams,
                    SNeo.PauseCtrl.CheckIdentifyingCode(full, other)
                        ? "pose file is for " + SexName(other) + " characters, but object " + id + " is " + SexName(sex)
                        : "not a Studio NEO V2 pose file: " + full);
            }
            if (!SNeo.PauseCtrl.Load(chr, full))
                throw new StudioApiException(StudioApiException.OperationFailed, "Studio failed to apply the pose file " + full);
            return ToCharacterDto(id, chr);
        }

        public static CharacterDto SetCharacterAnime(int id, AnimePatch patch)
        {
            var chr = RequireCharacter(id);
            if (patch == null || patch.IsEmpty) return ToCharacterDto(id, chr);

            if (patch.HasClip && !patch.HasFullClip)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'group', 'category' and 'no' must be given together (they identify one catalog entry)");
            if (patch.NormalizedTime.HasValue && !patch.HasClip)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'normalized_time' is a starting offset for the clip being loaded, so it needs 'group', 'category' and 'no'");

            // Same clamps as Studio's own anime panel (AnimeControl); values outside
            // them would save into a scene the vanilla UI cannot reproduce.
            RequireRange(patch.Speed, 0f, 3f, "speed");
            RequireRange(patch.Pattern, 0f, 1f, "pattern");
            RequireRange(patch.OptionParam1, 0f, 1f, "option_param1");
            RequireRange(patch.OptionParam2, 0f, 1f, "option_param2");

            if (patch.HasFullClip)
            {
                int group = patch.Group.Value, category = patch.Category.Value, no = patch.No.Value;
                var info = Singleton<SNeo.Info>.Instance;
                Dictionary<int, Dictionary<int, SNeo.Info.AnimeLoadInfo>> categories;
                Dictionary<int, SNeo.Info.AnimeLoadInfo> animes;
                if (info == null || !info.dicAnimeLoadInfo.TryGetValue(group, out categories)
                    || !categories.TryGetValue(category, out animes) || !animes.ContainsKey(no))
                    throw new StudioApiException(StudioApiException.NotFound,
                        "no animation with group=" + group + " category=" + category + " no=" + no + " (use studio_search_catalog with catalog='anime')");

                chr.LoadAnime(group, category, no, patch.NormalizedTime.HasValue ? patch.NormalizedTime.Value : 0f);
            }

            // Playback settings come after the load: LoadAnime re-pushes them into the
            // fresh animator, and isAnimeMotion/isHAnime only describe the loaded clip.
            var key = chr.objectInfo.dicKey;
            var undo = Singleton<SNeo.UndoRedoManager>.Instance;
            if (patch.Speed.HasValue)
                undo.Do(new SNeo.AnimeCommand.SpeedCommand(new[] { key }, patch.Speed.Value, new[] { chr.animeSpeed }));
            if (patch.Pattern.HasValue)
                undo.Do(new SNeo.AnimeCommand.PatternCommand(new[] { key }, patch.Pattern.Value, new[] { chr.animePattern }));
            if (patch.OptionParam1.HasValue)
                undo.Do(new SNeo.AnimeCommand.OptionParamCommand(new[] { key }, patch.OptionParam1.Value, new[] { chr.animeOptionParam1 }, 0));
            if (patch.OptionParam2.HasValue)
                undo.Do(new SNeo.AnimeCommand.OptionParamCommand(new[] { key }, patch.OptionParam2.Value, new[] { chr.animeOptionParam2 }, 1));
            return ToCharacterDto(id, chr);
        }

        public static CharacterDto SetCharacterHands(int id, HandsPatch patch)
        {
            var chr = RequireCharacter(id);
            if (patch == null || patch.IsEmpty) return ToCharacterDto(id, chr);

            var count = HandAnimeCount(chr);
            if (count < 0)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "the hand shape list is not loaded for character " + id);

            // ChangeHandAnime writes handPtn before it can fail, and its failures are
            // silent above the range (AnimationKeyInfo.GetInfo returns false) or a raw
            // IndexOutOfRange below it, so both hands are checked before either write.
            RequireHandPtn(patch.Left, count, "left");
            RequireHandPtn(patch.Right, count, "right");

            if (patch.Left.HasValue) chr.ChangeHandAnime(0, patch.Left.Value);
            if (patch.Right.HasValue) chr.ChangeHandAnime(1, patch.Right.Value);
            return ToCharacterDto(id, chr);
        }

        public static CharacterDto SetCharacterExpression(int id, ExpressionPatch patch)
        {
            var chr = RequireCharacter(id);
            if (patch == null || patch.IsEmpty) return ToCharacterDto(id, chr);

            if (patch.Eyebrows.HasValue)
                chr.charInfo.ChangeEyebrowPtn(PatternKey(chr, true, patch.Eyebrows.Value, "eyebrows"));
            if (patch.Eyes.HasValue)
                chr.charInfo.ChangeEyesPtn(PatternKey(chr, false, patch.Eyes.Value, "eyes"));
            if (patch.EyesOpen.HasValue) chr.ChangeEyesOpen(patch.EyesOpen.Value);
            if (patch.Blink.HasValue) chr.ChangeBlink(patch.Blink.Value);
            if (patch.Mouth.HasValue) chr.ChangeMouthPtn(patch.Mouth.Value);
            if (patch.MouthOpen.HasValue) chr.ChangeMouthOpen(patch.MouthOpen.Value);
            if (patch.LipSync.HasValue) chr.ChangeLipSync(patch.LipSync.Value);
            if (patch.Tears.HasValue) chr.SetTears(patch.Tears.Value);
            if (patch.Blush.HasValue) chr.SetHohoAkaRate(patch.Blush.Value);
            if (patch.LookEyes.HasValue)
            {
                if (patch.LookEyes.Value < 0 || patch.LookEyes.Value > 4)
                    throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'look_eyes' must be 0..4");
                chr.ChangeLookEyesPtn(patch.LookEyes.Value);
            }
            if (patch.LookNeck.HasValue)
            {
                // MPCharCtrl.NeckInfo only ever submits these four patterns.
                if (Array.IndexOf(NeckPatterns, patch.LookNeck.Value) < 0)
                    throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'look_neck' must be one of 0, 1, 3, 4");
                chr.ChangeLookNeckPtn(patch.LookNeck.Value);
            }
            if (patch.Categories != null)
            {
                var count = chr.oiCharInfo.expression.Length;
                foreach (var kv in patch.Categories)
                {
                    if (kv.Key < 0 || kv.Key >= count)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "expression category " + kv.Key + " is out of range (0.." + (count - 1) + ")");
                    chr.EnableExpressionCategory(kv.Key, kv.Value);
                }
            }
            return ToCharacterDto(id, chr);
        }

        public static List<CoordinateFileDto> ListCoordinates(string sex, string query, int limit)
        {
            if (sex != null && sex != "female" && sex != "male")
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'sex' must be 'female' or 'male'");
            var list = new List<CoordinateFileDto>();
            foreach (var folder in new[] { "female", "male" })
            {
                if (sex != null && sex != folder) continue;
                var root = Path.Combine(CoordinateRoot(), folder);
                if (!Directory.Exists(root)) continue;
                foreach (var file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    var dto = ReadCoordinate(file, folder);
                    if (dto == null) continue;
                    if (!Matches(query, dto.Name, dto.Path)) continue;
                    list.Add(dto);
                    if (list.Count >= limit) return list;
                }
            }
            return list;
        }

        public static OutfitDto ListOutfit(int id) => ToOutfitDto(id, RequireCharacter(id));

        public static OutfitDto SetCharacterOutfit(int id, OutfitPatch patch)
        {
            var chr = RequireCharacter(id);
            if (patch == null || patch.IsEmpty) return ToOutfitDto(id, chr);

            // Names first: they are wrong or right whatever is worn.
            byte? all = null;
            if (patch.ClothesAll != null) all = RequireClothesState(patch.ClothesAll, "clothes_all");
            var states = new List<KeyValuePair<int, byte>>();
            if (patch.Clothes != null)
                foreach (var kv in patch.Clothes)
                    states.Add(new KeyValuePair<int, byte>(RequireClothesSlot(kv.Key), RequireClothesState(kv.Value, "clothes." + kv.Key)));

            if (patch.Coordinate != null)
            {
                var sex = chr.oiCharInfo.sex;
                var full = ResolveCoordinatePath(patch.Coordinate, sex);
                // OCIChar.LoadClothesFile drops the two bools underneath it
                // (ChangeNowCoordinate's and Reload's), so an unreadable file would
                // silently leave the character dressed as it was. Reading the header
                // here is the same LoadFile ChangeNowCoordinate is about to do.
                var probe = new AIChara.ChaFileCoordinate();
                if (!probe.LoadFile(full))
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "not a readable coordinate file (error " + probe.GetLastErrorCode() + "): " + full);
                // Always the virtual: on a female it also refreshes bust softness, the
                // skirt dynamic bones and the FK skirt group, whose bone weights depend
                // on the garment that was just put on.
                chr.LoadClothesFile(full);
            }

            // Which states a slot accepts and which accessory slots are filled both
            // depend on what is worn, and the coordinate above has just changed that,
            // so this is the earliest point the rest of the patch can be checked — and
            // still before a single write, so a bad entry applies none of them.
            foreach (var kv in states) RequireSlotAccepts(chr, kv.Key, kv.Value);
            if (patch.Accessories != null)
                foreach (var kv in patch.Accessories) RequireAccessorySlot(chr, kv.Key);

            // Studio's own three "cos state" buttons do exactly this, and per slot
            // SetClothesState falls back to a state the slot does accept, so the bulk
            // value is deliberately not validated against every slot.
            if (all.HasValue) chr.SetClothesStateAll(all.Value);
            foreach (var kv in states) chr.SetClothesState(kv.Key, kv.Value);
            if (patch.Accessories != null)
                foreach (var kv in patch.Accessories) chr.ShowAccessory(kv.Key, kv.Value);

            return ToOutfitDto(id, chr);
        }

        public static CharacterDto SetCharacterState(int id, CharacterStatePatch patch)
        {
            var chr = RequireCharacter(id);
            if (patch == null || patch.IsEmpty) return ToCharacterDto(id, chr);

            var female = chr.oiCharInfo.sex == 1;
            if (patch.Nipple.HasValue && !female)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'nipple' only applies to female characters: OCIChar.SetNipStand does nothing on a male");
            if (patch.Siru != null && !female)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'siru' only applies to female characters: ChaControl.UpdateSiru returns immediately on a male");

            // A level is used as an index into a float[3] by UpdateSiru, which runs on
            // the next frame inside Studio's own update loop: an out-of-range level
            // takes the process down there rather than here, so every part is checked
            // before any is written.
            var siru = new List<KeyValuePair<int, byte>>();
            if (patch.Siru != null)
                foreach (var kv in patch.Siru)
                {
                    var part = OutfitNames.Index(OutfitNames.Siru, kv.Key);
                    if (part < 0)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "unknown siru part '" + kv.Key + "'; expected one of " + OutfitNames.Join(OutfitNames.Siru));
                    if (kv.Value < 0 || kv.Value > 2)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "siru." + kv.Key + " must be 0 (none), 1 or 2");
                    siru.Add(new KeyValuePair<int, byte>(part, (byte)kv.Value));
                }

            if (patch.Nipple.HasValue) chr.SetNipStand(patch.Nipple.Value);
            if (patch.SkinGloss.HasValue) chr.SetTuyaRate(patch.SkinGloss.Value);
            if (patch.Wet.HasValue) chr.SetWetRate(patch.Wet.Value);
            if (patch.Simple.HasValue) chr.SetVisibleSimple(patch.Simple.Value);
            if (patch.SimpleColor != null) chr.SetSimpleColor(Col(patch.SimpleColor));
            if (patch.Son.HasValue) chr.SetVisibleSon(patch.Son.Value);
            if (patch.SonLength.HasValue) chr.SetSonLength(patch.SonLength.Value);
            foreach (var kv in siru)
                chr.SetSiruFlags((AIChara.ChaFileDefine.SiruParts)kv.Key, kv.Value);

            return ToCharacterDto(id, chr);
        }

        private static OutfitDto ToOutfitDto(int id, SNeo.OCIChar chr)
        {
            var ci = chr.charInfo;
            var status = chr.charFileStatus;
            var dto = new OutfitDto
            {
                CharacterId = id,
                Sex = SafeSex(chr),
                Clothes = new List<ClothesSlotDto>(),
                Accessories = new List<AccessorySlotDto>(),
            };
            for (var i = 0; i < OutfitNames.Clothes.Length; i++)
            {
                var state = i < status.clothesState.Length ? status.clothesState[i] : (byte)0;
                dto.Clothes.Add(new ClothesSlotDto
                {
                    Index = i,
                    Slot = OutfitNames.Clothes[i],
                    Name = ClothesName(ci, i),
                    Equipped = ClothesEquipped(ci, i),
                    State = state < OutfitNames.States.Length ? OutfitNames.States[state] : null,
                    States = ClothesStates(ci, i),
                    LinkedTo = LinkedSlot(ci, i),
                });
            }
            var parts = ci.nowCoordinate != null && ci.nowCoordinate.accessory != null ? ci.nowCoordinate.accessory.parts : null;
            for (var slot = 0; slot < status.showAccessory.Length; slot++)
                dto.Accessories.Add(new AccessorySlotDto
                {
                    Slot = slot,
                    Name = AccessoryName(ci, slot),
                    // An empty slot keeps PartsInfo's default "", which reads as a
                    // parent bone that does not exist.
                    Parent = parts != null && slot < parts.Length && !string.IsNullOrEmpty(parts[slot].parentKey) ? parts[slot].parentKey : null,
                    Equipped = AccessoryEquipped(ci, slot),
                    Shown = status.showAccessory[slot],
                });
            return dto;
        }

        private static OutfitSummaryDto SafeOutfit(SNeo.OCIChar chr)
        {
            try
            {
                var ci = chr.charInfo;
                var status = chr.charFileStatus;
                var dto = new OutfitSummaryDto { Clothes = new string[OutfitNames.Clothes.Length] };
                for (var i = 0; i < dto.Clothes.Length && i < status.clothesState.Length; i++)
                {
                    var state = status.clothesState[i];
                    dto.Clothes[i] = state < OutfitNames.States.Length ? OutfitNames.States[state] : null;
                }
                for (var slot = 0; slot < status.showAccessory.Length; slot++)
                {
                    if (!AccessoryEquipped(ci, slot)) continue;
                    dto.AccessoriesEquipped++;
                    if (status.showAccessory[slot]) dto.AccessoriesShown++;
                }
                return dto;
            }
            catch { return null; }
        }

        private static CharacterStateDto SafeState(SNeo.OCIChar chr)
        {
            try
            {
                var oi = chr.oiCharInfo;
                var female = oi.sex == 1;
                return new CharacterStateDto
                {
                    Nipple = female ? (float?)oi.nipple : null,
                    SkinGloss = oi.SkinTuyaRate,
                    Wet = oi.WetRate,
                    Simple = chr.GetVisibleSimple(),
                    SimpleColor = C(oi.simpleColor),
                    Son = oi.visibleSon,
                    SonLength = chr.GetSonLength(),
                    // oiCharInfo.siru is what SetSiruFlags writes and what a scene load
                    // pushes back into the character (AddObjectAssist.UpdateState), and
                    // unlike ChaFileStatus.siruLv it is up to date in the same frame.
                    Siru = female ? (byte[])oi.siru.Clone() : null,
                };
            }
            catch { return null; }
        }

        private static string ClothesName(AIChara.ChaControl ci, int kind)
        {
            try
            {
                var info = ci.infoClothes[kind];
                return info != null ? info.Name : null;
            }
            catch { return null; }
        }

        private static bool ClothesEquipped(AIChara.ChaControl ci, int kind)
        {
            try { return ci.objClothes[kind] != null; }
            catch { return false; }
        }

        private static string AccessoryName(AIChara.ChaControl ci, int slot)
        {
            try
            {
                var info = ci.infoAccessory[slot];
                return info != null ? info.Name : null;
            }
            catch { return null; }
        }

        private static bool AccessoryEquipped(AIChara.ChaControl ci, int slot)
        {
            try { return ci.objAccessory[slot] != null; }
            catch { return false; }
        }

        // A slot with no garment, or one whose garment declares a StateType above 1,
        // gets no entry in dictStateType at all: SetClothesState then stores the raw
        // byte and nothing happens on screen.
        private static string[] ClothesStates(AIChara.ChaControl ci, int kind)
        {
            try
            {
                var kinds = ci.GetClothesStateKind(kind);
                if (kinds == null) return new string[0];
                var list = new List<string>();
                for (byte state = 0; state < OutfitNames.States.Length; state++)
                    if (kinds.ContainsKey(state)) list.Add(OutfitNames.States[state]);
                return list.ToArray();
            }
            catch { return new string[0]; }
        }

        // SetClothesState mirrors 'off' onto the sibling slot when one garment covers
        // both: top↔bot while notBot, inner_t↔inner_b while notInnerB. Writing one of
        // a linked pair therefore changes the other, which is why the read-back returns
        // every slot rather than the ones that were asked for.
        private static string LinkedSlot(AIChara.ChaControl ci, int kind)
        {
            try
            {
                if (ci.notBot && kind == 0) return OutfitNames.Clothes[1];
                if (ci.notBot && kind == 1) return OutfitNames.Clothes[0];
                if (ci.notInnerB && kind == 2) return OutfitNames.Clothes[3];
                if (ci.notInnerB && kind == 3) return OutfitNames.Clothes[2];
            }
            catch { }
            return null;
        }

        private static int RequireClothesSlot(string slot)
        {
            var index = OutfitNames.Index(OutfitNames.Clothes, slot);
            if (index < 0)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "unknown clothes slot '" + slot + "'; expected one of " + OutfitNames.Join(OutfitNames.Clothes));
            return index;
        }

        private static byte RequireClothesState(string state, string param)
        {
            var index = OutfitNames.Index(OutfitNames.States, state);
            if (index < 0)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "parameter '" + param + "' must be one of " + OutfitNames.Join(OutfitNames.States));
            return (byte)index;
        }

        // ChaControl.SetClothesState never refuses a state: it walks (state + 1) % 3
        // until it finds one the slot accepts, so an unsupported value is applied as a
        // different one without a word. Checking here is the only way a caller learns.
        private static void RequireSlotAccepts(SNeo.OCIChar chr, int kind, byte state)
        {
            var available = ClothesStates(chr.charInfo, kind);
            if (Array.IndexOf(available, OutfitNames.States[state]) >= 0) return;
            var slot = OutfitNames.Clothes[kind];
            throw new StudioApiException(StudioApiException.InvalidParams, available.Length == 0
                ? "clothes slot '" + slot + "' has no dressing states on this outfit (nothing is worn there, or the garment has none) — use studio_list_outfit to see which slots do"
                : "clothes slot '" + slot + "' does not accept state '" + OutfitNames.States[state] + "'; this garment only takes " + string.Join(", ", available));
        }

        // OCIChar.ShowAccessory writes charFileStatus.showAccessory[id] with no bounds
        // check of its own, and an empty slot stores the flag and shows nothing:
        // ChaControl.UpdateVisible only walks the slots that hold an object.
        private static void RequireAccessorySlot(SNeo.OCIChar chr, int slot)
        {
            var count = chr.charFileStatus.showAccessory.Length;
            if (slot < 0 || slot >= count)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "accessory slot " + slot + " is out of range (0.." + (count - 1) + ")");
            if (!AccessoryEquipped(chr.charInfo, slot))
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "accessory slot " + slot + " is empty on this character; use studio_list_outfit to see which slots are equipped");
        }

        // Coordinate files carry no sex marker of their own — unlike pose files, which
        // have an identifying code — so the folder is the only signal there is.
        private static string ResolveCoordinatePath(string path, int sex)
        {
            if (string.IsNullOrEmpty(path))
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'coordinate' must be a non-empty string");

            var normalized = path.Replace('/', '\\');
            string folder = null;
            if (normalized.StartsWith("female\\", StringComparison.OrdinalIgnoreCase)) folder = "female";
            else if (normalized.StartsWith("male\\", StringComparison.OrdinalIgnoreCase)) folder = "male";
            else if (Path.IsPathRooted(normalized))
            {
                var parent = Path.GetFileName(Path.GetDirectoryName(normalized));
                if (string.Equals(parent, "female", StringComparison.OrdinalIgnoreCase)) folder = "female";
                else if (string.Equals(parent, "male", StringComparison.OrdinalIgnoreCase)) folder = "male";
            }

            if (folder != null && folder != SexName(sex))
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "coordinate '" + path + "' is a " + folder + " outfit, but the character is " + SexName(sex));

            var candidate = Path.IsPathRooted(normalized)
                ? normalized
                : Path.Combine(CoordinateRoot(), folder != null ? normalized : Path.Combine(SexName(sex), normalized));
            if (!File.Exists(candidate) && !candidate.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                candidate += ".png";
            if (!File.Exists(candidate))
                throw new StudioApiException(StudioApiException.NotFound,
                    "coordinate file not found: " + candidate + " (use studio_list_coordinates to see available outfits)");
            return candidate;
        }

        public static CameraDto SetCamera(CameraPatch patch)
        {
            var studio = Require();
            var cc = studio.cameraCtrl;
            if (cc == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "camera control is not available");
            if (patch.TargetPos.HasValue) cc.targetPos = U(patch.TargetPos.Value);
            if (patch.Angle.HasValue) cc.cameraAngle = U(patch.Angle.Value);
            if (patch.Fov.HasValue) cc.fieldOfView = patch.Fov.Value;
            if (patch.Distance.HasValue)
            {
                // cameraData is protected; Export/Import is the public round-trip.
                // Vanilla zoom only uses the z axis (see CameraControl.SetCamera).
                var data = cc.Export();
                data.distance = new Vector3(0f, 0f, -Math.Abs(patch.Distance.Value));
                cc.Import(data);
            }
            return GetCamera();
        }

        public static ObjectDto CreateItem(int group, int category, int no, TransformPatch transform)
        {
            Require();
            var info = Singleton<SNeo.Info>.Instance;
            Dictionary<int, Dictionary<int, SNeo.Info.ItemLoadInfo>> categories;
            Dictionary<int, SNeo.Info.ItemLoadInfo> items;
            if (info == null || !info.dicItemLoadInfo.TryGetValue(group, out categories)
                || !categories.TryGetValue(category, out items) || !items.ContainsKey(no))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no item with group=" + group + " category=" + category + " no=" + no + " (use studio_search_catalog to find valid ids)");

            var oci = SNeo.AddObjectItem.Add(group, category, no);
            if (oci == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "Studio failed to create the item (asset could not be loaded)");
            ApplyTransform(oci, transform);
            return ToDto(oci.objectInfo.dicKey, oci);
        }

        public static ObjectDto CreateLight(int no, TransformPatch transform, LightPatch light)
        {
            Require();
            var info = Singleton<SNeo.Info>.Instance;
            if (info == null || !info.dicLightLoadInfo.ContainsKey(no))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no light type with no=" + no + " (use studio_search_catalog with catalog='light')");

            var oci = SNeo.AddObjectLight.Add(no);
            if (oci == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "Studio failed to create the light");
            ApplyTransform(oci, transform);
            ApplyLight(oci, light);
            return ToDto(oci.objectInfo.dicKey, oci);
        }

        public static ObjectDto SetLight(int id, LightPatch patch)
        {
            var oci = RequireObject(id) as SNeo.OCILight;
            if (oci == null)
                throw new StudioApiException(StudioApiException.InvalidParams, "object " + id + " is not a light");
            ApplyLight(oci, patch);
            return ToDto(id, oci);
        }

        public static ObjectDto SetObjectTransform(int id, TransformPatch patch)
        {
            var oci = RequireObject(id);
            ApplyTransform(oci, patch, id);
            return ToDto(id, oci);
        }

        public static int DeleteObject(int id)
        {
            var studio = Require();
            var oci = RequireObject(id);
            var node = oci.treeNodeObject;
            if (node == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "object " + id + " has no workspace node and cannot be deleted");
            if (!node.enableDelete)
                throw new StudioApiException(StudioApiException.OperationFailed, "object " + id + " is not deletable");

            var before = studio.dicObjectCtrl.Count;
            studio.treeNodeCtrl.DeleteNode(node);
            // Deletion is not undoable in Studio NEO V2 (no delete command exists),
            // so the undo stack would replay against destroyed objects.
            Singleton<SNeo.UndoRedoManager>.Instance.Clear();
            return before - studio.dicObjectCtrl.Count;
        }

        public static List<AttachPointDto> ListAttachPoints(int id)
        {
            var chr = RequireCharacter(id);
            var list = new List<AttachPointDto>();
            foreach (var kv in chr.dicAccessoryPoint)
                list.Add(new AttachPointDto
                {
                    Index = kv.Value,
                    Name = AttachPointName(kv.Value),
                    Available = AttachPointTransform(chr, kv.Value) != null,
                    Attached = AttachedIds(chr, kv.Value),
                });
            list.Sort((a, b) => a.Index.CompareTo(b.Index));
            return list;
        }

        public static ObjectDto AttachObject(int id, int? parentId, int? attachPoint, string attachPointName)
        {
            var studio = Require();
            var oci = RequireObject(id);
            var node = oci.treeNodeObject;
            if (node == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "object " + id + " has no workspace node and cannot be attached");
            if (!node.enableChangeParent)
                throw new StudioApiException(StudioApiException.OperationFailed, "object " + id + " cannot be reparented");

            var hasPoint = attachPoint.HasValue || attachPointName != null;
            SNeo.TreeNodeObject target = null;
            if (parentId.HasValue)
            {
                if (parentId.Value == id)
                    throw new StudioApiException(StudioApiException.InvalidParams, "an object cannot be attached to itself");
                var parent = RequireObject(parentId.Value);
                var parentChar = parent as SNeo.OCIChar;
                if (parentChar != null)
                {
                    var index = ResolveAttachPoint(parentChar, parentId.Value, attachPoint, attachPointName);
                    target = FindAttachPointNode(parentChar, index);
                    if (target == null)
                        throw new StudioApiException(StudioApiException.OperationFailed,
                            "character " + parentId.Value + " has no workspace node for attach point " + AttachPointName(index));
                }
                else
                {
                    if (hasPoint)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "'attach_point' only applies when 'parent_id' is a character; object " + parentId.Value + " is of kind '" + KindOf(parent) + "'");
                    target = parent.treeNodeObject;
                    if (target == null)
                        throw new StudioApiException(StudioApiException.OperationFailed, "object " + parentId.Value + " has no workspace node and cannot be a parent");
                }
                if (!target.enableAddChild)
                    throw new StudioApiException(StudioApiException.InvalidParams, "object " + parentId.Value + " does not accept children");
            }
            else if (hasPoint)
            {
                throw new StudioApiException(StudioApiException.InvalidParams, "'attach_point' needs 'parent_id'");
            }

            // TreeNodeCtrl.SetParent reports nothing: it silently skips the whole
            // parentage (and OnAttach with it) on a hierarchy loop or a node that
            // refuses children, so the result is checked instead of the call.
            studio.treeNodeCtrl.SetParent(node, target);
            var actual = SafeParentId(oci);
            if (target == null)
            {
                if (actual.HasValue)
                    throw new StudioApiException(StudioApiException.OperationFailed, "Studio refused to detach object " + id);
            }
            else if (!actual.HasValue || actual.Value != parentId.Value)
            {
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "Studio refused to attach object " + id + " to " + parentId.Value + " (it would close a loop in the workspace hierarchy)");
            }

            // OnAttach/OnDetach rebase changeAmount between world and parent space,
            // so every transform command already on the stack would be replayed in
            // the wrong space. Studio has no parentage command to undo this either.
            Singleton<SNeo.UndoRedoManager>.Instance.Clear();
            return ToDto(id, oci);
        }

        private static int ResolveAttachPoint(SNeo.OCIChar chr, int parentId, int? index, string name)
        {
            if (!index.HasValue && name == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "attaching to character " + parentId + " needs 'attach_point' (use studio_list_attach_points to pick one)");

            var value = index.HasValue ? index.Value : AttachPointIndex(name);
            if (value == 0)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "attach point 0 is 'none'; omit 'parent_id' to detach instead");
            if (!chr.dicAccessoryPoint.ContainsValue(value))
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "attach point " + value + " does not exist on character " + parentId + " (use studio_list_attach_points)");

            // The point nodes exist for every rig, but the bone behind one may not:
            // OnAttach would then run in full, reparent the object to the scene root
            // and still set parentInfo, so every read-back would look attached.
            if (AttachPointTransform(chr, value) == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "attach point " + AttachPointName(value) + " (" + value + ") has no bone on character " + parentId +
                    ", attaching there would leave the object orphaned (its 'available' is false in studio_list_attach_points)");
            return value;
        }

        private static int AttachPointIndex(string name)
        {
            var names = Enum.GetNames(typeof(AIChara.ChaAccessoryDefine.AccessoryParentKey));
            for (var i = 0; i < names.Length; i++)
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                    return (int)Enum.Parse(typeof(AIChara.ChaAccessoryDefine.AccessoryParentKey), names[i]);
            throw new StudioApiException(StudioApiException.InvalidParams,
                "unknown attach point '" + name + "' (use studio_list_attach_points for the names)");
        }

        // ChaAccessoryDefine.AccessoryParentName holds the Japanese labels Studio
        // shows; the enum member is the bone-ish name an agent can actually reason
        // about, so that is what the MCP surface uses.
        private static string AttachPointName(int index)
        {
            var name = Enum.GetName(typeof(AIChara.ChaAccessoryDefine.AccessoryParentKey), index);
            return name ?? ("point_" + index);
        }

        private static Transform AttachPointTransform(SNeo.OCIChar chr, int index)
        {
            try { return chr.charInfo.GetAccessoryParentTransform(index); }
            catch { return null; }
        }

        private static SNeo.TreeNodeObject FindAttachPointNode(SNeo.OCIChar chr, int index)
        {
            foreach (var kv in chr.dicAccessoryPoint)
                if (kv.Value == index) return kv.Key;
            return null;
        }

        private static List<int> AttachedIds(SNeo.OCIChar chr, int index)
        {
            var ids = new List<int>();
            try
            {
                List<SNeo.ObjectInfo> children;
                if (chr.oiCharInfo.child.TryGetValue(index, out children) && children != null)
                    foreach (var child in children)
                        if (child != null) ids.Add(child.dicKey);
            }
            catch { }
            return ids;
        }

        private static void ApplyTransform(SNeo.ObjectCtrlInfo oci, TransformPatch patch, int? undoableKey = null)
        {
            if (patch == null || patch.IsEmpty) return;
            var ca = oci.objectInfo.changeAmount;

            // Route through Studio's own undo commands when the object is registered
            // in dicChangeAmount, so agent edits stay undoable from the UI.
            var undoable = undoableKey.HasValue && ReferenceEquals(SNeo.Studio.GetChangeAmount(undoableKey.Value), ca);
            if (undoable)
            {
                var key = undoableKey.Value;
                var undo = Singleton<SNeo.UndoRedoManager>.Instance;
                if (patch.Pos.HasValue)
                    undo.Do(new SNeo.GuideCommand.MoveEqualsCommand(new[] { Equals(key, ca.pos, U(patch.Pos.Value)) }));
                if (patch.Rot.HasValue)
                    undo.Do(new SNeo.GuideCommand.RotationEqualsCommand(new[] { Equals(key, ca.rot, U(patch.Rot.Value)) }));
                if (patch.Scale.HasValue)
                    undo.Do(new SNeo.GuideCommand.ScaleEqualsCommand(new[] { Equals(key, ca.scale, U(patch.Scale.Value)) }));
                return;
            }

            if (patch.Pos.HasValue) ca.pos = U(patch.Pos.Value);
            if (patch.Rot.HasValue) ca.rot = U(patch.Rot.Value);
            if (patch.Scale.HasValue) ca.scale = U(patch.Scale.Value);
        }

        public static CharacterDto GetCharacter(int id) => ToCharacterDto(id, RequireCharacter(id));

        public static SkeletonDto ListBones(int id)
        {
            var chr = RequireCharacter(id);
            var dto = NewSkeleton(id, chr);
            foreach (var bone in chr.listBones) dto.Bones.Add(ToBoneDto(bone));
            for (var i = 0; i < chr.listIKTarget.Count; i++) dto.IkTargets.Add(ToIkDto(i, chr.listIKTarget[i]));
            return dto;
        }

        // Mode first, then the group flags: ActiveKinematicMode re-applies every group
        // from the stored flags, so a patch that carries both would otherwise lose its
        // group values. Neither call is undoable, but neither destroys an object nor
        // rebases a changeAmount, so unlike attach/delete the undo stack stays valid.
        public static void SetKinematics(int id, KinematicsPatch patch)
        {
            var chr = RequireCharacter(id);
            if (patch == null || patch.Mode == null && patch.Fk == null && patch.Ik == null) return;

            if (patch.Mode != null)
            {
                switch (patch.Mode)
                {
                    case "fk":
                        chr.ActiveKinematicMode(SNeo.OICharInfo.KinematicMode.FK, true, false);
                        break;
                    case "ik":
                        chr.ActiveKinematicMode(SNeo.OICharInfo.KinematicMode.IK, true, false);
                        break;
                    case "none":
                        chr.ActiveKinematicMode(SNeo.OICharInfo.KinematicMode.FK, false, false);
                        chr.ActiveKinematicMode(SNeo.OICharInfo.KinematicMode.IK, false, false);
                        break;
                    default:
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "parameter 'mode' must be 'fk', 'ik' or 'none'");
                }
            }

            if (patch.Fk != null)
                for (var i = 0; i < patch.Fk.Length; i++)
                    if (patch.Fk[i].HasValue)
                        chr.ActiveFK(SNeo.FKCtrl.parts[i], patch.Fk[i].Value);

            if (patch.Ik != null)
                for (var i = 0; i < patch.Ik.Length; i++)
                    if (patch.Ik[i].HasValue)
                        chr.ActiveIK((SNeo.OIBoneInfo.BoneGroup)(1 << i), patch.Ik[i].Value);
        }

        // The masks Studio's own reflect buttons use: FK skips the groups driven by
        // dynamic bones (hair, breast, skirt), IK covers all five of its groups.
        private const SNeo.OIBoneInfo.BoneGroup ReflectFkMask = (SNeo.OIBoneInfo.BoneGroup)353;
        private const SNeo.OIBoneInfo.BoneGroup ReflectIkMask = (SNeo.OIBoneInfo.BoneGroup)31;

        public static void RequireReflect(string reflect)
        {
            if (reflect == null || reflect == "fk" || reflect == "ik" || reflect == "both") return;
            throw new StudioApiException(StudioApiException.InvalidParams,
                "parameter 'reflect' must be 'fk', 'ik' or 'both'");
        }

        // Reads the skeleton as it currently stands into the change amounts. Must run
        // after the Animator has written this frame's pose, hence the caller's wait.
        public static void Reflect(int id, string reflect)
        {
            var chr = RequireCharacter(id);
            if (reflect == "fk" || reflect == "both")
            {
                if (chr.fkCtrl == null)
                    throw new StudioApiException(StudioApiException.OperationFailed, "character " + id + " has no FK controller");
                chr.fkCtrl.CopyBone(ReflectFkMask);
            }
            if (reflect == "ik" || reflect == "both")
            {
                if (chr.ikCtrl == null)
                    throw new StudioApiException(StudioApiException.OperationFailed, "character " + id + " has no IK controller");
                chr.ikCtrl.CopyBone(ReflectIkMask);
            }
        }

        public static SkeletonDto SetCharacterBone(int id, List<KeyValuePair<string, Vec3>> rotations)
        {
            var chr = RequireCharacter(id);
            var oi = chr.oiCharInfo;
            if (!oi.enableFK)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "FK is off on character " + id + ", so bone rotations would be stored and never applied; " +
                    "call studio_set_character_kinematics with mode='fk' first");

            var index = BoneIndex(chr);
            var targets = new List<SNeo.OCIChar.BoneInfo>(rotations.Count);
            var infos = new SNeo.GuideCommand.EqualsInfo[rotations.Count];
            for (var i = 0; i < rotations.Count; i++)
            {
                var name = rotations[i].Key;
                SNeo.OCIChar.BoneInfo bone;
                if (!index.TryGetValue(name, out bone))
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "character " + id + " has no bone '" + name + "' (use studio_list_bones)");

                var group = FkGroupIndex(bone.boneGroup);
                if (group < 0)
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "bone '" + name + "' belongs to no FK group and cannot be posed");
                if (!oi.activeFK[group])
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "bone '" + name + "' is in the FK group '" + KinematicGroups.Fk[group] + "', which is off on character " + id +
                        "; enable it with studio_set_character_kinematics fk={\"" + KinematicGroups.Fk[group] + "\": true}");
                // FKCtrl.Update applies a bone only while `enable & boneWeight`, so an
                // unweighted bone would take the value and never move the mesh.
                if (!bone.boneWeight)
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "bone '" + name + "' drives no mesh on this character's current clothes ('weighted': false in studio_list_bones), " +
                        "so FK skips it");

                var ca = bone.boneInfo.changeAmount;
                var key = bone.boneInfo.dicKey;
                if (!ReferenceEquals(SNeo.Studio.GetChangeAmount(key), ca))
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "bone '" + name + "' is not registered in Studio's change-amount table and cannot be posed");
                targets.Add(bone);
                infos[i] = Equals(key, ca.rot, U(rotations[i].Value));
            }

            // One command for the whole batch, so the agent's edit is a single undo step.
            Singleton<SNeo.UndoRedoManager>.Instance.Do(new SNeo.GuideCommand.RotationEqualsCommand(infos));
            var dto = NewSkeleton(id, chr);
            foreach (var bone in targets) dto.Bones.Add(ToBoneDto(bone));
            return dto;
        }

        public static SkeletonDto SetCharacterIk(int id, List<IkTargetPatch> patches)
        {
            var chr = RequireCharacter(id);
            var oi = chr.oiCharInfo;
            if (!oi.enableIK)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "IK is off on character " + id + ", so target moves would be stored and never applied; " +
                    "call studio_set_character_kinematics with mode='ik' first");

            var order = new List<int>(patches.Count);
            var moves = new List<SNeo.GuideCommand.EqualsInfo>();
            var rotations = new List<SNeo.GuideCommand.EqualsInfo>();
            foreach (var patch in patches)
            {
                var index = IkTargetIndex(patch.Target);
                if (index < 0 || index >= chr.listIKTarget.Count)
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "character " + id + " has no IK target '" + patch.Target + "' (use studio_list_bones)");

                var target = chr.listIKTarget[index];
                var group = IkGroupIndex(target.targetInfo.group);
                if (group < 0 || !oi.activeIK[group])
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "IK target '" + patch.Target + "' is in the group '" + (group < 0 ? "?" : KinematicGroups.Ik[group]) +
                        "', which is off on character " + id + "; enable it with studio_set_character_kinematics ik={\"" +
                        (group < 0 ? "?" : KinematicGroups.Ik[group]) + "\": true}");
                // IKInfo.CopyBone zeroes rot on targets whose guide has no rotation
                // handle, so a rotation written there is discarded on the next reflect.
                if (patch.Rot.HasValue && !Rotatable(target))
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "IK target '" + patch.Target + "' has no rotation handle ('rotatable': false in studio_list_bones); " +
                        "only the hand and foot targets can be rotated");

                var ca = target.targetInfo.changeAmount;
                var key = target.targetInfo.dicKey;
                if (!ReferenceEquals(SNeo.Studio.GetChangeAmount(key), ca))
                    throw new StudioApiException(StudioApiException.OperationFailed,
                        "IK target '" + patch.Target + "' is not registered in Studio's change-amount table and cannot be moved");

                if (patch.Pos.HasValue) moves.Add(Equals(key, ca.pos, U(patch.Pos.Value)));
                if (patch.Rot.HasValue) rotations.Add(Equals(key, ca.rot, U(patch.Rot.Value)));
                order.Add(index);
            }

            var undo = Singleton<SNeo.UndoRedoManager>.Instance;
            if (moves.Count > 0) undo.Do(new SNeo.GuideCommand.MoveEqualsCommand(moves.ToArray()));
            if (rotations.Count > 0) undo.Do(new SNeo.GuideCommand.RotationEqualsCommand(rotations.ToArray()));

            var dto = NewSkeleton(id, chr);
            foreach (var index in order) dto.IkTargets.Add(ToIkDto(index, chr.listIKTarget[index]));
            return dto;
        }

        private static SkeletonDto NewSkeleton(int id, SNeo.OCIChar chr)
        {
            var oi = chr.oiCharInfo;
            return new SkeletonDto
            {
                CharacterId = id,
                Mode = oi.enableIK ? "ik" : oi.enableFK ? "fk" : "none",
                Bones = new List<BoneDto>(),
                IkTargets = new List<IkTargetDto>(),
            };
        }

        private static BoneDto ToBoneDto(SNeo.OCIChar.BoneInfo bone)
        {
            var info = Singleton<SNeo.Info>.Instance;
            SNeo.Info.BoneInfo catalog = null;
            if (info != null) info.dicBoneInfo.TryGetValue(bone.boneID, out catalog);
            var group = FkGroupIndex(bone.boneGroup);
            return new BoneDto
            {
                Id = bone.boneID,
                Bone = BoneName(bone, catalog),
                Label = catalog != null ? catalog.name : null,
                Group = group >= 0 ? KinematicGroups.Fk[group] : null,
                Level = bone.boneInfo.level,
                Weighted = bone.boneWeight,
                Rot = V(bone.boneInfo.changeAmount.rot),
            };
        }

        private static IkTargetDto ToIkDto(int index, SNeo.OCIChar.IKInfo target)
        {
            var ca = target.targetInfo.changeAmount;
            var group = IkGroupIndex(target.targetInfo.group);
            return new IkTargetDto
            {
                Id = target.targetInfo.dicKey,
                Target = IkTargetName(index),
                Group = group >= 0 ? KinematicGroups.Ik[group] : null,
                Bone = target.boneObject != null ? target.boneObject.name : null,
                Rotatable = Rotatable(target),
                Pos = V(ca.pos),
                Rot = V(ca.rot),
            };
        }

        private static bool Rotatable(SNeo.OCIChar.IKInfo target)
        {
            try { return target.guideObject.enableRot; }
            catch { return false; }
        }

        // The Excel bone name is the stable identifier: it matches the Unity transform
        // and is unique per rig, while Info.BoneInfo.name is a shared Japanese label.
        private static string BoneName(SNeo.OCIChar.BoneInfo bone, SNeo.Info.BoneInfo catalog)
        {
            if (catalog != null && !string.IsNullOrEmpty(catalog.bone)) return catalog.bone;
            try { return bone.guideObject.transformTarget.name; }
            catch { return "bone_" + bone.boneID; }
        }

        private static Dictionary<string, SNeo.OCIChar.BoneInfo> BoneIndex(SNeo.OCIChar chr)
        {
            var info = Singleton<SNeo.Info>.Instance;
            var map = new Dictionary<string, SNeo.OCIChar.BoneInfo>();
            foreach (var bone in chr.listBones)
            {
                SNeo.Info.BoneInfo catalog = null;
                if (info != null) info.dicBoneInfo.TryGetValue(bone.boneID, out catalog);
                map[BoneName(bone, catalog)] = bone;
            }
            return map;
        }

        // OIBoneInfo.group is a mask: limb bones carry Body plus their own limb bit,
        // and FKCtrl.parts is what ActiveFK actually indexes, so the first matching
        // part is the group the bone answers to.
        private static int FkGroupIndex(SNeo.OIBoneInfo.BoneGroup group)
        {
            var parts = SNeo.FKCtrl.parts;
            for (var i = 0; i < parts.Length; i++)
                if ((group & parts[i]) != 0) return i;
            return -1;
        }

        private static int IkGroupIndex(SNeo.OIBoneInfo.BoneGroup group)
        {
            for (var i = 0; i < KinematicGroups.Ik.Length; i++)
                if (((int)group & (1 << i)) != 0) return i;
            return -1;
        }

        // listIKTarget is filled in OICharInfo.IKTargetEN order by AddObjectAssist.
        private static string IkTargetName(int index)
        {
            var names = Enum.GetNames(typeof(SNeo.OICharInfo.IKTargetEN));
            return index >= 0 && index < names.Length ? Snake(names[index]) : "target_" + index;
        }

        private static int IkTargetIndex(string name)
        {
            var names = Enum.GetNames(typeof(SNeo.OICharInfo.IKTargetEN));
            for (var i = 0; i < names.Length; i++)
                if (Snake(names[i]) == name) return i;
            return -1;
        }

        private static string Snake(string pascal)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < pascal.Length; i++)
            {
                if (char.IsUpper(pascal[i]) && sb.Length > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(pascal[i]));
            }
            return sb.ToString();
        }

        // ---- Environment: map, scene lighting, overlays, post-processing ----

        public static MapDto GetMap()
        {
            var studio = Require();
            var dto = new MapDto { No = -1 };
            var info = studio.sceneInfo != null ? studio.sceneInfo.mapInfo : null;
            if (info != null)
            {
                dto.No = info.no;
                dto.Name = MapName(info.no);
                dto.Pos = V(info.ca.pos);
                dto.Rot = V(info.ca.rot);
                dto.Option = info.option;
                dto.Light = info.light;
            }
            try
            {
                if (Singleton<SNeo.Map>.IsInstance())
                {
                    var map = Singleton<SNeo.Map>.Instance;
                    dto.Loaded = map.MapRoot != null;
                    // A map without optional objects or extra lights makes its two
                    // toggles non-interactable in Studio's own panel.
                    dto.OptionAvailable = map.IsOption;
                    dto.LightAvailable = map.IsLight;
                }
            }
            catch { }
            return dto;
        }

        private static string MapName(int no)
        {
            var info = Singleton<SNeo.Info>.Instance;
            SNeo.Info.MapLoadInfo entry;
            return info != null && info.dicMapLoadInfo.TryGetValue(no, out entry) ? entry.name : null;
        }

        // Studio.AddMap starts its own coroutine and returns void, so the caller has
        // to watch the scene for the change. mapInfo.no is not that signal: SetupMap
        // writes it and resets mapInfo.ca even when the requested map is the one
        // already loaded, so a transform applied too early would be wiped. onChangeMap
        // is fired by SetupMap at the end and is the one thing that means "settled".
        internal sealed class MapChange
        {
            public bool Started, Done;
            public Action Handler;
        }

        public static MapChange BeginMapChange(MapPatch patch)
        {
            var studio = Require();
            if (patch == null || patch.IsEmpty)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "nothing to change: pass at least one of 'no', 'pos', 'rot', 'option', 'light'");

            var change = new MapChange();
            if (!patch.No.HasValue) return change;

            var no = patch.No.Value;
            var info = Singleton<SNeo.Info>.Instance;
            // Map.LoadMapCoroutine releases the map for an unknown no and SetupMap then
            // stores it anyway, leaving the scene claiming a map it does not have.
            if (no != -1 && (info == null || !info.dicMapLoadInfo.ContainsKey(no)))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no map with no=" + no + " (use studio_search_catalog with catalog='map'; -1 removes the map)");

            change.Started = true;
            change.Handler = () => change.Done = true;
            studio.onChangeMap += change.Handler;
            try
            {
                studio.AddMap(no, false, false, true);
            }
            catch
            {
                studio.onChangeMap -= change.Handler;
                throw;
            }
            return change;
        }

        public static bool MapChangeDone(MapChange change) => change == null || !change.Started || change.Done;

        public static void EndMapChange(MapChange change)
        {
            if (change == null || !change.Started || change.Handler == null) return;
            try { Singleton<SNeo.Studio>.Instance.onChangeMap -= change.Handler; }
            catch { }
            change.Handler = null;
        }

        public static MapDto CompleteMapChange(MapPatch patch, bool mapChanged)
        {
            var studio = Require();
            var info = studio.sceneInfo != null ? studio.sceneInfo.mapInfo : null;
            if (info == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "the scene has no map slot");

            if (patch.Option.HasValue || patch.Light.HasValue)
            {
                var map = Singleton<SNeo.Map>.IsInstance() ? Singleton<SNeo.Map>.Instance : null;
                if (patch.Option.HasValue)
                {
                    if (map == null || !map.IsOption)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "the current map has no optional objects, so 'option' would be stored and change nothing (see map.option_available)");
                    map.VisibleOption = patch.Option.Value;
                }
                if (patch.Light.HasValue)
                {
                    if (map == null || !map.IsLight)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "the current map has no lights of its own, so 'light' would be stored and change nothing (see map.light_available)");
                    map.VisibleLight = patch.Light.Value;
                }
            }

            var oldPos = info.ca.pos;
            var oldRot = info.ca.rot;
            if (patch.Pos.HasValue) info.ca.pos = U(patch.Pos.Value);
            if (patch.Rot.HasValue) info.ca.rot = U(patch.Rot.Value);

            if (Singleton<SNeo.MapCtrl>.IsInstance()) Singleton<SNeo.MapCtrl>.Instance.Reflect();
            if (studio.mapList != null) studio.mapList.UpdateInfo();

            if (mapChanged)
            {
                // SetupMap resets mapInfo.ca with no command of its own, so every
                // MapCommand already on the stack would replay against a transform
                // that no longer means anything.
                Singleton<SNeo.UndoRedoManager>.Instance.Clear();
            }
            else
            {
                // MapCtrl's own input fields work the same way round: the value is
                // applied first and the command is pushed, not executed.
                if (patch.Pos.HasValue && info.ca.pos != oldPos)
                    Singleton<SNeo.UndoRedoManager>.Instance.Push(new SNeo.MapCommand.MoveEqualsCommand(
                        new SNeo.MapCommand.EqualsInfo { oldValue = oldPos, newValue = info.ca.pos }));
                if (patch.Rot.HasValue && info.ca.rot != oldRot)
                    Singleton<SNeo.UndoRedoManager>.Instance.Push(new SNeo.MapCommand.RotationEqualsCommand(
                        new SNeo.MapCommand.EqualsInfo { oldValue = oldRot, newValue = info.ca.rot }));
            }
            return GetMap();
        }

        public static SceneLightingDto GetSceneLighting()
        {
            var studio = Require();
            try
            {
                var light = studio.sceneInfo != null ? studio.sceneInfo.charaLight : null;
                if (light == null) return null;
                return new SceneLightingDto
                {
                    Color = C(light.color),
                    Intensity = light.intensity,
                    Rot = new[] { light.rot[0], light.rot[1] },
                    Shadow = light.shadow,
                };
            }
            catch { return null; }
        }

        public static SceneLightingDto SetSceneLighting(SceneLightingPatch patch)
        {
            var studio = Require();
            if (patch == null || patch.IsEmpty)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "nothing to change: pass at least one of 'color', 'intensity', 'rot', 'shadow'");
            var light = studio.sceneInfo != null ? studio.sceneInfo.charaLight : null;
            if (light == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "the scene has no chara light");

            // CameraLightCtrl's own input fields clamp to these, so anything outside
            // saves a scene the vanilla UI cannot reproduce.
            RequireRange(patch.Intensity, 0.1f, 2f, "intensity");
            if (patch.Rot != null)
            {
                RequireRange(patch.Rot[0], 0f, 359f, "rot[0]");
                RequireRange(patch.Rot[1], 0f, 359f, "rot[1]");
            }

            if (patch.Color != null) light.color = new Color(patch.Color[0], patch.Color[1], patch.Color[2], patch.Color[3]);
            if (patch.Intensity.HasValue) light.intensity = patch.Intensity.Value;
            if (patch.Rot != null)
            {
                light.rot[0] = patch.Rot[0];
                light.rot[1] = patch.Rot[1];
            }
            if (patch.Shadow.HasValue) light.shadow = patch.Shadow.Value;

            ReflectSceneLighting();
            return GetSceneLighting();
        }

        // CameraLightCtrl is the only thing in the assembly that pushes charaLight into
        // the actual Light, and Studio never calls it outside the light panel's own
        // Start/OnEnable — not even after loading a scene, which is why a loaded scene
        // keeps the previous lighting until that panel is opened. Every write reflects.
        public static void ReflectSceneLighting()
        {
            try
            {
                if (!IsStudioReady()) return;
                var ctrl = Singleton<SNeo.Studio>.Instance.cameraLightCtrl;
                if (ctrl != null) ctrl.Reflect();
            }
            catch { }
        }

        public static List<OverlayFileDto> ListOverlays(string kind, string query, int limit)
        {
            if (kind != null && kind != "background" && kind != "frame")
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'kind' must be 'background' or 'frame'");

            var list = new List<OverlayFileDto>();
            if (kind == null || kind == "background")
            {
                // BackgroundList reads both roots and keeps the root name in the value;
                // BackgroundCtrl.Load resolves it against Studio.ApplicationPath.
                AddOverlays(list, "background", DefaultData.Create(SNeo.BackgroundList.dirName),
                    DefaultData.RootName + "/" + SNeo.BackgroundList.dirName + "/", "default", query, limit);
                AddOverlays(list, "background", UserData.Create(SNeo.BackgroundList.dirName),
                    UserData.RootName + "/" + SNeo.BackgroundList.dirName + "/", "user", query, limit);
            }
            // Frames are the other way round: FrameList stores a bare file name and
            // FrameCtrl.Load prefixes UserData/frame itself.
            if (kind == null || kind == "frame")
                AddOverlays(list, "frame", UserData.Create(FrameDir), "", "user", query, limit);
            return list;
        }

        private const string FrameDir = "frame";

        private static void AddOverlays(List<OverlayFileDto> list, string kind, string root, string prefix, string source, string query, int limit)
        {
            if (list.Count >= limit || !Directory.Exists(root)) return;
            foreach (var file in Directory.GetFiles(root, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var value = prefix + Path.GetFileName(file);
                if (!Matches(query, name, value)) continue;
                list.Add(new OverlayFileDto { Kind = kind, Path = value, Name = name, Source = source });
                if (list.Count >= limit) return;
            }
        }

        public static OverlaysDto SetOverlay(OverlayPatch patch)
        {
            var studio = Require();
            if (patch == null || patch.IsEmpty)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "nothing to change: pass 'background' and/or 'frame' (an empty string clears one)");

            if (patch.Background != null)
            {
                // Load("") is the clear path: the file check fails, the mesh is hidden
                // and sceneInfo.background is emptied — it returns false either way.
                if (!BackgroundControl().Load(patch.Background) && patch.Background.Length > 0)
                    throw new StudioApiException(StudioApiException.NotFound,
                        "background image not found or unreadable: " + patch.Background + " (use studio_list_overlays)");
            }
            if (patch.Frame != null)
            {
                var frame = studio.frameCtrl;
                if (frame == null)
                    throw new StudioApiException(StudioApiException.OperationFailed, "the frame overlay is not available");
                if (!frame.Load(patch.Frame) && patch.Frame.Length > 0)
                    throw new StudioApiException(StudioApiException.NotFound,
                        "frame image not found or unreadable: " + patch.Frame + " (use studio_list_overlays with kind='frame')");
            }

            return new OverlaysDto
            {
                Background = Convert.ToString(studio.sceneInfo.background),
                Frame = Convert.ToString(studio.sceneInfo.frame),
            };
        }

        // Studio keeps its BackgroundCtrl private with no accessor, unlike frameCtrl.
        private static SNeo.BackgroundCtrl _backgroundCtrl;

        private static SNeo.BackgroundCtrl BackgroundControl()
        {
            if (_backgroundCtrl == null) _backgroundCtrl = UnityEngine.Object.FindObjectOfType<SNeo.BackgroundCtrl>();
            if (_backgroundCtrl == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "the background overlay is not available");
            return _backgroundCtrl;
        }

        public static PostProcessingDto GetPostProcessing()
        {
            var studio = Require();
            var si = studio.sceneInfo;
            if (si == null) return null;
            return new PostProcessingDto
            {
                ColorGrading = new PostProcessingDto.ColorGradingSection
                {
                    LookupTexture = si.cgLookupTexture,
                    Blend = si.cgBlend,
                    Saturation = si.cgSaturation,
                    Brightness = si.cgBrightness,
                    Contrast = si.cgContrast,
                },
                AmbientOcclusion = new PostProcessingDto.AmbientOcclusionSection
                {
                    Enabled = si.enableAmbientOcclusion,
                    Intensity = si.aoIntensity,
                    ThicknessModifier = si.aoThicknessModeifier,
                    Color = C(si.aoColor),
                },
                Bloom = new PostProcessingDto.BloomSection
                {
                    Enabled = si.enableBloom,
                    Intensity = si.bloomIntensity,
                    Threshold = si.bloomThreshold,
                    SoftKnee = si.bloomSoftKnee,
                    Clamp = si.bloomClamp,
                    Diffusion = si.bloomDiffusion,
                    Color = C(si.bloomColor),
                },
                DepthOfField = new PostProcessingDto.DepthOfFieldSection
                {
                    Enabled = si.enableDepth,
                    FocusObjectId = si.depthForcus,
                    FocalSize = si.depthFocalSize,
                    Aperture = si.depthAperture,
                },
                Vignette = new PostProcessingDto.VignetteSection
                {
                    Enabled = si.enableVignette,
                    Intensity = si.vignetteIntensity,
                },
                ScreenSpaceReflections = new PostProcessingDto.ToggleSection { Enabled = si.enableSSR },
                ReflectionProbe = new PostProcessingDto.ReflectionProbeSection
                {
                    Enabled = si.enableReflectionProbe,
                    Cubemap = si.reflectionProbeCubemap,
                    Intensity = si.reflectionProbeIntensity,
                },
                Fog = new PostProcessingDto.FogSection
                {
                    Enabled = si.enableFog,
                    ExcludeFarPixels = si.fogExcludeFarPixels,
                    Height = si.fogHeight,
                    HeightDensity = si.fogHeightDensity,
                    Color = C(si.fogColor),
                    Density = si.fogDensity,
                },
                SunShafts = new PostProcessingDto.SunShaftsSection
                {
                    Enabled = si.enableSunShafts,
                    CasterObjectId = si.sunCaster,
                    ThresholdColor = C(si.sunThresholdColor),
                    Color = C(si.sunColor),
                    DistanceFalloff = si.sunDistanceFallOff,
                    BlurSize = si.sunBlurSize,
                    Intensity = si.sunIntensity,
                },
                SelfShadow = new PostProcessingDto.ToggleSection { Enabled = si.enableShadow },
                EnvironmentLighting = new PostProcessingDto.EnvironmentLightingSection
                {
                    SkyColor = C(si.environmentLightingSkyColor),
                    EquatorColor = C(si.environmentLightingEquatorColor),
                    GroundColor = C(si.environmentLightingGroundColor),
                },
                Sky = new PostProcessingDto.SkySection
                {
                    Enabled = si.skyInfo.Enable,
                    Pattern = si.skyInfo.Pattern,
                },
            };
        }

        public static PostProcessingDto SetPostProcessing(PostProcessingDto patch)
        {
            var studio = Require();
            if (patch == null || patch.IsEmpty)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "nothing to change: pass at least one effect section");
            var si = studio.sceneInfo;
            var panel = studio.systemButtonCtrl;
            if (si == null || panel == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "the screen-effect panel is not available");

            // Everything refusable is refused before a single field is written: the
            // panel re-applies the whole of sceneInfo in one pass, so a patch that
            // failed halfway would already be visible on screen.
            var info = Singleton<SNeo.Info>.Instance;
            int? focus = null, caster = null;
            if (patch.DepthOfField != null && patch.DepthOfField.FocusObjectId.HasValue)
                focus = RequireItemTarget(patch.DepthOfField.FocusObjectId.Value, "depth_of_field.focus_object_id");
            if (patch.SunShafts != null && patch.SunShafts.CasterObjectId.HasValue)
                caster = RequireItemTarget(patch.SunShafts.CasterObjectId.Value, "sun_shafts.caster_object_id");
            if (patch.ColorGrading != null && patch.ColorGrading.LookupTexture.HasValue)
                RequireCatalogKey(info != null ? info.dicColorGradingLoadInfo : null,
                    patch.ColorGrading.LookupTexture.Value, "color_grading");
            if (patch.ReflectionProbe != null && patch.ReflectionProbe.Cubemap.HasValue)
                RequireCatalogKey(info != null ? info.dicReflectionProbeLoadInfo : null,
                    patch.ReflectionProbe.Cubemap.Value, "reflection_probe");
            if (patch.Sky != null && patch.Sky.Pattern.HasValue)
                RequireCatalogKey(info != null ? info.dicSkyPatternInfo : null,
                    patch.Sky.Pattern.Value, "sky");

            var cg = patch.ColorGrading;
            if (cg != null)
            {
                if (cg.LookupTexture.HasValue) si.cgLookupTexture = cg.LookupTexture.Value;
                if (cg.Blend.HasValue) si.cgBlend = cg.Blend.Value;
                if (cg.Saturation.HasValue) si.cgSaturation = cg.Saturation.Value;
                if (cg.Brightness.HasValue) si.cgBrightness = cg.Brightness.Value;
                if (cg.Contrast.HasValue) si.cgContrast = cg.Contrast.Value;
            }

            var ao = patch.AmbientOcclusion;
            if (ao != null)
            {
                if (ao.Enabled.HasValue) si.enableAmbientOcclusion = ao.Enabled.Value;
                if (ao.Intensity.HasValue) si.aoIntensity = ao.Intensity.Value;
                if (ao.ThicknessModifier.HasValue) si.aoThicknessModeifier = ao.ThicknessModifier.Value;
                if (ao.Color != null) si.aoColor = Col(ao.Color);
            }

            var bloom = patch.Bloom;
            if (bloom != null)
            {
                if (bloom.Enabled.HasValue) si.enableBloom = bloom.Enabled.Value;
                if (bloom.Intensity.HasValue) si.bloomIntensity = bloom.Intensity.Value;
                if (bloom.Threshold.HasValue) si.bloomThreshold = bloom.Threshold.Value;
                if (bloom.SoftKnee.HasValue) si.bloomSoftKnee = bloom.SoftKnee.Value;
                if (bloom.Clamp.HasValue) si.bloomClamp = bloom.Clamp.Value;
                if (bloom.Diffusion.HasValue) si.bloomDiffusion = bloom.Diffusion.Value;
                if (bloom.Color != null) si.bloomColor = Col(bloom.Color);
            }

            var dof = patch.DepthOfField;
            if (dof != null)
            {
                if (dof.Enabled.HasValue) si.enableDepth = dof.Enabled.Value;
                if (dof.FocalSize.HasValue) si.depthFocalSize = dof.FocalSize.Value;
                if (dof.Aperture.HasValue) si.depthAperture = dof.Aperture.Value;
            }

            var vignette = patch.Vignette;
            if (vignette != null)
            {
                if (vignette.Enabled.HasValue) si.enableVignette = vignette.Enabled.Value;
                if (vignette.Intensity.HasValue) si.vignetteIntensity = vignette.Intensity.Value;
            }

            if (patch.ScreenSpaceReflections != null && patch.ScreenSpaceReflections.Enabled.HasValue)
                si.enableSSR = patch.ScreenSpaceReflections.Enabled.Value;

            var probe = patch.ReflectionProbe;
            if (probe != null)
            {
                if (probe.Enabled.HasValue) si.enableReflectionProbe = probe.Enabled.Value;
                if (probe.Cubemap.HasValue) si.reflectionProbeCubemap = probe.Cubemap.Value;
                if (probe.Intensity.HasValue) si.reflectionProbeIntensity = probe.Intensity.Value;
            }

            var fog = patch.Fog;
            if (fog != null)
            {
                if (fog.Enabled.HasValue) si.enableFog = fog.Enabled.Value;
                if (fog.ExcludeFarPixels.HasValue) si.fogExcludeFarPixels = fog.ExcludeFarPixels.Value;
                if (fog.Height.HasValue) si.fogHeight = fog.Height.Value;
                if (fog.HeightDensity.HasValue) si.fogHeightDensity = fog.HeightDensity.Value;
                if (fog.Color != null) si.fogColor = Col(fog.Color);
                if (fog.Density.HasValue) si.fogDensity = fog.Density.Value;
            }

            var sun = patch.SunShafts;
            if (sun != null)
            {
                if (sun.Enabled.HasValue) si.enableSunShafts = sun.Enabled.Value;
                if (sun.ThresholdColor != null) si.sunThresholdColor = Col(sun.ThresholdColor);
                if (sun.Color != null) si.sunColor = Col(sun.Color);
                if (sun.DistanceFalloff.HasValue) si.sunDistanceFallOff = sun.DistanceFalloff.Value;
                if (sun.BlurSize.HasValue) si.sunBlurSize = sun.BlurSize.Value;
                if (sun.Intensity.HasValue) si.sunIntensity = sun.Intensity.Value;
            }

            // SelfShadowInfo does not touch the scene at all: it moves the global
            // quality level by one step, so this flag outlives the scene it came from.
            if (patch.SelfShadow != null && patch.SelfShadow.Enabled.HasValue)
                si.enableShadow = patch.SelfShadow.Enabled.Value;

            var env = patch.EnvironmentLighting;
            if (env != null)
            {
                if (env.SkyColor != null) si.environmentLightingSkyColor = Col(env.SkyColor);
                if (env.EquatorColor != null) si.environmentLightingEquatorColor = Col(env.EquatorColor);
                if (env.GroundColor != null) si.environmentLightingGroundColor = Col(env.GroundColor);
            }

            var sky = patch.Sky;
            if (sky != null)
            {
                if (sky.Enabled.HasValue) si.skyInfo.Enable = sky.Enabled.Value;
                if (sky.Pattern.HasValue) si.skyInfo.Pattern = sky.Pattern.Value;
            }

            // Init is a no-op once done and resolves the image effects living on
            // Camera.main, which SetSunCaster dereferences without a guard.
            panel.Init();
            if (focus.HasValue) studio.SetDepthOfFieldForcus(focus.Value);
            // Unlike the depth-of-field focus, the sun caster is not restored by
            // SunShaftsInfo.Apply — only SetSunCaster ever writes sunShafts.sunTransform.
            if (caster.HasValue) studio.SetSunCaster(caster.Value);
            // UpdateInfo pushes every effect from sceneInfo into its component and
            // refreshes the panel's widgets, exactly as Studio does after a scene load.
            panel.UpdateInfo();
            return GetPostProcessing();
        }

        // SetDepthOfFieldForcus and SetSunCaster accept any key and quietly fall back
        // to -1 for anything that is not an item, so the refusal has to happen here.
        private static int RequireItemTarget(int id, string param)
        {
            if (id == -1) return -1;
            var oci = SNeo.Studio.GetCtrlInfo(id);
            if (oci == null)
                throw new StudioApiException(StudioApiException.NotFound,
                    "no scene object with id " + id + " for '" + param + "' (-1 means none)");
            if (!(oci is SNeo.OCIItem))
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'" + param + "' must be the id of an item (kind='item') or -1 for none; object " + id + " is a " + KindOf(oci));
            return id;
        }

        private static void RequireCatalogKey<T>(Dictionary<int, T> catalog, int no, string name)
        {
            if (catalog == null || !catalog.ContainsKey(no))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no " + name + " entry with no=" + no + " (use studio_search_catalog with catalog='" + name + "')");
        }

        private static float[] C(Color c) => new[] { c.r, c.g, c.b, c.a };

        private static Color Col(float[] rgba) => new Color(rgba[0], rgba[1], rgba[2], rgba[3]);

        private static SNeo.GuideCommand.EqualsInfo Equals(int key, Vector3 oldValue, Vector3 newValue) =>
            new SNeo.GuideCommand.EqualsInfo { dicKey = key, oldValue = oldValue, newValue = newValue };

        private static void ApplyLight(SNeo.OCILight oci, LightPatch patch)
        {
            if (patch == null) return;
            if (patch.Color != null) oci.SetColor(new Color(patch.Color[0], patch.Color[1], patch.Color[2], patch.Color[3]));
            if (patch.Intensity.HasValue) oci.SetIntensity(patch.Intensity.Value, true);
            if (patch.Range.HasValue) oci.SetRange(patch.Range.Value, true);
            if (patch.SpotAngle.HasValue) oci.SetSpotAngle(patch.SpotAngle.Value, true);
            if (patch.Enabled.HasValue) oci.SetEnable(patch.Enabled.Value, true);
            if (patch.Shadow.HasValue) oci.SetShadow(patch.Shadow.Value, true);
        }

        private static SNeo.ObjectCtrlInfo RequireObject(int id)
        {
            Require();
            var oci = SNeo.Studio.GetCtrlInfo(id);
            if (oci == null)
                throw new StudioApiException(StudioApiException.NotFound, "no scene object with id " + id);
            return oci;
        }

        private static ObjectDto ToDto(int id, SNeo.ObjectCtrlInfo oci) => new ObjectDto
        {
            Id = id,
            Kind = KindOf(oci),
            Name = SafeName(oci),
            Visible = SafeVisible(oci),
            ParentId = SafeParentId(oci),
            AttachPointIndex = SafeAttachPointIndex(oci),
            AttachPointName = SafeAttachPointName(oci),
            Transform = SafeTransform(oci),
            Light = SafeLight(oci as SNeo.OCILight),
        };

        private static LightDto SafeLight(SNeo.OCILight oci)
        {
            if (oci == null) return null;
            try
            {
                var li = oci.lightInfo;
                return new LightDto
                {
                    No = li.no,
                    Type = oci.lightType.ToString(),
                    Target = oci.lightTarget.ToString(),
                    Color = new[] { li.color.r, li.color.g, li.color.b, li.color.a },
                    Intensity = li.intensity,
                    Range = li.range,
                    SpotAngle = li.spotAngle,
                    Enabled = li.enable,
                    Shadow = li.shadow,
                };
            }
            catch { return null; }
        }

        private static string GroupName(SNeo.Info info, int group)
        {
            SNeo.Info.GroupInfo gi;
            return info.dicItemGroupCategory.TryGetValue(group, out gi) ? gi.name : null;
        }

        private static string CategoryName(SNeo.Info info, int group, int category)
        {
            SNeo.Info.GroupInfo gi;
            SNeo.Info.CategoryInfo ci;
            if (info.dicItemGroupCategory.TryGetValue(group, out gi) && gi.dicCategory.TryGetValue(category, out ci))
                return ci.name;
            return null;
        }

        private static bool Matches(string query, params string[] fields)
        {
            if (string.IsNullOrEmpty(query)) return true;
            foreach (var f in fields)
                if (f != null && f.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static string SceneRoot() => Path.Combine(UserData.Path, "Studio\\scene");

        // UserData.Path is built from Application.dataPath + "/../", so SceneRoot is
        // not normalised and cannot be used as a prefix on its own.
        private static string SceneRootPrefix() => Path.GetFullPath(SceneRoot()).TrimEnd('\\') + "\\";

        private static string SceneRelative(string full)
        {
            var prefix = SceneRootPrefix();
            return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? full.Substring(prefix.Length).Replace('\\', '/')
                : full.Replace('\\', '/');
        }

        // Studio's own naming (Studio.SaveScene): millisecond-stamped, never collides.
        private static string DefaultSceneName()
        {
            var now = DateTime.Now;
            return string.Format("{0}_{1:00}{2:00}_{3:00}{4:00}_{5:00}_{6:000}.png",
                now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
        }

        // Hardcoded at the CreatePngScreen call site inside SceneInfo.Save.
        private const int ThumbnailWidth = 320;
        private const int ThumbnailHeight = 180;

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private static bool HasPngHeader(string path)
        {
            var head = new byte[PngHeader.Length];
            using (var stream = File.OpenRead(path))
                if (stream.Read(head, 0, head.Length) != head.Length) return false;
            for (var i = 0; i < head.Length; i++)
                if (head[i] != PngHeader[i]) return false;
            return true;
        }

        private static string CharaRoot() => Path.Combine(UserData.Path, "chara");

        private static string PoseRoot() => UserData.Create(SNeo.PauseCtrl.savePath);

        private static string CoordinateRoot() => Path.Combine(UserData.Path, "coordinate");

        // Reading a card means deserialising it (Studio does the same in
        // CharaList.InitFemaleList); the result is cached per file+timestamp so a
        // full 1000-card sweep is only paid on the first call.
        private sealed class Cached<T> where T : class
        {
            public long Stamp;
            public T Dto;
        }

        private static readonly Dictionary<string, Cached<CharacterCardDto>> CardCache = new Dictionary<string, Cached<CharacterCardDto>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Cached<PoseFileDto>> PoseCache = new Dictionary<string, Cached<PoseFileDto>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Cached<CoordinateFileDto>> CoordinateCache = new Dictionary<string, Cached<CoordinateFileDto>>(StringComparer.OrdinalIgnoreCase);

        private static CharacterCardDto ReadCard(string file, string folder)
        {
            var stamp = File.GetLastWriteTimeUtc(file).Ticks;
            Cached<CharacterCardDto> cached;
            if (CardCache.TryGetValue(file, out cached) && cached.Stamp == stamp)
                return cached.Dto;

            CharacterCardDto dto = null;
            try
            {
                var chaFile = new AIChara.ChaFileControl();
                if (chaFile.LoadCharaFile(file, folder == "male" ? (byte)0 : (byte)1, true))
                {
                    dto = new CharacterCardDto
                    {
                        Path = folder + "/" + file.Substring(Path.Combine(CharaRoot(), folder).Length).TrimStart('\\', '/').Replace('\\', '/'),
                        Name = chaFile.parameter != null ? chaFile.parameter.fullname : null,
                        Sex = folder,
                        Modified = new DateTime(stamp, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                    };
                }
            }
            catch { dto = null; }

            CardCache[file] = new Cached<CharacterCardDto> { Stamp = stamp, Dto = dto };
            return dto;
        }

        private static PoseFileDto ReadPose(string file, string root)
        {
            var stamp = File.GetLastWriteTimeUtc(file).Ticks;
            Cached<PoseFileDto> cached;
            if (PoseCache.TryGetValue(file, out cached) && cached.Stamp == stamp)
                return cached.Dto;

            PoseFileDto dto = null;
            try
            {
                // CheckIdentifyingCode validates the 【pose】 header and the sex in one
                // pass, so failing both means the file is not a Studio pose file.
                string sex = null;
                if (SNeo.PauseCtrl.CheckIdentifyingCode(file, 1)) sex = "female";
                else if (SNeo.PauseCtrl.CheckIdentifyingCode(file, 0)) sex = "male";
                if (sex != null)
                {
                    dto = new PoseFileDto
                    {
                        Path = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/'),
                        Name = SNeo.PauseCtrl.LoadName(file),
                        Sex = sex,
                    };
                }
            }
            catch { dto = null; }

            PoseCache[file] = new Cached<PoseFileDto> { Stamp = stamp, Dto = dto };
            return dto;
        }

        private static CoordinateFileDto ReadCoordinate(string file, string folder)
        {
            var stamp = File.GetLastWriteTimeUtc(file).Ticks;
            Cached<CoordinateFileDto> cached;
            if (CoordinateCache.TryGetValue(file, out cached) && cached.Stamp == stamp)
                return cached.Dto;

            CoordinateFileDto dto = null;
            try
            {
                var name = ReadCoordinateName(file);
                if (name != null)
                    dto = new CoordinateFileDto
                    {
                        Path = folder + "/" + file.Substring(Path.Combine(CoordinateRoot(), folder).Length).TrimStart('\\', '/').Replace('\\', '/'),
                        Name = name,
                        Sex = folder,
                        Modified = new DateTime(stamp, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                    };
            }
            catch { dto = null; }

            CoordinateCache[file] = new Cached<CoordinateFileDto> { Stamp = stamp, Dto = dto };
            return dto;
        }

        // Studio's own outfit list deserialises every file through ChaFileCoordinate.LoadFile
        // just to read its name (MPCharCtrl.CostumeInfo.InitFileList), which is two thousand
        // MessagePack payloads for one string. This reads the same header LoadFile reads and
        // stops before the payload; null means the file is not a coordinate this build can load.
        private static string ReadCoordinateName(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                PngFile.SkipPng(reader);
                if (reader.BaseStream.Length - reader.BaseStream.Position == 0L) return null;
                if (reader.ReadInt32() > 100) return null;
                if (reader.ReadString() != "【AIS_Clothes】") return null;
                if (new Version(reader.ReadString()) > AIChara.ChaFileDefine.ChaFileClothesVersion) return null;
                reader.ReadInt32();
                return reader.ReadString();
            }
        }

        public static string ResolvePosePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'path' must be a non-empty string");
            var candidate = Path.IsPathRooted(path) ? path : Path.Combine(PoseRoot(), path.Replace('/', '\\'));
            if (!File.Exists(candidate) && !candidate.EndsWith(SNeo.PauseCtrl.saveExtension, StringComparison.OrdinalIgnoreCase))
                candidate += SNeo.PauseCtrl.saveExtension;
            if (!File.Exists(candidate))
                throw new StudioApiException(StudioApiException.NotFound, "pose file not found: " + candidate + " (use studio_list_poses to see available poses)");
            return candidate;
        }

        // Relative card paths carry their sex as the first segment ("female/x.png"),
        // which is what studio_list_character_cards returns; 'sex' is only needed for
        // an absolute path outside UserData\chara.
        private static string ResolveCardPath(string card, string sex, out int cardSex)
        {
            if (string.IsNullOrEmpty(card))
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'card' must be a non-empty string");
            if (sex != null && sex != "female" && sex != "male")
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'sex' must be 'female' or 'male'");

            var normalized = card.Replace('/', '\\');
            var folder = sex;
            if (folder == null)
            {
                if (normalized.StartsWith("female\\", StringComparison.OrdinalIgnoreCase)) folder = "female";
                else if (normalized.StartsWith("male\\", StringComparison.OrdinalIgnoreCase)) folder = "male";
                else
                {
                    var parent = Path.GetFileName(Path.GetDirectoryName(normalized));
                    if (string.Equals(parent, "female", StringComparison.OrdinalIgnoreCase)) folder = "female";
                    else if (string.Equals(parent, "male", StringComparison.OrdinalIgnoreCase)) folder = "male";
                }
            }
            if (folder == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "cannot tell the sex of card '" + card + "': pass a path starting with 'female/' or 'male/' (as returned by studio_list_character_cards) or set 'sex'");

            var candidate = Path.IsPathRooted(normalized) ? normalized : Path.Combine(CharaRoot(), normalized);
            if (!File.Exists(candidate) && !candidate.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                candidate += ".png";
            if (!File.Exists(candidate))
                throw new StudioApiException(StudioApiException.NotFound, "character card not found: " + candidate + " (use studio_list_character_cards to see available cards)");

            cardSex = folder == "male" ? 0 : 1;
            return candidate;
        }

        private static SNeo.OCIChar RequireCharacter(int id)
        {
            var chr = RequireObject(id) as SNeo.OCIChar;
            if (chr == null)
                throw new StudioApiException(StudioApiException.InvalidParams, "object " + id + " is not a character");
            return chr;
        }

        private static string SexName(int sex) => sex == 0 ? "male" : "female";

        // OCIChar.HandAnimeNum dereferences ChaControl.sibHand without a guard, so it
        // throws until the shape bundle is initialised. -1 means "unknown".
        private static int HandAnimeCount(SNeo.OCIChar chr)
        {
            try { return chr.HandAnimeNum; }
            catch { return -1; }
        }

        // 0 turns the hand shape off, 1..count select a shape (ChangeHandAnime passes
        // ptn-1 to the shape list). Studio's own panel offers the same count+1 values.
        private static void RequireHandPtn(int? ptn, int count, string param)
        {
            if (!ptn.HasValue) return;
            if (ptn.Value < 0 || ptn.Value > count)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "parameter '" + param + "' must be 0.." + count + " for this character (0 disables the hand shape)");
        }

        private static void RequireRange(float? value, float min, float max, string param)
        {
            if (!value.HasValue) return;
            if (value.Value < min || value.Value > max || float.IsNaN(value.Value))
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "parameter '" + param + "' must be " + min.ToString("0.##") + ".." + max.ToString("0.##"));
        }

        // MPCharCtrl.NeckInfo maps its four buttons onto these neckLookPtn values.
        private static readonly int[] NeckPatterns = { 0, 1, 3, 4 };

        // Eye/eyebrow patterns are asset ids, not contiguous indices; the Studio UI
        // (MPCharCtrl.EtcInfo) shows the position in the category list and maps it
        // through the key array, so agents get to use that same index space.
        private static int[] PatternKeys(SNeo.OCIChar chr, bool eyebrows)
        {
            var listCtrl = Singleton<Manager.Character>.Instance.chaListCtrl;
            var category = chr.sex == 0
                ? (eyebrows ? AIChara.ChaListDefine.CategoryNo.custom_eyebrow_m : AIChara.ChaListDefine.CategoryNo.custom_eye_m)
                : (eyebrows ? AIChara.ChaListDefine.CategoryNo.custom_eyebrow_f : AIChara.ChaListDefine.CategoryNo.custom_eye_f);
            var info = listCtrl != null ? listCtrl.GetCategoryInfo(category) : null;
            if (info == null) return new int[0];
            var keys = new int[info.Count];
            info.Keys.CopyTo(keys, 0);
            return keys;
        }

        private static int PatternKey(SNeo.OCIChar chr, bool eyebrows, int index, string param)
        {
            var keys = PatternKeys(chr, eyebrows);
            if (keys.Length == 0)
                throw new StudioApiException(StudioApiException.OperationFailed, "the " + param + " list is not loaded");
            if (index < 0 || index >= keys.Length)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "parameter '" + param + "' must be 0.." + (keys.Length - 1) + " for this character");
            return keys[index];
        }

        private static string AnimeGroupName(SNeo.Info info, int group)
        {
            SNeo.Info.GroupInfo gi;
            return info.dicAGroupCategory.TryGetValue(group, out gi) ? gi.name : null;
        }

        private static string AnimeCategoryName(SNeo.Info info, int group, int category)
        {
            SNeo.Info.GroupInfo gi;
            SNeo.Info.CategoryInfo ci;
            if (info.dicAGroupCategory.TryGetValue(group, out gi) && gi.dicCategory.TryGetValue(category, out ci))
                return ci.name;
            return null;
        }

        private static CharacterDto ToCharacterDto(int id, SNeo.OCIChar chr) => new CharacterDto
        {
            Id = id,
            Name = SafeName(chr),
            FullName = SafeFullName(chr),
            Sex = SafeSex(chr),
            Visible = SafeVisible(chr),
            Transform = SafeTransform(chr),
            Anime = SafeAnime(chr),
            Kinematic = SafeKinematic(chr),
            Face = SafeFace(chr),
            Hands = SafeHands(chr),
            Outfit = SafeOutfit(chr),
            State = SafeState(chr),
            Expression = SafeExpression(chr),
        };

        private static AnimeDto SafeAnime(SNeo.OCIChar chr)
        {
            try
            {
                var ai = chr.oiCharInfo.animeInfo;
                var dto = new AnimeDto
                {
                    Group = ai.group,
                    Category = ai.category,
                    No = ai.no,
                    NormalizedTime = chr.charAnimeCtrl != null ? chr.charAnimeCtrl.normalizedTime : 0f,
                    Speed = chr.animeSpeed,
                    Pattern = chr.animePattern,
                    OptionParam1 = chr.animeOptionParam1,
                    OptionParam2 = chr.animeOptionParam2,
                    IsAnimeMotion = chr.isAnimeMotion,
                    IsHAnime = chr.isHAnime,
                };
                var info = Singleton<SNeo.Info>.Instance;
                Dictionary<int, Dictionary<int, SNeo.Info.AnimeLoadInfo>> categories;
                Dictionary<int, SNeo.Info.AnimeLoadInfo> animes;
                SNeo.Info.AnimeLoadInfo entry;
                if (info != null && info.dicAnimeLoadInfo.TryGetValue(ai.group, out categories)
                    && categories.TryGetValue(ai.category, out animes) && animes.TryGetValue(ai.no, out entry))
                {
                    dto.Name = entry.name;
                    dto.GroupName = AnimeGroupName(info, ai.group);
                    dto.CategoryName = AnimeCategoryName(info, ai.group, ai.category);
                }
                return dto;
            }
            catch { return null; }
        }

        private static KinematicDto SafeKinematic(SNeo.OCIChar chr)
        {
            try
            {
                var oi = chr.oiCharInfo;
                return new KinematicDto
                {
                    // oiCharInfo.kinematicMode is dead data: OICharInfo.Load is its
                    // only writer in the whole assembly, so it keeps reporting what
                    // the scene file held after any ActiveKinematicMode call. The
                    // live state is enableIK/enableFK, which are mutually exclusive.
                    Mode = oi.enableIK ? "ik" : oi.enableFK ? "fk" : "none",
                    Ik = oi.enableIK,
                    Fk = oi.enableFK,
                    ActiveIk = (bool[])oi.activeIK.Clone(),
                    ActiveFk = (bool[])oi.activeFK.Clone(),
                };
            }
            catch { return null; }
        }

        private static FaceDto SafeFace(SNeo.OCIChar chr)
        {
            try
            {
                var status = chr.charFileStatus;
                var eyebrowKeys = PatternKeys(chr, true);
                var eyeKeys = PatternKeys(chr, false);
                return new FaceDto
                {
                    Eyebrows = Array.IndexOf(eyebrowKeys, chr.charInfo.GetEyebrowPtn()),
                    EyebrowsCount = eyebrowKeys.Length,
                    Eyes = Array.IndexOf(eyeKeys, chr.charInfo.GetEyesPtn()),
                    EyesCount = eyeKeys.Length,
                    EyesOpen = status.eyesOpenMax,
                    Blink = status.eyesBlink,
                    Mouth = chr.charInfo.GetMouthPtn(),
                    MouthOpen = chr.oiCharInfo.mouthOpen,
                    LipSync = chr.oiCharInfo.lipSync,
                    Tears = chr.GetTears(),
                    Blush = chr.GetHohoAkaRate(),
                    LookEyes = status.eyesLookPtn,
                    LookNeck = status.neckLookPtn,
                };
            }
            catch { return null; }
        }

        private static HandsDto SafeHands(SNeo.OCIChar chr)
        {
            try
            {
                var ptn = chr.oiCharInfo.handPtn;
                return new HandsDto { Left = ptn[0], Right = ptn[1], Count = HandAnimeCount(chr) };
            }
            catch { return null; }
        }

        private static bool[] SafeExpression(SNeo.OCIChar chr)
        {
            try { return (bool[])chr.oiCharInfo.expression.Clone(); }
            catch { return null; }
        }

        private static string ReadGameVersionFile()
        {
            try
            {
                var path = Path.Combine(BepInEx.Paths.GameRootPath, "version");
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
            catch { return null; }
        }

        private static string KindOf(SNeo.ObjectCtrlInfo oci)
        {
            if (oci is SNeo.OCIChar) return "character";
            if (oci is SNeo.OCIItem) return "item";
            if (oci is SNeo.OCILight) return "light";
            if (oci is SNeo.OCIFolder) return "folder";
            if (oci is SNeo.OCIRoute) return "route";
            if (oci is SNeo.OCICamera) return "camera";
            return "unknown";
        }

        private static string SafeName(SNeo.ObjectCtrlInfo oci)
        {
            try { return oci.treeNodeObject != null ? oci.treeNodeObject.textName : null; }
            catch { return null; }
        }

        private static bool SafeVisible(SNeo.ObjectCtrlInfo oci)
        {
            try { return oci.treeNodeObject == null || oci.treeNodeObject.visible; }
            catch { return true; }
        }

        private static int? SafeParentId(SNeo.ObjectCtrlInfo oci)
        {
            try
            {
                if (oci.parentInfo != null && oci.parentInfo.objectInfo != null)
                    return oci.parentInfo.objectInfo.dicKey;
            }
            catch { }
            return null;
        }

        // OCIChar.OnDetachChild identifies an attached object the same way: by
        // scanning oiCharInfo.child, which is the only record of which point holds it.
        private static int? SafeAttachPointIndex(SNeo.ObjectCtrlInfo oci)
        {
            try
            {
                var parent = oci.parentInfo as SNeo.OCIChar;
                if (parent == null) return null;
                foreach (var kv in parent.oiCharInfo.child)
                    if (kv.Value != null && kv.Value.Contains(oci.objectInfo)) return kv.Key;
            }
            catch { }
            return null;
        }

        private static string SafeAttachPointName(SNeo.ObjectCtrlInfo oci)
        {
            var index = SafeAttachPointIndex(oci);
            return index.HasValue ? AttachPointName(index.Value) : null;
        }

        private static TransformDto SafeTransform(SNeo.ObjectCtrlInfo oci)
        {
            try
            {
                var ca = oci.objectInfo.changeAmount;
                return new TransformDto { Pos = V(ca.pos), Rot = V(ca.rot), Scale = V(ca.scale) };
            }
            catch { return null; }
        }

        private static string SafeFullName(SNeo.OCIChar chr)
        {
            try
            {
                var charFile = chr.oiCharInfo != null ? chr.oiCharInfo.charFile : null;
                return charFile != null && charFile.parameter != null ? charFile.parameter.fullname : null;
            }
            catch { return null; }
        }

        private static string SafeSex(SNeo.OCIChar chr)
        {
            try
            {
                var sex = Convert.ToInt32(chr.sex);
                return sex == 0 ? "male" : "female";
            }
            catch { return "unknown"; }
        }

        private static Vec3 V(Vector3 v) => new Vec3(v.x, v.y, v.z);

        private static Vector3 U(Vec3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
