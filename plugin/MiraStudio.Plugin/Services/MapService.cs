using System;
using System.Collections;
using System.Threading.Tasks;
using MiraStudio.Adapters;
using MiraStudio.Core;
using UnityEngine;

namespace MiraStudio.Services
{
    public static class MapService
    {
        // Changing the map spans frames: Studio.AddMap starts a coroutine that loads a
        // whole Unity scene, and SetupMap — which resets the map transform — only runs
        // once that is done. Position, rotation and the two visibility toggles are
        // therefore applied afterwards, against the map that actually ended up loaded.
        public static Task<JsonNode> SetMap(MonoBehaviour host, MapPatch patch)
        {
            var change = StudioAdapter.BeginMapChange(patch);
            var tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.StartCoroutine(Apply(tcs, patch, change));
            return tcs.Task;
        }

        private const float LoadTimeoutSeconds = 120f;

        private static IEnumerator Apply(TaskCompletionSource<JsonNode> tcs, MapPatch patch, StudioAdapter.MapChange change)
        {
            var changed = patch.No.HasValue;
            if (changed)
            {
                var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
                while (!StudioAdapter.MapChangeDone(change))
                {
                    if (Time.realtimeSinceStartup > deadline)
                    {
                        StudioAdapter.EndMapChange(change);
                        tcs.TrySetException(new StudioApiException(StudioApiException.OperationFailed,
                            "the map did not finish loading within " + LoadTimeoutSeconds + " s"));
                        yield break;
                    }
                    yield return null;
                }
                StudioAdapter.EndMapChange(change);
                // SetupMap's own listeners (camera colliders, vanish objects) run from
                // the same call, so the map is only really in place the next frame.
                yield return null;
            }

            try
            {
                tcs.TrySetResult(StudioAdapter.CompleteMapChange(patch, changed).ToJson());
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }
    }
}
