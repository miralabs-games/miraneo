using System;
using System.Collections;
using System.Threading.Tasks;
using MiraStudio.Adapters;
using MiraStudio.Core;
using UnityEngine;

namespace MiraStudio.Services
{
    public static class KinematicsService
    {
        // Without a reflect this is a plain synchronous write. With one it has to
        // cross a frame: the capture reads the bones the Animator wrote this frame,
        // and it runs *before* the mode change, because enabling a chain makes its
        // own controller overwrite those bones from the stored change amounts.
        public static Task<JsonNode> SetKinematics(MonoBehaviour host, int id, KinematicsPatch patch)
        {
            StudioAdapter.RequireReflect(patch.Reflect);
            if (patch.Reflect == null)
            {
                StudioAdapter.SetKinematics(id, patch);
                return Task.FromResult(StudioAdapter.GetCharacter(id).ToJson());
            }

            var tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.StartCoroutine(ReflectThenApply(id, patch, tcs));
            return tcs.Task;
        }

        private static IEnumerator ReflectThenApply(int id, KinematicsPatch patch, TaskCompletionSource<JsonNode> tcs)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                StudioAdapter.Reflect(id, patch.Reflect);
                StudioAdapter.SetKinematics(id, patch);
                tcs.TrySetResult(StudioAdapter.GetCharacter(id).ToJson());
            }
            catch (Exception e) { tcs.TrySetException(e); }
        }
    }
}
