import { MiraBridge } from "./bridge.ts";
import getInfo from "../../shared/schemas/studio_get_info.json";
import getScene from "../../shared/schemas/studio_get_scene.json";
import listCharacters from "../../shared/schemas/studio_list_characters.json";
import listObjects from "../../shared/schemas/studio_list_objects.json";
import getCamera from "../../shared/schemas/studio_get_camera.json";
import screenshot from "../../shared/schemas/studio_screenshot.json";
import searchCatalog from "../../shared/schemas/studio_search_catalog.json";
import listScenes from "../../shared/schemas/studio_list_scenes.json";
import listCharacterCards from "../../shared/schemas/studio_list_character_cards.json";
import listPoses from "../../shared/schemas/studio_list_poses.json";
import listCoordinates from "../../shared/schemas/studio_list_coordinates.json";
import listOutfit from "../../shared/schemas/studio_list_outfit.json";
import setCharacterOutfit from "../../shared/schemas/studio_set_character_outfit.json";
import setCharacterState from "../../shared/schemas/studio_set_character_state.json";
import createCharacter from "../../shared/schemas/studio_create_character.json";
import setCharacterPose from "../../shared/schemas/studio_set_character_pose.json";
import setCharacterAnime from "../../shared/schemas/studio_set_character_anime.json";
import setCharacterExpression from "../../shared/schemas/studio_set_character_expression.json";
import setCharacterHands from "../../shared/schemas/studio_set_character_hands.json";
import listAttachPoints from "../../shared/schemas/studio_list_attach_points.json";
import listBones from "../../shared/schemas/studio_list_bones.json";
import setCharacterKinematics from "../../shared/schemas/studio_set_character_kinematics.json";
import setCharacterBone from "../../shared/schemas/studio_set_character_bone.json";
import setCharacterIk from "../../shared/schemas/studio_set_character_ik.json";
import attachObject from "../../shared/schemas/studio_attach_object.json";
import loadScene from "../../shared/schemas/studio_load_scene.json";
import saveScene from "../../shared/schemas/studio_save_scene.json";
import setCamera from "../../shared/schemas/studio_set_camera.json";
import createObject from "../../shared/schemas/studio_create_object.json";
import createLight from "../../shared/schemas/studio_create_light.json";
import setLight from "../../shared/schemas/studio_set_light.json";
import setObjectTransform from "../../shared/schemas/studio_set_object_transform.json";
import deleteObject from "../../shared/schemas/studio_delete_object.json";
import listOverlays from "../../shared/schemas/studio_list_overlays.json";
import setMap from "../../shared/schemas/studio_set_map.json";
import setSceneLighting from "../../shared/schemas/studio_set_scene_lighting.json";
import setOverlay from "../../shared/schemas/studio_set_overlay.json";
import setPostProcessing from "../../shared/schemas/studio_set_post_processing.json";
import timelineGetState from "../../shared/schemas/studio_timeline_get_state.json";
import timelinePlayback from "../../shared/schemas/studio_timeline_playback.json";
import timelineSetSettings from "../../shared/schemas/studio_timeline_set_settings.json";
import timelineListModels from "../../shared/schemas/studio_timeline_list_models.json";
import timelineAddInterpolable from "../../shared/schemas/studio_timeline_add_interpolable.json";
import timelineRemoveInterpolable from "../../shared/schemas/studio_timeline_remove_interpolable.json";
import timelineSetKeyframe from "../../shared/schemas/studio_timeline_set_keyframe.json";
import timelineDeleteKeyframe from "../../shared/schemas/studio_timeline_delete_keyframe.json";
import timelineSingleFile from "../../shared/schemas/studio_timeline_single_file.json";
import constraintList from "../../shared/schemas/studio_constraint_list.json";
import constraintCreate from "../../shared/schemas/studio_constraint_create.json";
import constraintSet from "../../shared/schemas/studio_constraint_set.json";
import constraintDelete from "../../shared/schemas/studio_constraint_delete.json";
import executeBatch from "../../shared/schemas/studio_execute_batch.json";

interface ToolDef {
  name: string;
  description: string;
  inputSchema: Record<string, unknown>;
  timeoutMs: number;
  image?: boolean;
}

interface ScreenshotResult {
  format: string;
  width: number;
  height: number;
  base64: string;
}

const def = (name: string, schema: { description: string; input: unknown }, timeoutMs: number, image = false): ToolDef => ({
  name,
  description: schema.description,
  inputSchema: schema.input as Record<string, unknown>,
  timeoutMs,
  image,
});

export const TOOLS: ToolDef[] = [
  def("studio_get_info", getInfo, 10000),
  def("studio_get_scene", getScene, 15000),
  def("studio_list_characters", listCharacters, 15000),
  def("studio_list_objects", listObjects, 15000),
  def("studio_get_camera", getCamera, 15000),
  def("studio_screenshot", screenshot, 90000, true),
  def("studio_search_catalog", searchCatalog, 30000),
  def("studio_list_scenes", listScenes, 15000),
  def("studio_list_character_cards", listCharacterCards, 240000),
  def("studio_list_poses", listPoses, 240000),
  def("studio_list_coordinates", listCoordinates, 240000),
  def("studio_list_outfit", listOutfit, 15000),
  def("studio_list_attach_points", listAttachPoints, 15000),
  def("studio_list_bones", listBones, 15000),
  def("studio_list_overlays", listOverlays, 15000),
  def("studio_load_scene", loadScene, 240000),
  def("studio_save_scene", saveScene, 90000),
  def("studio_set_camera", setCamera, 15000),
  def("studio_create_object", createObject, 90000),
  def("studio_create_light", createLight, 90000),
  def("studio_set_light", setLight, 15000),
  def("studio_set_object_transform", setObjectTransform, 15000),
  def("studio_delete_object", deleteObject, 15000),
  def("studio_create_character", createCharacter, 180000),
  def("studio_set_character_pose", setCharacterPose, 90000),
  def("studio_set_character_anime", setCharacterAnime, 90000),
  def("studio_set_character_expression", setCharacterExpression, 15000),
  def("studio_set_character_hands", setCharacterHands, 15000),
  def("studio_set_character_outfit", setCharacterOutfit, 180000),
  def("studio_set_character_state", setCharacterState, 15000),
  def("studio_set_character_kinematics", setCharacterKinematics, 30000),
  def("studio_set_character_bone", setCharacterBone, 15000),
  def("studio_set_character_ik", setCharacterIk, 15000),
  def("studio_attach_object", attachObject, 15000),
  def("studio_set_map", setMap, 240000),
  def("studio_set_scene_lighting", setSceneLighting, 15000),
  def("studio_set_overlay", setOverlay, 90000),
  def("studio_set_post_processing", setPostProcessing, 90000),
  def("studio_timeline_get_state", timelineGetState, 15000),
  def("studio_timeline_playback", timelinePlayback, 15000),
  def("studio_timeline_set_settings", timelineSetSettings, 15000),
  def("studio_timeline_list_models", timelineListModels, 15000),
  def("studio_timeline_add_interpolable", timelineAddInterpolable, 15000),
  def("studio_timeline_remove_interpolable", timelineRemoveInterpolable, 15000),
  def("studio_timeline_set_keyframe", timelineSetKeyframe, 15000),
  def("studio_timeline_delete_keyframe", timelineDeleteKeyframe, 15000),
  def("studio_timeline_single_file", timelineSingleFile, 90000),
  def("studio_constraint_list", constraintList, 15000),
  def("studio_constraint_create", constraintCreate, 15000),
  def("studio_constraint_set", constraintSet, 15000),
  def("studio_constraint_delete", constraintDelete, 15000),
  def("studio_execute_batch", executeBatch, 300000),
];

const bridge = new MiraBridge();

export async function runTool(name: string, args: Record<string, unknown>) {
  const tool = TOOLS.find((t) => t.name === name);
  if (!tool) return errorResult(`unknown tool: ${name}`);
  try {
    const result = await bridge.call(name, args, tool.timeoutMs);
    if (tool.image) {
      const shot = result as ScreenshotResult;
      return {
        content: [
          {
            type: "image" as const,
            data: shot.base64,
            mimeType: shot.format === "png" ? "image/png" : "image/jpeg",
          },
          { type: "text" as const, text: `screenshot ${shot.width}x${shot.height} (${shot.format})` },
        ],
      };
    }
    return { content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }] };
  } catch (e) {
    return errorResult(e instanceof Error ? e.message : String(e));
  }
}

const errorResult = (text: string) => ({
  isError: true,
  content: [{ type: "text" as const, text }],
});
