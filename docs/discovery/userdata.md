# User-Content Layout — HS2 DX BetterRepack R16

Game root: `D:\[ScrewThisNoise] HoneySelect 2 DX BetterRepack R16(1)`
Scan date: 2026-08-30 (read-only inventory)

## 1. UserData layout (2 levels)

Top level of `UserData\`:

| Entry | Type | Notes |
|---|---|---|
| bg | dir | background images |
| cardframe | dir | subdirs: `Back`, `Front` |
| chara | dir | subdirs: `female`, `male`, `navi` — character cards |
| config | dir | `system.xml`, `voice.xml` |
| coordinate | dir | subdirs: `female`, `male` — coordinate (outfit) cards |
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

### Key folders + file counts (recursive)

| Folder | Files | Role |
|---|---|---|
| `UserData\Studio\scene` | 107 | **Studio NEO V2 scene cards** (PNG scenes) |
| `UserData\Studio\pose` | 3,868 | Studio pose data |
| `UserData\chara\female` | 1,050 | Female character cards |
| `UserData\chara\male` | 30 | Male character cards |
| `UserData\coordinate\female` | 1,800 | Female coordinate cards |
| `UserData\coordinate\male` | 166 | Male coordinate cards |

`UserData\Studio\scene` subfolders: `_autosave`, `[BR] BetterRepack`, `[Community]`, `Enduro88 Pose Pack Scene`, `Freelancer604 pose examples`, `Modpack`.

### Screenshot / cap folder

`UserData\cap` does **not exist yet** — the ScreenshotManager plugin is installed (`BepInEx\config\com.bepis.bepinex.screenshotmanager.cfg`, no custom Folder/Path key found), and its default output is `UserData\cap`, created on first capture. No other screenshot directory exists under UserData or the game root (`ScreenEffect` is filter presets, not captures).

## 2. Root data folders (one-liners)

- **DefaultData** — 5 items (`bg`, `cardframe`, `ScreenEffect`, `system`, `url`): pristine copies of default user-data content.
- **DHH_Data** — 15 items (`LutTextures`, `ModData`, `Presets`, `*.dhs` graphics presets, `HowToLocalizeDHH.txt`): DHH graphics-enhancement plugin data/presets.
- **cubemaps** — 1 item (`exssynthetic_disc_a.cube`): LUT/cubemap file for graphics plugins.
- **manual / manual_s / manual_v** — HTML game manuals (`manual_en.html`, `お読み下さい.html`, img folders) for main game, Studio, and VR respectively.

## 3. Utilities (one-liners)

- **[UTILITY] KKManager** — `KKManager.exe` FileVersion **1.3.1.0**; mod/card manager with localization folders (de/fr/ja/ru/zh…), `ModpackTool`, `SB3UGS` bundled.
- **[UTILITY] HS2CharEdit** — .NET card editor (`HS2CharEdit.exe` + .dll/.deps.json/.pdb, Octokit.dll, README.md): edits HS2 character card data directly.
- **[UTILITY] Card Image Replacer** — `CardImageReplacer.exe` (+ .config, README.txt): swaps the visible PNG image on cards without touching embedded data.
