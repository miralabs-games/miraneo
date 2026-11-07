# User Content — UserData layout of HS2 DX BetterRepack R16

Where scenes, poses, character cards and other user content live in the install, with file counts, plus the root data folders and bundled utilities.

Game root: `<game>`. Scan date: 2026-08-30 (read-only inventory).

## UserData layout

Top level of `UserData\`:

| Entry | Type | Notes |
|---|---|---|
| bg | dir | background images |
| cardframe | dir | subdirs: `Back`, `Front` |
| chara | dir | subdirs: `female`, `male`, `navi` (character cards) |
| config | dir | `system.xml`, `voice.xml` |
| coordinate | dir | subdirs: `female`, `male` (coordinate / outfit cards) |
| Custom | dir | |
| frame | dir | |
| MaterialEditor | dir | |
| overlay | dir | subdir: `Sena Track and Field` |
| Overlays | dir | subdirs: `[Community]`, `[xq111222] Skin 5-6`, `Body`, `Body - Cum`, `Body - Cum Splatter`, `Boy2239`, `Face`, `overlay`, `sena`, `Uncensor` |
| pattern_thumb | dir | |
| save | dir | main-game saves |
| ScreenEffect | dir | |
| Studio | dir | subdirs: `pose`, `scene` |
| system | dir | |
| HS2TakeChargeRules.xml | file | |
| setup.xml | file | |

### Key folders and file counts

Counts are recursive.

| Folder | Files | Role |
|---|---|---|
| `UserData\Studio\scene` | 107 | **Studio NEO V2 scene cards** (PNG scenes) |
| `UserData\Studio\pose` | 3,868 | Studio pose data |
| `UserData\chara\female` | 1,050 | Female character cards |
| `UserData\chara\male` | 30 | Male character cards |
| `UserData\coordinate\female` | 1,800 | Female coordinate cards |
| `UserData\coordinate\male` | 166 | Male coordinate cards |

`UserData\Studio\scene` subfolders: `_autosave`, `[BR] BetterRepack`, `[Community]`, `Enduro88 Pose Pack Scene`, `Freelancer604 pose examples`, `Modpack`.

### Screenshot folder

`UserData\cap` does not exist yet. The ScreenshotManager plugin is installed (`BepInEx\config\com.bepis.bepinex.screenshotmanager.cfg`, no custom Folder/Path key), and its default output is `UserData\cap`, created on the first capture. No other screenshot directory exists under `UserData` or the game root (`ScreenEffect` holds filter presets, not captures).

## Root data folders

| Folder | Content |
|---|---|
| DefaultData | 5 items (`bg`, `cardframe`, `ScreenEffect`, `system`, `url`): pristine copies of the default user-data content |
| DHH_Data | 15 items (`LutTextures`, `ModData`, `Presets`, `*.dhs` graphics presets, `HowToLocalizeDHH.txt`): data and presets of the DHH graphics-enhancement plugin |
| cubemaps | 1 item (`exssynthetic_disc_a.cube`): LUT / cubemap file for graphics plugins |
| manual / manual_s / manual_v | HTML game manuals (`manual_en.html`, `お読み下さい.html`, image folders) for the main game, Studio and VR respectively |

## Utilities

| Utility | Content |
|---|---|
| **[UTILITY] KKManager** | `KKManager.exe` FileVersion **1.3.1.0**; mod and card manager with localization folders (de/fr/ja/ru/zh…), `ModpackTool` and `SB3UGS` bundled |
| **[UTILITY] HS2CharEdit** | .NET card editor (`HS2CharEdit.exe` + .dll/.deps.json/.pdb, `Octokit.dll`, `README.md`); edits HS2 character card data directly |
| **[UTILITY] Card Image Replacer** | `CardImageReplacer.exe` (+ .config, `README.txt`); swaps the visible PNG image on cards without touching the embedded data |

## See also

- [Runtime](runtime.md)
- [BepInEx](bepinex.md)
