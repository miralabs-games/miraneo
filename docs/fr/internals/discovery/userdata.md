# User Content — arborescence UserData de HS2 DX BetterRepack R16

Où se trouvent les scènes, poses, cartes de personnage et autres contenus utilisateur de l'installation, avec les nombres de fichiers, plus les dossiers de données à la racine et les utilitaires fournis.

Racine du jeu : `<game>`. Date du scan : 2026-08-30 (inventaire en lecture seule).

## Arborescence UserData

Premier niveau de `UserData\` :

| Entrée | Type | Notes |
|---|---|---|
| bg | dossier | images de fond |
| cardframe | dossier | sous-dossiers : `Back`, `Front` |
| chara | dossier | sous-dossiers : `female`, `male`, `navi` (cartes de personnage) |
| config | dossier | `system.xml`, `voice.xml` |
| coordinate | dossier | sous-dossiers : `female`, `male` (cartes de coordonnées / tenues) |
| Custom | dossier | |
| frame | dossier | |
| MaterialEditor | dossier | |
| overlay | dossier | sous-dossier : `Sena Track and Field` |
| Overlays | dossier | sous-dossiers : `[Community]`, `[xq111222] Skin 5-6`, `Body`, `Body - Cum`, `Body - Cum Splatter`, `Boy2239`, `Face`, `overlay`, `sena`, `Uncensor` |
| pattern_thumb | dossier | |
| save | dossier | sauvegardes du jeu principal |
| ScreenEffect | dossier | |
| Studio | dossier | sous-dossiers : `pose`, `scene` |
| system | dossier | |
| HS2TakeChargeRules.xml | fichier | |
| setup.xml | fichier | |

### Dossiers clés et nombres de fichiers

Les nombres sont récursifs.

| Dossier | Fichiers | Rôle |
|---|---|---|
| `UserData\Studio\scene` | 107 | **Cartes de scène Studio NEO V2** (scènes PNG) |
| `UserData\Studio\pose` | 3,868 | Données de pose Studio |
| `UserData\chara\female` | 1,050 | Cartes de personnage féminin |
| `UserData\chara\male` | 30 | Cartes de personnage masculin |
| `UserData\coordinate\female` | 1,800 | Cartes de coordonnées féminines |
| `UserData\coordinate\male` | 166 | Cartes de coordonnées masculines |

Sous-dossiers de `UserData\Studio\scene` : `_autosave`, `[BR] BetterRepack`, `[Community]`, `Enduro88 Pose Pack Scene`, `Freelancer604 pose examples`, `Modpack`.

### Dossier de captures

`UserData\cap` n'existe pas encore. Le plugin ScreenshotManager est installé (`BepInEx\config\com.bepis.bepinex.screenshotmanager.cfg`, aucune clé Folder/Path personnalisée) et sa sortie par défaut est `UserData\cap`, créé à la première capture. Aucun autre dossier de captures n'existe sous `UserData` ni à la racine du jeu (`ScreenEffect` contient des préréglages de filtres, pas des captures).

## Dossiers de données à la racine

| Dossier | Contenu |
|---|---|
| DefaultData | 5 éléments (`bg`, `cardframe`, `ScreenEffect`, `system`, `url`) : copies d'origine du contenu utilisateur par défaut |
| DHH_Data | 15 éléments (`LutTextures`, `ModData`, `Presets`, préréglages graphiques `*.dhs`, `HowToLocalizeDHH.txt`) : données et préréglages du plugin d'amélioration graphique DHH |
| cubemaps | 1 élément (`exssynthetic_disc_a.cube`) : fichier LUT / cubemap pour les plugins graphiques |
| manual / manual_s / manual_v | manuels HTML du jeu (`manual_en.html`, `お読み下さい.html`, dossiers d'images) pour le jeu principal, Studio et la VR respectivement |

## Utilitaires

| Utilitaire | Contenu |
|---|---|
| **[UTILITY] KKManager** | `KKManager.exe` FileVersion **1.3.1.0** ; gestionnaire de mods et de cartes avec dossiers de localisation (de/fr/ja/ru/zh…), `ModpackTool` et `SB3UGS` inclus |
| **[UTILITY] HS2CharEdit** | éditeur de cartes .NET (`HS2CharEdit.exe` + .dll/.deps.json/.pdb, `Octokit.dll`, `README.md`) ; modifie directement les données des cartes de personnage HS2 |
| **[UTILITY] Card Image Replacer** | `CardImageReplacer.exe` (+ .config, `README.txt`) ; remplace l'image PNG visible des cartes sans toucher aux données embarquées |

## Voir aussi

- [Runtime](runtime.md)
- [BepInEx](bepinex.md)
