using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MiraStudio.Core;
using UnityEngine;
using SNeo = global::Studio;

namespace MiraStudio.Adapters
{
    // Timeline (joan6694, com.joan6694.illusionplugins.timeline 1.4.1) ships no
    // reference assembly, so every member is resolved by reflection at first use.
    // Binding is all-or-nothing, like Timeline's own TimelineCompatibility.Init:
    // one missing member and the bridge reports itself unavailable instead of
    // half-working. Everything here runs on the Unity main thread.
    internal static class TimelineAdapter
    {
        public const string Guid = "com.joan6694.illusionplugins.timeline";

        private const BindingFlags Inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags Stat = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static bool _tried;
        private static string _error;
        private static string _version;

        private static Type _tTimeline, _tInterpolable, _tModel, _tKeyframe;
        private static FieldInfo _fSelf, _fSingleFilesFolder;
        private static PropertyInfo _pPlaybackTime, _pDuration, _pIsPlaying;
        private static MethodInfo _mPlay, _mPause, _mStop, _mSeek, _mNextFrame, _mPrevFrame;
        private static FieldInfo _fInterpolables, _fTree, _fModels, _fSelectedOci, _fDuration, _fFrameRate;
        private static MethodInfo _mUpdateGrid, _mUpdateView, _mRemoveInterpolable, _mLoadSingle, _mSaveSingle;
        private static MethodInfo _mTreeAddLeaf;
        private static ConstructorInfo _ctorInterpolable2, _ctorInterpolable3, _ctorKeyframe;
        private static FieldInfo _fKeyframes, _fEnabled, _fAlias, _fOci;
        private static PropertyInfo _pInterpolableName;
        private static MethodInfo _mGetValue;
        private static FieldInfo _fOwner, _fModelId, _fParameter, _fGetParameter;
        private static PropertyInfo _pModelName;
        private static MethodInfo _mIsCompatible;
        private static FieldInfo _fKeyValue, _fKeyCurve;

        public static bool Available
        {
            get
            {
                Bind();
                return _error == null && _fSelf.GetValue(null) != null;
            }
        }

        // Reported by studio_get_info so an agent can tell "no Timeline installed"
        // from "installed but its UI has not finished loading" before calling.
        public static string Version
        {
            get
            {
                Bind();
                return _error == null ? _version : null;
            }
        }

        private static object Require()
        {
            Bind();
            if (_error != null)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "the Timeline plugin (" + Guid + ") is not available: " + _error);
            var self = _fSelf.GetValue(null);
            if (self == null)
                throw new StudioApiException(StudioApiException.StudioNotReady,
                    "Timeline " + _version + " is loaded but has not initialised yet; open a Studio scene and try again");
            return self;
        }

        private static void Bind()
        {
            if (_tried) return;
            _tried = true;
            try
            {
                _tTimeline = FindType("Timeline.Timeline");
                if (_tTimeline == null)
                {
                    _error = "assembly not loaded (plugin not installed)";
                    return;
                }
                var asm = _tTimeline.Assembly;
                _tInterpolable = asm.GetType("Timeline.Interpolable", true);
                _tModel = asm.GetType("Timeline.InterpolableModel", true);
                _tKeyframe = asm.GetType("Timeline.Keyframe", true);
                _version = Convert.ToString(Field(_tTimeline, "Version", Stat).GetRawConstantValue());

                _fSelf = Field(_tTimeline, "_self", Stat);
                _fSingleFilesFolder = Field(_tTimeline, "_singleFilesFolder", Stat);
                _pPlaybackTime = Prop(_tTimeline, "playbackTime", Stat);
                _pDuration = Prop(_tTimeline, "duration", Stat);
                _pIsPlaying = Prop(_tTimeline, "isPlaying", Stat);
                _mPlay = Method(_tTimeline, "Play", Stat, Type.EmptyTypes);
                _mPause = Method(_tTimeline, "Pause", Stat, Type.EmptyTypes);
                _mStop = Method(_tTimeline, "Stop", Stat, Type.EmptyTypes);
                _mSeek = Method(_tTimeline, "Seek", Stat, new[] { typeof(float) });
                _mNextFrame = Method(_tTimeline, "NextFrame", Stat, Type.EmptyTypes);
                _mPrevFrame = Method(_tTimeline, "PreviousFrame", Stat, Type.EmptyTypes);

                _fInterpolables = Field(_tTimeline, "_interpolables", Inst);
                _fTree = Field(_tTimeline, "_interpolablesTree", Inst);
                _fModels = Field(_tTimeline, "_interpolableModelsList", Inst);
                _fSelectedOci = Field(_tTimeline, "_selectedOCI", Inst);
                _fDuration = Field(_tTimeline, "_duration", Inst);
                _fFrameRate = Field(_tTimeline, "_desiredFrameRate", Inst);
                _mUpdateGrid = Method(_tTimeline, "UpdateGrid", Inst, Type.EmptyTypes);
                _mUpdateView = Method(_tTimeline, "UpdateInterpolablesView", Inst, Type.EmptyTypes);
                _mRemoveInterpolable = Method(_tTimeline, "RemoveInterpolable", Inst, new[] { _tInterpolable });
                _mLoadSingle = Method(_tTimeline, "LoadSingle", Inst, new[] { typeof(string) });
                _mSaveSingle = Method(_tTimeline, "SaveSingle", Inst, new[] { typeof(string) });
                _mTreeAddLeaf = MethodByName(_fTree.FieldType, "AddLeaf");

                _ctorInterpolable2 = Ctor(_tInterpolable, new[] { typeof(SNeo.ObjectCtrlInfo), _tModel });
                _ctorInterpolable3 = Ctor(_tInterpolable, new[] { typeof(SNeo.ObjectCtrlInfo), typeof(object), _tModel });
                _ctorKeyframe = Ctor(_tKeyframe, new[] { typeof(object), _tInterpolable, typeof(AnimationCurve) });

                _fKeyframes = Field(_tInterpolable, "keyframes", Inst);
                _fEnabled = Field(_tInterpolable, "enabled", Inst);
                _fAlias = Field(_tInterpolable, "alias", Inst);
                _fOci = Field(_tInterpolable, "oci", Inst);
                _pInterpolableName = Prop(_tInterpolable, "name", Inst);
                _mGetValue = Method(_tInterpolable, "GetValue", Inst, Type.EmptyTypes);

                _fOwner = Field(_tModel, "owner", Inst);
                _fModelId = Field(_tModel, "id", Inst);
                _fParameter = Field(_tModel, "parameter", Inst);
                _fGetParameter = Field(_tModel, "_getParameter", Inst);
                _pModelName = Prop(_tModel, "name", Inst);
                _mIsCompatible = Method(_tModel, "IsCompatibleWithTarget", Inst, new[] { typeof(SNeo.ObjectCtrlInfo) });

                _fKeyValue = Field(_tKeyframe, "value", Inst);
                _fKeyCurve = Field(_tKeyframe, "curve", Inst);
            }
            catch (Exception e)
            {
                _error = e.Message;
            }
        }

        private static Type FindType(string name)
        {
            var t = Type.GetType(name + ",Timeline", false);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(name, false); }
                catch { continue; }
                if (t != null) return t;
            }
            return null;
        }

        private static FieldInfo Field(Type t, string name, BindingFlags flags)
        {
            var f = t.GetField(name, flags);
            if (f == null) throw new MissingMemberException("field " + t.Name + "." + name + " is gone (unsupported Timeline version)");
            return f;
        }

        private static PropertyInfo Prop(Type t, string name, BindingFlags flags)
        {
            var p = t.GetProperty(name, flags);
            if (p == null) throw new MissingMemberException("property " + t.Name + "." + name + " is gone (unsupported Timeline version)");
            return p;
        }

        private static MethodInfo Method(Type t, string name, BindingFlags flags, Type[] args)
        {
            var m = t.GetMethod(name, flags, null, args, null);
            if (m == null) throw new MissingMemberException("method " + t.Name + "." + name + " is gone (unsupported Timeline version)");
            return m;
        }

        private static MethodInfo MethodByName(Type t, string name)
        {
            var m = t.GetMethod(name);
            if (m == null) throw new MissingMemberException("method " + t.Name + "." + name + " is gone (unsupported Timeline version)");
            return m;
        }

        private static ConstructorInfo Ctor(Type t, Type[] args)
        {
            var c = t.GetConstructor(Inst, null, args, null);
            if (c == null) throw new MissingMemberException("constructor " + t.Name + " is gone (unsupported Timeline version)");
            return c;
        }

        // ---- state -------------------------------------------------------

        public static TimelineStateDto GetState(int? objectId, bool withKeyframes)
        {
            Bind();
            var dto = new TimelineStateDto { Available = false, Unavailable = _error, Version = _version };
            if (_error != null) return dto;
            var self = _fSelf.GetValue(null);
            if (self == null)
            {
                dto.Unavailable = "Timeline is loaded but has not initialised yet";
                return dto;
            }

            dto.Available = true;
            dto.IsPlaying = (bool)_pIsPlaying.GetValue(null, null);
            dto.PlaybackTime = (float)_pPlaybackTime.GetValue(null, null);
            dto.Duration = (float)_pDuration.GetValue(null, null);
            dto.FrameRate = (int)_fFrameRate.GetValue(self);
            dto.Interpolables = new List<TimelineInterpolableDto>();

            SNeo.ObjectCtrlInfo filter = null;
            if (objectId.HasValue) filter = StudioAdapter.RequireObject(objectId.Value);

            foreach (DictionaryEntry entry in (IDictionary)_fInterpolables.GetValue(self))
            {
                var interpolable = entry.Value;
                var oci = (SNeo.ObjectCtrlInfo)_fOci.GetValue(interpolable);
                dto.InterpolableCount++;
                dto.KeyframeCount += ((IDictionary)_fKeyframes.GetValue(interpolable)).Count;
                if (filter != null && !ReferenceEquals(oci, filter)) continue;
                dto.Interpolables.Add(ToDto((int)entry.Key, interpolable, oci, withKeyframes));
            }
            return dto;
        }

        private static TimelineInterpolableDto ToDto(int id, object interpolable, SNeo.ObjectCtrlInfo oci, bool withKeyframes)
        {
            var parameter = _fParameter.GetValue(interpolable);
            var guide = parameter as SNeo.GuideObject;
            var dto = new TimelineInterpolableDto
            {
                Id = id,
                Owner = Convert.ToString(_fOwner.GetValue(interpolable)),
                Model = Convert.ToString(_fModelId.GetValue(interpolable)),
                Name = Convert.ToString(_pInterpolableName.GetValue(interpolable, null)),
                Alias = Convert.ToString(_fAlias.GetValue(interpolable)),
                Enabled = (bool)_fEnabled.GetValue(interpolable),
                ObjectId = FindObjectId(oci),
                Target = guide != null && guide.transformTarget != null ? guide.transformTarget.name : null,
            };
            var keyframes = (IDictionary)_fKeyframes.GetValue(interpolable);
            dto.KeyframeCount = keyframes.Count;
            if (!withKeyframes) return dto;

            dto.Keyframes = new List<TimelineKeyframeDto>(keyframes.Count);
            foreach (DictionaryEntry k in keyframes)
                dto.Keyframes.Add(new TimelineKeyframeDto
                {
                    Time = (float)k.Key,
                    Value = ValueToJson(_fKeyValue.GetValue(k.Value)),
                    Curve = CurveToJson(_fKeyCurve.GetValue(k.Value) as AnimationCurve),
                });
            return dto;
        }

        private static int? FindObjectId(SNeo.ObjectCtrlInfo oci)
        {
            if (oci == null) return null;
            foreach (var kv in Singleton<SNeo.Studio>.Instance.dicObjectCtrl)
                if (ReferenceEquals(kv.Value, oci)) return kv.Key;
            return null;
        }

        // ---- playback ----------------------------------------------------

        public static TimelineStateDto Playback(string action, float? time)
        {
            Require();
            switch (action)
            {
                case "play":
                    // Timeline.Play is a toggle: calling it while playing pauses.
                    if (!(bool)_pIsPlaying.GetValue(null, null)) _mPlay.Invoke(null, null);
                    break;
                case "pause": _mPause.Invoke(null, null); break;
                case "stop": _mStop.Invoke(null, null); break;
                case "next_frame": _mNextFrame.Invoke(null, null); break;
                case "previous_frame": _mPrevFrame.Invoke(null, null); break;
                case "seek":
                    if (!time.HasValue)
                        throw new StudioApiException(StudioApiException.InvalidParams, "action 'seek' needs 'time'");
                    _mSeek.Invoke(null, new object[] { RequireTime(time.Value) });
                    break;
                default:
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "parameter 'action' must be 'play', 'pause', 'stop', 'seek', 'next_frame' or 'previous_frame'");
            }
            return GetState(null, false);
        }

        public static TimelineStateDto SetSettings(float? duration, int? frameRate)
        {
            var self = Require();
            if (duration.HasValue)
            {
                if (duration.Value <= 0f)
                    throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'duration' must be greater than 0");
                _fDuration.SetValue(self, duration.Value);
            }
            if (frameRate.HasValue)
            {
                if (frameRate.Value < 1)
                    throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'frame_rate' must be at least 1");
                _fFrameRate.SetValue(self, frameRate.Value);
            }
            // The grid, the duration field and the scroll width all derive from
            // _duration; Timeline's own UpdateDuration redraws them the same way.
            if (duration.HasValue) _mUpdateGrid.Invoke(self, null);
            return GetState(null, false);
        }

        private static float RequireTime(float time)
        {
            var duration = (float)_pDuration.GetValue(null, null);
            if (time < 0f || time > duration)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "time " + time + " is outside the timeline (0 to " + duration + " s); change it with studio_timeline_set_settings");
            return time;
        }

        // ---- models ------------------------------------------------------

        public static List<TimelineModelDto> ListModels(int? objectId, string query, int limit)
        {
            var self = Require();
            SNeo.ObjectCtrlInfo oci = null;
            if (objectId.HasValue) oci = StudioAdapter.RequireObject(objectId.Value);

            var list = new List<TimelineModelDto>();
            foreach (var model in (IList)_fModels.GetValue(self))
            {
                var dto = new TimelineModelDto
                {
                    Owner = Convert.ToString(_fOwner.GetValue(model)),
                    Model = Convert.ToString(_fModelId.GetValue(model)),
                    Name = Convert.ToString(_pModelName.GetValue(model, null)),
                    Dynamic = _fGetParameter.GetValue(model) != null,
                };
                if (objectId.HasValue)
                {
                    dto.Compatible = (bool)_mIsCompatible.Invoke(model, new object[] { oci });
                    if (!dto.Compatible) continue;
                }
                if (!string.IsNullOrEmpty(query) &&
                    dto.Model.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    dto.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                list.Add(dto);
                if (list.Count >= limit) break;
            }
            return list;
        }

        // ---- interpolables -----------------------------------------------

        public static TimelineInterpolableDto AddInterpolable(int objectId, string modelId, string owner, string name, string target, string alias)
        {
            var self = Require();
            var oci = StudioAdapter.RequireObject(objectId);
            var model = FindModel(self, modelId, owner, name);

            if (!(bool)_mIsCompatible.Invoke(model, new object[] { oci }))
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "model '" + modelId + "' does not apply to object " + objectId +
                    " (list what does with studio_timeline_list_models and object_id)");

            var dynamic = _fGetParameter.GetValue(model) != null;
            object interpolable;
            if (target != null)
            {
                var guide = ResolveGuideObject(oci, objectId, target);
                interpolable = _ctorInterpolable3.Invoke(new object[] { oci, guide, model });
            }
            else
            {
                interpolable = _ctorInterpolable2.Invoke(new object[] { oci, model });
                // A dynamic model resolves its parameter from Studio's current
                // selection; with nothing selected it lands null and checkIntegrity
                // then silently refuses to interpolate the keyframes we would add.
                if (dynamic && _fParameter.GetValue(interpolable) == null)
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "model '" + modelId + "' is per-target: pass 'target' (a bone or IK target from studio_list_bones, or 'root')");
            }

            var interpolables = (IDictionary)_fInterpolables.GetValue(self);
            var id = interpolable.GetHashCode();
            if (interpolables.Contains(id))
            {
                var existing = interpolables[id];
                var oci2 = (SNeo.ObjectCtrlInfo)_fOci.GetValue(existing);
                return ToDto(id, existing, oci2, true);
            }

            if (!string.IsNullOrEmpty(alias)) _fAlias.SetValue(interpolable, alias);
            interpolables.Add(id, interpolable);
            _mTreeAddLeaf.Invoke(_fTree.GetValue(self), new object[] { interpolable, null });
            _mUpdateView.Invoke(self, null);
            return ToDto(id, interpolable, oci, true);
        }

        public static TimelineStateDto RemoveInterpolable(List<int> ids)
        {
            var self = Require();
            var interpolables = (IDictionary)_fInterpolables.GetValue(self);
            var targets = new List<object>(ids.Count);
            foreach (var id in ids)
            {
                if (!interpolables.Contains(id))
                    throw new StudioApiException(StudioApiException.NotFound,
                        "no interpolable " + id + " on the timeline (list them with studio_timeline_get_state)");
                targets.Add(interpolables[id]);
            }
            // RemoveInterpolable also drops the selection entries that would keep a
            // dead interpolable alive in the keyframe window.
            foreach (var t in targets) _mRemoveInterpolable.Invoke(self, new[] { t });
            return GetState(null, false);
        }

        private static object FindModel(object self, string modelId, string owner, string name)
        {
            var matches = new List<object>();
            foreach (var model in (IList)_fModels.GetValue(self))
            {
                if (Convert.ToString(_fModelId.GetValue(model)) != modelId) continue;
                if (owner != null && Convert.ToString(_fOwner.GetValue(model)) != owner) continue;
                if (name != null && Convert.ToString(_pModelName.GetValue(model, null)) != name) continue;
                matches.Add(model);
            }
            if (matches.Count == 1) return matches[0];
            if (matches.Count == 0)
                throw new StudioApiException(StudioApiException.NotFound,
                    "no interpolable model '" + modelId + "'" +
                    (owner != null ? " owned by '" + owner + "'" : "") +
                    (name != null ? " named '" + name + "'" : "") +
                    " (list them with studio_timeline_list_models)");

            // charClothes and juice register one model per slot under a single id,
            // each carrying its own parameter; only the label tells them apart.
            var names = new List<string>(matches.Count);
            foreach (var model in matches) names.Add("'" + Convert.ToString(_pModelName.GetValue(model, null)) + "'");
            throw new StudioApiException(StudioApiException.InvalidParams,
                "model '" + modelId + "' covers " + matches.Count + " properties; pass 'name' to pick one of " +
                string.Join(", ", names.ToArray()));
        }

        // Studio's own guide objects: the character root, any FK bone or any IK
        // target already carries one, which is exactly what guideObject* models
        // animate. Nothing here touches Studio's selection.
        private static SNeo.GuideObject ResolveGuideObject(SNeo.ObjectCtrlInfo oci, int objectId, string target)
        {
            if (target == "root")
            {
                if (oci.guideObject == null)
                    throw new StudioApiException(StudioApiException.OperationFailed, "object " + objectId + " has no guide object");
                return oci.guideObject;
            }

            var chr = oci as SNeo.OCIChar;
            if (chr == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "object " + objectId + " is not a character, so 'target' can only be 'root'");

            SNeo.OCIChar.BoneInfo bone;
            if (StudioAdapter.BoneIndex(chr).TryGetValue(target, out bone))
            {
                if (bone.guideObject == null)
                    throw new StudioApiException(StudioApiException.OperationFailed, "bone '" + target + "' has no guide object");
                return bone.guideObject;
            }

            var index = StudioAdapter.IkTargetIndex(target);
            if (index >= 0 && index < chr.listIKTarget.Count)
            {
                var guide = chr.listIKTarget[index].guideObject;
                if (guide == null)
                    throw new StudioApiException(StudioApiException.OperationFailed, "IK target '" + target + "' has no guide object");
                return guide;
            }

            throw new StudioApiException(StudioApiException.InvalidParams,
                "character " + objectId + " has no bone or IK target '" + target + "' (use studio_list_bones, or 'root' for the object itself)");
        }

        // ---- keyframes ---------------------------------------------------

        public static TimelineInterpolableDto SetKeyframe(int interpolableId, float time, JsonNode value, JsonNode curve)
        {
            var self = Require();
            var interpolable = RequireInterpolable(self, interpolableId);
            RequireTime(time);

            var keyframes = (IDictionary)_fKeyframes.GetValue(interpolable);
            // Omitting the value captures the scene as it stands, which is what
            // Timeline's own AddKeyframe does; it is also the only way to keyframe
            // a value type this bridge cannot express in JSON.
            var current = _mGetValue.Invoke(interpolable, null);
            var newValue = value == null || value is JsonNull
                ? current
                : JsonToValue(value, current != null ? current.GetType() : null, interpolableId);

            var resolved = ResolveCurve(curve, keyframes, time);
            if (keyframes.Contains(time))
            {
                var existing = keyframes[time];
                _fKeyValue.SetValue(existing, newValue);
                if (resolved != null) _fKeyCurve.SetValue(existing, resolved);
            }
            else
            {
                keyframes.Add(time, _ctorKeyframe.Invoke(new object[]
                {
                    newValue, interpolable, resolved ?? AnimationCurve.Linear(0f, 0f, 1f, 1f),
                }));
            }

            _mUpdateGrid.Invoke(self, null);
            return ToDto(interpolableId, interpolable, (SNeo.ObjectCtrlInfo)_fOci.GetValue(interpolable), true);
        }

        public static TimelineInterpolableDto DeleteKeyframe(int interpolableId, float time)
        {
            var self = Require();
            var interpolable = RequireInterpolable(self, interpolableId);
            var keyframes = (IDictionary)_fKeyframes.GetValue(interpolable);
            if (!keyframes.Contains(time))
                throw new StudioApiException(StudioApiException.NotFound,
                    "interpolable " + interpolableId + " has no keyframe at " + time + " s (times must match exactly; read them with studio_timeline_get_state)");
            keyframes.Remove(time);
            _mUpdateGrid.Invoke(self, null);
            return ToDto(interpolableId, interpolable, (SNeo.ObjectCtrlInfo)_fOci.GetValue(interpolable), true);
        }

        private static object RequireInterpolable(object self, int id)
        {
            var interpolables = (IDictionary)_fInterpolables.GetValue(self);
            if (!interpolables.Contains(id))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no interpolable " + id + " on the timeline (list them with studio_timeline_get_state)");
            return interpolables[id];
        }

        // ---- single files ------------------------------------------------

        public static List<TimelineSingleFileDto> ListSingleFiles()
        {
            Require();
            var list = new List<TimelineSingleFileDto>();
            var folder = Convert.ToString(_fSingleFilesFolder.GetValue(null));
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return list;
            foreach (var path in Directory.GetFiles(folder, "*.xml"))
            {
                var info = new FileInfo(path);
                list.Add(new TimelineSingleFileDto
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Size = info.Length,
                    Modified = info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                });
            }
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        public static TimelineStateDto LoadSingleFile(string name, int objectId)
        {
            var self = Require();
            var path = SingleFilePath(name);
            if (!File.Exists(path))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no single file '" + name + "' (list them with action 'list')");
            InSelection(self, StudioAdapter.RequireObject(objectId), () => _mLoadSingle.Invoke(self, new object[] { path }));
            return GetState(objectId, true);
        }

        public static TimelineSingleFileDto SaveSingleFile(string name, int objectId, bool overwrite)
        {
            var self = Require();
            var path = SingleFilePath(name);
            if (File.Exists(path) && !overwrite)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "single file '" + name + "' already exists; pass overwrite=true to replace it");
            var folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            InSelection(self, StudioAdapter.RequireObject(objectId), () => _mSaveSingle.Invoke(self, new object[] { path }));
            var info = new FileInfo(path);
            if (!info.Exists)
                throw new StudioApiException(StudioApiException.OperationFailed, "Timeline did not write '" + name + "'");
            return new TimelineSingleFileDto
            {
                Name = name,
                Size = info.Length,
                Modified = info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
        }

        public static void DeleteSingleFile(string name)
        {
            Require();
            var path = SingleFilePath(name);
            if (!File.Exists(path))
                throw new StudioApiException(StudioApiException.NotFound,
                    "no single file '" + name + "' (list them with action 'list')");
            File.Delete(path);
        }

        private static string SingleFilePath(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new StudioApiException(StudioApiException.InvalidParams, "parameter 'name' is required for this action");
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new StudioApiException(StudioApiException.InvalidParams, "single file name '" + name + "' contains invalid characters");
            var folder = Convert.ToString(_fSingleFilesFolder.GetValue(null));
            if (string.IsNullOrEmpty(folder))
                throw new StudioApiException(StudioApiException.OperationFailed, "Timeline has no single-files folder");
            return Path.Combine(folder, name + ".xml");
        }

        // LoadSingle and SaveSingle both read _selectedOCI rather than taking a
        // target. Timeline only rewrites that field when the selected guide object
        // itself changes, so borrowing it for the length of one call — on the main
        // thread, between two of its Update passes — is invisible to the plugin.
        private static void InSelection(object self, SNeo.ObjectCtrlInfo oci, Action action)
        {
            var previous = _fSelectedOci.GetValue(self);
            _fSelectedOci.SetValue(self, oci);
            try { action(); }
            finally { _fSelectedOci.SetValue(self, previous); }
        }

        // ---- value marshalling -------------------------------------------

        private static JsonNode ValueToJson(object v)
        {
            if (v == null) return JsonNull.Instance;
            if (v is bool) return new JsonBool((bool)v);
            if (v is string) return new JsonString((string)v);
            if (v is float) return new JsonNumber((float)v);
            if (v is double) return new JsonNumber((double)v);
            if (v is Enum) return new JsonNumber(Convert.ToDouble(v));
            if (v is int || v is byte || v is sbyte || v is short || v is ushort || v is uint || v is long)
                return new JsonNumber(Convert.ToDouble(v));
            if (v is Vector2) { var x = (Vector2)v; return new JsonArray().Add(x.x).Add(x.y); }
            if (v is Vector3) { var x = (Vector3)v; return new JsonArray().Add(x.x).Add(x.y).Add(x.z); }
            if (v is Vector4) { var x = (Vector4)v; return new JsonArray().Add(x.x).Add(x.y).Add(x.z).Add(x.w); }
            // Exposed as euler degrees, like every other rotation in this API.
            if (v is Quaternion) { var e = ((Quaternion)v).eulerAngles; return new JsonArray().Add(e.x).Add(e.y).Add(e.z); }
            if (v is Color) { var c = (Color)v; return new JsonArray().Add(c.r).Add(c.g).Add(c.b).Add(c.a); }
            var anime = v as SNeo.OICharInfo.AnimeInfo;
            if (anime != null)
                return new JsonObject().Add("group", anime.group).Add("category", anime.category).Add("no", anime.no);
            return new JsonString(Convert.ToString(v));
        }

        private static object JsonToValue(JsonNode node, Type type, int interpolableId)
        {
            if (type == null)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "interpolable " + interpolableId + " has no current value to type 'value' against; omit it to capture the scene");

            if (type == typeof(bool)) return Bool(node, type);
            if (type == typeof(string)) return Str(node, type);
            if (type == typeof(float)) return (float)Num(node, type);
            if (type == typeof(double)) return Num(node, type);
            if (type == typeof(int)) return (int)Num(node, type);
            if (type == typeof(byte)) return (byte)Num(node, type);
            if (type == typeof(sbyte)) return (sbyte)Num(node, type);
            if (type == typeof(short)) return (short)Num(node, type);
            if (type == typeof(ushort)) return (ushort)Num(node, type);
            if (type == typeof(uint)) return (uint)Num(node, type);
            if (type == typeof(long)) return (long)Num(node, type);
            if (type.IsEnum) return Enum.ToObject(type, (long)Num(node, type));
            if (type == typeof(Vector2)) { var a = Floats(node, type, 2, 2); return new Vector2(a[0], a[1]); }
            if (type == typeof(Vector3)) { var a = Floats(node, type, 3, 3); return new Vector3(a[0], a[1], a[2]); }
            if (type == typeof(Vector4)) { var a = Floats(node, type, 4, 4); return new Vector4(a[0], a[1], a[2], a[3]); }
            if (type == typeof(Quaternion)) { var a = Floats(node, type, 3, 3); return Quaternion.Euler(a[0], a[1], a[2]); }
            if (type == typeof(Color))
            {
                var a = Floats(node, type, 3, 4);
                return new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f);
            }
            if (type == typeof(SNeo.OICharInfo.AnimeInfo))
            {
                var o = node as JsonObject;
                if (o == null) throw Mismatch(type, "an object {group, category, no}");
                return new SNeo.OICharInfo.AnimeInfo
                {
                    group = o.GetInt("group", 0),
                    category = o.GetInt("category", 0),
                    no = o.GetInt("no", 0),
                };
            }
            throw new StudioApiException(StudioApiException.InvalidParams,
                "this interpolable holds a " + type.Name + ", which this bridge cannot build from JSON; " +
                "pose the scene with the other tools and omit 'value' to capture it");
        }

        private static bool Bool(JsonNode node, Type type)
        {
            var b = node as JsonBool;
            if (b == null) throw Mismatch(type, "true or false");
            return b.Value;
        }

        private static string Str(JsonNode node, Type type)
        {
            var s = node as JsonString;
            if (s == null) throw Mismatch(type, "a string");
            return s.Value;
        }

        private static double Num(JsonNode node, Type type)
        {
            var n = node as JsonNumber;
            if (n == null) throw Mismatch(type, "a number");
            return n.Value;
        }

        private static float[] Floats(JsonNode node, Type type, int min, int max)
        {
            var arr = node as JsonArray;
            if (arr == null || arr.Count < min || arr.Count > max)
                throw Mismatch(type, "an array of " + (min == max ? min.ToString() : min + " to " + max) + " numbers");
            var values = new float[arr.Count];
            for (var i = 0; i < arr.Count; i++)
            {
                var n = arr[i] as JsonNumber;
                if (n == null) throw Mismatch(type, "an array of numbers");
                values[i] = (float)n.Value;
            }
            return values;
        }

        private static StudioApiException Mismatch(Type type, string expected)
        {
            return new StudioApiException(StudioApiException.InvalidParams,
                "this interpolable holds a " + type.Name + ", so 'value' must be " + expected);
        }

        // ---- curves ------------------------------------------------------

        // Timeline's own five presets, tangents included, so a curve set from here
        // is indistinguishable from one picked in its keyframe window.
        private static AnimationCurve Preset(string name)
        {
            switch (name)
            {
                case "line": return AnimationCurve.Linear(0f, 0f, 1f, 1f);
                case "top": return new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
                case "bottom": return new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));
                case "hermite": return new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));
                case "stairs": return new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, float.PositiveInfinity, 0f));
                default: return null;
            }
        }

        private static JsonNode CurveToJson(AnimationCurve curve)
        {
            if (curve == null) return JsonNull.Instance;
            var arr = new JsonArray();
            foreach (var k in curve.keys)
                arr.Add(new JsonArray().Add(k.time).Add(k.value).Add(k.inTangent).Add(k.outTangent));
            return arr;
        }

        // Omitting the curve reuses the one of the keyframe before this time, then
        // falls back to linear — the same inheritance Timeline's AddKeyframe applies.
        private static AnimationCurve ResolveCurve(JsonNode curve, IDictionary keyframes, float time)
        {
            if (curve == null || curve is JsonNull)
            {
                object previous = null;
                var best = float.NegativeInfinity;
                foreach (DictionaryEntry k in keyframes)
                {
                    var t = (float)k.Key;
                    if (t < time && t > best) { best = t; previous = k.Value; }
                }
                if (previous == null) return null;
                var inherited = _fKeyCurve.GetValue(previous) as AnimationCurve;
                return inherited != null ? new AnimationCurve(inherited.keys) : null;
            }

            var name = curve as JsonString;
            if (name != null)
            {
                var preset = Preset(name.Value);
                if (preset == null)
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "unknown curve preset '" + name.Value + "'; use 'line', 'top', 'bottom', 'hermite', 'stairs' or an array of points");
                return preset;
            }

            var points = curve as JsonArray;
            if (points == null || points.Count < 2)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'curve' must be a preset name or an array of at least 2 points [time, value, in_tangent, out_tangent]");
            var keys = new Keyframe[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i] as JsonArray;
                if (p == null || p.Count != 4)
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "curve point " + i + " must be [time, value, in_tangent, out_tangent]");
                // A null tangent is the infinite one a 'stairs' curve carries: JSON
                // has no way to spell infinity, so it round-trips through null.
                keys[i] = new Keyframe(
                    (float)CurveNumber(p[0], i, "time"),
                    (float)CurveNumber(p[1], i, "value"),
                    Tangent(p[2], i, "in_tangent"),
                    Tangent(p[3], i, "out_tangent"));
            }
            return new AnimationCurve(keys);
        }

        private static double CurveNumber(JsonNode node, int index, string field)
        {
            var n = node as JsonNumber;
            if (n == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "curve point " + index + ": '" + field + "' must be a number");
            return n.Value;
        }

        private static float Tangent(JsonNode node, int index, string field)
        {
            if (node is JsonNull) return float.PositiveInfinity;
            var n = node as JsonNumber;
            if (n == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "curve point " + index + ": '" + field + "' must be a number, or null for an infinite tangent");
            return (float)n.Value;
        }
    }
}
