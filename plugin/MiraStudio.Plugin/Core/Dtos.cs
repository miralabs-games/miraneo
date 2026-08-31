namespace MiraStudio.Core
{
    public struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public JsonNode ToJson() => new JsonArray().Add(X).Add(Y).Add(Z);
    }

    public sealed class TransformDto
    {
        public Vec3 Pos, Rot, Scale;

        public JsonNode ToJson() => new JsonObject()
            .Add("pos", Pos.ToJson())
            .Add("rot", Rot.ToJson())
            .Add("scale", Scale.ToJson());
    }

    public sealed class RuntimeInfoDto
    {
        public string ProductName, UnityVersion, GameVersion, PluginVersion, BepInExVersion, ActiveScene;
        public bool StudioLoaded;
        public int SceneObjectCount;
        // Optional third-party plugins MiraStudio bridges to, name -> version or
        // null when absent: the timeline tools are registered either way, so this
        // is how an agent tells "not installed" from "call it".
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> Plugins;

        public JsonNode ToJson()
        {
            var plugins = new JsonObject();
            if (Plugins != null)
                foreach (var kv in Plugins) plugins.Add(kv.Key, kv.Value);
            return new JsonObject()
                .Add("product_name", ProductName)
                .Add("unity_version", UnityVersion)
                .Add("game_version", GameVersion)
                .Add("plugin_version", PluginVersion)
                .Add("bepinex_version", BepInExVersion)
                .Add("active_scene", ActiveScene)
                .Add("studio_loaded", StudioLoaded)
                .Add("scene_object_count", SceneObjectCount)
                .Add("plugins", plugins);
        }
    }

    public sealed class SceneSummaryDto
    {
        public int Total, Characters, Items, Lights, Folders, Routes, Cameras;
        public string Background, Frame;
        public MapDto Map;
        public SceneLightingDto Lighting;
        // Only studio_get_scene fills this one: the scene summary is also embedded in
        // the load and save results, where forty post-processing fields are noise.
        public PostProcessingDto PostProcessing;

        public JsonNode ToJson() => new JsonObject()
            .Add("total_objects", Total)
            .Add("characters", Characters)
            .Add("items", Items)
            .Add("lights", Lights)
            .Add("folders", Folders)
            .Add("routes", Routes)
            .Add("cameras", Cameras)
            .Add("background", Background)
            .Add("frame", Frame)
            .Add("map", Map != null ? Map.ToJson() : JsonNull.Instance)
            .Add("lighting", Lighting != null ? Lighting.ToJson() : JsonNull.Instance)
            .Add("post_processing", PostProcessing != null ? PostProcessing.ToJson() : JsonNull.Instance);
    }

    // JSON helpers for the optional-field DTOs below: a section doubles as its own
    // patch, so every field is nullable and a read-back is simply a fully populated one.
    internal static class J
    {
        public static JsonNode N(int? v) => v.HasValue ? (JsonNode)v.Value : JsonNull.Instance;
        public static JsonNode N(float? v) => v.HasValue ? (JsonNode)v.Value : JsonNull.Instance;
        public static JsonNode N(bool? v) => v.HasValue ? (JsonNode)v.Value : JsonNull.Instance;

        public static JsonNode Color(float[] rgba)
        {
            if (rgba == null) return JsonNull.Instance;
            var arr = new JsonArray();
            foreach (var c in rgba) arr.Add(c);
            return arr;
        }

        public static JsonNode Pair(float[] xy)
        {
            if (xy == null) return JsonNull.Instance;
            var arr = new JsonArray();
            foreach (var v in xy) arr.Add(v);
            return arr;
        }
    }

    public sealed class MapDto
    {
        public int No;
        public string Name;
        public bool Loaded, Option, Light, OptionAvailable, LightAvailable;
        public Vec3 Pos, Rot;

        public JsonNode ToJson() => new JsonObject()
            .Add("no", No)
            .Add("name", Name)
            .Add("loaded", Loaded)
            .Add("pos", Pos.ToJson())
            .Add("rot", Rot.ToJson())
            .Add("option", Option)
            .Add("option_available", OptionAvailable)
            .Add("light", Light)
            .Add("light_available", LightAvailable);
    }

    // SceneInfo.charaLight. Its sibling mapLight is deliberately absent: SceneInfo.Save
    // and Load are its only users in the whole assembly, so writing it changes nothing.
    public sealed class SceneLightingDto
    {
        public float[] Color, Rot;
        public float? Intensity;
        public bool? Shadow;

        public JsonNode ToJson() => new JsonObject()
            .Add("color", J.Color(Color))
            .Add("intensity", J.N(Intensity))
            .Add("rot", J.Pair(Rot))
            .Add("shadow", J.N(Shadow));
    }

    public sealed class OverlaysDto
    {
        public string Background, Frame;

        public JsonNode ToJson() => new JsonObject()
            .Add("background", Background)
            .Add("frame", Frame);
    }

    public sealed class OverlayFileDto
    {
        public string Kind, Path, Name, Source;

        public JsonNode ToJson() => new JsonObject()
            .Add("kind", Kind)
            .Add("path", Path)
            .Add("name", Name)
            .Add("source", Source);
    }

    // One class for both directions: as a patch only the supplied fields are set, and
    // a read-back returns the same shape fully populated. Section names and field names
    // follow Studio's own screen-effect panel (SystemButtonCtrl).
    public sealed class PostProcessingDto
    {
        public sealed class ColorGradingSection
        {
            public int? LookupTexture, Saturation, Brightness, Contrast;
            public float? Blend;

            public JsonNode ToJson() => new JsonObject()
                .Add("lookup_texture", J.N(LookupTexture))
                .Add("blend", J.N(Blend))
                .Add("saturation", J.N(Saturation))
                .Add("brightness", J.N(Brightness))
                .Add("contrast", J.N(Contrast));
        }

        public sealed class AmbientOcclusionSection
        {
            public bool? Enabled;
            public float? Intensity, ThicknessModifier;
            public float[] Color;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("intensity", J.N(Intensity))
                .Add("thickness_modifier", J.N(ThicknessModifier))
                .Add("color", J.Color(Color));
        }

        public sealed class BloomSection
        {
            public bool? Enabled, Clamp;
            public float? Intensity, Threshold, SoftKnee, Diffusion;
            public float[] Color;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("intensity", J.N(Intensity))
                .Add("threshold", J.N(Threshold))
                .Add("soft_knee", J.N(SoftKnee))
                .Add("clamp", J.N(Clamp))
                .Add("diffusion", J.N(Diffusion))
                .Add("color", J.Color(Color));
        }

        public sealed class DepthOfFieldSection
        {
            public bool? Enabled;
            public int? FocusObjectId;
            public float? FocalSize, Aperture;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("focus_object_id", J.N(FocusObjectId))
                .Add("focal_size", J.N(FocalSize))
                .Add("aperture", J.N(Aperture));
        }

        public sealed class VignetteSection
        {
            public bool? Enabled;
            public float? Intensity;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("intensity", J.N(Intensity));
        }

        public sealed class ToggleSection
        {
            public bool? Enabled;

            public JsonNode ToJson() => new JsonObject().Add("enabled", J.N(Enabled));
        }

        public sealed class ReflectionProbeSection
        {
            public bool? Enabled;
            public int? Cubemap;
            public float? Intensity;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("cubemap", J.N(Cubemap))
                .Add("intensity", J.N(Intensity));
        }

        public sealed class FogSection
        {
            public bool? Enabled, ExcludeFarPixels;
            public float? Height, HeightDensity, Density;
            public float[] Color;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("exclude_far_pixels", J.N(ExcludeFarPixels))
                .Add("height", J.N(Height))
                .Add("height_density", J.N(HeightDensity))
                .Add("color", J.Color(Color))
                .Add("density", J.N(Density));
        }

        public sealed class SunShaftsSection
        {
            public bool? Enabled;
            public int? CasterObjectId;
            public float? DistanceFalloff, BlurSize, Intensity;
            public float[] ThresholdColor, Color;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("caster_object_id", J.N(CasterObjectId))
                .Add("threshold_color", J.Color(ThresholdColor))
                .Add("color", J.Color(Color))
                .Add("distance_falloff", J.N(DistanceFalloff))
                .Add("blur_size", J.N(BlurSize))
                .Add("intensity", J.N(Intensity));
        }

        public sealed class EnvironmentLightingSection
        {
            public float[] SkyColor, EquatorColor, GroundColor;

            public JsonNode ToJson() => new JsonObject()
                .Add("sky_color", J.Color(SkyColor))
                .Add("equator_color", J.Color(EquatorColor))
                .Add("ground_color", J.Color(GroundColor));
        }

        public sealed class SkySection
        {
            public bool? Enabled;
            public int? Pattern;

            public JsonNode ToJson() => new JsonObject()
                .Add("enabled", J.N(Enabled))
                .Add("pattern", J.N(Pattern));
        }

        public ColorGradingSection ColorGrading;
        public AmbientOcclusionSection AmbientOcclusion;
        public BloomSection Bloom;
        public DepthOfFieldSection DepthOfField;
        public VignetteSection Vignette;
        public ToggleSection ScreenSpaceReflections, SelfShadow;
        public ReflectionProbeSection ReflectionProbe;
        public FogSection Fog;
        public SunShaftsSection SunShafts;
        public EnvironmentLightingSection EnvironmentLighting;
        public SkySection Sky;

        public bool IsEmpty =>
            ColorGrading == null && AmbientOcclusion == null && Bloom == null && DepthOfField == null &&
            Vignette == null && ScreenSpaceReflections == null && SelfShadow == null && ReflectionProbe == null &&
            Fog == null && SunShafts == null && EnvironmentLighting == null && Sky == null;

        public JsonNode ToJson() => new JsonObject()
            .Add("color_grading", ColorGrading != null ? ColorGrading.ToJson() : JsonNull.Instance)
            .Add("ambient_occlusion", AmbientOcclusion != null ? AmbientOcclusion.ToJson() : JsonNull.Instance)
            .Add("bloom", Bloom != null ? Bloom.ToJson() : JsonNull.Instance)
            .Add("depth_of_field", DepthOfField != null ? DepthOfField.ToJson() : JsonNull.Instance)
            .Add("vignette", Vignette != null ? Vignette.ToJson() : JsonNull.Instance)
            .Add("screen_space_reflections", ScreenSpaceReflections != null ? ScreenSpaceReflections.ToJson() : JsonNull.Instance)
            .Add("reflection_probe", ReflectionProbe != null ? ReflectionProbe.ToJson() : JsonNull.Instance)
            .Add("fog", Fog != null ? Fog.ToJson() : JsonNull.Instance)
            .Add("sun_shafts", SunShafts != null ? SunShafts.ToJson() : JsonNull.Instance)
            .Add("self_shadow", SelfShadow != null ? SelfShadow.ToJson() : JsonNull.Instance)
            .Add("environment_lighting", EnvironmentLighting != null ? EnvironmentLighting.ToJson() : JsonNull.Instance)
            .Add("sky", Sky != null ? Sky.ToJson() : JsonNull.Instance);
    }

    public sealed class AnimeDto
    {
        public int Group, Category, No;
        public string Name, GroupName, CategoryName;
        public float NormalizedTime, Speed, Pattern, OptionParam1, OptionParam2;
        public bool IsAnimeMotion, IsHAnime;

        public JsonNode ToJson() => new JsonObject()
            .Add("group", Group)
            .Add("category", Category)
            .Add("no", No)
            .Add("name", Name)
            .Add("group_name", GroupName)
            .Add("category_name", CategoryName)
            .Add("normalized_time", NormalizedTime)
            .Add("speed", Speed)
            .Add("pattern", Pattern)
            .Add("option_param1", OptionParam1)
            .Add("option_param2", OptionParam2)
            .Add("is_anime_motion", IsAnimeMotion)
            .Add("is_h_anime", IsHAnime);
    }

    public sealed class HandsDto
    {
        public int Left, Right, Count;

        public JsonNode ToJson() => new JsonObject()
            .Add("left", Left)
            .Add("right", Right)
            .Add("count", Count);
    }

    // Clothes slots in ChaFileDefine.ClothesKind order (the index ChaControl.SetClothesState
    // takes), the three dressing states in ChaFileStatus.clothesState order, and the liquid
    // parts in ChaFileDefine.SiruParts order.
    public static class OutfitNames
    {
        public static readonly string[] Clothes = { "top", "bot", "inner_t", "inner_b", "gloves", "panst", "socks", "shoes" };
        public static readonly string[] States = { "worn", "half", "off" };
        public static readonly string[] Siru = { "face", "breast", "belly", "back", "hip" };

        public static int Index(string[] names, string name)
        {
            for (var i = 0; i < names.Length; i++)
                if (names[i] == name) return i;
            return -1;
        }

        public static string Join(string[] names)
        {
            return string.Join(", ", names);
        }
    }

    public sealed class CoordinateFileDto
    {
        public string Path, Name, Sex, Modified;

        public JsonNode ToJson() => new JsonObject()
            .Add("path", Path)
            .Add("name", Name)
            .Add("sex", Sex)
            .Add("modified", Modified);
    }

    public sealed class ClothesSlotDto
    {
        public int Index;
        public string Slot, Name, State, LinkedTo;
        public bool Equipped;
        public string[] States;

        public JsonNode ToJson()
        {
            var states = new JsonArray();
            if (States != null)
                foreach (var s in States) states.Add(s);
            return new JsonObject()
                .Add("slot", Slot)
                .Add("index", Index)
                .Add("name", Name)
                .Add("equipped", Equipped)
                .Add("state", State)
                .Add("states", states)
                .Add("linked_to", LinkedTo);
        }
    }

    public sealed class AccessorySlotDto
    {
        public int Slot;
        public string Name, Parent;
        public bool Equipped, Shown;

        public JsonNode ToJson() => new JsonObject()
            .Add("slot", Slot)
            .Add("name", Name)
            .Add("parent", Parent)
            .Add("equipped", Equipped)
            .Add("shown", Shown);
    }

    public sealed class OutfitDto
    {
        public int CharacterId;
        public string Sex;
        public System.Collections.Generic.List<ClothesSlotDto> Clothes;
        public System.Collections.Generic.List<AccessorySlotDto> Accessories;

        public JsonNode ToJson()
        {
            var clothes = new JsonArray();
            if (Clothes != null)
                foreach (var c in Clothes) clothes.Add(c.ToJson());
            var accessories = new JsonArray();
            if (Accessories != null)
                foreach (var a in Accessories) accessories.Add(a.ToJson());
            return new JsonObject()
                .Add("character_id", CharacterId)
                .Add("sex", Sex)
                .Add("clothes", clothes)
                .Add("accessories", accessories);
        }
    }

    // The compact form studio_list_characters carries: the eight states and how many
    // accessory slots are filled and shown. Worn item names, the states each slot
    // actually accepts and the per-slot accessory flags are in studio_list_outfit.
    public sealed class OutfitSummaryDto
    {
        public string[] Clothes;
        public int AccessoriesEquipped, AccessoriesShown;

        public JsonNode ToJson()
        {
            var clothes = new JsonObject();
            if (Clothes != null)
                for (var i = 0; i < OutfitNames.Clothes.Length && i < Clothes.Length; i++)
                    clothes.Add(OutfitNames.Clothes[i], Clothes[i]);
            return new JsonObject()
                .Add("clothes", clothes)
                .Add("accessories", new JsonObject()
                    .Add("equipped", AccessoriesEquipped)
                    .Add("shown", AccessoriesShown));
        }
    }

    // Studio's per-character state panel, minus the face sliders (they belong to
    // studio_set_character_expression). 'nipple' and 'siru' are female-only: their
    // setters are no-ops on a male.
    public sealed class CharacterStateDto
    {
        public float? Nipple;
        public float SkinGloss, Wet, SonLength;
        public bool Simple, Son;
        public float[] SimpleColor;
        public byte[] Siru;

        public JsonNode ToJson()
        {
            JsonNode siru = JsonNull.Instance;
            if (Siru != null)
            {
                var obj = new JsonObject();
                for (var i = 0; i < OutfitNames.Siru.Length && i < Siru.Length; i++)
                    obj.Add(OutfitNames.Siru[i], (int)Siru[i]);
                siru = obj;
            }
            return new JsonObject()
                .Add("nipple", J.N(Nipple))
                .Add("skin_gloss", SkinGloss)
                .Add("wet", Wet)
                .Add("simple", Simple)
                .Add("simple_color", J.Color(SimpleColor))
                .Add("son", Son)
                .Add("son_length", SonLength)
                .Add("siru", siru);
        }
    }

    // Group names for the two kinematic chains, in the exact order Studio indexes
    // them: activeIK by bit position of OIBoneInfo.BoneGroup, activeFK by FKCtrl.parts.
    public static class KinematicGroups
    {
        public static readonly string[] Ik = { "body", "right_leg", "left_leg", "right_arm", "left_arm" };
        public static readonly string[] Fk = { "hair", "neck", "breast", "body", "right_hand", "left_hand", "skirt" };

        public static int Index(string[] names, string name)
        {
            for (var i = 0; i < names.Length; i++)
                if (names[i] == name) return i;
            return -1;
        }

        public static string Join(string[] names)
        {
            return string.Join(", ", names);
        }
    }

    public sealed class KinematicDto
    {
        public string Mode;
        public bool Ik, Fk;
        public bool[] ActiveIk, ActiveFk;

        public JsonNode ToJson() => new JsonObject()
            .Add("mode", Mode)
            .Add("ik", Ik)
            .Add("fk", Fk)
            .Add("ik_groups", Named(KinematicGroups.Ik, ActiveIk))
            .Add("fk_groups", Named(KinematicGroups.Fk, ActiveFk));

        private static JsonNode Named(string[] names, bool[] values)
        {
            if (values == null) return JsonNull.Instance;
            var obj = new JsonObject();
            for (var i = 0; i < names.Length && i < values.Length; i++) obj.Add(names[i], values[i]);
            return obj;
        }

        internal static JsonArray Bools(bool[] values)
        {
            var arr = new JsonArray();
            if (values != null)
                foreach (var v in values) arr.Add(v);
            return arr;
        }
    }

    public sealed class BoneDto
    {
        public int Id, Level;
        public string Bone, Label, Group;
        public bool Weighted;
        public Vec3 Rot;

        public JsonNode ToJson() => new JsonObject()
            .Add("id", Id)
            .Add("bone", Bone)
            .Add("label", Label)
            .Add("group", Group)
            .Add("level", Level)
            .Add("weighted", Weighted)
            .Add("rot", Rot.ToJson());
    }

    public sealed class IkTargetDto
    {
        public int Id;
        public string Target, Group, Bone;
        public bool Rotatable;
        public Vec3 Pos, Rot;

        public JsonNode ToJson() => new JsonObject()
            .Add("id", Id)
            .Add("target", Target)
            .Add("group", Group)
            .Add("bone", Bone)
            .Add("rotatable", Rotatable)
            .Add("pos", Pos.ToJson())
            .Add("rot", Rot.ToJson());
    }

    public sealed class SkeletonDto
    {
        public int CharacterId;
        public string Mode;
        public System.Collections.Generic.List<BoneDto> Bones;
        public System.Collections.Generic.List<IkTargetDto> IkTargets;

        public JsonNode ToJson()
        {
            var bones = new JsonArray();
            if (Bones != null)
                foreach (var b in Bones) bones.Add(b.ToJson());
            var targets = new JsonArray();
            if (IkTargets != null)
                foreach (var t in IkTargets) targets.Add(t.ToJson());
            return new JsonObject()
                .Add("character_id", CharacterId)
                .Add("mode", Mode)
                .Add("bones", bones)
                .Add("ik_targets", targets);
        }
    }

    public sealed class FaceDto
    {
        public int Eyebrows, EyebrowsCount, Eyes, EyesCount, Mouth, LookEyes, LookNeck;
        public float EyesOpen, MouthOpen, Tears, Blush;
        public bool Blink, LipSync;

        public JsonNode ToJson() => new JsonObject()
            .Add("eyebrows", Eyebrows)
            .Add("eyebrows_count", EyebrowsCount)
            .Add("eyes", Eyes)
            .Add("eyes_count", EyesCount)
            .Add("eyes_open", EyesOpen)
            .Add("blink", Blink)
            .Add("mouth", Mouth)
            .Add("mouth_open", MouthOpen)
            .Add("lip_sync", LipSync)
            .Add("tears", Tears)
            .Add("blush", Blush)
            .Add("look_eyes", LookEyes)
            .Add("look_neck", LookNeck);
    }

    public sealed class CharacterDto
    {
        public int Id;
        public string Name, FullName, Sex;
        public bool Visible;
        public TransformDto Transform;
        public AnimeDto Anime;
        public KinematicDto Kinematic;
        public FaceDto Face;
        public HandsDto Hands;
        public OutfitSummaryDto Outfit;
        public CharacterStateDto State;
        public bool[] Expression;

        public JsonNode ToJson() => new JsonObject()
            .Add("id", Id)
            .Add("name", Name)
            .Add("full_name", FullName)
            .Add("sex", Sex)
            .Add("visible", Visible)
            .Add("transform", Transform != null ? Transform.ToJson() : JsonNull.Instance)
            .Add("anime", Anime != null ? Anime.ToJson() : JsonNull.Instance)
            .Add("kinematic", Kinematic != null ? Kinematic.ToJson() : JsonNull.Instance)
            .Add("face", Face != null ? Face.ToJson() : JsonNull.Instance)
            .Add("hands", Hands != null ? Hands.ToJson() : JsonNull.Instance)
            .Add("outfit", Outfit != null ? Outfit.ToJson() : JsonNull.Instance)
            .Add("state", State != null ? State.ToJson() : JsonNull.Instance)
            .Add("expression", KinematicDto.Bools(Expression));
    }

    public sealed class CharacterCardDto
    {
        public string Path, Name, Sex, Modified;

        public JsonNode ToJson() => new JsonObject()
            .Add("path", Path)
            .Add("name", Name)
            .Add("sex", Sex)
            .Add("modified", Modified);
    }

    public sealed class PoseFileDto
    {
        public string Path, Name, Sex;

        public JsonNode ToJson() => new JsonObject()
            .Add("path", Path)
            .Add("name", Name)
            .Add("sex", Sex);
    }

    public sealed class LightDto
    {
        public int No;
        public string Type, Target;
        public float[] Color;
        public float Intensity, Range, SpotAngle;
        public bool Enabled, Shadow;

        public JsonNode ToJson()
        {
            var color = new JsonArray();
            if (Color != null)
                foreach (var c in Color) color.Add(c);
            return new JsonObject()
                .Add("no", No)
                .Add("type", Type)
                .Add("target", Target)
                .Add("color", color)
                .Add("intensity", Intensity)
                .Add("range", Range)
                .Add("spot_angle", SpotAngle)
                .Add("enabled", Enabled)
                .Add("shadow", Shadow);
        }
    }

    public sealed class AttachPointDto
    {
        public int Index;
        public string Name;
        public bool Available;
        public System.Collections.Generic.List<int> Attached;

        public JsonNode ToJson()
        {
            var attached = new JsonArray();
            if (Attached != null)
                foreach (var id in Attached) attached.Add(id);
            return new JsonObject()
                .Add("index", Index)
                .Add("name", Name)
                .Add("available", Available)
                .Add("attached", attached);
        }
    }

    public sealed class ObjectDto
    {
        public int Id;
        public string Kind, Name, AttachPointName;
        public bool Visible;
        public int? ParentId, AttachPointIndex;
        public TransformDto Transform;
        public LightDto Light;

        public JsonNode ToJson() => new JsonObject()
            .Add("id", Id)
            .Add("kind", Kind)
            .Add("name", Name)
            .Add("visible", Visible)
            .Add("parent_id", ParentId.HasValue ? (JsonNode)ParentId.Value : JsonNull.Instance)
            .Add("attach_point", AttachPointIndex.HasValue
                ? new JsonObject().Add("index", AttachPointIndex.Value).Add("name", AttachPointName)
                : (JsonNode)JsonNull.Instance)
            .Add("transform", Transform != null ? Transform.ToJson() : JsonNull.Instance)
            .Add("light", Light != null ? Light.ToJson() : JsonNull.Instance);
    }

    public sealed class CameraDto
    {
        public Vec3 TargetPos, Angle, WorldPos, WorldRot;
        public float Fov, Distance;

        public JsonNode ToJson() => new JsonObject()
            .Add("target_pos", TargetPos.ToJson())
            .Add("angle", Angle.ToJson())
            .Add("fov", Fov)
            .Add("distance", Distance)
            .Add("world_pos", WorldPos.ToJson())
            .Add("world_rot", WorldRot.ToJson());
    }

    public sealed class CatalogEntryDto
    {
        public string Catalog, Name, GroupName, CategoryName, Target;
        public int No;
        public int? Group, Category;

        public JsonNode ToJson() => new JsonObject()
            .Add("catalog", Catalog)
            .Add("group", Group.HasValue ? (JsonNode)Group.Value : JsonNull.Instance)
            .Add("category", Category.HasValue ? (JsonNode)Category.Value : JsonNull.Instance)
            .Add("no", No)
            .Add("name", Name)
            .Add("group_name", GroupName)
            .Add("category_name", CategoryName)
            .Add("target", Target);
    }

    public sealed class SceneFileDto
    {
        public string Path, Folder;
        public long Size;

        public JsonNode ToJson() => new JsonObject()
            .Add("path", Path)
            .Add("folder", Folder)
            .Add("size", Size);
    }

    public sealed class SceneSaveDto
    {
        public string Path, RelativePath;
        public long Bytes;
        public int Objects, ThumbnailWidth, ThumbnailHeight;
        public bool Overwritten;
        public SceneSummaryDto Scene;

        public JsonNode ToJson() => new JsonObject()
            .Add("path", Path)
            .Add("relative_path", RelativePath)
            .Add("bytes", Bytes)
            .Add("objects", Objects)
            .Add("overwritten", Overwritten)
            .Add("thumbnail", new JsonObject().Add("width", ThumbnailWidth).Add("height", ThumbnailHeight))
            .Add("scene", Scene != null ? Scene.ToJson() : JsonNull.Instance);
    }

    // Absolute-value patch: only the fields the caller supplied are applied.
    public sealed class TransformPatch
    {
        public Vec3? Pos, Rot, Scale;
        public bool IsEmpty => !Pos.HasValue && !Rot.HasValue && !Scale.HasValue;
    }

    public sealed class LightPatch
    {
        public float[] Color;
        public float? Intensity, Range, SpotAngle;
        public bool? Enabled, Shadow;
    }

    public sealed class CameraPatch
    {
        public Vec3? TargetPos, Angle;
        public float? Fov, Distance;
    }

    public sealed class HandsPatch
    {
        public int? Left, Right;
        public bool IsEmpty => !Left.HasValue && !Right.HasValue;
    }

    public sealed class AnimePatch
    {
        public int? Group, Category, No;
        public float? NormalizedTime, Speed, Pattern, OptionParam1, OptionParam2;

        public bool HasClip => Group.HasValue || Category.HasValue || No.HasValue;
        public bool HasFullClip => Group.HasValue && Category.HasValue && No.HasValue;

        public bool IsEmpty =>
            !HasClip && !NormalizedTime.HasValue && !Speed.HasValue && !Pattern.HasValue &&
            !OptionParam1.HasValue && !OptionParam2.HasValue;
    }

    // Mode and group flags are independent: a group patch stores its flags whatever
    // the mode is, and only takes visible effect while its chain is enabled.
    public sealed class KinematicsPatch
    {
        public string Mode, Reflect;
        public bool?[] Fk, Ik;

        public bool IsEmpty => Mode == null && Reflect == null && !Any(Fk) && !Any(Ik);

        private static bool Any(bool?[] values)
        {
            if (values == null) return false;
            foreach (var v in values)
                if (v.HasValue) return true;
            return false;
        }
    }

    public sealed class IkTargetPatch
    {
        public string Target;
        public Vec3? Pos, Rot;
    }

    // 'no' spans frames (the map loads in a coroutine) and resets the map transform,
    // so a patch carrying both applies 'pos'/'rot' after the new map has settled.
    public sealed class MapPatch
    {
        public int? No;
        public Vec3? Pos, Rot;
        public bool? Option, Light;

        public bool IsEmpty => !No.HasValue && !Pos.HasValue && !Rot.HasValue && !Option.HasValue && !Light.HasValue;
    }

    public sealed class SceneLightingPatch
    {
        public float[] Color, Rot;
        public float? Intensity;
        public bool? Shadow;

        public bool IsEmpty => Color == null && Rot == null && !Intensity.HasValue && !Shadow.HasValue;
    }

    // null leaves an overlay untouched, "" clears it — which is why these are strings
    // and not the usual nullable value types.
    public sealed class OverlayPatch
    {
        public string Background, Frame;

        public bool IsEmpty => Background == null && Frame == null;
    }

    // 'Coordinate' is applied first and on its own frame of reference: loading one
    // rebuilds which states each slot accepts and which accessory slots are filled,
    // so the rest of the patch can only be checked once it has landed.
    public sealed class OutfitPatch
    {
        public string Coordinate, ClothesAll;
        public System.Collections.Generic.Dictionary<string, string> Clothes;
        public System.Collections.Generic.Dictionary<int, bool> Accessories;

        public bool IsEmpty =>
            Coordinate == null && ClothesAll == null &&
            (Clothes == null || Clothes.Count == 0) &&
            (Accessories == null || Accessories.Count == 0);
    }

    public sealed class CharacterStatePatch
    {
        public float? Nipple, SkinGloss, Wet, SonLength;
        public bool? Simple, Son;
        public float[] SimpleColor;
        public System.Collections.Generic.Dictionary<string, int> Siru;

        public bool IsEmpty =>
            !Nipple.HasValue && !SkinGloss.HasValue && !Wet.HasValue && !SonLength.HasValue &&
            !Simple.HasValue && !Son.HasValue && SimpleColor == null &&
            (Siru == null || Siru.Count == 0);
    }

    public sealed class ExpressionPatch
    {
        public int? Eyebrows, Eyes, Mouth, LookEyes, LookNeck;
        public float? EyesOpen, MouthOpen, Tears, Blush;
        public bool? Blink, LipSync;
        public System.Collections.Generic.Dictionary<int, bool> Categories;

        public bool IsEmpty =>
            !Eyebrows.HasValue && !Eyes.HasValue && !Mouth.HasValue && !LookEyes.HasValue && !LookNeck.HasValue &&
            !EyesOpen.HasValue && !MouthOpen.HasValue && !Tears.HasValue && !Blush.HasValue &&
            !Blink.HasValue && !LipSync.HasValue && (Categories == null || Categories.Count == 0);
    }

    // A keyframe value is whatever type its interpolable holds, so it crosses the
    // adapter boundary already rendered as JSON rather than as a boxed Unity type.
    public sealed class TimelineKeyframeDto
    {
        public float Time;
        public JsonNode Value;
        public JsonNode Curve;

        public JsonNode ToJson() => new JsonObject()
            .Add("time", Time)
            .Add("value", Value)
            .Add("curve", Curve);
    }

    public sealed class TimelineInterpolableDto
    {
        public int Id;
        public string Owner, Model, Name, Alias, Target;
        public bool Enabled;
        public int? ObjectId;
        public int KeyframeCount;
        public System.Collections.Generic.List<TimelineKeyframeDto> Keyframes;

        public JsonNode ToJson()
        {
            var node = new JsonObject()
                .Add("id", Id)
                .Add("owner", Owner)
                .Add("model", Model)
                .Add("name", Name)
                .Add("alias", Alias)
                .Add("enabled", Enabled)
                .Add("object_id", ObjectId.HasValue ? (JsonNode)ObjectId.Value : JsonNull.Instance)
                .Add("target", Target)
                .Add("keyframe_count", KeyframeCount);
            if (Keyframes != null)
            {
                var arr = new JsonArray();
                foreach (var k in Keyframes) arr.Add(k.ToJson());
                node.Add("keyframes", arr);
            }
            return node;
        }
    }

    public sealed class TimelineStateDto
    {
        public bool Available;
        public string Version, Unavailable;
        public bool IsPlaying;
        public float PlaybackTime, Duration;
        public int FrameRate, InterpolableCount, KeyframeCount;
        public System.Collections.Generic.List<TimelineInterpolableDto> Interpolables;

        public JsonNode ToJson()
        {
            var node = new JsonObject()
                .Add("available", Available)
                .Add("version", Version)
                .Add("unavailable", Available ? JsonNull.Instance : (JsonNode)Unavailable)
                .Add("is_playing", IsPlaying)
                .Add("playback_time", PlaybackTime)
                .Add("duration", Duration)
                .Add("frame_rate", FrameRate)
                .Add("interpolable_count", InterpolableCount)
                .Add("keyframe_count", KeyframeCount);
            if (Interpolables != null)
            {
                var arr = new JsonArray();
                foreach (var i in Interpolables) arr.Add(i.ToJson());
                node.Add("interpolables", arr);
            }
            return node;
        }
    }

    public sealed class TimelineModelDto
    {
        public string Owner, Model, Name;
        public bool Dynamic, Compatible;

        public JsonNode ToJson() => new JsonObject()
            .Add("owner", Owner)
            .Add("model", Model)
            .Add("name", Name)
            .Add("dynamic", Dynamic);
    }

    public sealed class TimelineSingleFileDto
    {
        public string Name, Modified;
        public long Size;

        public JsonNode ToJson() => new JsonObject()
            .Add("name", Name)
            .Add("size", Size)
            .Add("modified", Modified);
    }

    // One end of a constraint. A constraint links two raw transforms, so the pair
    // (object_id, target) is how the API names one: 'path' is the same
    // root-relative form NodesConstraints writes into the scene file.
    public sealed class ConstraintNodeDto
    {
        public int? ObjectId;
        public string Target, Path;

        public JsonNode ToJson() => new JsonObject()
            .Add("object_id", ObjectId.HasValue ? (JsonNode)ObjectId.Value : JsonNull.Instance)
            .Add("target", Target)
            .Add("path", Path);
    }

    public sealed class ConstraintDto
    {
        public int Id;
        public bool Enabled;
        public string Alias;
        public ConstraintNodeDto Parent, Child;
        public bool Position, Rotation, Scale, LookAt;
        public bool MirrorPosition, MirrorRotation, MirrorScale;
        public Vec3 PositionOffset, RotationOffset, ScaleOffset;
        public float PositionFactor, RotationFactor, ScaleFactor;
        public float PositionDamp, RotationDamp, ScaleDamp;
        public bool[] PositionLocks, RotationLocks, ScaleLocks;
        public bool ResetPosition, ResetRotation, ResetScale;
        public bool FixDynamicBone;

        public JsonNode ToJson() => new JsonObject()
            .Add("id", Id)
            .Add("enabled", Enabled)
            .Add("alias", Alias)
            .Add("parent", Parent != null ? Parent.ToJson() : JsonNull.Instance)
            .Add("child", Child != null ? Child.ToJson() : JsonNull.Instance)
            .Add("position", Position)
            .Add("rotation", Rotation)
            .Add("scale", Scale)
            .Add("look_at", LookAt)
            .Add("mirror_position", MirrorPosition)
            .Add("mirror_rotation", MirrorRotation)
            .Add("mirror_scale", MirrorScale)
            .Add("position_offset", PositionOffset.ToJson())
            .Add("rotation_offset", RotationOffset.ToJson())
            .Add("scale_offset", ScaleOffset.ToJson())
            .Add("position_factor", PositionFactor)
            .Add("rotation_factor", RotationFactor)
            .Add("scale_factor", ScaleFactor)
            .Add("position_damp", PositionDamp)
            .Add("rotation_damp", RotationDamp)
            .Add("scale_damp", ScaleDamp)
            .Add("position_locks", Axes(PositionLocks))
            .Add("rotation_locks", Axes(RotationLocks))
            .Add("scale_locks", Axes(ScaleLocks))
            .Add("reset_position", ResetPosition)
            .Add("reset_rotation", ResetRotation)
            .Add("reset_scale", ResetScale)
            .Add("fix_dynamic_bone", FixDynamicBone);

        private static JsonNode Axes(bool[] axes) => axes == null
            ? (JsonNode)JsonNull.Instance
            : new JsonArray().Add(axes[0]).Add(axes[1]).Add(axes[2]);
    }

    public sealed class ConstraintListDto
    {
        public bool Available;
        public string Version, Unavailable;
        public int Count;
        public System.Collections.Generic.List<ConstraintDto> Constraints;

        public JsonNode ToJson()
        {
            var node = new JsonObject()
                .Add("available", Available)
                .Add("version", Version)
                .Add("unavailable", Available ? JsonNull.Instance : (JsonNode)Unavailable)
                .Add("count", Count);
            if (Constraints != null)
            {
                var arr = new JsonArray();
                foreach (var c in Constraints) arr.Add(c.ToJson());
                node.Add("constraints", arr);
            }
            return node;
        }
    }

    public sealed class ConstraintEndpoint
    {
        public int ObjectId;
        public string Target;
    }

    public sealed class ConstraintPatch
    {
        // Create only: a constraint's two nodes are its identity.
        public ConstraintEndpoint Parent, Child;
        public bool? Enabled, Position, Rotation, Scale, LookAt;
        public bool? MirrorPosition, MirrorRotation, MirrorScale;
        public Vec3? PositionOffset, RotationOffset, ScaleOffset;
        public float? PositionFactor, RotationFactor, ScaleFactor;
        public float? PositionDamp, RotationDamp, ScaleDamp;
        public bool[] PositionLocks, RotationLocks, ScaleLocks;
        public bool? ResetPosition, ResetRotation, ResetScale;
        public bool? FixDynamicBone;
        public string Alias;
    }
}
