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

        public CameraDto SetCamera(CameraPatch patch) => StudioAdapter.SetCamera(patch);
        public ObjectDto CreateItem(int group, int category, int no, TransformPatch transform) => StudioAdapter.CreateItem(group, category, no, transform);
        public ObjectDto CreateLight(int no, TransformPatch transform, LightPatch light) => StudioAdapter.CreateLight(no, transform, light);
        public ObjectDto SetLight(int id, LightPatch light) => StudioAdapter.SetLight(id, light);
        public ObjectDto SetObjectTransform(int id, TransformPatch transform) => StudioAdapter.SetObjectTransform(id, transform);
        public int DeleteObject(int id) => StudioAdapter.DeleteObject(id);
        public ObjectDto CreateCharacter(string card, string sex, TransformPatch transform) => StudioAdapter.CreateCharacter(card, sex, transform);
        public CharacterDto SetCharacterPose(int id, string path) => StudioAdapter.SetCharacterPose(id, path);
        public CharacterDto SetCharacterAnime(int id, int group, int category, int no, float? normalizedTime) => StudioAdapter.SetCharacterAnime(id, group, category, no, normalizedTime);
        public CharacterDto SetCharacterExpression(int id, ExpressionPatch patch) => StudioAdapter.SetCharacterExpression(id, patch);
    }
}
