using System;
using System.Collections;
using System.Threading.Tasks;
using Miraneo.Core;
using UnityEngine;

namespace Miraneo.Services
{
    public static class ScreenshotService
    {
        // Must be called on the Unity main thread (starts a coroutine on the host).
        public static Task<JsonNode> Capture(MonoBehaviour host, string format, int quality)
        {
            var tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.StartCoroutine(CaptureCo(tcs, format, quality));
            return tcs.Task;
        }

        private static IEnumerator CaptureCo(TaskCompletionSource<JsonNode> tcs, string format, int quality)
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex = null;
            try
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                var png = string.Equals(format, "png", StringComparison.OrdinalIgnoreCase);
                var bytes = png ? tex.EncodeToPNG() : tex.EncodeToJPG(Mathf.Clamp(quality, 1, 100));
                tcs.TrySetResult(new JsonObject()
                    .Add("format", png ? "png" : "jpg")
                    .Add("width", tex.width)
                    .Add("height", tex.height)
                    .Add("base64", Convert.ToBase64String(bytes)));
            }
            catch (Exception e)
            {
                tcs.TrySetException(new StudioApiException(StudioApiException.OperationFailed, "screenshot failed: " + e.Message));
            }
            finally
            {
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
        }
    }
}
