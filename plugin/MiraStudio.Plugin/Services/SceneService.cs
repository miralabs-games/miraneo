using System;
using System.Collections;
using System.Threading.Tasks;
using MiraStudio.Adapters;
using MiraStudio.Core;
using UnityEngine;
using SNeo = global::Studio;

namespace MiraStudio.Services
{
    public static class SceneService
    {
        // Scene loading is asynchronous in Studio (the map loads in a coroutine),
        // so the RPC only completes once the whole scene is actually in place.
        public static Task<JsonNode> LoadScene(MonoBehaviour host, string path)
        {
            var resolved = StudioAdapter.ResolveScenePath(path);
            if (!StudioAdapter.IsStudioReady())
                throw new StudioApiException(StudioApiException.StudioNotReady, "Studio NEO V2 scene is not loaded yet");

            var tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.StartCoroutine(LoadCo(host, tcs, resolved));
            return tcs.Task;
        }

        private static IEnumerator LoadCo(MonoBehaviour host, TaskCompletionSource<JsonNode> tcs, string path)
        {
            var failure = (string)null;
            var routine = (Coroutine)null;
            try
            {
                routine = host.StartCoroutine(Singleton<SNeo.Studio>.Instance.LoadSceneCoroutine(path));
            }
            catch (Exception e)
            {
                failure = e.Message;
            }

            if (failure == null)
            {
                yield return routine;
                yield return null;
            }

            if (failure != null)
            {
                tcs.TrySetException(new StudioApiException(StudioApiException.OperationFailed, "scene load failed: " + failure));
                yield break;
            }

            try
            {
                tcs.TrySetResult(new JsonObject()
                    .Add("path", path)
                    .Add("scene", StudioAdapter.GetScene().ToJson()));
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }
    }
}
