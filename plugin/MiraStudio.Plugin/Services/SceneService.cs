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

        // Saving itself is synchronous, but SceneInfo.Save embeds a thumbnail
        // rendered from the live cameras: the viewport transform is only applied in
        // LateUpdate, and a character added in the same dispatcher drain has not been
        // posed by its Animator yet (OCIChar.OnSavePreprocessing dereferences it with
        // no guard). Waiting for the end of the frame is what Studio's own
        // GameScreenShot.CaptureFunc does before capturing.
        public static Task<JsonNode> SaveScene(MonoBehaviour host, string name, bool overwrite)
        {
            if (!StudioAdapter.IsStudioReady())
                throw new StudioApiException(StudioApiException.StudioNotReady, "Studio NEO V2 scene is not loaded yet");
            var target = StudioAdapter.ResolveSaveScenePath(name, overwrite, out var overwriting);

            var tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.StartCoroutine(SaveCo(tcs, target, overwriting));
            return tcs.Task;
        }

        private static IEnumerator SaveCo(TaskCompletionSource<JsonNode> tcs, string target, bool overwriting)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                tcs.TrySetResult(StudioAdapter.SaveScene(target, overwriting).ToJson());
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
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
                // Studio reloads the map, the overlays and the screen effects from the
                // scene it just read, but never the chara light: CameraLightCtrl is
                // only driven by its own panel, so without this the viewport keeps the
                // previous lighting while sceneInfo already holds the new one.
                StudioAdapter.ReflectSceneLighting();
                tcs.TrySetResult(new JsonObject()
                    .Add("path", path)
                    .Add("scene", StudioAdapter.GetScene(false).ToJson()));
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }
    }
}
