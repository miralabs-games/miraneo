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

        public static SceneSummaryDto GetScene()
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
            else
            {
                throw new StudioApiException(StudioApiException.InvalidParams, "unknown catalog: " + catalog + " (expected 'item', 'light' or 'anime')");
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
                Scene = GetScene(),
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

        public static CharacterDto SetCharacterAnime(int id, int group, int category, int no, float? normalizedTime)
        {
            var chr = RequireCharacter(id);
            var info = Singleton<SNeo.Info>.Instance;
            Dictionary<int, Dictionary<int, SNeo.Info.AnimeLoadInfo>> categories;
            Dictionary<int, SNeo.Info.AnimeLoadInfo> animes;
            if (info == null || !info.dicAnimeLoadInfo.TryGetValue(group, out categories)
                || !categories.TryGetValue(category, out animes) || !animes.ContainsKey(no))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no animation with group=" + group + " category=" + category + " no=" + no + " (use studio_search_catalog with catalog='anime')");

            chr.LoadAnime(group, category, no, normalizedTime.HasValue ? normalizedTime.Value : 0f);
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
