using System;
using System.Collections;
using System.Threading.Tasks;
using Miraneo.Adapters;
using Miraneo.Core;
using UnityEngine;

namespace Miraneo.Services
{
    public static class CameraService
    {
        // Studio applies the camera transform in LateUpdate, so reading the world
        // pose back in the same frame would return the pre-move values. Apply now,
        // report after the frame has been rendered.
        public static Task<JsonNode> SetCamera(MonoBehaviour host, CameraPatch patch)
        {
            StudioAdapter.SetCamera(patch);
            var tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.StartCoroutine(ReadBack(tcs));
            return tcs.Task;
        }

        private static IEnumerator ReadBack(TaskCompletionSource<JsonNode> tcs)
        {
            yield return null;
            try { tcs.TrySetResult(StudioAdapter.GetCamera().ToJson()); }
            catch (Exception e) { tcs.TrySetException(e); }
        }
    }
}
