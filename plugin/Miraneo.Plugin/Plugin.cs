using BepInEx;
using BepInEx.Configuration;
using Miraneo.Adapters;
using Miraneo.Core;
using Miraneo.Services;
using Miraneo.Transport;
using UnityEngine;

namespace Miraneo
{
    // Soft dependencies so BepInEx loads the optional plugins first when they are
    // installed; both bridges bind by reflection either way and every other tool
    // works without them.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(TimelineAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(NodesConstraintsAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MiraneoPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.miraneo.plugin";
        public const string PluginName = "Miraneo";
        public const string PluginVersion = "0.1.0";

        private ConfigEntry<int> _port;
        private ConfigEntry<bool> _runInBackground;
        private MainThreadDispatcher _dispatcher;
        private StudioService _service;
        private RpcServer _server;
        // Updated from Update() so /health never touches Unity from a worker thread.
        private volatile bool _studioReady;

        private void Awake()
        {
            _port = Config.Bind("Server", "Port", 6472, "TCP port of the local Miraneo JSON-RPC server (http://localhost:<port>)");
            _runInBackground = Config.Bind("Server", "RunInBackground", true, "Keep Unity running while the window is unfocused so external agents can keep driving Studio");
            if (_runInBackground.Value) Application.runInBackground = true;

            _dispatcher = new MainThreadDispatcher();
            _service = new StudioService(PluginVersion);
            _server = new RpcServer(Logger, _dispatcher, () => new JsonObject()
                .Add("ok", true)
                .Add("studio", _studioReady)
                .Add("version", PluginVersion));

            _server.Register("studio_get_info", p => _service.GetRuntimeInfo().ToJson());
            _server.Register("studio_get_scene", p => _service.GetScene().ToJson());
            _server.Register("studio_list_characters", p =>
            {
                var arr = new JsonArray();
                foreach (var c in _service.ListCharacters()) arr.Add(c.ToJson());
                return arr;
            });
            _server.Register("studio_list_objects", p =>
            {
                var arr = new JsonArray();
                foreach (var o in _service.ListObjects()) arr.Add(o.ToJson());
                return arr;
            });
            _server.Register("studio_get_camera", p => _service.GetCamera().ToJson());
            _server.RegisterAsync("studio_screenshot", p =>
            {
                if (!StudioAdapter.IsStudioReady())
                    throw new StudioApiException(StudioApiException.StudioNotReady, "Studio NEO V2 scene is not loaded yet");
                return ScreenshotService.Capture(this, p.GetString("format", "jpg"), p.GetInt("quality", 85));
            }, 60000);

            _server.Register("studio_search_catalog", p => ToArray(_service.SearchCatalog(
                Params.OptString(p, "catalog", "item"),
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 50)).ConvertAll(e => e.ToJson())));
            _server.Register("studio_list_scenes", p => ToArray(_service.ListScenes(
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 100)).ConvertAll(e => e.ToJson())));
            // First call deserialises every card once, then serves from cache.
            _server.Register("studio_list_character_cards", p => ToArray(_service.ListCharacterCards(
                Params.OptString(p, "sex"),
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 100)).ConvertAll(e => e.ToJson())), 180000);
            _server.Register("studio_list_poses", p => ToArray(_service.ListPoses(
                Params.OptString(p, "sex"),
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 100)).ConvertAll(e => e.ToJson())), 180000);
            // Same first-call sweep as the cards, over ~2000 outfit files.
            _server.Register("studio_list_coordinates", p => ToArray(_service.ListCoordinates(
                Params.OptString(p, "sex"),
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 100)).ConvertAll(e => e.ToJson())), 180000);
            _server.Register("studio_list_outfit", p => _service.ListOutfit(Params.ReqInt(p, "id")).ToJson());
            _server.Register("studio_list_attach_points", p => ToArray(
                _service.ListAttachPoints(Params.ReqInt(p, "id")).ConvertAll(e => e.ToJson())));
            _server.Register("studio_list_bones", p => _service.ListBones(Params.ReqInt(p, "id")).ToJson());
            _server.Register("studio_list_overlays", p => ToArray(_service.ListOverlays(
                Params.OptString(p, "kind"),
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 100)).ConvertAll(e => e.ToJson())));

            _server.RegisterAsync("studio_set_camera", p => CameraService.SetCamera(this, new CameraPatch
            {
                TargetPos = Params.OptVec3(p, "target_pos"),
                Angle = Params.OptVec3(p, "angle"),
                Fov = Params.OptFloat(p, "fov"),
                Distance = Params.OptFloat(p, "distance"),
            }), 30000);
            _server.Register("studio_create_object", p => _service.CreateItem(
                Params.ReqInt(p, "group"),
                Params.ReqInt(p, "category"),
                Params.ReqInt(p, "no"),
                Params.Transform(p)).ToJson(), 60000);
            _server.Register("studio_create_light", p => _service.CreateLight(
                Params.ReqInt(p, "no"),
                Params.Transform(p),
                Params.Light(p)).ToJson(), 60000);
            _server.Register("studio_set_light", p => _service.SetLight(
                Params.ReqInt(p, "id"),
                Params.Light(p)).ToJson());
            _server.Register("studio_set_object_transform", p => _service.SetObjectTransform(
                Params.ReqInt(p, "id"),
                Params.Transform(p)).ToJson());
            _server.Register("studio_delete_object", p => new JsonObject()
                .Add("deleted", _service.DeleteObject(Params.ReqInt(p, "id"))));
            _server.Register("studio_set_object_visible", p => _service.SetObjectVisible(
                Params.ReqInt(p, "id"),
                Params.ReqBool(p, "visible")).ToJson());
            _server.Register("studio_rename_object", p => _service.RenameObject(
                Params.ReqInt(p, "id"),
                Params.ReqString(p, "name")).ToJson());
            _server.Register("studio_create_folder", p => _service.CreateFolder(
                Params.OptString(p, "name"),
                Params.Transform(p)).ToJson());
            // Duplicating a character re-imports its whole card through the scene
            // serialiser, so this is the one workspace tool that is not instant.
            _server.Register("studio_duplicate_object", p => _service.DuplicateObject(
                Params.IntList(p, "id")).ToJson(), 60000);
            _server.Register("studio_create_character", p => _service.CreateCharacter(
                Params.ReqString(p, "card"),
                Params.OptString(p, "sex"),
                Params.Transform(p)).ToJson(), 120000);
            _server.Register("studio_set_character_pose", p => _service.SetCharacterPose(
                Params.ReqInt(p, "id"),
                Params.ReqString(p, "path")).ToJson(), 60000);
            _server.Register("studio_set_character_anime", p => _service.SetCharacterAnime(
                Params.ReqInt(p, "id"),
                Params.Anime(p)).ToJson(), 60000);
            _server.Register("studio_set_character_expression", p => _service.SetCharacterExpression(
                Params.ReqInt(p, "id"),
                Params.Expression(p)).ToJson());
            _server.Register("studio_set_character_hands", p => _service.SetCharacterHands(
                Params.ReqInt(p, "id"),
                Params.Hands(p)).ToJson());
            // Loading a coordinate reloads the character's clothes and accessories.
            _server.Register("studio_set_character_outfit", p => _service.SetCharacterOutfit(
                Params.ReqInt(p, "id"),
                Params.Outfit(p)).ToJson(), 120000);
            _server.Register("studio_set_character_state", p => _service.SetCharacterState(
                Params.ReqInt(p, "id"),
                Params.CharacterState(p)).ToJson());
            _server.Register("studio_attach_object", p =>
            {
                int? point;
                string pointName;
                Params.OptIntOrString(p, "attach_point", out point, out pointName);
                return _service.AttachObject(
                    Params.ReqInt(p, "id"),
                    Params.OptIntN(p, "parent_id"),
                    point,
                    pointName).ToJson();
            });
            _server.RegisterAsync("studio_set_character_kinematics", p => KinematicsService.SetKinematics(this,
                Params.ReqInt(p, "id"),
                Params.Kinematics(p)), 30000);
            _server.Register("studio_set_character_bone", p => _service.SetCharacterBone(
                Params.ReqInt(p, "id"),
                Params.BoneRotations(p)).ToJson());
            _server.Register("studio_set_character_ik", p => _service.SetCharacterIk(
                Params.ReqInt(p, "id"),
                Params.IkTargets(p)).ToJson());
            // Loading a map is a full Unity scene load, like loading a studio scene.
            _server.RegisterAsync("studio_set_map", p => MapService.SetMap(this, Params.Map(p)), 180000);
            _server.Register("studio_set_scene_lighting", p => _service.SetSceneLighting(Params.SceneLighting(p)).ToJson());
            _server.Register("studio_set_overlay", p => _service.SetOverlay(Params.Overlay(p)).ToJson(), 60000);
            _server.Register("studio_set_post_processing", p => _service.SetPostProcessing(Params.PostProcessing(p)).ToJson(), 60000);
            _server.RegisterAsync("studio_load_scene", p => SceneService.LoadScene(this, Params.ReqString(p, "path")), 180000);
            _server.RegisterAsync("studio_save_scene", p => SceneService.SaveScene(this,
                Params.OptString(p, "name"),
                Params.OptBool(p, "overwrite") ?? false), 60000);

            // Registered whether or not Timeline is installed: the MCP tool list is
            // static, so a missing plugin must answer with a readable error rather
            // than 'unknown method'. studio_get_info reports which it is.
            _server.Register("studio_timeline_get_state", p => TimelineService.GetState(
                Params.OptIntN(p, "object_id"),
                Params.OptBool(p, "include_keyframes") ?? true).ToJson());
            _server.Register("studio_timeline_playback", p => TimelineService.Playback(
                Params.ReqString(p, "action"),
                Params.OptFloat(p, "time")).ToJson());
            _server.Register("studio_timeline_set_settings", p => TimelineService.SetSettings(
                Params.OptFloat(p, "duration"),
                Params.OptIntN(p, "frame_rate")).ToJson());
            _server.Register("studio_timeline_list_models", p => ToArray(TimelineService.ListModels(
                Params.OptIntN(p, "object_id"),
                Params.OptString(p, "query"),
                Params.OptInt(p, "limit", 100)).ConvertAll(e => e.ToJson())));
            _server.Register("studio_timeline_add_interpolable", p => TimelineService.AddInterpolable(
                Params.ReqInt(p, "object_id"),
                Params.ReqString(p, "model"),
                Params.OptString(p, "owner"),
                Params.OptString(p, "name"),
                Params.OptString(p, "target"),
                Params.OptString(p, "alias")).ToJson());
            _server.Register("studio_timeline_remove_interpolable", p => TimelineService.RemoveInterpolable(
                Params.IntList(p, "id")).ToJson());
            _server.Register("studio_timeline_set_keyframe", p => TimelineService.SetKeyframe(
                Params.ReqInt(p, "interpolable_id"),
                Params.ReqFloat(p, "time"),
                p.Get("value"),
                p.Get("curve")).ToJson());
            _server.Register("studio_timeline_delete_keyframe", p => TimelineService.DeleteKeyframe(
                Params.ReqInt(p, "interpolable_id"),
                Params.ReqFloat(p, "time")).ToJson());
            _server.Register("studio_timeline_single_file", p => TimelineService.SingleFile(
                Params.ReqString(p, "action"),
                Params.OptString(p, "name"),
                Params.OptIntN(p, "object_id"),
                Params.OptBool(p, "overwrite") ?? false), 60000);

            // Same deal as the timeline tools: always registered, and failing with
            // a readable -32001 when NodesConstraints is not installed.
            _server.Register("studio_constraint_list", p => ConstraintService.List(
                Params.OptIntN(p, "object_id")).ToJson());
            _server.Register("studio_constraint_create", p => ConstraintService.Create(Params.Constraint(p)).ToJson());
            _server.Register("studio_constraint_set", p => ConstraintService.Set(
                Params.ReqInt(p, "id"),
                Params.Constraint(p)).ToJson());
            _server.Register("studio_constraint_delete", p => ConstraintService.Delete(
                Params.IntList(p, "id")).ToJson());

            _server.RegisterBatch("studio_execute_batch", 32);

            _server.Start(_port.Value);
        }

        private static JsonArray ToArray(System.Collections.Generic.List<JsonNode> nodes)
        {
            var arr = new JsonArray();
            foreach (var n in nodes) arr.Add(n);
            return arr;
        }

        private void Update()
        {
            _studioReady = StudioAdapter.IsStudioReady();
            _dispatcher.Drain();
        }

        private void OnDestroy()
        {
            if (_server != null) _server.Dispose();
        }
    }
}
