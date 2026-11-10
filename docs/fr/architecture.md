# Architecture — couches, règles, décisions

Comment Miraneo est découpé, les règles que suit chaque couche, et pourquoi les principaux choix ont été faits.

## Couches

```text
Client MCP (Claude / ChatGPT)
  │  MCP sur stdio
  ▼
Miraneo MCP  (TypeScript / Bun)          mcp/
  │  HTTP JSON-RPC 2.0, localhost:6472
  ▼
Miraneo.Plugin (BepInEx, C#, net46)      plugin/Miraneo.Plugin/
  ├── Transport   — HttpListener sur localhost, parse les requêtes, dispatche, dispatcher du thread principal
  ├── Core        — contrats indépendants du jeu : DTO, modèle JSON, parsing des paramètres, exceptions d'API
  ├── Services    — implémentent les contrats, orchestrent les appels d'adapter, coroutines du thread principal
  └── Adapters    — la couche qui référence les types Unity / Studio (StudioAdapter, TimelineAdapter, NodesConstraintsAdapter)
  ▼
Studio NEO V2 (Unity 2018.4.11f1 / Mono)
```

Partagé par les deux côtés : `shared/schemas/`, un fichier JSON par tool avec sa `description`, son `input` et son `output`. Le serveur MCP importe ces fichiers comme définitions de tools ; le plugin les implémente.

| Couche | Dossier | Rôle |
|---|---|---|
| Serveur MCP | `mcp/src/` | `index.ts` sert le MCP sur stdio ; `tools.ts` liste les 56 tools avec un timeout chacun ; `bridge.ts` poste le JSON-RPC au plugin |
| Transport | `Transport/` | `RpcServer` (`POST /rpc`, `GET /health`, batch) et `MainThreadDispatcher` |
| Core | `Core/` | `IStudioApi`, `Dtos`, `Json`, `Params` |
| Services | `Services/` | `StudioService`, `SceneService`, `CameraService`, `MapService`, `KinematicsService`, `ScreenshotService`, `TimelineService`, `ConstraintService` |
| Adapters | `Adapters/` | `StudioAdapter`, plus les ponts par réflexion `TimelineAdapter` et `NodesConstraintsAdapter` |

## Règles

- **Les types Unity ne franchissent jamais la frontière de l'adapter.** Core, Services et Transport ne parlent qu'en DTO : des enregistrements sérialisables simples, reflétés par les schémas JSON de `shared/schemas/`.
- **Discipline du thread principal.** Les API de Studio et d'Unity ne s'utilisent que sur le thread principal. Le transport reçoit les requêtes sur des threads de travail et délègue l'exécution au thread principal d'Unity (une file vidée dans `Update()`) ; les réponses se terminent de façon asynchrone. Un appel Unity depuis un thread de travail n'échoue pas proprement : il fait planter Studio.
- **Ids stables.** Les clés du dictionnaire `ObjectCtrlInfo` de Studio sont la source de vérité de l'identité des objets. Miraneo les expose comme ids entiers.
- **Lecture seule d'abord.** Le premier jeu de tools livré était l'inspection plus la capture d'écran ; les écritures sont venues après.
- **Batch et idempotence.** `studio_execute_batch` exécute une liste d'opérations en un aller-retour. Les opérations d'écriture acceptent des valeurs absolues, pas des deltas, et des patchs partiels (un champ omis est inchangé).
- **Schémas stricts.** Chaque tool a un schéma JSON ; le serveur MCP le sert comme schéma d'entrée du tool.
- **Les plugins optionnels sont reliés par réflexion.** Pas d'assembly de référence, chaque membre résolu en une seule passe tout-ou-rien, tools enregistrés que le plugin soit là ou non. Voir [optional-plugins.md](./optional-plugins.md).

## Journal des décisions

| # | Décision | Statut |
|---|---|---|
| 1 | Le plugin cible BepInEx 5 x64 / net46 (Unity 2018.4.11f1 + Mono, BepInEx 5.4.23.2, HarmonyX 2.9) | confirmé par la découverte |
| 2 | MCP en TypeScript sur Bun, SDK MCP officiel, transport stdio | fait |
| 3 | Transport plugin⇄MCP : HTTP JSON-RPC 2.0 via `HttpListener` (sans dépendance, débogable avec curl) ; événements push reportés | fait |
| 4 | Pas de dépendance à HS2API : classes Studio brutes uniquement, à réévaluer pour la création de personnages et l'extended save | fait |
| 5 | Capture d'écran : `ScreenCapture.CaptureScreenshotAsTexture` maison après `WaitForEndOfFrame`, pas le plugin Screencap | fait |
| 6 | Les écritures passent par les commandes de l'`UndoRedoManager` de Studio quand elles existent, pour que les modifications de l'agent soient annulables dans l'interface | fait |
| 7 | Les écritures prennent des valeurs absolues et des patchs partiels (champ omis = inchangé) pour l'idempotence | fait |
| 8 | Les plugins tiers (Timeline, NodesConstraints) sont des dépendances souples de BepInEx reliées par réflexion ; leurs tools sont toujours enregistrés et échouent avec un `-32001` lisible quand le plugin est absent | fait |
| 9 | `studio_execute_batch` valide chaque étape d'avance, tourne sur le thread de travail HTTP, et n'a pas de rollback | fait |

## Pour aller plus loin

- Format sur le fil : [protocol.md](./protocol.md).
- Tous les tools : [tools.md](./tools.md).
- La surface de l'API Studio visée par l'adapter et la méthode de rétro-ingénierie : [internals/studio-api.md](./internals/studio-api.md), [internals/reverse-engineering.md](./internals/reverse-engineering.md).
- Notes de découverte brutes : [assemblies](./internals/discovery/assemblies.md), [BepInEx](./internals/discovery/bepinex.md), [API de modding](./internals/discovery/modding-apis.md), [runtime](./internals/discovery/runtime.md), [classes Studio](./internals/discovery/studio-classes.md), [UserData](./internals/discovery/userdata.md).
