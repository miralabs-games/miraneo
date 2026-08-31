using BepInEx;
using BepInEx.Configuration;
using MiraStudio.Adapters;
using MiraStudio.Core;
using MiraStudio.Services;
using MiraStudio.Transport;
using UnityEngine;

namespace MiraStudio
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class MiraStudioPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mirastudio.plugin";
        public const string PluginName = "MiraStudio";
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
            _port = Config.Bind("Server", "Port", 6472, "TCP port of the local MiraStudio JSON-RPC server (http://localhost:<port>)");
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
            _server.Register("studio_list_attach_points", p => ToArray(
                _service.ListAttachPoints(Params.ReqInt(p, "id")).ConvertAll(e => e.ToJson())));

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
            _server.RegisterAsync("studio_load_scene", p => SceneService.LoadScene(this, Params.ReqString(p, "path")), 180000);
            _server.RegisterAsync("studio_save_scene", p => SceneService.SaveScene(this,
                Params.OptString(p, "name"),
                Params.OptBool(p, "overwrite") ?? false), 60000);

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
