using System.Collections.Generic;
using Miraneo.Adapters;
using Miraneo.Core;

namespace Miraneo.Services
{
    // Timeline edits all land within one frame, so unlike the map and scene
    // services none of this needs a coroutine. It stays off IStudioApi because
    // Timeline is optional: the contract there describes the vanilla surface.
    public static class TimelineService
    {
        public static string Version => TimelineAdapter.Version;

        public static TimelineStateDto GetState(int? objectId, bool withKeyframes) => TimelineAdapter.GetState(objectId, withKeyframes);
        public static TimelineStateDto Playback(string action, float? time) => TimelineAdapter.Playback(action, time);
        public static TimelineStateDto SetSettings(float? duration, int? frameRate) => TimelineAdapter.SetSettings(duration, frameRate);
        public static List<TimelineModelDto> ListModels(int? objectId, string query, int limit) => TimelineAdapter.ListModels(objectId, query, limit);

        public static TimelineInterpolableDto AddInterpolable(int objectId, string model, string owner, string name, string target, string alias) =>
            TimelineAdapter.AddInterpolable(objectId, model, owner, name, target, alias);

        public static TimelineStateDto RemoveInterpolable(List<int> ids) => TimelineAdapter.RemoveInterpolable(ids);

        public static TimelineInterpolableDto SetKeyframe(int interpolableId, float time, JsonNode value, JsonNode curve) =>
            TimelineAdapter.SetKeyframe(interpolableId, time, value, curve);

        public static TimelineInterpolableDto DeleteKeyframe(int interpolableId, float time) =>
            TimelineAdapter.DeleteKeyframe(interpolableId, time);

        public static JsonNode SingleFile(string action, string name, int? objectId, bool overwrite)
        {
            switch (action)
            {
                case "list":
                {
                    var arr = new JsonArray();
                    foreach (var f in TimelineAdapter.ListSingleFiles()) arr.Add(f.ToJson());
                    return new JsonObject().Add("files", arr);
                }
                case "load":
                    if (!objectId.HasValue)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "action 'load' needs 'object_id': a single file carries one object's interpolables");
                    return TimelineAdapter.LoadSingleFile(name, objectId.Value).ToJson();
                case "save":
                    if (!objectId.HasValue)
                        throw new StudioApiException(StudioApiException.InvalidParams,
                            "action 'save' needs 'object_id': a single file carries one object's interpolables");
                    return TimelineAdapter.SaveSingleFile(name, objectId.Value, overwrite).ToJson();
                case "delete":
                    TimelineAdapter.DeleteSingleFile(name);
                    return new JsonObject().Add("deleted", name);
                default:
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "parameter 'action' must be 'list', 'load', 'save' or 'delete'");
            }
        }
    }
}
