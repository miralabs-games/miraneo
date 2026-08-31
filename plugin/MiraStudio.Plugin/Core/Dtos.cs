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

        public JsonNode ToJson() => new JsonObject()
            .Add("product_name", ProductName)
            .Add("unity_version", UnityVersion)
            .Add("game_version", GameVersion)
            .Add("plugin_version", PluginVersion)
            .Add("bepinex_version", BepInExVersion)
            .Add("active_scene", ActiveScene)
            .Add("studio_loaded", StudioLoaded)
            .Add("scene_object_count", SceneObjectCount);
    }

    public sealed class SceneSummaryDto
    {
        public int Total, Characters, Items, Lights, Folders, Routes, Cameras;
        public string Background, Frame;

        public JsonNode ToJson() => new JsonObject()
            .Add("total_objects", Total)
            .Add("characters", Characters)
            .Add("items", Items)
            .Add("lights", Lights)
            .Add("folders", Folders)
            .Add("routes", Routes)
            .Add("cameras", Cameras)
            .Add("background", Background)
            .Add("frame", Frame);
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
}
