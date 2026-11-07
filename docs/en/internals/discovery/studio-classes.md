# Studio Classes — Assembly-CSharp.dll class surface

The types of `Assembly-CSharp.dll` that matter for Studio NEO V2: the `Studio` namespace, member dumps of the key classes, the kinematics and neck-look controllers, the character API, and the probable role of each key class.

Source: `<game>\StudioNEOV2_Data\Managed\Assembly-CSharp.dll`. Dump date: 2026-08-30. The dump is a pure metadata read (`System.Reflection.Metadata`); no code was executed. Total TypeDefinitions in the assembly: 4660.

Reading the listings:

- `(xN)` after a member name means N overloads of that name.
- `<name>k__BackingField` is the backing field of an auto-property.
- `<>c`, `<>c__DisplayClassN_M` and `<Name>d__N` are compiler-generated types (lambda caches, closures, iterator state machines).

## `Studio` namespace types

184 top-level types, with method and field counts.

| Type | Methods | Fields |
|---|---|---|
| AddButtonCtrl | 3 | 3 |
| AddObjectAssist | 19 | 0 |
| AddObjectCamera | 4 | 2 |
| AddObjectCommand | 0 | 0 |
| AddObjectFemale | 9 | 0 |
| AddObjectFolder | 3 | 0 |
| AddObjectItem | 4 | 0 |
| AddObjectLight | 3 | 0 |
| AddObjectMale | 3 | 0 |
| AddObjectRoute | 6 | 0 |
| AnimeCategoryList | 7 | 10 |
| AnimeCommand | 0 | 0 |
| AnimeControl | 26 | 19 |
| AnimeGroupList | 5 | 9 |
| AnimeList | 6 | 11 |
| AssetBundleCheck | 6 | 0 |
| AssociateSound | 2 | 1 |
| AutoLayoutCtrl | 3 | 3 |
| BackgroundCtrl | 9 | 6 |
| BackgroundList | 8 | 7 |
| BackgroundListAssist | 1 | 0 |
| BGMControl | 11 | 12 |
| BGMCtrl | 15 | 5 |
| BoneLineCtrl | 6 | 1 |
| CameraControl | 59 | 40 |
| CameraLightCtrl | 5 | 1 |
| CameraSelector | 6 | 3 |
| CanvasSortOrder | 2 | 1 |
| ChangeAmount | 16 | 8 |
| ChangeFocus | 8 | 2 |
| ChangeFocusSender | 3 | 2 |
| CharaFileInfo | 6 | 6 |
| CharaFileSort | 11 | 7 |
| CharaList | 19 | 8 |
| CharAnimeCtrl | 24 | 8 |
| CheckScene | 5 | 7 |
| ColorInfo | 3 | 4 |
| ColorPalette | 11 | 9 |
| ColorPresets | 17 | 14 |
| ConfigCtrl | 9 | 6 |
| DrawLightLine | 7 | 2 |
| ENVControl | 11 | 12 |
| ENVCtrl | 12 | 4 |
| ExitScene | 4 | 3 |
| FKCtrl | 14 | 4 |
| FrameCtrl | 3 | 2 |
| FrameList | 7 | 6 |
| GameScreenShot | 13 | 11 |
| GuideBase | 18 | 8 |
| GuideCommand | 0 | 0 |
| GuideInput | 38 | 11 |
| GuideInputSender | 3 | 2 |
| GuideMove | 16 | 9 |
| GuideObject | 63 | 24 |
| GuideObjectManager | 31 | 11 |
| GuideRotation | 10 | 6 |
| GuideScale | 10 | 7 |
| GuideSelect | 7 | 1 |
| GuideViewCtrl | 3 | 5 |
| ICommand | 3 | 0 |
| IconComponent | 7 | 3 |
| IKCtrl | 8 | 1 |
| ILateUpdatable | 1 | 0 |
| Info | 53 | 23 |
| InputFieldManager | 5 | 1 |
| InputFieldToCamera | 5 | 3 |
| ItemCategoryList | 6 | 9 |
| ItemColorData | 3 | 1 |
| ItemCommand | 0 | 0 |
| ItemComponent | 30 | 15 |
| ItemFKCtrl | 6 | 2 |
| ItemGroupList | 6 | 7 |
| ItemList | 6 | 7 |
| ItemShader | 67 | 33 |
| LateUpdateManager | 11 | 4 |
| LightColor | 3 | 2 |
| LightLine | 19 | 7 |
| LightList | 4 | 2 |
| ListNode | 16 | 4 |
| LogoList | 6 | 8 |
| LookAtCamera | 3 | 1 |
| LookAtTargetInfo | 5 | 0 |
| ManipulatePanelCtrl | 14 | 9 |
| Map | 20 | 5 |
| MapCommand | 0 | 0 |
| MapComponent | 9 | 4 |
| MapCtrl | 21 | 8 |
| MapDragButton | 7 | 4 |
| MapList | 5 | 5 |
| MPCameraCtrl | 10 | 5 |
| MPCharCtrl | 28 | 20 |
| MPFolderCtrl | 9 | 4 |
| MPItemCtrl | 100 | 16 |
| MPLightCtrl | 22 | 14 |
| MPRouteCtrl | 16 | 11 |
| MPRoutePointCtrl | 16 | 8 |
| NotificationScene | 4 | 6 |
| ObjectCtrl | 15 | 4 |
| ObjectCtrlInfo | 14 | 5 |
| ObjectInfo | 14 | 4 |
| ObjectInfoAssist | 3 | 0 |
| OCICamera | 15 | 5 |
| OCIChar | 99 | 24 |
| OCICharFemale | 9 | 0 |
| OCICharMale | 6 | 0 |
| OCIFolder | 12 | 2 |
| OCIItem | 64 | 16 |
| OCILight | 20 | 4 |
| OCIRoute | 34 | 11 |
| OCIRoutePoint | 39 | 7 |
| OIBoneInfo | 9 | 2 |
| OICameraInfo | 5 | 2 |
| OICharInfo | 19 | 33 |
| OIFolderInfo | 7 | 2 |
| OIIKTargetInfo | 1 | 0 |
| OIItemInfo | 16 | 19 |
| OILightInfo | 7 | 8 |
| OIRouteInfo | 9 | 8 |
| OIRoutePointAidInfo | 5 | 1 |
| OIRoutePointInfo | 9 | 6 |
| OptionCtrl | 21 | 17 |
| OptionItemCtrl | 20 | 4 |
| OptionSystem | 8 | 23 |
| OutsideSoundControl | 11 | 14 |
| OutsideSoundCtrl | 12 | 5 |
| PanelComponent | 4 | 5 |
| ParticleAssist | 8 | 3 |
| ParticleComponent | 8 | 4 |
| ParticleParent | 8 | 2 |
| PatternInfo | 18 | 6 |
| PatternSelectInfo | 4 | 8 |
| PatternSelectInfoComponent | 3 | 3 |
| PatternSelectListCtrl | 33 | 10 |
| PauseCtrl | 5 | 4 |
| PauseRegistrationList | 15 | 12 |
| PickerSliderInput | 14 | 7 |
| PointerAction | 6 | 5 |
| Preparation | 6 | 5 |
| PVCopy | 8 | 3 |
| RootButtonCtrl | 6 | 7 |
| RouteComponent | 2 | 1 |
| RouteControl | 13 | 12 |
| RoutePointComponent | 6 | 4 |
| SampleColor | 9 | 6 |
| SceneInfo | 21 | 61 |
| SceneLoadScene | 17 | 20 |
| ScreenEffectDefine | 1 | 42 |
| ScrollSizeCorrect | 2 | 3 |
| ShortcutKeyCtrl | 3 | 8 |
| ShortcutMenuScene | 4 | 1 |
| SortCanvas | 4 | 1 |
| SoundButtonCtrl | 3 | 2 |
| StartScene | 3 | 0 |
| Studio | 86 | 43 |
| StudioInputField | 5 | 0 |
| StudioNode | 16 | 6 |
| StudioTween | 214 | 45 |
| SystemButtonCtrl | 16 | 26 |
| TextEffect | 5 | 1 |
| TextMeshSlideEffectCtrl | 11 | 5 |
| TextSlideEffect | 4 | 1 |
| TextSlideEffectCtrl | 11 | 7 |
| ThumbnailNode | 10 | 3 |
| TreeNodeCommand | 0 | 0 |
| TreeNodeCtrl | 31 | 12 |
| TreeNodeObject | 55 | 30 |
| Tween | 245 | 39 |
| UI_OnMouseOverMessage | 4 | 4 |
| UI_OnOffColor | 3 | 3 |
| UndoRedoManager | 17 | 6 |
| Utility | 10 | 0 |
| VoiceCategoryList | 6 | 7 |
| VoiceControl | 18 | 19 |
| VoiceCtrl | 21 | 11 |
| VoiceEndChecker | 2 | 1 |
| VoiceGroupList | 6 | 6 |
| VoiceList | 5 | 6 |
| VoiceNode | 12 | 3 |
| VoicePlayNode | 6 | 3 |
| VoiceRegistrationList | 16 | 14 |
| Wait | 6 | 3 |
| WorkInfo | 4 | 10 |
| WorkspaceCtrl | 19 | 12 |
| YureCtrl | 15 | 9 |

### Nested types

459 nested types under the `Studio` namespace types.

```text
Studio.AddButtonCtrl+CommonInfo
Studio.AddObjectAssist+<>c
Studio.AddObjectCamera+<>c__DisplayClass4_0
Studio.AddObjectCommand+AddItemCommand
Studio.AddObjectCommand+AddLightCommand
Studio.AddObjectFemale+<>c
Studio.AddObjectFemale+<>c__DisplayClass1_0
Studio.AddObjectFemale+<>c__DisplayClass5_0
Studio.AddObjectFemale+<AddCoroutine>d__8
Studio.AddObjectFemale+<AddCoroutine>d__9
Studio.AddObjectFemale+NecessaryInfo
Studio.AddObjectItem+<>c
Studio.AddObjectMale+<>c
Studio.AddObjectMale+<>c__DisplayClass1_0
Studio.AnimeCategoryList+<>c
Studio.AnimeCategoryList+<>c__DisplayClass14_0
Studio.AnimeCategoryList+ListNodePool
Studio.AnimeCommand+OptionParamCommand
Studio.AnimeCommand+PatternCommand
Studio.AnimeCommand+SpeedCommand
Studio.AnimeControl+<>c
Studio.AnimeControl+<>c__DisplayClass33_0
Studio.AnimeControl+<>c__DisplayClass35_0
Studio.AnimeControl+<>c__DisplayClass42_0
Studio.AnimeGroupList+<>c
Studio.AnimeGroupList+<>c__DisplayClass13_0
Studio.AnimeGroupList+SEX
Studio.AnimeList+<>c__DisplayClass15_0
Studio.AnimeList+ListNodePool
Studio.AssetBundleCheck+<>c
Studio.AssetBundleCheck+<>c__DisplayClass3_0
Studio.AutoLayoutCtrl+<WaitFuncCoroutine>d__4
Studio.AutoLayoutCtrl+Func
Studio.BackgroundList+<>c
Studio.BackgroundList+<>c__DisplayClass11_0
Studio.BGMControl+<>c__DisplayClass18_0
Studio.BGMCtrl+Repeat
Studio.BoneLineCtrl+<>c
Studio.BoneLineCtrl+<>c__DisplayClass3_0
Studio.CameraControl+<>c
Studio.CameraControl+<>c__DisplayClass114_0
Studio.CameraControl+<>c__DisplayClass115_0
Studio.CameraControl+<>c__DisplayClass80_0
Studio.CameraControl+<Start>d__112
Studio.CameraControl+CameraData
Studio.CameraControl+Config
Studio.CameraControl+NoCtrlFunc
Studio.CameraControl+VisibleObject
Studio.CameraLightCtrl+LightCalc
Studio.CameraLightCtrl+LightCalc+<>c__DisplayClass15_0
Studio.CameraLightCtrl+LightInfo
Studio.CameraLightCtrl+MapLightInfo
Studio.CameraSelector+<>c
Studio.CameraSelector+<>c__DisplayClass4_0
Studio.ChangeFocus+<>c
Studio.CharaFileSort+<>c
Studio.CharaList+<>c
Studio.CharaList+<>c__DisplayClass11_0
Studio.ColorPresets+<>c
Studio.ColorPresets+<>c__DisplayClass23_0
Studio.ColorPresets+<>c__DisplayClass26_0
Studio.ColorPresets+<>c__DisplayClass26_1
Studio.ColorPresets+ColorInfo
Studio.ConfigCtrl+<>c
Studio.ConfigCtrl+<>c__DisplayClass14_0
Studio.ConfigCtrl+<>c__DisplayClass14_1
Studio.ENVControl+<>c__DisplayClass18_0
Studio.ExitScene+<>c
Studio.FKCtrl+<>c
Studio.FKCtrl+<>c__DisplayClass10_0
Studio.FKCtrl+<>c__DisplayClass10_1
Studio.FKCtrl+<>c__DisplayClass12_0
Studio.FKCtrl+<>c__DisplayClass13_0
Studio.FKCtrl+<>c__DisplayClass14_0
Studio.FKCtrl+<>c__DisplayClass14_1
Studio.FKCtrl+<>c__DisplayClass17_0
Studio.FKCtrl+TargetInfo
Studio.FKCtrl+TargetInfo+<>c
Studio.FrameList+<>c
Studio.FrameList+<>c__DisplayClass10_0
Studio.GameScreenShot+<>c
Studio.GameScreenShot+<CaptureFunc>d__24
Studio.GuideCommand+AddInfo
Studio.GuideCommand+EqualsInfo
Studio.GuideCommand+MoveAddCommand
Studio.GuideCommand+MoveEqualsCommand
Studio.GuideCommand+RotationAddCommand
Studio.GuideCommand+RotationEqualsCommand
Studio.GuideCommand+ScaleEqualsCommand
Studio.GuideInput+<>c
Studio.GuideInput+<>c__DisplayClass38_0
Studio.GuideInput+<>c__DisplayClass38_1
Studio.GuideInput+<>c__DisplayClass39_0
Studio.GuideInput+<>c__DisplayClass39_1
Studio.GuideInput+<>c__DisplayClass40_0
Studio.GuideInput+<>c__DisplayClass40_1
Studio.GuideInput+OnVisible
Studio.GuideMove+<>c
Studio.GuideMove+MoveAxis
Studio.GuideMove+MoveCalc
Studio.GuideObject+<>c
Studio.GuideObject+IsActiveFunc
Studio.GuideObject+Mode
Studio.GuideObjectManager+<>c
Studio.GuideRotation+<>c
Studio.GuideRotation+RotationAxis
Studio.GuideScale+<>c
Studio.GuideScale+ScaleAxis
Studio.IKCtrl+<>c__DisplayClass6_0
Studio.IKCtrl+<>c__DisplayClass7_0
Studio.IKCtrl+<InitTargetCoroutine>d__8
Studio.Info+<>c
Studio.Info+<>c__DisplayClass67_0
Studio.Info+<>c__DisplayClass68_0
Studio.Info+<LoadAnimeLoadInfoCoroutine>d__88
Studio.Info+<LoadExcelDataCoroutine>d__58
Studio.Info+<LoadItemLoadInfoCoroutine>d__71
Studio.Info+<LoadVoiceLoadInfoCoroutine>d__91
Studio.Info+AccessoryGroupInfo
Studio.Info+AccessoryGroupInfo+<>c
Studio.Info+AnimeLoadInfo
Studio.Info+AnimeLoadInfo+<>c__DisplayClass3_0
Studio.Info+BoneInfo
Studio.Info+CategoryInfo
Studio.Info+FileCheck
Studio.Info+FileInfo
Studio.Info+FileListInfo
Studio.Info+FileListInfo+Data
Studio.Info+FileListInfo+Data+<>c
Studio.Info+GroupInfo
Studio.Info+HAnimeLoadInfo
Studio.Info+ItemLoadInfo
Studio.Info+LightLoadInfo
Studio.Info+LightLoadInfo+Target
Studio.Info+LoadAnimeInfoCoroutineFunc
Studio.Info+LoadCommonInfo
Studio.Info+MapLoadInfo
Studio.Info+OptionItemInfo
Studio.Info+ParentageInfo
Studio.Info+SkyPatternInfo
Studio.Info+WaitTime
Studio.ItemCategoryList+<>c
Studio.ItemCategoryList+<>c__DisplayClass12_0
Studio.ItemColorData+ColorData
Studio.ItemColorData+ColorData+<>c
Studio.ItemCommand+ColorInfo
Studio.ItemComponent+<>c
Studio.ItemComponent+<>c__DisplayClass56_0
Studio.ItemComponent+<>c__DisplayClass62_0
Studio.ItemComponent+AnimeInfo
Studio.ItemComponent+Info
Studio.ItemComponent+MaterialInfo
Studio.ItemComponent+OptionInfo
Studio.ItemComponent+OptionInfo+<>c
Studio.ItemComponent+RendererInfo
Studio.ItemComponent+RendererInfo+<>c
Studio.ItemFKCtrl+TargetInfo
Studio.ItemGroupList+<>c
Studio.ItemGroupList+<>c__DisplayClass10_0
Studio.ItemList+<>c__DisplayClass10_0
Studio.LightList+<>c__DisplayClass3_0
Studio.ListNode+<>c__DisplayClass13_0
Studio.ListNode+<>c__DisplayClass16_0
Studio.ListNode+<>c__DisplayClass6_0
Studio.ListNode+<>c__DisplayClass9_0
Studio.LogoList+<>c__DisplayClass11_0
Studio.ManipulatePanelCtrl+CameraPanelInfo
Studio.ManipulatePanelCtrl+CharaPanelInfo
Studio.ManipulatePanelCtrl+FolderPanelInfo
Studio.ManipulatePanelCtrl+ItemPanelInfo
Studio.ManipulatePanelCtrl+LightPanelInfo
Studio.ManipulatePanelCtrl+RootInfo
Studio.ManipulatePanelCtrl+RoutePanelInfo
Studio.ManipulatePanelCtrl+RoutePointPanelInfo
Studio.Map+<>c__DisplayClass27_0
Studio.Map+<>c__DisplayClass28_0
Studio.Map+<LoadMapCoroutine>d__27
Studio.MapCommand+EqualsInfo
Studio.MapCommand+MoveEqualsCommand
Studio.MapCommand+RotationEqualsCommand
Studio.MapComponent+<>c
Studio.MapComponent+<>c__DisplayClass11_0
Studio.MapComponent+OptionInfo
Studio.MapComponent+OptionInfo+<>c
Studio.MapComponent+SeaInfo
Studio.MapList+<>c__DisplayClass8_0
Studio.MPCharCtrl+<>c__DisplayClass52_0
Studio.MPCharCtrl+<>c__DisplayClass53_0
Studio.MPCharCtrl+AccessoriesInfo
Studio.MPCharCtrl+AccessoriesInfo+<>c__DisplayClass2_0
Studio.MPCharCtrl+AccessoriesInfo+<>c__DisplayClass2_1
Studio.MPCharCtrl+AccessoriesInfo+<>c__DisplayClass2_2
Studio.MPCharCtrl+AccessoriesInfo+OnClickFunc
Studio.MPCharCtrl+ClothingDetailsInfo
Studio.MPCharCtrl+ClothingDetailsInfo+<>c
Studio.MPCharCtrl+ClothingDetailsInfo+<>c__DisplayClass15_0
Studio.MPCharCtrl+ClothingDetailsInfo+<>c__DisplayClass15_1
Studio.MPCharCtrl+ClothingDetailsInfo+OnClickFunc
Studio.MPCharCtrl+CommonInfo
Studio.MPCharCtrl+CostumeInfo
Studio.MPCharCtrl+CostumeInfo+<>c__DisplayClass8_0
Studio.MPCharCtrl+EtcInfo
Studio.MPCharCtrl+EtcInfo+<>c__DisplayClass10_0
Studio.MPCharCtrl+FKInfo
Studio.MPCharCtrl+HandInfo
Studio.MPCharCtrl+IKInfo
Studio.MPCharCtrl+JointInfo
Studio.MPCharCtrl+JointInfo+<>c__DisplayClass1_0
Studio.MPCharCtrl+LiquidInfo
Studio.MPCharCtrl+LiquidInfo+<>c__DisplayClass8_0
Studio.MPCharCtrl+LiquidInfo+<>c__DisplayClass8_1
Studio.MPCharCtrl+LiquidInfo+OnClickFunc
Studio.MPCharCtrl+LookAtInfo
Studio.MPCharCtrl+LookAtInfo+<>c__DisplayClass2_0
Studio.MPCharCtrl+NeckInfo
Studio.MPCharCtrl+NeckInfo+<>c__DisplayClass2_0
Studio.MPCharCtrl+NeckInfo+<>c__DisplayClass3_0
Studio.MPCharCtrl+NeckInfo+<>c__DisplayClass4_0
Studio.MPCharCtrl+OtherInfo
Studio.MPCharCtrl+OtherInfo+OnClickTears
Studio.MPCharCtrl+PatternInfo
Studio.MPCharCtrl+PatternInfo+OnClickFunc
Studio.MPCharCtrl+PoseInfo
Studio.MPCharCtrl+RootButtonInfo
Studio.MPCharCtrl+StateButtonInfo
Studio.MPCharCtrl+StateCommonInfo
Studio.MPCharCtrl+StateInfo
Studio.MPCharCtrl+StateSliderInfo
Studio.MPCharCtrl+StateToggleInfo
Studio.MPItemCtrl+<>c
Studio.MPItemCtrl+<>c__DisplayClass35_0
Studio.MPItemCtrl+<>c__DisplayClass36_0
Studio.MPItemCtrl+<>c__DisplayClass37_0
Studio.MPItemCtrl+<>c__DisplayClass41_0
Studio.MPItemCtrl+<>c__DisplayClass41_1
Studio.MPItemCtrl+<>c__DisplayClass44_0
Studio.MPItemCtrl+<>c__DisplayClass44_1
Studio.MPItemCtrl+<>c__DisplayClass45_0
Studio.MPItemCtrl+<>c__DisplayClass50_0
Studio.MPItemCtrl+<>c__DisplayClass50_1
Studio.MPItemCtrl+<>c__DisplayClass53_0
Studio.MPItemCtrl+<>c__DisplayClass53_1
Studio.MPItemCtrl+<>c__DisplayClass56_0
Studio.MPItemCtrl+<>c__DisplayClass56_1
Studio.MPItemCtrl+<>c__DisplayClass59_0
Studio.MPItemCtrl+<>c__DisplayClass59_1
Studio.MPItemCtrl+<>c__DisplayClass83_0
Studio.MPItemCtrl+<>c__DisplayClass94_0
Studio.MPItemCtrl+<>c__DisplayClass97_0
Studio.MPItemCtrl+AnimeInfo
Studio.MPItemCtrl+AnimeInfo+<>c
Studio.MPItemCtrl+ColorCombination
Studio.MPItemCtrl+ColorInfo
Studio.MPItemCtrl+ColorInputCombination
Studio.MPItemCtrl+EmissionInfo
Studio.MPItemCtrl+InputCombination
Studio.MPItemCtrl+KinematicInfo
Studio.MPItemCtrl+LineInfo
Studio.MPItemCtrl+OptionInfo
Studio.MPItemCtrl+PanelInfo
Studio.MPItemCtrl+PanelList
Studio.MPItemCtrl+PanelList+<>c
Studio.MPItemCtrl+PanelList+<>c__DisplayClass11_0
Studio.MPItemCtrl+PanelList+<>c__DisplayClass14_0
Studio.MPLightCtrl+BackgroundInfo
Studio.MPLightCtrl+ValueInfo
Studio.MPRouteCtrl+<>c
Studio.MPRouteCtrl+<>c__DisplayClass27_0
Studio.MPRouteCtrl+ToggleGroup
Studio.MPRouteCtrl+ToggleGroup+<>c
Studio.MPRouteCtrl+ToggleGroup+<>c__DisplayClass9_0
Studio.MPRouteCtrl+ToggleGroup+<>c__DisplayClass9_1
Studio.MPRoutePointCtrl+<>c
Studio.MPRoutePointCtrl+EaseInfo
Studio.MPRoutePointCtrl+InputCombination
Studio.MPRoutePointCtrl+ToggleGroup
Studio.MPRoutePointCtrl+ToggleGroup+<>c
Studio.MPRoutePointCtrl+ToggleGroup+<>c__DisplayClass9_0
Studio.MPRoutePointCtrl+ToggleGroup+<>c__DisplayClass9_1
Studio.NotificationScene+<NotificationCoroutine>d__6
Studio.ObjectCtrl+<>c
Studio.OCIChar+<>c
Studio.OCIChar+<>c__DisplayClass110_0
Studio.OCIChar+<>c__DisplayClass110_1
Studio.OCIChar+<>c__DisplayClass113_0
Studio.OCIChar+<>c__DisplayClass83_0
Studio.OCIChar+<>c__DisplayClass83_1
Studio.OCIChar+<>c__DisplayClass84_0
Studio.OCIChar+<>c__DisplayClass85_0
Studio.OCIChar+<>c__DisplayClass86_0
Studio.OCIChar+<>c__DisplayClass86_1
Studio.OCIChar+<>c__DisplayClass87_0
Studio.OCIChar+<>c__DisplayClass89_0
Studio.OCIChar+AccessPointInfo
Studio.OCIChar+BoneInfo
Studio.OCIChar+IKInfo
Studio.OCIChar+LoadedAnimeInfo
Studio.OCIChar+LookAtInfo
Studio.OCIChar+SyncBoneInfo
Studio.OCIItem+<>c__DisplayClass13_0
Studio.OCIItem+<>c__DisplayClass13_1
Studio.OCIItem+<>c__DisplayClass17_0
Studio.OCIItem+<>c__DisplayClass17_1
Studio.OCIItem+<>c__DisplayClass36_0
Studio.OCIItem+<>c__DisplayClass78_0
Studio.OCIRoute+<>c
Studio.OCIRoutePoint+PointAidInfo
Studio.OIBoneInfo+BoneGroup
Studio.OICharInfo+AnimeInfo
Studio.OICharInfo+IKTargetEN
Studio.OICharInfo+KinematicMode
Studio.OIRouteInfo+Connection
Studio.OIRouteInfo+Orient
Studio.OIRoutePointInfo+Connection
Studio.OptionCtrl+<>c
Studio.OptionCtrl+CharaFKColor
Studio.OptionCtrl+CharaFKColor+<>c
Studio.OptionCtrl+CharaFKColor+<>c__DisplayClass2_0
Studio.OptionCtrl+CharaFKColor+<>c__DisplayClass4_0
Studio.OptionCtrl+CommonInfo
Studio.OptionCtrl+Etc
Studio.OptionCtrl+Etc+<>c
Studio.OptionCtrl+InputCombination
Studio.OptionCtrl+ItemFKColor
Studio.OptionCtrl+RouteSystem
Studio.OptionCtrl+RouteSystem+<>c
Studio.OptionItemCtrl+<>c
Studio.OptionItemCtrl+<>c__DisplayClass22_0
Studio.OptionItemCtrl+ChildInfo
Studio.OptionItemCtrl+ItemInfo
Studio.OptionItemCtrl+ItemInfo+<>c
Studio.OutsideSoundControl+<>c
Studio.OutsideSoundControl+<>c__DisplayClass20_0
Studio.ParticleAssist+<EndOfFrame>d__10
Studio.ParticleAssist+<YieldNull>d__9
Studio.ParticleAssist+TransformC
Studio.ParticleComponent+<>c
Studio.PatternInfo+<>c__DisplayClass25_0
Studio.PatternSelectListCtrl+<>c
Studio.PatternSelectListCtrl+<>c__DisplayClass17_0
Studio.PatternSelectListCtrl+<>c__DisplayClass18_0
Studio.PatternSelectListCtrl+<>c__DisplayClass19_0
Studio.PatternSelectListCtrl+<>c__DisplayClass20_0
Studio.PatternSelectListCtrl+<>c__DisplayClass28_0
Studio.PatternSelectListCtrl+<>c__DisplayClass29_0
Studio.PatternSelectListCtrl+<>c__DisplayClass36_0
Studio.PatternSelectListCtrl+<>c__DisplayClass37_0
Studio.PatternSelectListCtrl+<>c__DisplayClass38_0
Studio.PatternSelectListCtrl+<>c__DisplayClass39_0
Studio.PatternSelectListCtrl+<>c__DisplayClass40_0
Studio.PatternSelectListCtrl+OnChangeItemFunc
Studio.PauseCtrl+FileInfo
Studio.PauseCtrl+FileInfo+<>c__DisplayClass11_0
Studio.PauseRegistrationList+<>c__DisplayClass26_0
Studio.PauseRegistrationList+<>c__DisplayClass26_1
Studio.RootButtonCtrl+CommonInfo
Studio.RootButtonCtrl+ManipulateInfo
Studio.RouteComponent+OnCompleteDel
Studio.RouteControl+<>c
Studio.RouteControl+<>c__DisplayClass15_0
Studio.SceneInfo+MapInfo
Studio.SceneInfo+SkyInfo
Studio.SceneLoadScene+<>c
Studio.SceneLoadScene+<>c__DisplayClass32_0
Studio.SceneLoadScene+<LoadScene>d__24
Studio.SceneLoadScene+<NotificationImportCoroutine>d__27
Studio.SceneLoadScene+<NotificationLoadCoroutine>d__25
Studio.SortCanvas+<>c
Studio.SoundButtonCtrl+CommonInfo
Studio.StartScene+<>c
Studio.StartScene+<LoadCoroutine>d__0
Studio.Studio+<>c
Studio.Studio+<>c__DisplayClass149_0
Studio.Studio+<AddFemaleCoroutine>d__100
Studio.Studio+<AddMapAsync>d__104
Studio.Studio+<AddMapCoroutine>d__103
Studio.Studio+<LoadSceneCoroutine>d__122
Studio.Studio+DuplicateParentInfo
Studio.StudioNode+<>c
Studio.StudioNode+<>c__DisplayClass12_0
Studio.StudioNode+<>c__DisplayClass14_0
Studio.StudioNode+ClickSound
Studio.StudioTween+<>c
Studio.StudioTween+<TweenDelay>d__127
Studio.StudioTween+<TweenRestart>d__129
Studio.StudioTween+<TweenStartCoroutine>d__224
Studio.StudioTween+ApplyTween
Studio.StudioTween+CompleteFunction
Studio.StudioTween+CRSpline
Studio.StudioTween+Defaults
Studio.StudioTween+EaseType
Studio.StudioTween+EasingFunction
Studio.StudioTween+LoopType
Studio.StudioTween+NamedValueColor
Studio.SystemButtonCtrl+<>c
Studio.SystemButtonCtrl+AmbientOcclusionInfo
Studio.SystemButtonCtrl+BloomInfo
Studio.SystemButtonCtrl+ColorGradingInfo
Studio.SystemButtonCtrl+ColorGradingInfo+<>c
Studio.SystemButtonCtrl+ColorGradingInfo+<>c__DisplayClass20_0
Studio.SystemButtonCtrl+ColorGradingInfo+<>c__DisplayClass22_0
Studio.SystemButtonCtrl+ColorGradingInfo+<>c__DisplayClass24_0
Studio.SystemButtonCtrl+CommonInfo
Studio.SystemButtonCtrl+DOFInfo
Studio.SystemButtonCtrl+EffectInfo
Studio.SystemButtonCtrl+EnvironmentLightingInfo
Studio.SystemButtonCtrl+FogInfo
Studio.SystemButtonCtrl+InputCombination
Studio.SystemButtonCtrl+ReflectionProbeInfo
Studio.SystemButtonCtrl+ReflectionProbeInfo+<>c
Studio.SystemButtonCtrl+ScreenSpaceReflectionInfo
Studio.SystemButtonCtrl+Selector
Studio.SystemButtonCtrl+SelfShadowInfo
Studio.SystemButtonCtrl+SkyInfo
Studio.SystemButtonCtrl+SkyInfo+<>c
Studio.SystemButtonCtrl+SunShaftsInfo
Studio.SystemButtonCtrl+VignetteInfo
Studio.ThumbnailNode+<>c
Studio.ThumbnailNode+ClickSound
Studio.TreeNodeCommand+MoveCopyCommand
Studio.TreeNodeCommand+MoveCopyInfo
Studio.TreeNodeCtrl+Calc
Studio.TreeNodeObject+CheckFunc
Studio.TreeNodeObject+OnVisibleFunc
Studio.TreeNodeObject+TreeState
Studio.Tween+<Start>d__234
Studio.Tween+<TweenDelay>d__148
Studio.Tween+<TweenRestart>d__150
Studio.Tween+ApplyTween
Studio.Tween+CompleteFunction
Studio.Tween+CRSpline
Studio.Tween+Defaults
Studio.Tween+EaseType
Studio.Tween+EasingFunction
Studio.Tween+LoopType
Studio.Tween+NamedValueColor
Studio.UndoRedoManager+Command
Studio.Utility+<>c
Studio.Utility+<>c__DisplayClass9_0
Studio.VoiceCategoryList+<>c
Studio.VoiceCategoryList+<>c__DisplayClass10_0
Studio.VoiceControl+<>c
Studio.VoiceControl+<>c__DisplayClass25_0
Studio.VoiceControl+<>c__DisplayClass34_0
Studio.VoiceControl+<>c__DisplayClass35_0
Studio.VoiceCtrl+Repeat
Studio.VoiceCtrl+VoiceInfo
Studio.VoiceEndChecker+OnEndFunc
Studio.VoiceGroupList+<>c__DisplayClass9_0
Studio.VoiceList+<>c
Studio.VoiceList+<>c__DisplayClass9_0
Studio.VoiceNode+<>c
Studio.VoiceNode+ClickSound
Studio.VoiceRegistrationList+<>c
Studio.VoiceRegistrationList+<>c__DisplayClass29_0
Studio.WorkspaceCtrl+<>c
Studio.YureCtrl+<>c__DisplayClass24_0
Studio.YureCtrl+BreastShapeInfo
Studio.YureCtrl+Info
```

## Key class member dumps

### Existence check

Names requested, and where each one is found.

| Requested name | Found as |
|---|---|
| Studio | Studio.Studio |
| SceneInfo | Studio.SceneInfo, PlayfulSystems.LoadingScreen.SceneInfo |
| CameraControl | CameraControl, Studio.CameraControl |
| CameraLightCtrl | Studio.CameraLightCtrl |
| ObjectCtrlInfo | Studio.ObjectCtrlInfo |
| OCIChar | Studio.OCIChar |
| OCIItem | Studio.OCIItem |
| OCILight | Studio.OCILight |
| OCIFolder | Studio.OCIFolder |
| OCICamera | Studio.OCICamera |
| OCIRoute | Studio.OCIRoute |
| TreeNodeCtrl | Studio.TreeNodeCtrl |
| GuideObject | Studio.GuideObject |
| AnimeControl | Studio.AnimeControl |
| AddObjectAssist | Studio.AddObjectAssist |
| AddObjectFemale | Studio.AddObjectFemale |
| AddObjectMale | Studio.AddObjectMale |
| AddObjectItem | Studio.AddObjectItem |
| Info | Studio.Info, ADV.Info, Illusion.Component.UI.ColorPicker.Info |
| LightCtrl | NOT FOUND (top-level) |
| MoveAnimator | NOT FOUND (top-level) |

### `Studio.Studio`

**Methods (86)**
```text
.ctor
AddCamera
AddChangeAmount
AddCtrlInfo
AddFemale
AddFemaleCoroutine
AddFolder
AddInfo
AddItem
AddLight
AddMale
AddMap
AddMapAsync
AddMapCoroutine
AddNode
AddObjectCtrlInfo
AddRoute
Awake
ChangeCamera (x2)
DeleteCamera
DeleteChangeAmount
DeleteIndex
DeleteInfo
DeleteNode
DeselectInputField
Duplicate
FindLoop
get_ApplicationPath
get_bgmCtrl
get_cameraCount
get_cameraCtrl
get_cameraLightCtrl
get_cameraSelector
get_colorPalette
get_envCtrl
get_frameCtrl
get_gameScreenShot
get_isInputNow
get_isVRMode
get_logoList
get_manipulatePanelCtrl
get_mapList
get_ociCamera
get_optionSystem
get_outsideSoundCtrl
get_patternSelectListCtrl
get_rootButtonCtrl
get_routeControl
get_sceneInfo
get_systemButtonCtrl
get_textureLine
get_treeNodeCtrl
GetChangeAmount
GetCtrlInfo (x2)
GetInfo
GetNewIndex
GetSelectObjectCtrl
ImportScene
Init
InitScene
LoadOption
LoadScene
LoadSceneCoroutine
OnApplicationQuit
OnDeleteNode
OnParentage
ResetOption
SaveOption
SavePreprocessingLoop
SaveScene
SelectInputField
set_cameraCount
set_isVRMode
set_ociCamera
set_optionSystem
set_sceneInfo
SetDepthOfFieldForcus
SetNewIndex
SetSunCaster
SetupMap
SetVisibleGimmick
ShowName
UpdateCharaFKColor
UpdateItemFKColor
```

**Fields (43)**
```text
savePath
m_TreeNodeCtrl
m_RootButtonCtrl
_manipulatePanelCtrl
m_CameraCtrl
m_SystemButtonCtrl
m_CameraLightCtrl
_mapList
_colorPalette
m_WorkspaceCtrl
m_BackgroundCtrl
m_BackgroundList
_patternSelectListCtrl
_gameScreenShot
_frameCtrl
_frameList
_logoList
_inputFieldNow
_inputFieldTMPNow
rectName
textName
disposableName
imageCamera
textCamera
_cameraSelector
_textureLine
_routeControl
<sceneInfo>k__BackingField
dicInfo
dicObjectCtrl
dicChangeAmount
UserPath
FileName
RootName
<optionSystem>k__BackingField
xmlCtrl
_ociCamera
<cameraCount>k__BackingField
<isVRMode>k__BackingField
onDelete
onChangeMap
fdApplicationPath
workInfo
```

### `Studio.SceneInfo`

**Methods (21)**
```text
.ctor
CheckNewIndex
DeleteIndex
get_dataVersion
get_dicChangeKey
get_dicImport
get_isLightCheck
get_isLightLimitOver
get_version
GetNewIndex
Import (x2)
Init
Load (x2)
Save (x2)
set_dataVersion
set_dicChangeKey
set_dicImport
SetNewIndex
```

**Fields (61)**
```text
m_Version
dicObject
mapInfo
cgLookupTexture
cgBlend
cgSaturation
cgBrightness
cgContrast
enableAmbientOcclusion
aoIntensity
aoThicknessModeifier
aoColor
enableBloom
bloomIntensity
bloomThreshold
bloomSoftKnee
bloomClamp
bloomDiffusion
bloomColor
enableDepth
depthForcus
depthFocalSize
depthAperture
enableVignette
vignetteIntensity
enableSSR
enableReflectionProbe
reflectionProbeCubemap
reflectionProbeIntensity
enableFog
fogExcludeFarPixels
fogHeight
fogHeightDensity
fogColor
fogDensity
enableSunShafts
sunCaster
sunThresholdColor
sunColor
sunDistanceFallOff
sunBlurSize
sunIntensity
enableShadow
environmentLightingSkyColor
environmentLightingEquatorColor
environmentLightingGroundColor
skyInfo
cameraSaveData
cameraData
charaLight
mapLight
bgmCtrl
envCtrl
outsideSoundCtrl
background
frame
hashIndex
<dicImport>k__BackingField
<dicChangeKey>k__BackingField
lightCount
<dataVersion>k__BackingField
```

### `Studio.CameraControl`

**Methods (59)**
```text
.ctor
Awake
CloerListCollider
Export
ExportResetData
FrontTarget
get_active
get_cameraAngle
get_fieldOfView
get_Follow
get_isConfigTargetTex
get_isConfigVanish
get_isControlNow
get_isCursorLock
get_IsOutsideSetting
get_isOutsideTargetTex
get_LookAt
get_mainCmaera
get_mapLayer
get_State
get_subCamera
get_targetPos
get_targetTex
Import
InputKeyProc
InputMouseProc
InputMouseWheelZoomProc
InternalUpdateCameraState
LateUpdate
LoadVanish
OnDrawGizmos
OnTriggerEnter
OnTriggerExit
OnTriggerStay
ReflectOption
Reset
set_active
set_cameraAngle
set_fieldOfView
set_Follow
set_isConfigTargetTex
set_isConfigVanish
set_isControlNow
set_isCursorLock
set_IsOutsideSetting
set_isOutsideTargetTex
set_LookAt
set_mainCmaera
set_targetPos
SetBase
SetCamera (x2)
SetPositionAndRotation
Start
TargetSet
VanishDelayVisible
VanishProc
VisibleFroceVanish (x2)
```

**Fields (40)**
```text
m_MapLayer
<mainCmaera>k__BackingField
transBase
targetObj
xRotSpeed
yRotSpeed
zoomSpeed
moveSpeed
noneTargetDir
isLimitPos
limitPos
isLimitDir
limitDir
limitFov
m_SubCamera
noCtrlCondition
zoomCondition
keyCondition
<isControlNow>k__BackingField
CONFIG_SIZE
cameraData
cameraReset
isInit
INIT_FOV
viewCollider
rateAddSpeed
dragging
<isOutsideTargetTex>k__BackingField
<isCursorLock>k__BackingField
<isConfigTargetTex>k__BackingField
m_ConfigVanish
m_TargetTex
m_TargetRender
objRoot
<IsOutsideSetting>k__BackingField
lstMapVanish
listCollider
isFlashVisible
lensSettings
cameraState
```

### `Studio.CameraLightCtrl`

**Methods (5)**
```text
.ctor
Init
OnEnable
Reflect
Start
```

**Fields (1)**
```text
lightChara
```

### `Studio.ObjectCtrlInfo`

**Methods (14)**
```text
.ctor
get_animeSpeed
get_Item
get_kind
get_kinds
OnAttach
OnDelete
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
set_animeSpeed
```

**Fields (5)**
```text
objectInfo
treeNodeObject
guideObject
parentInfo
<animeSpeed>k__BackingField
```

### `Studio.OCIChar`

**Methods (99)**
```text
.ctor
<LoadAnime>b__107_0
ActiveFK
ActiveFKGroup
ActiveIK
ActiveIKGroup
ActiveKinematicMode
AddVoice
ChangeBlink
ChangeChara
ChangeEyesOpen
ChangeHandAnime
ChangeLipSync
ChangeLookEyesPtn
ChangeLookNeckPtn
ChangeMouthOpen
ChangeMouthPtn
CheckAnimeParam (x3)
CheckParam
DeleteAllVoice
DeleteVoice
EnableDynamicBonesBustAndHip
EnableExpressionCategory
get_animeOptionParam
get_animeOptionParam1
get_animeOptionParam2
get_animePattern
get_animeSpeed
get_breastLayer
get_charFileStatus
get_DynamicAnimeBustL
get_DynamicAnimeBustR
get_DynamicFKBustL
get_DynamicFKBustR
get_HandAnimeNum
get_loadedAnimeInfo
get_neckPtnOld
get_oiCharInfo
get_preparation
get_sex
get_transSon
get_voiceCtrl
get_voiceRepeat
GetHohoAkaRate
GetSiruFlags
GetSonLength
GetTears
GetVisibleSimple
InitFK
InitFKBone
InitKinematic
IsFKGroup
LoadAnime
LoadClothesFile
OnAttach
OnDelete
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
PlayVoice
RestartAnime
set_animeOptionParam1
set_animeOptionParam2
set_animePattern
set_animeSpeed
set_breastLayer
set_DynamicAnimeBustL
set_DynamicAnimeBustR
set_DynamicFKBustL
set_DynamicFKBustR
set_neckPtnOld
set_preparation
set_transSon
set_voiceRepeat
SetClothesState
SetClothesStateAll
SetHohoAkaRate
SetNipStand
SetSimpleColor
SetSiruFlags
SetSonLength
SetTears
SetTuyaRate
SetVisibleSimple
SetVisibleSon
SetVoice
SetWetRate
ShowAccessory
StopVoice
UpdateDynamicBonesBust
UpdateFKColor (x2)
VisibleFKGuide
VisibleIKGuide
```

**Fields (24)**
```text
charReference
dicAccessPoint
listBones
listIKTarget
lookAtInfo
charInfo
fkCtrl
ikCtrl
finalIK
neckLookCtrl
skirtDynamic
dynamicBust
enablePV
optionItemCtrl
isAnimeMotion
isHAnime
charAnimeCtrl
yureCtrl
animeParam
dicAccessoryPoint
<neckPtnOld>k__BackingField
<breastLayer>k__BackingField
_loadedAnimeInfo
<preparation>k__BackingField
```

### `Studio.OCIItem`

**Methods (64)**
```text
.ctor
ActiveDynamicBone
ActiveFK
get_animeSpeed
get_CheckAlpha
get_CheckAnim
get_CheckAnimePattern
get_CheckEmission
get_CheckEmissionColor
get_CheckEmissionPower
get_CheckLightCancel
get_CheckOption
get_checkPanel
get_defColor
get_isAnime
get_isChangeColor
get_isDynamicBone
get_isFK
get_IsParticle
get_IsParticleArray
get_itemInfo
get_useColor
get_useColor4
get_useMetallic
get_usePattern
get_visible
OnAttach
OnDelete
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
ReleasePatternTex
RestartAnime
set_animeSpeed
set_visible
set_VisibleIcon
SetAlpha
SetAnimePattern
SetColor
SetEmissionColor
SetEmissionPower
SetGlossiness
SetLightCancel
SetMainTex (x2)
SetMetallic
SetOptionVisible (x2)
SetPatternClamp
SetPatternColor
SetPatternRot
SetPatternTex (x2)
SetPatternUS
SetPatternUT
SetPatternVS
SetPatternVT
SetupPatternTex
UpdateColor
UpdateFKColor
UpdateOption
```

**Fields (16)**
```text
objectItem
childRoot
animator
itemComponent
particleComponent
texturePattern
iconComponent
panelComponent
textureMain
seComponent
itemFKCtrl
listBones
dynamicBones
m_Visible
arrayRender
arrayParticle
```

### `Studio.OCILight`

**Methods (20)**
```text
.ctor
get_light
get_lightInfo
get_lightType
OnAttach
OnDelete
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
SetColor
SetDrawTarget
SetEnable
SetIntensity
SetRange
SetShadow
SetSpotAngle
Update
```

**Fields (4)**
```text
objectLight
m_Light
lightTarget
lightColor
```

### `Studio.OCIFolder`

**Methods (12)**
```text
.ctor
get_folderInfo
get_name
OnAttach
OnDelete
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
set_name
```

**Fields (2)**
```text
objectItem
childRoot
```

### `Studio.OCICamera`

**Methods (15)**
```text
.ctor
<SetActive>b__10_0
<SetActive>b__10_1
get_cameraInfo
get_name
OnAttach
OnDelete
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
set_name
SetActive
```

**Fields (5)**
```text
objectItem
meshRenderer
disposable
visibleOutside
cameraControl
```

### `Studio.OCIRoute`

**Methods (34)**
```text
.ctor
<Play>b__40_0
<Stop>b__43_0
AddPoint
CheckParentLoop
DeleteLine
DeletePoint
ForceUpdateLine
get_isEnd
get_isPlay
get_listPoint
get_name
get_routeInfo
get_visibleLine
Move
OnAttach
OnDelete
OnDeleteNode
OnDetach
OnDetachChild
OnLoadAttach
OnSavePreprocessing
OnSelect
OnVisible
Play
set_listPoint
set_name
set_visibleLine
SetLineColor
SetPath
SetVisible
Stop
UpdateLine
UpdateNumber
```

**Fields (11)**
```text
limitNum
objectItem
childRoot
childNodeRoot
<listPoint>k__BackingField
routeComponent
nowIndex
disposable
line
segments
objLine
```

### `Studio.TreeNodeCtrl`

**Methods (31)**
```text
.ctor
AddNode (x2)
AddSelectNode
CheckNode
CheckSelect
CopyChangeAmount
DeleteAllNode
DeleteAllNodeLoop
DeleteNode (x2)
DeleteNodeLoop
DeselectNode
get_selectNode
get_selectNodes
get_selectObjectCtrl
GetNode
OnPointerDown
RefreshHierachy
RefreshHierachyLoop
RefreshVisibleLoop
RemoveNode (x2)
SelectMultiple
SelectMultipleLoop
SelectSingle
set_selectNode
SetParent (x2)
SetSelectNode
Start
```

**Fields (12)**
```text
m_ObjectNode
m_ObjectRoot
m_TreeRoot
m_TreeNodeObject
hashSelectNode
onParentage
onDelete
onSelect
onSelectMultiple
onDeselect
canvas
scrollRect
```

### `Studio.GuideObject`

**Methods (63)**
```text
.ctor
<Awake>b__104_0
<Awake>b__104_1
Awake
CalcPosition
CalcRotation
CalcScale
ForceUpdate
get_calcScale
get_changeAmount
get_dicKey
get_enableMaluti
get_enablePos
get_enableRot
get_enables
get_enableScale
get_guideMove
get_guideSelect
get_isActive
get_isChild
get_isQuit
get_layer
get_moveCalc
get_objCenter
get_scaleRate
get_scaleRot
get_scaleSelect
get_visible
get_visibleCenter
get_visibleOutside
LateUpdate
MoveLocal (x2)
MoveWorld
OnApplicationQuit
OnDestroy
Parse
Rotation
set_calcScale
set_changeAmount
set_dicKey
set_enableMaluti
set_enablePos
set_enableRot
set_enableScale
set_isActive
set_isQuit
set_moveCalc
set_scaleRate
set_scaleRot
set_scaleSelect
set_visible
set_visibleCenter
set_visibleOutside
set_visibleTranslation
SetActive
SetEnable
SetLayer
SetMode
SetScale
SetVisibleCenter
SetWorldPos
Start
```

**Fields (24)**
```text
transformTarget
m_DicKey
m_ChangeAmount
roots
objectSelect
guide
m_objCenter
rendererCenter
m_Enables
_calcScale
<enableMaluti>k__BackingField
isActiveFunc
m_IsActive
m_ScaleRate
m_ScaleRot
m_ScaleSelect
parentGuide
_visible
_visibleOutside
mode
parent
_moveCalc
nonconnect
<isQuit>k__BackingField
```

### `Studio.AnimeControl`

**Methods (26)**
```text
.ctor
AddEventTrigger
Awake
ChangedOptionParam
DownOptionParam
EndEditOptionParam
get_objectCtrlInfo
Init
OnClickAllRestart
OnClickCopy
OnClickPaste
OnClickRestart
OnEndEditPattern
OnEndEditSpeed
OnPointerDownPattern
OnPointerDownSpeed
OnPointerUpPattern
OnPointerUpSpeed
OnValueChangedLoop
OnValueChangedOption
OnValueChangedPattern
OnValueChangedSpeed
set_active
set_objectCtrlInfo
UpdateInfo
UpOptionParam
```

**Fields (19)**
```text
sliderSpeed
eventSpeed
inputSpeed
sliderPattern
eventPattern
inputPattern
sliderOptionParam
eventOptionParam
inputOptionParam
toggleOption
toggleLoop
buttons
m_ObjectCtrlInfo
isUpdateInfo
arrayTarget
num
oldValue
sex
animeInfo
```

### `Studio.AddObjectAssist`

**Methods (19)**
```text
AddBoneGuide
AddIKTarget (x3)
ArrangeNames
ArrangeNamesLoop
DisableComponent
InitAccessoryPoint
InitBone
InitHairBone
InitIKTarget
InitLookAt
LoadAsset
LoadChild (x3)
SetupAccessoryDynamicBones
TransformLoop
UpdateState
```

**Fields (0)** — none

### `Studio.AddObjectFemale`

**Methods (9)**
```text
Add (x2)
AddCoroutine (x2)
CheckNameLoop
GetHairDynamic
GetSkirtDynamic (x2)
Load
```

**Fields (0)** — none

### `Studio.AddObjectMale`

**Methods (3)**
```text
Add (x2)
Load
```

**Fields (0)** — none

### `Studio.AddObjectItem`

**Methods (4)**
```text
Add
GetLoadInfo
Load (x2)
```

**Fields (0)** — none

### `Studio.Info`

**Methods (53)**
```text
.ctor
Awake
CheckIsAdd
CheckIsAI
ExistAnimeCategory
ExistAnimeGroup
ExistItemCategory
ExistItemGroup
FindAllAssetName
FindAllAssetNameFromBundle
get_accessoryPointGroup
get_AccessoryPointNum
get_AccessoryPointsIndex
get_IsInstallAdd
get_IsInstallAI
get_isLoadList
GetVoiceInfo
LoadAccessoryGroupInfo
LoadAnimeCategoryInfo (x4)
LoadAnimeGroupInfo (x2)
LoadAnimeLoadInfo
LoadAnimeLoadInfoCoroutine
LoadBGMLoadInfo
LoadBoneInfo
LoadExcelData
LoadExcelDataCoroutine
LoadExcelDataFromBundle
LoadHAnimeLoadInfo
LoadItemBoneInfo (x2)
LoadItemCategoryInfo (x2)
LoadItemColorData
LoadItemGroupInfo
LoadItemLoadInfo
LoadItemLoadInfoCoroutine
LoadLightLoadInfo
LoadMapLoadInfo
LoadSkyPatternInfo
LoadSoundLoadInfo
LoadVoiceLoadInfo (x2)
LoadVoiceLoadInfoCoroutine
SafeGetItemColorData
set_AccessoryPointNum
set_IsInstallAdd
set_IsInstallAI
set_isLoadList
SortDictionary
```

**Fields (23)**
```text
dicBoneInfo
dicItemGroupCategory
dicItemLoadInfo
dicItemColorData
dicAccessoryGroup
<AccessoryPointNum>k__BackingField
m_AccessoryPointGroup
dicLightLoadInfo
dicAGroupCategory
dicAnimeLoadInfo
dicVoiceGroupCategory
dicVoiceLoadInfo
dicBGMLoadInfo
dicENVLoadInfo
dicMapLoadInfo
dicColorGradingLoadInfo
dicReflectionProbeLoadInfo
dicSkyPatternInfo
fileCheck
<isLoadList>k__BackingField
waitTime
<IsInstallAI>k__BackingField
<IsInstallAdd>k__BackingField
```

### Nested types of `Studio.OCIChar` (18)

#### `Studio.OCIChar+SyncBoneInfo`

**Methods (6)**
```text
.ctor
get_GameObject
get_Transform
set_GameObject
set_LocalRotation
set_Rotation
```

**Fields (2)**
```text
<GameObject>k__BackingField
_transform
```

#### `Studio.OCIChar+BoneInfo`

**Methods (20)**
```text
.ctor
<AddSyncBone>b__35_0
AddSyncBone
get_active
get_boneGroup
get_boneID
get_boneInfo
get_boneWeight
get_gameObject
get_guideObject
get_posision
get_scaleRate
set_active
set_boneID
set_boneInfo
set_boneWeight
set_color
set_guideObject
set_layer
set_scaleRate
```

**Fields (6)**
```text
<guideObject>k__BackingField
<boneInfo>k__BackingField
m_GameObject
<boneID>k__BackingField
<boneWeight>k__BackingField
syncBoneInfo
```

#### `Studio.OCIChar+IKInfo`

**Methods (21)**
```text
.ctor
CopyBaseValue
CopyBone
CopyBoneRotation
get_active
get_baseObject
get_boneGroup
get_boneObject
get_gameObject
get_guideObject
get_scaleRate
get_targetInfo
get_targetObject
set_active
set_baseObject
set_boneObject
set_guideObject
set_layer
set_scaleRate
set_targetInfo
set_targetObject
```

**Fields (6)**
```text
<guideObject>k__BackingField
<targetInfo>k__BackingField
<baseObject>k__BackingField
<targetObject>k__BackingField
<boneObject>k__BackingField
m_GameObject
```

#### `Studio.OCIChar+LookAtInfo`

**Methods (10)**
```text
.ctor
get_active
get_gameObject
get_guideObject
get_target
get_targetInfo
set_active
set_guideObject
set_layer
set_targetInfo
```

**Fields (3)**
```text
<guideObject>k__BackingField
<targetInfo>k__BackingField
m_GameObject
```

#### `Studio.OCIChar+LoadedAnimeInfo`

**Methods (3)**
```text
.ctor
BaseCheck
OverrideCheck
```

**Fields (2)**
```text
baseFile
overrideFile
```

#### `Studio.OCIChar+AccessPointInfo`

**Methods (1)**
```text
.ctor
```

**Fields (2)**
```text
root
child
```

#### `Studio.OCIChar+<>c__DisplayClass83_0`

**Methods (1)**
```text
.ctor
```

**Fields (1)**
```text
parts
```

#### `Studio.OCIChar+<>c__DisplayClass83_1`

**Methods (2)**
```text
.ctor
<ActiveFK>b__0
```

**Fields (3)**
```text
i
CS$<>8__locals1
<>9__0
```

#### `Studio.OCIChar+<>c__DisplayClass84_0`

**Methods (2)**
```text
.ctor
<IsFKGroup>b__0
```

**Fields (1)**
```text
_group
```

#### `Studio.OCIChar+<>c__DisplayClass85_0`

**Methods (2)**
```text
.ctor
<InitFKBone>b__0
```

**Fields (2)**
```text
_group
<>9__0
```

#### `Studio.OCIChar+<>c__DisplayClass86_0`

**Methods (1)**
```text
.ctor
```

**Fields (1)**
```text
_active
```

#### `Studio.OCIChar+<>c__DisplayClass86_1`

**Methods (2)**
```text
.ctor
<ActiveFKGroup>b__0
```

**Fields (2)**
```text
v
CS$<>8__locals1
```

#### `Studio.OCIChar+<>c`

**Methods (8)**
```text
.cctor
.ctor
<ActiveFKGroup>b__86_1
<ChangeChara>b__114_0
<ChangeChara>b__114_1
<ChangeChara>b__114_2
<ChangeChara>b__114_3
<ChangeChara>b__114_4
```

**Fields (7)**
```text
<>9
<>9__86_1
<>9__114_3
<>9__114_0
<>9__114_1
<>9__114_2
<>9__114_4
```

#### `Studio.OCIChar+<>c__DisplayClass87_0`

**Methods (2)**
```text
.ctor
<ActiveIK>b__0
```

**Fields (2)**
```text
target
<>9__0
```

#### `Studio.OCIChar+<>c__DisplayClass89_0`

**Methods (2)**
```text
.ctor
<UpdateFKColor>b__0
```

**Fields (1)**
```text
v
```

#### `Studio.OCIChar+<>c__DisplayClass110_0`

**Methods (1)**
```text
.ctor
```

**Fields (1)**
```text
_names
```

#### `Studio.OCIChar+<>c__DisplayClass110_1`

**Methods (2)**
```text
.ctor
<CheckAnimeParam>b__0
```

**Fields (2)**
```text
i
CS$<>8__locals1
```

#### `Studio.OCIChar+<>c__DisplayClass113_0`

**Methods (2)**
```text
.ctor
<CheckParam>b__0
```

**Fields (1)**
```text
_name
```

## Kinematics: FKCtrl, IKCtrl, ItemFKCtrl

A search for `IKCtrl`, `FKCtrl` and `NeckLookControl` across all namespaces returns these three types. No type named exactly `NeckLookControl` exists in any Managed DLL.

| Full name | Methods | Fields |
|---|---|---|
| Studio.FKCtrl | 14 | 4 |
| Studio.IKCtrl | 8 | 1 |
| Studio.ItemFKCtrl | 6 | 2 |

### `Studio.FKCtrl`

**Methods (14)**
```text
.cctor
.ctor
CopyBone (x2)
get_count
get_transform
InitBones
LateUpdate
ResetUsedBone
set_count
SetEnable
UsedBone (x3)
```

**Fields (4)**
```text
parts
m_Transform
listBones
<count>k__BackingField
```

### `Studio.IKCtrl`

**Methods (8)**
```text
.ctor
Awake
CopyBone
CopyBoneRotation
get_count
InitTarget
InitTargetCoroutine
set_addIKInfo
```

**Fields (1)**
```text
listIKInfo
```

### `Studio.ItemFKCtrl`

**Methods (6)**
```text
.ctor
get_count
InitBone
LateUpdate
OnDisable
set_count
```

**Fields (2)**
```text
listBones
<count>k__BackingField
```

## Neck look (IL.dll)

The neck-look system lives in `IL.dll` (root namespace): `NeckLookCalc`, `NeckLookCalcVer2`, `NeckLookController`, `NeckLookControllerVer2`. `LightCtrl` and `MoveAnimator` do not exist in any Managed assembly; light control is the `Studio.MPLightCtrl` UI panel plus `OCILight` (see the dumps above).

### `NeckLookCalcVer2`

`IL.dll`, root namespace.

**Methods**
```text
Awake
Start
OnDrawGizmos
Init
UpdateCall
NeckUpdateCalc
GetAngleToTarget
RotateToAngle
CalcNeckBone
AngleAroundAxis
SetEnable
SetBoneFixAngle
.ctor
```

**Fields**
```text
isEnabled
transformAim
boneCalcAngle
aBones
neckTypeStates
ptnDraw
drawLineLength
changeTypeLeapTime
changeTypeLerpCurve
nowAngle
calcLerp
skipCalc
nowPtnNo
initEnd
backupPos
changeTypeTimer
lookType
neckLookController2
```

### `NeckLookControllerVer2`

`IL.dll`, root namespace.

**Methods**
```text
Start
LateUpdate
ForceLateUpdate
SaveNeckLookCtrl
LoadNeckLookCtrl
.ctor
```

**Fields**
```text
neckLookScript
ptnNo
target
rate
```

## Character API (`AIChara` namespace)

`ChaControl` and `ChaFileControl` are not in the root namespace. They live in the `AIChara` namespace of `Assembly-CSharp.dll`. Listings give method names only.

### `AIChara.ChaControl`

426 methods.

```text
.ctor
<ChangeHeadAsync>b__477_0
<ChangeHeadAsync>b__477_1
AddClothesStateKind
AddClothesStateKindSub
AddUpdateCMBodyColorFlags
AddUpdateCMBodyGlossFlags
AddUpdateCMBodyLayoutFlags
AddUpdateCMBodyTexFlags
AddUpdateCMFaceColorFlags
AddUpdateCMFaceGlossFlags
AddUpdateCMFaceLayoutFlags
AddUpdateCMFaceTexFlags
AnimPlay
AssignCoordinate (x3)
ChangeAccessory (x2)
ChangeAccessoryAsync (x2)
ChangeAccessoryColor
ChangeAccessoryParent
ChangeAlphaMask
ChangeAlphaMask2
ChangeAlphaMaskEx
ChangeBeardColor
ChangeBeardKind
ChangeBlackEyesColor
ChangeBlackEyesKind
ChangeBlackEyesWH
ChangeBodyDetailPower
ChangeBodyGlossPower
ChangeBodyMetallicPower
ChangeBreakClothes
ChangeBustGravity
ChangeBustInert
ChangeBustSoftness
ChangeClothes (x2)
ChangeClothesAsync (x2)
ChangeClothesBot
ChangeClothesBotAsync
ChangeClothesGloves
ChangeClothesGlovesAsync
ChangeClothesInnerB
ChangeClothesInnerBAsync
ChangeClothesInnerT
ChangeClothesInnerTAsync
ChangeClothesPanst
ChangeClothesPanstAsync
ChangeClothesShoes
ChangeClothesShoesAsync
ChangeClothesSocks
ChangeClothesSocksAsync
ChangeClothesTop
ChangeClothesTopAsync
ChangeCustomBodyWithoutCustomTexture
ChangeCustomClothes
ChangeCustomFaceWithoutCustomTexture
ChangeEyebrowColor
ChangeEyebrowKind
ChangeEyebrowLayout
ChangeEyebrowOpenMax
ChangeEyebrowPtn
ChangeEyebrowTilt
ChangeEyelashesColor
ChangeEyelashesKind
ChangeEyesBasePosY
ChangeEyesBlinkFlag
ChangeEyesColor
ChangeEyesEmission
ChangeEyesHighlighLayout
ChangeEyesHighlightColor
ChangeEyesHighlighTilt
ChangeEyesHighlightKind
ChangeEyesKind
ChangeEyesOpenMax
ChangeEyesPtn
ChangeEyesShadowRange
ChangeEyesWH
ChangeFaceDetailKind
ChangeFaceDetailPower
ChangeFaceGlossPower
ChangeFaceMetallicPower
ChangeHair (x2)
ChangeHairAll
ChangeHairAllAsync
ChangeHairAsync
ChangeHairTypeAccessoryColor
ChangeHead (x2)
ChangeHeadAsync (x2)
ChangeHohoAkaRate
ChangeLookEyesPtn
ChangeLookEyesTarget
ChangeLookNeckPtn
ChangeLookNeckTarget
ChangeMouthFixed
ChangeMouthOpenMax
ChangeMouthOpenMin
ChangeMouthPtn
ChangeNailColor
ChangeNailGloss
ChangeNipColor
ChangeNipGloss
ChangeNipKind
ChangeNipRate
ChangeNipScale
ChangeNowCoordinate (x3)
ChangeSettingHairAcsColor
ChangeSettingHairColor
ChangeSettingHairCorrectPos
ChangeSettingHairCorrectPosAll
ChangeSettingHairCorrectRot
ChangeSettingHairCorrectRotAll
ChangeSettingHairMeshColor
ChangeSettingHairMeshLayout
ChangeSettingHairMeshType
ChangeSettingHairMetallic
ChangeSettingHairShader
ChangeSettingHairSmoothness
ChangeSettingHairSpecular
ChangeSettingHairTypeAccessoryShader
ChangeSettingHairTypeAccessoryShaderAll
ChangeSettingMannequin
ChangeSimpleBodyColor
ChangeSimpleBodyDraw
ChangeSiriAkaRate
ChangeTearsRate
ChangeTexture (x2)
ChangeTongueState
ChangeUnderHairColor
ChangeUnderHairKind
ChangeWhiteEyesColor
CreateBodyTexture
CreateFaceTexture
CreateHairColor (x2)
DisableShapeBodyID
DisableShapeBust
DisableShapeMouth
DisableShapeNip
DrawOption
get_asVoice
get_bustGravity
get_bustNormal
get_bustSoft
get_changeShapeBodyMask
get_dictStateType
get_fbsaaVoice
get_hideHairAcs
get_hideInnerBWithBot
get_hohoAkaRate
get_IsBareFoot
get_IsVisibleInCamera
get_notBot
get_notInnerB
get_notInnerT
get_nowCoordinate
get_ShapeBodyNum
get_ShapeFaceNum
get_sibBody
get_sibFace
get_sibHand
get_siriAkaRate
get_siruNewLv
get_skinGlossRate
get_tearsRate
get_texBodyAlphaMask
get_texBodyBAlphaMask
get_texBraAlphaMask
get_texInnerBAlphaMask
get_texInnerTBAlphaMask
get_texPanstAlphaMask
get_underMaskBreakDisable
get_underMaskReflectionType
get_updateAlphaMask
get_updateAlphaMask2
get_updateCMBodyColor
get_updateCMBodyGloss
get_updateCMBodyLayout
get_updateCMBodyTex
get_updateCMFaceColor
get_updateCMFaceGloss
get_updateCMFaceLayout
get_updateCMFaceTex
get_wetRate
GetAccessoryDefaultColor
GetAccessoryDefaultParentStr (x2)
GetActiveTop
getAnimatorParamBool
getAnimatorStateInfo
GetClothesDefaultSetting
GetClothesStateKind
GetDefaultHairCorrectPosRate
GetDefaultHairCorrectRotRate
GetDynamicBoneBustAndHip
GetEnableShapeHand
GetEyebrowOpenMax
GetEyebrowPtn
GetEyesBlinkFlag
GetEyesOpenMax
GetEyesPtn
GetEyesPtnNum
GetHairAcsColorNum
getLayerWeight
GetLookEyesPtn
GetLookEyesRate
GetLookNeckPtn
GetLookNeckRate
GetMouthFixed
GetMouthOpenMax
GetMouthOpenMin
GetMouthPtn
GetNowClothesType
GetPosition
GetRandomFemaleCard
GetRotation
GetShapeBodyValue
GetShapeFaceValue
GetShapeHandBlendValue
GetShapeHandIndex
GetShapeIndexHandCount
GetSiruFlag
GetTexture
GetTongueState
HideEyeHighlight
InitBaseCustomTextureBody
InitBaseCustomTextureClothes
InitBaseCustomTextureFace
Initialize
InitializeControlAccessoryAll
InitializeControlAccessoryObject
InitializeControlBodyAll
InitializeControlBodyObject
InitializeControlCoordinateAll
InitializeControlCoordinateObject
InitializeControlCustomBodyAll
InitializeControlCustomBodyObject
InitializeControlCustomFaceAll
InitializeControlCustomFaceObject
InitializeControlCustomHairAll
InitializeControlCustomHairObject
InitializeControlFaceAll
InitializeControlFaceObject
InitializeControlHandAll
InitializeControlHandObject
InitializeControlLoadAll
InitializeControlLoadObject
InitializeExpression
InitShapeBody
InitShapeFace
InitShapeHand
IsAccessory
isBlend
IsClothes
IsClothesStateKind
IsClothesStateType
IsKokanHide
IsNextHash
IsParameterInAnimator
LateUpdateForce
Load
LoadAlphaMaskTexture
LoadAnimation
LoadAsync
LoadCharaFbxData
LoadCharaFbxDataAsync
LoadHitObject
LoadPreset
OnDestroy
playDynamicBoneBust (x2)
ReleaseAll
ReleaseAlphaMaskTexture
ReleaseBaseCustomTextureClothes
ReleaseBodyCustomTexture
ReleaseControlAccessoryAll
ReleaseControlAccessoryObject
ReleaseControlBodyAll
ReleaseControlBodyObject
ReleaseControlCoordinateAll
ReleaseControlCoordinateObject
ReleaseControlCustomBodyAll
ReleaseControlCustomBodyObject
ReleaseControlCustomFaceAll
ReleaseControlCustomFaceObject
ReleaseControlCustomHairAll
ReleaseControlCustomHairObject
ReleaseControlFaceAll
ReleaseControlFaceObject
ReleaseControlHandAll
ReleaseControlHandObject
ReleaseControlLoadAll
ReleaseControlLoadObject
ReleaseFaceCustomTexture
ReleaseHitObject
ReleaseObject
ReleaseShapeBody
ReleaseShapeFace
ReleaseShapeHand
Reload
ReloadAsync
RemoveClothesStateKind
ResetAccessoryMove
ResetDynamicBoneAccessories
ResetDynamicBoneAll
ResetDynamicBoneBustAndHip
ResetDynamicBoneClothes
ResetDynamicBoneHair
ReSetupDynamicBoneBust
RestoreMannequinHair
set_asVoice
set_bustGravity
set_bustNormal
set_bustSoft
set_changeShapeBodyMask
set_dictStateType
set_fbsaaVoice
set_hideHairAcs
set_hideInnerBWithBot
set_notBot
set_notInnerB
set_notInnerT
set_nowCoordinate
set_ShapeBodyNum
set_ShapeFaceNum
set_sibBody
set_sibFace
set_sibHand
set_siruNewLv
set_skinGlossRate
set_texBodyAlphaMask
set_texBodyBAlphaMask
set_texBraAlphaMask
set_texInnerBAlphaMask
set_texInnerTBAlphaMask
set_texPanstAlphaMask
set_underMaskBreakDisable
set_underMaskReflectionType
set_updateAlphaMask
set_updateAlphaMask2
set_updateCMBodyColor
set_updateCMBodyGloss
set_updateCMBodyLayout
set_updateCMBodyTex
set_updateCMFaceColor
set_updateCMFaceGloss
set_updateCMFaceLayout
set_updateCMFaceTex
set_wetRate
SetAccessoryDefaultColor
SetAccessoryPos
SetAccessoryRot
SetAccessoryScl
SetAccessoryState
SetAccessoryStateAll
SetActiveTop
setAllLayerWeight
setAnimatorParamBool
setAnimatorParamFloat
setAnimatorParamResetTrigger
setAnimatorParamTrigger
setAnimPtnCrossFade
SetBaseMaterial
SetBodyBaseMaterial
SetClothesDefaultSetting
SetClothesState
SetClothesStateAll
SetClothesStateNext
SetClothesStatePrev
SetCreateTexture
SetDefaultHairCorrectPosRate
SetDefaultHairCorrectPosRateAll
SetDefaultHairCorrectRotRate
SetDefaultHairCorrectRotRateAll
SetEnableShapeHand
SetFaceBaseMaterial
SetHairAcsDefaultColorParameterOnly
SetHairCorrectPosValue
SetHairCorrectRotValue
setLayerWeight
setPlay
SetPosition (x2)
SetRotation (x3)
SetShapeBodyValue
SetShapeFaceValue
SetShapeHandBlend
SetShapeHandIndex
SetShapeHandValue
SetSiruFlag
SetTransform
SetVoiceTransform
syncPlay (x3)
UpdateAccessoryMoveAllFromInfo
UpdateAccessoryMoveFromInfo
UpdateAlwaysShapeBody
UpdateAlwaysShapeHand
UpdateBlendShapeVoice
UpdateBustGravity
UpdateBustSoftness
UpdateBustSoftnessAndGravity
UpdateClothesSiru
UpdateClothesStateAll
UpdateForce
UpdateShapeBody
UpdateShapeBodyCalcForce
UpdateShapeBodyValueFromCustomInfo
UpdateShapeFace
UpdateShapeFaceValueFromCustomInfo
UpdateSiru
UpdateVisible
UpdateWet
```

### `AIChara.ChaFileControl`

25 methods.

```text
.ctor
CheckDataRange
CheckDataRangeBody
CheckDataRangeCoordinate (x2)
CheckDataRangeFace
CheckDataRangeHair
CheckDataRangeParameter
ConvertCharaFilePath
ConvertCoordinateFilePath
InitGameInfoParam
LoadCharaFile (x3)
LoadFace
LoadFacePreset
LoadFileLimited
LoadFromAssetBundle
LoadFromBytes
LoadFromTextAsset
LoadMannequinFile
SaveCharaFile (x3)
SaveFace
```

## Class roles

Roles are inferred from member names only (metadata, no decompilation).

| Class | Probable role |
|---|---|
| `Studio.Studio` | Scene-level singleton / god object. Owns `dicObjectCtrl` (all scene objects keyed by index), `sceneInfo`, `treeNodeCtrl`, `cameraCtrl`, and the Add*/Delete*/Duplicate/LoadScene/SaveScene/ImportScene entry points. The primary hook target for an MCP plugin (`Studio.Studio.Init`, `AddFemale`, `AddItem`, `GetCtrlInfo`, etc.). |
| `Studio.SceneInfo` | Serializable scene data model: object dictionary (`dicObject`), map, post-processing (bloom/AO/DOF/fog/sun shafts/vignette), chara/map lights, BGM. `Save`/`Load`/`Import` are the .png scene-file (de)serialization. |
| `Studio.CameraControl` | The user-driven viewport camera: orbit/zoom input processing (`InputMouseProc`, `InputKeyProc`), `Import`/`Export` of `CameraData`, FOV, LookAt, target position. Programmatic camera moves go through `Import(CameraData)` / `SetCamera`. |
| `Studio.CameraLightCtrl` | Thin controller binding the character/map light rigs to the camera (`lightChara`, nested `LightInfo`/`MapLightInfo`); light values live in `SceneInfo.charaLight`/`mapLight`. |
| `Studio.ObjectCtrlInfo` | Abstract base of every scene object (OCI = ObjectCtrlInfo). Holds `objectInfo` (serialized data), `treeNodeObject` (hierarchy node), `guideObject` (gizmo), and the lifecycle hooks (`OnSelect`, `OnDelete`, `OnAttach`/`OnDetach`, `OnVisible`). |
| `Studio.OCIChar` | Character scene object. Wraps `charInfo` (`AIChara.ChaControl` via `charReference`), FK/IK (`fkCtrl`, `ikCtrl`, `finalIK`, `ActiveKinematicMode`), neck look (`neckLookCtrl`), expressions (mouth/eyes/blink/tears), animation (`LoadAnime`, `animeSpeed`, `animePattern`), voice and clothing state. Subclassed by `OCICharFemale` / `OCICharMale`. |
| `Studio.OCIItem` | Item scene object: color/emission/metallic/pattern/alpha setters, item FK (`itemFKCtrl`), dynamic bones, anime pattern, panels. |
| `Studio.OCILight` | Light scene object: `SetColor`, `SetIntensity`, `SetRange`, `SetSpotAngle`, `SetShadow`, `SetEnable` over a Unity `Light`. |
| `Studio.OCIFolder` | Folder scene object: a pure hierarchy grouping node with a `name`. |
| `Studio.OCICamera` | Camera scene object (a camera saved in the scene, `SetActive` switches to it), rendered via `cameraControl`. |
| `Studio.OCIRoute` | Motion-path object: ordered `listPoint` of `OCIRoutePoint`, `Play`/`Stop`/`Move`, line rendering. |
| `Studio.TreeNodeCtrl` | Workspace (hierarchy) tree UI controller: selection (`selectNode`, `selectNodes`, `SelectSingle`, `SelectMultiple`), parenting (`SetParent`), node add/delete. `Studio.TreeNodeObject` is one row. |
| `Studio.GuideObject` | Translate/rotate/scale gizmo attached to any manipulable transform; `MoveLocal`/`MoveWorld`/`Rotation`/`SetScale` plus `changeAmount` bridge to an undoable transform state (`Studio.ChangeAmount` is the pos/rot/scale record). `GuideObjectManager` tracks and selects them. |
| `Studio.AnimeControl` | Anim panel UI controller (speed/pattern/option-param sliders) bound to the selected `ObjectCtrlInfo`. Not the animation engine itself. |
| `Studio.AddObjectFemale/Male/Item/Assist` | Static-style factories that instantiate scene objects: `Add()` returns the new OCI; `Assist` builds bone/IK-target/look-at scaffolding on characters. Key API for AI-driven object spawning. |
| `Studio.Info` | Master list/catalog singleton: dictionaries of every loadable asset (items `dicItemLoadInfo`, anims `dicAnimeLoadInfo`, lights, maps, BGM, voices) loaded from ExcelData/AssetBundles. The source for enumerating what can be spawned. |
| `Studio.FKCtrl` / `Studio.IKCtrl` / `Studio.ItemFKCtrl` | Per-character (or per-item) kinematics drivers: `FKCtrl` applies stored bone rotations in `LateUpdate`; `IKCtrl` manages the IK target list (`listIKInfo`) feeding FinalIK. Mode switching goes through `OCIChar.ActiveKinematicMode` (`OICharInfo.KinematicMode` enum: static/anime/ik/fk). |
| `NeckLookControllerVer2` (IL.dll) | Neck/eye look-at driver referenced by `OCIChar.neckLookCtrl`: pattern (`ptnNo`), target transform and rate. |
| `AIChara.ChaControl` | The character body/runtime API shared with the main game (clothes/accessory state, blend shapes, siru/hoho, bone refs). `OCIChar.charInfo` points at it. |
| `AIChara.ChaFileControl` | Character card (de)serialization: `LoadCharaFile`/`SaveCharaFile`, `LoadFileLimited`, png-embedded data. |

## Implications for MiraStudio

- `Studio.Studio` exposes static-looking accessors (`GetCtrlInfo`, `AddCtrlInfo`, `dicObjectCtrl` keyed by `Info` index): the natural surface for an MCP command layer.
- Undo/redo exists natively: `Studio.UndoRedoManager` plus the `*Command` classes (`GuideCommand.MoveAddCommand`, `RotationEqualsCommand`, ...). MCP mutations should route through these to stay undoable.
- Post-processing and lighting are all `SceneInfo` fields, settable without touching UI classes.
- `MPCharCtrl` and the other `MP*Ctrl` classes are the Manipulate Panel UI. They are a reference for the correct call sequence when mutating an OCI, but should not be driven directly.
- `LightCtrl` and `MoveAnimator` do not exist in this build: light control is `OCILight` plus `SceneInfo` light data; route/motion is `OCIRoute`.

## See also

- [Studio API](../studio-api.md)
- [Reverse engineering](../reverse-engineering.md)
- [Assemblies](assemblies.md)
