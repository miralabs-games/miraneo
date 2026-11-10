using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Miraneo.Core
{
    public abstract class JsonNode
    {
        public abstract void Write(StringBuilder sb);

        public override string ToString()
        {
            var sb = new StringBuilder();
            Write(sb);
            return sb.ToString();
        }

        public static implicit operator JsonNode(string value) => value == null ? (JsonNode)JsonNull.Instance : new JsonString(value);
        public static implicit operator JsonNode(bool value) => new JsonBool(value);
        public static implicit operator JsonNode(int value) => new JsonNumber(value);
        public static implicit operator JsonNode(long value) => new JsonNumber(value);
        public static implicit operator JsonNode(float value) => new JsonNumber(value);
        public static implicit operator JsonNode(double value) => new JsonNumber(value);
    }

    public sealed class JsonNull : JsonNode
    {
        public static readonly JsonNull Instance = new JsonNull();
        private JsonNull() { }
        public override void Write(StringBuilder sb) => sb.Append("null");
    }

    public sealed class JsonBool : JsonNode
    {
        public readonly bool Value;
        public JsonBool(bool value) { Value = value; }
        public override void Write(StringBuilder sb) => sb.Append(Value ? "true" : "false");
    }

    public sealed class JsonNumber : JsonNode
    {
        public readonly double Value;
        public JsonNumber(double value) { Value = value; }

        public override void Write(StringBuilder sb)
        {
            if (double.IsNaN(Value) || double.IsInfinity(Value))
            {
                sb.Append("null");
            }
            else if (Value == Math.Floor(Value) && Math.Abs(Value) < 9007199254740992d)
            {
                sb.Append(((long)Value).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(Value.ToString("R", CultureInfo.InvariantCulture));
            }
        }
    }

    public sealed class JsonString : JsonNode
    {
        public readonly string Value;
        public JsonString(string value) { Value = value ?? string.Empty; }
        public override void Write(StringBuilder sb) => WriteEscaped(sb, Value);

        public static void WriteEscaped(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    public sealed class JsonArray : JsonNode, IEnumerable<JsonNode>
    {
        private readonly List<JsonNode> _items = new List<JsonNode>();
        public int Count => _items.Count;
        public JsonNode this[int index] => _items[index];

        public JsonArray Add(JsonNode value)
        {
            _items.Add(value ?? JsonNull.Instance);
            return this;
        }

        public override void Write(StringBuilder sb)
        {
            sb.Append('[');
            for (var i = 0; i < _items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                _items[i].Write(sb);
            }
            sb.Append(']');
        }

        public IEnumerator<JsonNode> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    public sealed class JsonObject : JsonNode
    {
        private readonly List<KeyValuePair<string, JsonNode>> _items = new List<KeyValuePair<string, JsonNode>>();

        public JsonObject Add(string key, JsonNode value)
        {
            _items.Add(new KeyValuePair<string, JsonNode>(key, value ?? JsonNull.Instance));
            return this;
        }

        public JsonNode Get(string key)
        {
            foreach (var kv in _items)
                if (kv.Key == key)
                    return kv.Value;
            return null;
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var kv in _items) yield return kv.Key;
            }
        }

        public string GetString(string key, string fallback = null)
        {
            var s = Get(key) as JsonString;
            return s != null ? s.Value : fallback;
        }

        public int GetInt(string key, int fallback)
        {
            var n = Get(key) as JsonNumber;
            return n != null ? (int)n.Value : fallback;
        }

        public override void Write(StringBuilder sb)
        {
            sb.Append('{');
            for (var i = 0; i < _items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                JsonString.WriteEscaped(sb, _items[i].Key);
                sb.Append(':');
                _items[i].Value.Write(sb);
            }
            sb.Append('}');
        }
    }

    public static class Json
    {
        public static JsonNode Parse(string text)
        {
            if (text == null) throw new FormatException("JSON input is null");
            var pos = 0;
            var value = ParseValue(text, ref pos);
            SkipWhitespace(text, ref pos);
            if (pos != text.Length) throw Error(pos, "trailing characters after JSON value");
            return value;
        }

        private static JsonNode ParseValue(string s, ref int pos)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) throw Error(pos, "unexpected end of input");
            switch (s[pos])
            {
                case '{': return ParseObject(s, ref pos);
                case '[': return ParseArray(s, ref pos);
                case '"': return new JsonString(ParseString(s, ref pos));
                case 't': Expect(s, ref pos, "true"); return new JsonBool(true);
                case 'f': Expect(s, ref pos, "false"); return new JsonBool(false);
                case 'n': Expect(s, ref pos, "null"); return JsonNull.Instance;
                default: return ParseNumber(s, ref pos);
            }
        }

        private static JsonObject ParseObject(string s, ref int pos)
        {
            var obj = new JsonObject();
            pos++;
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == '}') { pos++; return obj; }
            while (true)
            {
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length || s[pos] != '"') throw Error(pos, "expected object key");
                var key = ParseString(s, ref pos);
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length || s[pos] != ':') throw Error(pos, "expected ':'");
                pos++;
                obj.Add(key, ParseValue(s, ref pos));
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) throw Error(pos, "unterminated object");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == '}') { pos++; return obj; }
                throw Error(pos, "expected ',' or '}'");
            }
        }

        private static JsonArray ParseArray(string s, ref int pos)
        {
            var arr = new JsonArray();
            pos++;
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == ']') { pos++; return arr; }
            while (true)
            {
                arr.Add(ParseValue(s, ref pos));
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) throw Error(pos, "unterminated array");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == ']') { pos++; return arr; }
                throw Error(pos, "expected ',' or ']'");
            }
        }

        private static string ParseString(string s, ref int pos)
        {
            pos++;
            var sb = new StringBuilder();
            while (true)
            {
                if (pos >= s.Length) throw Error(pos, "unterminated string");
                var c = s[pos++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (pos >= s.Length) throw Error(pos, "unterminated escape");
                var e = s[pos++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (pos + 4 > s.Length) throw Error(pos, "invalid \\u escape");
                        sb.Append((char)ushort.Parse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        pos += 4;
                        break;
                    default: throw Error(pos - 1, "invalid escape '\\" + e + "'");
                }
            }
        }

        private static JsonNumber ParseNumber(string s, ref int pos)
        {
            var start = pos;
            while (pos < s.Length && (s[pos] == '-' || s[pos] == '+' || s[pos] == '.' || s[pos] == 'e' || s[pos] == 'E' || (s[pos] >= '0' && s[pos] <= '9')))
                pos++;
            if (pos == start) throw Error(pos, "unexpected character '" + s[pos] + "'");
            double value;
            if (!double.TryParse(s.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw Error(start, "invalid number");
            return new JsonNumber(value);
        }

        private static void Expect(string s, ref int pos, string literal)
        {
            if (pos + literal.Length > s.Length || s.Substring(pos, literal.Length) != literal)
                throw Error(pos, "expected '" + literal + "'");
            pos += literal.Length;
        }

        private static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length && (s[pos] == ' ' || s[pos] == '\t' || s[pos] == '\r' || s[pos] == '\n'))
                pos++;
        }

        private static FormatException Error(int pos, string message) => new FormatException("JSON error at " + pos + ": " + message);
    }
}
