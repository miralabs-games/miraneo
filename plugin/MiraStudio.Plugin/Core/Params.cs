namespace MiraStudio.Core
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

        public static float? OptFloat(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var n = node as JsonNumber;
            if (n == null) throw Invalid(key, "a number");
            return (float)n.Value;
        }

        public static bool? OptBool(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var b = node as JsonBool;
            if (b == null) throw Invalid(key, "a boolean");
            return b.Value;
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

        // {"0": true, "3": false} — keys are expression category indices.
        private static System.Collections.Generic.Dictionary<int, bool> BoolMap(JsonObject p, string key)
        {
            var node = p.Get(key);
            if (node == null || node is JsonNull) return null;
            var obj = node as JsonObject;
            if (obj == null) throw Invalid(key, "an object mapping category index to boolean");
            var map = new System.Collections.Generic.Dictionary<int, bool>();
            foreach (var name in obj.Keys)
            {
                int index;
                var value = obj.Get(name) as JsonBool;
                if (!int.TryParse(name, out index) || value == null)
                    throw Invalid(key, "an object mapping category index to boolean");
                map[index] = value.Value;
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
