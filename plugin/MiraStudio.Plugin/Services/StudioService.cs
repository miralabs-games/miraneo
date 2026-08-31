using System.Collections.Generic;
using MiraStudio.Adapters;
using MiraStudio.Core;

namespace MiraStudio.Services
{
    public sealed class StudioService : IStudioApi
    {
        private readonly string _pluginVersion;

        public StudioService(string pluginVersion)
        {
            _pluginVersion = pluginVersion;
        }

        public RuntimeInfoDto GetRuntimeInfo() => StudioAdapter.GetRuntimeInfo(_pluginVersion);
        public SceneSummaryDto GetScene() => StudioAdapter.GetScene();
        public List<CharacterDto> ListCharacters() => StudioAdapter.ListCharacters();
        public List<ObjectDto> ListObjects() => StudioAdapter.ListObjects();
        public CameraDto GetCamera() => StudioAdapter.GetCamera();
        public List<CatalogEntryDto> SearchCatalog(string catalog, string query, int limit) => StudioAdapter.SearchCatalog(catalog, query, limit);
        public List<SceneFileDto> ListScenes(string query, int limit) => StudioAdapter.ListScenes(query, limit);
        public List<CharacterCardDto> ListCharacterCards(string sex, string query, int limit) => StudioAdapter.ListCharacterCards(sex, query, limit);
        public List<PoseFileDto> ListPoses(string sex, string query, int limit) => StudioAdapter.ListPoses(sex, query, limit);
        public List<CoordinateFileDto> ListCoordinates(string sex, string query, int limit) => StudioAdapter.ListCoordinates(sex, query, limit);
        public List<AttachPointDto> ListAttachPoints(int id) => StudioAdapter.ListAttachPoints(id);
        public SkeletonDto ListBones(int id) => StudioAdapter.ListBones(id);
        public OutfitDto ListOutfit(int id) => StudioAdapter.ListOutfit(id);
        public List<OverlayFileDto> ListOverlays(string kind, string query, int limit) => StudioAdapter.ListOverlays(kind, query, limit);

        public CameraDto SetCamera(CameraPatch patch) => StudioAdapter.SetCamera(patch);
        public ObjectDto CreateItem(int group, int category, int no, TransformPatch transform) => StudioAdapter.CreateItem(group, category, no, transform);
        public ObjectDto CreateLight(int no, TransformPatch transform, LightPatch light) => StudioAdapter.CreateLight(no, transform, light);
        public ObjectDto SetLight(int id, LightPatch light) => StudioAdapter.SetLight(id, light);
        public ObjectDto SetObjectTransform(int id, TransformPatch transform) => StudioAdapter.SetObjectTransform(id, transform);
        public int DeleteObject(int id) => StudioAdapter.DeleteObject(id);
        public ObjectDto SetObjectVisible(int id, bool visible) => StudioAdapter.SetObjectVisible(id, visible);
        public ObjectDto RenameObject(int id, string name) => StudioAdapter.RenameObject(id, name);
        public ObjectDto CreateFolder(string name, TransformPatch transform) => StudioAdapter.CreateFolder(name, transform);
        public DuplicateDto DuplicateObject(List<int> ids) => StudioAdapter.DuplicateObject(ids);
        public ObjectDto CreateCharacter(string card, string sex, TransformPatch transform) => StudioAdapter.CreateCharacter(card, sex, transform);
        public CharacterDto SetCharacterPose(int id, string path) => StudioAdapter.SetCharacterPose(id, path);
        public CharacterDto SetCharacterAnime(int id, AnimePatch patch) => StudioAdapter.SetCharacterAnime(id, patch);
        public CharacterDto SetCharacterExpression(int id, ExpressionPatch patch) => StudioAdapter.SetCharacterExpression(id, patch);
        public CharacterDto SetCharacterHands(int id, HandsPatch patch) => StudioAdapter.SetCharacterHands(id, patch);
        public OutfitDto SetCharacterOutfit(int id, OutfitPatch patch) => StudioAdapter.SetCharacterOutfit(id, patch);
        public CharacterDto SetCharacterState(int id, CharacterStatePatch patch) => StudioAdapter.SetCharacterState(id, patch);
        public ObjectDto AttachObject(int id, int? parentId, int? attachPoint, string attachPointName) => StudioAdapter.AttachObject(id, parentId, attachPoint, attachPointName);
        public SkeletonDto SetCharacterBone(int id, List<KeyValuePair<string, Vec3>> rotations) => StudioAdapter.SetCharacterBone(id, rotations);
        public SkeletonDto SetCharacterIk(int id, List<IkTargetPatch> targets) => StudioAdapter.SetCharacterIk(id, targets);
        public SceneLightingDto SetSceneLighting(SceneLightingPatch patch) => StudioAdapter.SetSceneLighting(patch);
        public OverlaysDto SetOverlay(OverlayPatch patch) => StudioAdapter.SetOverlay(patch);
        public PostProcessingDto SetPostProcessing(PostProcessingDto patch) => StudioAdapter.SetPostProcessing(patch);
    }
}
