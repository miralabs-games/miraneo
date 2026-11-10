using System.Collections.Generic;

namespace Miraneo.Core
{
    // Extraction of RPC parameters with strict validation: anything malformed is
    // an InvalidParams error rather than a silently coerced value.
    public static class Params
    {
        public static string ReqString(JsonObject p, string key)
        {
            var value = p.GetString(key);
            if (string.IsNullOrEmpty(value)) throw Invalid(key, "a non-empty string");
            return value;
        }

        public static string OptString(JsonObject p, string key, string fallback = null)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return fallback;
            var s = node as JsonString;
            if (s == null) throw Invalid(key, "a string");
            return s.Value;
        }

        public static int ReqInt(JsonObject p, string key)
        {
            var n = p.Get(key) as JsonNumber;
            if (n == null) throw Invalid(key, "an integer");
            return (int)n.Value;
        }

        public static int OptInt(JsonObject p, string key, int fallback)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return fallback;
            var n = node as JsonNumber;
            if (n == null) throw Invalid(key, "an integer");
            return (int)n.Value;
        }

        public static int? OptIntN(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var n = node as JsonNumber;
            if (n == null) throw Invalid(key, "an integer");
            return (int)n.Value;
        }

        public static float ReqFloat(JsonObject p, string key)
        {
            var n = p.Get(key) as JsonNumber;
            if (n == null) throw Invalid(key, "a number");
            return (float)n.Value;
        }

        // One id or a list of them: removing several interpolables in one call keeps
        // the timeline consistent between reads.
        public static List<int> IntList(JsonObject p, string key)
        {
            var node = p.Get(key);
            var single = node as JsonNumber;
            if (single != null) return new List<int> { (int)single.Value };
            var arr = node as JsonArray;
            if (arr == null || arr.Count == 0) throw Invalid(key, "an integer or a non-empty array of integers");
            var list = new List<int>(arr.Count);
            foreach (var item in arr)
            {
                var n = item as JsonNumber;
                if (n == null) throw Invalid(key, "an integer or a non-empty array of integers");
                list.Add((int)n.Value);
            }
            return list;
        }

        public static float? OptFloat(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var n = node as JsonNumber;
            if (n == null) throw Invalid(key, "a number");
            return (float)n.Value;
        }

        public static bool ReqBool(JsonObject p, string key)
        {
            var b = p.Get(key) as JsonBool;
            if (b == null) throw Invalid(key, "a boolean");
            return b.Value;
        }

        public static bool? OptBool(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var b = node as JsonBool;
            if (b == null) throw Invalid(key, "a boolean");
            return b.Value;
        }

        // One field that accepts either form, e.g. an attach point given as its
        // index or as its name. Both outputs null means the field was absent.
        public static void OptIntOrString(JsonObject p, string key, out int? number, out string text)
        {
            number = null;
            text = null;
            var node = p.Get(key);
            if (node == null || node is JsonNull) return;
            var n = node as JsonNumber;
            if (n != null) { number = (int)n.Value; return; }
            var s = node as JsonString;
            if (s == null || string.IsNullOrEmpty(s.Value)) throw Invalid(key, "an integer or a non-empty string");
            text = s.Value;
        }

        public static Vec3? OptVec3(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var arr = node as JsonArray;
            if (arr == null || arr.Count != 3) throw Invalid(key, "an array of 3 numbers");
            return new Vec3(Number(arr, 0, key), Number(arr, 1, key), Number(arr, 2, key));
        }

        // Accepts [r,g,b] or [r,g,b,a], components in 0..1.
        public static float[] OptColor(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var arr = node as JsonArray;
            if (arr == null || (arr.Count != 3 && arr.Count != 4)) throw Invalid(key, "an array of 3 or 4 numbers (r,g,b[,a]) in 0..1");
            var rgba = new float[4];
            rgba[3] = 1f;
            for (var i = 0; i < arr.Count; i++) rgba[i] = Number(arr, i, key);
            return rgba;
        }

        // Two angles, as Studio stores the chara light's orientation (LightInfo.rot).
        public static float[] OptPair(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var arr = node as JsonArray;
            if (arr == null || arr.Count != 2) throw Invalid(key, "an array of 2 numbers");
            return new[] { Number(arr, 0, key), Number(arr, 1, key) };
        }

        // A named group of fields inside a patch, e.g. post-processing's "bloom".
        private static JsonObject OptSection(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, "an object");
            return obj;
        }

        public static TransformPatch Transform(JsonObject p) => new TransformPatch
        {
            Pos = OptVec3(p, "pos"),
            Rot = OptVec3(p, "rot"),
            Scale = OptVec3(p, "scale"),
        };

        public static LightPatch Light(JsonObject p) => new LightPatch
        {
            Color = OptColor(p, "color"),
            Intensity = OptFloat(p, "intensity"),
            Range = OptFloat(p, "range"),
            SpotAngle = OptFloat(p, "spot_angle"),
            Enabled = OptBool(p, "enabled"),
            Shadow = OptBool(p, "shadow"),
        };

        public static HandsPatch Hands(JsonObject p) => new HandsPatch
        {
            Left = OptIntN(p, "left"),
            Right = OptIntN(p, "right"),
        };

        public static AnimePatch Anime(JsonObject p) => new AnimePatch
        {
            Group = OptIntN(p, "group"),
            Category = OptIntN(p, "category"),
            No = OptIntN(p, "no"),
            NormalizedTime = OptFloat(p, "normalized_time"),
            Speed = OptFloat(p, "speed"),
            Pattern = OptFloat(p, "pattern"),
            OptionParam1 = OptFloat(p, "option_param1"),
            OptionParam2 = OptFloat(p, "option_param2"),
        };

        public static ExpressionPatch Expression(JsonObject p) => new ExpressionPatch
        {
            Eyebrows = OptIntN(p, "eyebrows"),
            Eyes = OptIntN(p, "eyes"),
            EyesOpen = OptFloat(p, "eyes_open"),
            Blink = OptBool(p, "blink"),
            Mouth = OptIntN(p, "mouth"),
            MouthOpen = OptFloat(p, "mouth_open"),
            LipSync = OptBool(p, "lip_sync"),
            Tears = OptFloat(p, "tears"),
            Blush = OptFloat(p, "blush"),
            LookEyes = OptIntN(p, "look_eyes"),
            LookNeck = OptIntN(p, "look_neck"),
            Categories = BoolMap(p, "expression_categories"),
        };

        public static OutfitPatch Outfit(JsonObject p) => new OutfitPatch
        {
            Coordinate = OptString(p, "coordinate"),
            ClothesAll = OptString(p, "clothes_all"),
            Clothes = StringMap(p, "clothes", "an object mapping a clothes slot (" + OutfitNames.Join(OutfitNames.Clothes) + ") to a state (" + OutfitNames.Join(OutfitNames.States) + ")"),
            Accessories = BoolMap(p, "accessories"),
        };

        public static CharacterStatePatch CharacterState(JsonObject p) => new CharacterStatePatch
        {
            Nipple = OptFloat(p, "nipple"),
            SkinGloss = OptFloat(p, "skin_gloss"),
            Wet = OptFloat(p, "wet"),
            Simple = OptBool(p, "simple"),
            SimpleColor = OptColor(p, "simple_color"),
            Son = OptBool(p, "son"),
            SonLength = OptFloat(p, "son_length"),
            Siru = IntMap(p, "siru", "an object mapping a body part (" + OutfitNames.Join(OutfitNames.Siru) + ") to a level 0..2"),
        };

        public static MapPatch Map(JsonObject p) => new MapPatch
        {
            No = OptIntN(p, "no"),
            Pos = OptVec3(p, "pos"),
            Rot = OptVec3(p, "rot"),
            Option = OptBool(p, "option"),
            Light = OptBool(p, "light"),
        };

        public static SceneLightingPatch SceneLighting(JsonObject p) => new SceneLightingPatch
        {
            Color = OptColor(p, "color"),
            Intensity = OptFloat(p, "intensity"),
            Rot = OptPair(p, "rot"),
            Shadow = OptBool(p, "shadow"),
        };

        public static OverlayPatch Overlay(JsonObject p) => new OverlayPatch
        {
            Background = OptString(p, "background"),
            Frame = OptString(p, "frame"),
        };

        public static ConstraintPatch Constraint(JsonObject p) => new ConstraintPatch
        {
            Parent = ConstraintNode(p, "parent"),
            Child = ConstraintNode(p, "child"),
            Enabled = OptBool(p, "enabled"),
            Alias = OptString(p, "alias"),
            Position = OptBool(p, "position"),
            Rotation = OptBool(p, "rotation"),
            Scale = OptBool(p, "scale"),
            LookAt = OptBool(p, "look_at"),
            MirrorPosition = OptBool(p, "mirror_position"),
            MirrorRotation = OptBool(p, "mirror_rotation"),
            MirrorScale = OptBool(p, "mirror_scale"),
            PositionOffset = OptVec3(p, "position_offset"),
            RotationOffset = OptVec3(p, "rotation_offset"),
            ScaleOffset = OptVec3(p, "scale_offset"),
            PositionFactor = OptFloat(p, "position_factor"),
            RotationFactor = OptFloat(p, "rotation_factor"),
            ScaleFactor = OptFloat(p, "scale_factor"),
            PositionDamp = OptFloat(p, "position_damp"),
            RotationDamp = OptFloat(p, "rotation_damp"),
            ScaleDamp = OptFloat(p, "scale_damp"),
            PositionLocks = OptAxes(p, "position_locks"),
            RotationLocks = OptAxes(p, "rotation_locks"),
            ScaleLocks = OptAxes(p, "scale_locks"),
            ResetPosition = OptBool(p, "reset_position"),
            ResetRotation = OptBool(p, "reset_rotation"),
            ResetScale = OptBool(p, "reset_scale"),
            FixDynamicBone = OptBool(p, "fix_dynamic_bone"),
        };

        // {"object_id": 3, "target": "cf_J_Hand_L"} — one end of a constraint.
        private static ConstraintEndpoint ConstraintNode(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, "an object {object_id, target}");
            var id = obj.Get("object_id") as JsonNumber;
            if (id == null) throw Invalid(key + ".object_id", "an integer (a scene object id from studio_list_objects)");
            return new ConstraintEndpoint
            {
                ObjectId = (int)id.Value,
                Target = OptString(obj, "target"),
            };
        }

        // [true, false, true] — which of x, y, z a constraint drives on its child.
        private static bool[] OptAxes(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var arr = node as JsonArray;
            if (arr == null || arr.Count != 3) throw Invalid(key, "an array of 3 booleans");
            var axes = new bool[3];
            for (var i = 0; i < 3; i++)
            {
                var b = arr[i] as JsonBool;
                if (b == null) throw Invalid(key, "an array of 3 booleans");
                axes[i] = b.Value;
            }
            return axes;
        }

        public static PostProcessingDto PostProcessing(JsonObject p)
        {
            var dto = new PostProcessingDto();

            var s = OptSection(p, "color_grading");
            if (s != null) dto.ColorGrading = new PostProcessingDto.ColorGradingSection
            {
                LookupTexture = OptIntN(s, "lookup_texture"),
                Blend = OptFloat(s, "blend"),
                Saturation = OptIntN(s, "saturation"),
                Brightness = OptIntN(s, "brightness"),
                Contrast = OptIntN(s, "contrast"),
            };

            s = OptSection(p, "ambient_occlusion");
            if (s != null) dto.AmbientOcclusion = new PostProcessingDto.AmbientOcclusionSection
            {
                Enabled = OptBool(s, "enabled"),
                Intensity = OptFloat(s, "intensity"),
                ThicknessModifier = OptFloat(s, "thickness_modifier"),
                Color = OptColor(s, "color"),
            };

            s = OptSection(p, "bloom");
            if (s != null) dto.Bloom = new PostProcessingDto.BloomSection
            {
                Enabled = OptBool(s, "enabled"),
                Intensity = OptFloat(s, "intensity"),
                Threshold = OptFloat(s, "threshold"),
                SoftKnee = OptFloat(s, "soft_knee"),
                Clamp = OptBool(s, "clamp"),
                Diffusion = OptFloat(s, "diffusion"),
                Color = OptColor(s, "color"),
            };

            s = OptSection(p, "depth_of_field");
            if (s != null) dto.DepthOfField = new PostProcessingDto.DepthOfFieldSection
            {
                Enabled = OptBool(s, "enabled"),
                FocusObjectId = OptIntN(s, "focus_object_id"),
                FocalSize = OptFloat(s, "focal_size"),
                Aperture = OptFloat(s, "aperture"),
            };

            s = OptSection(p, "vignette");
            if (s != null) dto.Vignette = new PostProcessingDto.VignetteSection
            {
                Enabled = OptBool(s, "enabled"),
                Intensity = OptFloat(s, "intensity"),
            };

            s = OptSection(p, "screen_space_reflections");
            if (s != null) dto.ScreenSpaceReflections = new PostProcessingDto.ToggleSection { Enabled = OptBool(s, "enabled") };

            s = OptSection(p, "reflection_probe");
            if (s != null) dto.ReflectionProbe = new PostProcessingDto.ReflectionProbeSection
            {
                Enabled = OptBool(s, "enabled"),
                Cubemap = OptIntN(s, "cubemap"),
                Intensity = OptFloat(s, "intensity"),
            };

            s = OptSection(p, "fog");
            if (s != null) dto.Fog = new PostProcessingDto.FogSection
            {
                Enabled = OptBool(s, "enabled"),
                ExcludeFarPixels = OptBool(s, "exclude_far_pixels"),
                Height = OptFloat(s, "height"),
                HeightDensity = OptFloat(s, "height_density"),
                Color = OptColor(s, "color"),
                Density = OptFloat(s, "density"),
            };

            s = OptSection(p, "sun_shafts");
            if (s != null) dto.SunShafts = new PostProcessingDto.SunShaftsSection
            {
                Enabled = OptBool(s, "enabled"),
                CasterObjectId = OptIntN(s, "caster_object_id"),
                ThresholdColor = OptColor(s, "threshold_color"),
                Color = OptColor(s, "color"),
                DistanceFalloff = OptFloat(s, "distance_falloff"),
                BlurSize = OptFloat(s, "blur_size"),
                Intensity = OptFloat(s, "intensity"),
            };

            s = OptSection(p, "self_shadow");
            if (s != null) dto.SelfShadow = new PostProcessingDto.ToggleSection { Enabled = OptBool(s, "enabled") };

            s = OptSection(p, "environment_lighting");
            if (s != null) dto.EnvironmentLighting = new PostProcessingDto.EnvironmentLightingSection
            {
                SkyColor = OptColor(s, "sky_color"),
                EquatorColor = OptColor(s, "equator_color"),
                GroundColor = OptColor(s, "ground_color"),
            };

            s = OptSection(p, "sky");
            if (s != null) dto.Sky = new PostProcessingDto.SkySection
            {
                Enabled = OptBool(s, "enabled"),
                Pattern = OptIntN(s, "pattern"),
            };

            return dto;
        }

        public static KinematicsPatch Kinematics(JsonObject p) => new KinematicsPatch
        {
            Mode = OptString(p, "mode"),
            Reflect = OptString(p, "reflect"),
            Fk = Groups(p, "fk", KinematicGroups.Fk),
            Ik = Groups(p, "ik", KinematicGroups.Ik),
        };

        // {"body": true, "skirt": false} — unlisted groups keep their current flag.
        private static bool?[] Groups(JsonObject p, string key, string[] names)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, "an object mapping group name to boolean (" + KinematicGroups.Join(names) + ")");
            var flags = new bool?[names.Length];
            foreach (var name in obj.Keys)
            {
                var index = KinematicGroups.Index(names, name);
                if (index < 0)
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "unknown " + key + " group '" + name + "'; expected one of " + KinematicGroups.Join(names));
                var value = obj.Get(name) as JsonBool;
                if (value == null) throw Invalid(key + "." + name, "a boolean");
                flags[index] = value.Value;
            }
            return flags;
        }

        // {"cf_J_ArmUp00_L": [0, 0, 30]} — one entry per bone, absolute euler angles.
        public static List<KeyValuePair<string, Vec3>> BoneRotations(JsonObject p)
        {
            var obj = p.Get("bones") as JsonObject;
            if (obj == null) throw Invalid("bones", "an object mapping bone name to a rotation [x, y, z]");
            var list = new List<KeyValuePair<string, Vec3>>();
            foreach (var name in obj.Keys)
            {
                var rot = OptVec3(obj, name);
                if (!rot.HasValue) throw Invalid("bones." + name, "an array of 3 numbers");
                list.Add(new KeyValuePair<string, Vec3>(name, rot.Value));
            }
            if (list.Count == 0) throw Invalid("bones", "a non-empty object");
            return list;
        }

        // {"left_hand": {"pos": [...], "rot": [...]}} — 'rot' only on rotatable targets.
        public static List<IkTargetPatch> IkTargets(JsonObject p)
        {
            var obj = p.Get("targets") as JsonObject;
            if (obj == null) throw Invalid("targets", "an object mapping IK target name to {pos, rot}");
            var list = new List<IkTargetPatch>();
            foreach (var name in obj.Keys)
            {
                var entry = obj.Get(name) as JsonObject;
                if (entry == null) throw Invalid("targets." + name, "an object with 'pos' and/or 'rot'");
                var patch = new IkTargetPatch
                {
                    Target = name,
                    Pos = OptVec3(entry, "pos"),
                    Rot = OptVec3(entry, "rot"),
                };
                if (!patch.Pos.HasValue && !patch.Rot.HasValue)
                    throw Invalid("targets." + name, "an object with 'pos' and/or 'rot'");
                list.Add(patch);
            }
            if (list.Count == 0) throw Invalid("targets", "a non-empty object");
            return list;
        }

        // {"0": true, "3": false} — keys are indices (expression categories, accessory slots).
        private static System.Collections.Generic.Dictionary<int, bool> BoolMap(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, "an object mapping index to boolean");
            var map = new System.Collections.Generic.Dictionary<int, bool>();
            foreach (var name in obj.Keys)
            {
                int index;
                var value = obj.Get(name) as JsonBool;
                if (!int.TryParse(name, out index) || value == null)
                    throw Invalid(key, "an object mapping index to boolean");
                map[index] = value.Value;
            }
            return map;
        }

        // {"top": "off"} — named entries whose value is a name too.
        private static Dictionary<string, string> StringMap(JsonObject p, string key, string expected)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, expected);
            var map = new Dictionary<string, string>();
            foreach (var name in obj.Keys)
            {
                var value = obj.Get(name) as JsonString;
                if (value == null || string.IsNullOrEmpty(value.Value)) throw Invalid(key, expected);
                map[name] = value.Value;
            }
            return map;
        }

        // {"face": 2} — named entries whose value is a small integer.
        private static Dictionary<string, int> IntMap(JsonObject p, string key, string expected)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, expected);
            var map = new Dictionary<string, int>();
            foreach (var name in obj.Keys)
            {
                var value = obj.Get(name) as JsonNumber;
                if (value == null) throw Invalid(key, expected);
                map[name] = (int)value.Value;
            }
            return map;
        }

        private static float Number(JsonArray arr, int index, string key)
        {
            var n = arr[index] as JsonNumber;
            if (n == null) throw Invalid(key, "an array of numbers");
            return (float)n.Value;
        }

        private static StudioApiException Invalid(string key, string expected) =>
            new StudioApiException(StudioApiException.InvalidParams, "parameter '" + key + "' must be " + expected);
    }
}
