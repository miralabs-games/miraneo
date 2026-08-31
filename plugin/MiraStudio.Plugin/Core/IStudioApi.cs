using System;
using System.Collections.Generic;

namespace MiraStudio.Core
{
    public interface IStudioApi
    {
        // Read
        RuntimeInfoDto GetRuntimeInfo();
        SceneSummaryDto GetScene();
        List<CharacterDto> ListCharacters();
        List<ObjectDto> ListObjects();
        CameraDto GetCamera();
        List<CatalogEntryDto> SearchCatalog(string catalog, string query, int limit);
        List<SceneFileDto> ListScenes(string query, int limit);
        List<CharacterCardDto> ListCharacterCards(string sex, string query, int limit);
        List<PoseFileDto> ListPoses(string sex, string query, int limit);
        List<AttachPointDto> ListAttachPoints(int id);
        SkeletonDto ListBones(int id);

        // Write
        CameraDto SetCamera(CameraPatch patch);
        ObjectDto CreateItem(int group, int category, int no, TransformPatch transform);
        ObjectDto CreateLight(int no, TransformPatch transform, LightPatch light);
        ObjectDto SetLight(int id, LightPatch light);
        ObjectDto SetObjectTransform(int id, TransformPatch transform);
        int DeleteObject(int id);
        ObjectDto CreateCharacter(string card, string sex, TransformPatch transform);
        CharacterDto SetCharacterPose(int id, string path);
        CharacterDto SetCharacterAnime(int id, AnimePatch patch);
        CharacterDto SetCharacterExpression(int id, ExpressionPatch patch);
        CharacterDto SetCharacterHands(int id, HandsPatch patch);
        ObjectDto AttachObject(int id, int? parentId, int? attachPoint, string attachPointName);
        SkeletonDto SetCharacterBone(int id, List<KeyValuePair<string, Vec3>> rotations);
        SkeletonDto SetCharacterIk(int id, List<IkTargetPatch> targets);
    }

    public sealed class StudioApiException : Exception
    {
        public const int InvalidParams = -32602;
        public const int StudioNotReady = -32000;
        public const int OperationFailed = -32001;
        public const int NotFound = -32003;

        public int Code { get; }

        public StudioApiException(int code, string message) : base(message)
        {
            Code = code;
        }
    }
}
