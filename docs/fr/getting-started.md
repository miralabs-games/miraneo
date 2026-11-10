# Getting started — compiler, déployer, connecter

D'un dépôt tout neuf à un agent qui pilote Studio NEO V2 : prérequis, build du plugin, déclaration du MCP, contrôle de santé, smoke test.

## Prérequis

La cible exacte contre laquelle le plugin est compilé et testé :

| Composant | Version |
|---|---|
| Jeu | Honey Select 2 DX BetterRepack R16 |
| Studio | Studio NEO V2 (`StudioNEOV2.exe`, x64) |
| Moteur | Unity 2018.4.11f1, x64, Mono |
| Mod loader | BepInEx 5.4.23.2 |
| Bibliothèque de patch | HarmonyX 2.9 |
| Runtime du plugin | .NET Framework 4.6 (`net46`) |
| Runtime du MCP | Bun |

Pour compiler, il faut aussi un SDK .NET capable de construire des projets de style SDK (le projet récupère les assemblies de référence `net46` via NuGet) et Bun.

Plugins tiers optionnels, versions testées : Timeline 1.4.1 et NodesConstraints 1.3.3. Voir [optional-plugins.md](./optional-plugins.md).

Comment ces versions ont été établies : [internals/reverse-engineering.md](./internals/reverse-engineering.md) et [internals/discovery/runtime.md](./internals/discovery/runtime.md).

## Compiler et déployer le plugin

Le projet doit savoir où le jeu est installé. Copiez le fichier props d'exemple et renseignez `GameDir` :

```text
plugin/Miraneo.Plugin/GamePath.props.example  →  plugin/Miraneo.Plugin/GamePath.local.props
```

```xml
<Project>
  <PropertyGroup>
    <GameDir>C:\Path\To\HoneySelect2</GameDir>
  </PropertyGroup>
</Project>
```

`GamePath.local.props` n'est pas suivi par git. Puis compilez :

```sh
dotnet build plugin/Miraneo.Plugin -c Release
```

Le build compile `Miraneo.dll` contre les assemblies du jeu (`StudioNEOV2_Data\Managed`, `BepInEx\core\BepInEx.dll`) et le copie dans `BepInEx\Plugins\Miraneo` du dossier du jeu. Ajoutez `-p:SkipDeploy=true` pour compiler sans copier.

## Configuration du plugin

Au premier lancement, BepInEx écrit `BepInEx\config\com.miraneo.plugin.cfg` :

| Section | Clé | Défaut | Signification |
|---|---|---|---|
| `Server` | `Port` | `6472` | Port TCP du serveur JSON-RPC local (`http://localhost:<port>`) |
| `Server` | `RunInBackground` | `true` | Garde Unity actif quand la fenêtre Studio n'a plus le focus, pour qu'un agent puisse continuer à le piloter |

Si vous changez le port, indiquez-le au serveur MCP avec la variable d'environnement `MIRANEO_URL` (défaut `http://localhost:6472`).

## Déclarer le serveur MCP

Le dépôt fournit un `.mcp.json`. Tout client MCP qui le lit lance le serveur en stdio :

```json
{
  "mcpServers": {
    "miraneo": {
      "command": "bun",
      "args": ["run", "mcp/src/index.ts"]
    }
  }
}
```

Installez les dépendances une fois :

```sh
cd mcp && bun install
```

Le serveur peut aussi être lancé à la main avec `bun run start` depuis `mcp/`.

## Lancer Studio et vérifier le plugin

1. Lancez `StudioNEOV2.exe`.
2. Attendez le chargement de la scène Studio.
3. Sondez le plugin :

```sh
curl http://localhost:6472/health
```

```json
{"ok":true,"studio":true,"version":"0.1.0"}
```

`ok` signifie que le plugin écoute. `studio` passe à `true` quand la scène Studio est prête. Les tools qui ont besoin de la scène, comme `studio_get_scene` et `studio_screenshot`, échouent avec `-32000` (studio pas prêt) jusque-là ; `studio_get_info` fonctionne dans les deux cas.

Si le log indique que le plugin n'a pas pu lier le port avec une erreur d'accès, exécutez une fois depuis une invite élevée (remplacez le nom d'utilisateur) :

```text
netsh http add urlacl url=http://localhost:6472/ user=<votre utilisateur>
```

## Smoke test

```sh
cd mcp && bun install && bun x tsc --noEmit && bun run test/smoke.ts
```

Le typecheck doit passer. Le smoke test lance lui-même le serveur MCP, affiche la liste des tools (`tools (56): ...`) et appelle `studio_get_info`. Sans Studio, la liste fonctionne et l'appel signale que le plugin est injoignable à `http://localhost:6472`.

## Première scène

Les ids ne se devinent pas ; cherchez avant de créer. Les valeurs entre chevrons représentent des ids renvoyés par la recherche :

```text
studio_get_info                                  # plugins, versions, studio_loaded
studio_search_catalog {"catalog":"item","query":"chair"}
studio_create_object {"group":<g>,"category":<c>,"no":<n>,"pos":[0,0,0]}
studio_set_camera {"target_pos":[0,1,0],"distance":4}
studio_screenshot {}
studio_save_scene {"name":"agent/first"}
```

Suite : tous les tools dans [tools.md](./tools.md), le format sur le fil dans [protocol.md](./protocol.md).
