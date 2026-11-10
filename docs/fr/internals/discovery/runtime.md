# Runtime — versions du moteur et du loader de HS2 DX BetterRepack R16

Les versions d'Unity, de Mono et de BepInEx sur lesquelles tourne l'installation, la façon dont le loader s'accroche, et ce que cela implique pour le plugin Miraneo.

Racine du jeu : `<game>`. Date de l'inventaire : 2026-08-30.

## Fichiers de version

- Contenu du fichier `version` : `BetterRepack R16`
- `[BR] README.txt` : `Release 16 // 28.08.2024`, jeu de base à jour au 30.10.2020.

### Composants livrés dans R16

Versions telles que listées dans le changelog du README.

| Composant | Version |
|---|---|
| BepInEx | 5.4.23.1 |
| BepisPlugins | R20.0 |
| BepInEx.ConfigurationManager | 18.3 |
| IllusionFixes HS2 | 21.6 |
| HSPlugins | R2.13.1 |
| HS2API (ModdingAPI) | 1.41 |
| HS2ABMX | 5.2.2 |
| HS2_MaterialEditor | 3.9.1 |
| RuntimeUnityEditor | 5.4.0 |
| HS2_CheatTools | 3.3.2 |
| VNGE | 43.0 (optionnel) |
| XUnity.AutoTranslator | 5.3.1 |
| KKManager | 1.3.1 (utilitaire) |

Le `BepInEx.dll` installé annonce 5.4.23.2 (voir [BepInEx](bepinex.md)) ; le changelog dit 5.4.23.1.

## Moteur et exécutables

`FileVersionInfo` des binaires principaux :

| Fichier | FileVersion | ProductVersion |
|---|---|---|
| HoneySelect2.exe | 2018.4.11.7379119 | 2018.4.11.7379119 |
| StudioNEOV2.exe | 2018.4.11.7379119 | 2018.4.11.7379119 |
| UnityPlayer.dll | 2018.4.11.7379119 | 2018.4.11.7379119 |

Le moteur est **Unity 2018.4.11f1** (LTS). Les champs FileDescription, ProductName et CompanyName sont vides.

## Architecture et backend de script

- Type de machine PE de `StudioNEOV2.exe` : **Amd64** (x86-64, 64 bits).
- Backend de script : **Mono** (`MonoBleedingEdge` présent), pas IL2CPP.
- Contenu de `MonoBleedingEdge\EmbedRuntime` :
  - `mono-2.0-bdwgc.dll` (4,946,376 octets)
  - `MonoPosixHelper.dll` (781,312 octets)

## Loader

### `doorstop_config.ini`

- `enabled = true`
- `target_assembly = BepInEx\core\BepInEx.Preloader.dll` (le point d'entrée du preloader BepInEx 5)
- `redirect_output_log = false`
- Section `[UnityMono]` présente (style Doorstop 4.x) : `debug_enabled = false`, `debug_address = 127.0.0.1:10000`, `debug_suspend = false`
- Le serveur de debug Mono peut être activé depuis cette config pour le développement.

### `InitSetting.exe.config`

- Lanceur .NET Framework : `supportedRuntime v4.0` (repli sur v2.0.50727).
- Chemin de résolution des assemblies : `BepInEx\LauncherEN`.

## Vérification des processus

`Get-Process HoneySelect2, StudioNEOV2, HoneySelect2VR` n'a rien renvoyé : aucun processus du jeu ne tournait pendant l'inventaire.

## Implications pour le plugin Miraneo

- Framework cible : .NET Framework 4.x, compatible net35 à net46 (le runtime Mono d'Unity 2018.4 correspond au profil d'API .NET 4.x ; compatibilité C# 7.3 via Mono). Voir [Assemblies](assemblies.md) pour le choix de net46.
- API de plugin : BepInEx 5.4.23.x (`BaseUnityPlugin`, espace de noms BepInEx 5), pas BepInEx 6.
- Le processus est en 64 bits et le backend Mono donne la réflexion et Harmony complets, plus le serveur de debug Mono via la config Doorstop.

## Voir aussi

- [Assemblies](assemblies.md)
- [BepInEx](bepinex.md)
- [Modding APIs](modding-apis.md)
