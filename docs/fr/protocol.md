# Protocol — JSON-RPC sur localhost

Le plugin sert du JSON-RPC 2.0 en HTTP sur `localhost:6472`. Le serveur MCP en est un client ; `curl` en est un autre.

## Endpoints

| Méthode et chemin | Rôle |
|---|---|
| `POST /rpc` | Appel JSON-RPC 2.0. Les noms de méthode sont les noms de tools MCP ; `params` est un objet. |
| `GET /health` | Sonde de vivacité peu coûteuse. |

Toute autre méthode ou chemin répond HTTP 404 avec un corps d'erreur `-32600` (`use POST /rpc or GET /health`).

Le serveur est un `HttpListener` lié à `http://localhost:<port>/`. Le port est `Server.Port` dans `BepInEx\config\com.miraneo.plugin.cfg` (défaut `6472`) ; côté MCP, la variable `MIRANEO_URL` (défaut `http://localhost:6472`).

## Health

```sh
curl http://localhost:6472/health
```

```json
{"ok":true,"studio":true,"version":"0.1.0"}
```

`studio` vaut `false` tant que la scène Studio n'est pas prête. Le drapeau est rafraîchi depuis l'`Update()` d'Unity, donc `/health` ne touche jamais Unity depuis un thread de travail.

## Un appel

```sh
curl -s http://localhost:6472/rpc -d '{"jsonrpc":"2.0","id":1,"method":"studio_get_camera","params":{}}'
```

```json
{"jsonrpc":"2.0","id":1,"result":{ "...": "même forme que la sortie du tool" }}
```

Le résultat est l'`output` du tool dans [`shared/schemas/`](../../shared/schemas/). Les erreurs, y compris tout échec d'un tool, reviennent avec HTTP 200 et un objet `error` :

```json
{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"..."}}
```

Côté MCP, une erreur devient un résultat de tool avec `isError: true` et le texte `Studio error <code>: <message>`. Si le plugin est totalement injoignable, le message le dit et nomme l'URL.

## Codes d'erreur

| Code | Signification |
|---|---|
| `-32700` | Erreur de parsing : le corps n'est pas du JSON valide. |
| `-32600` | Requête invalide : pas un objet JSON, pas de `method`, ou mauvais chemin. |
| `-32601` | Méthode inconnue. |
| `-32602` | Paramètres invalides. |
| `-32000` | Studio pas prêt : la scène Studio n'est pas encore chargée. |
| `-32001` | Opération échouée. C'est aussi la réponse d'un tool dont le plugin optionnel est absent, avec la raison. |
| `-32002` | Timeout du thread principal : l'opération n'a pas fini à temps. |
| `-32003` | Introuvable : l'id, le chemin ou le nom n'existe pas. |
| `-32603` | Erreur interne. |

## Threads et timeouts

Les API de Studio et d'Unity ne s'utilisent que sur le thread principal. Le listener HTTP reçoit chaque requête sur un thread de travail ; le handler est mis en file et s'exécute sur le thread principal d'Unity, vidé dans `Update()`. Les tools qui attendent une coroutine (capture d'écran, chargement et sauvegarde de scène, chargement de map, caméra, kinematics) démarrent sur le thread principal et se terminent plus tard.

Chaque méthode a son propre timeout, et le côté MCP a aussi les siens :

| Timeout côté MCP | Tools |
|---|---|
| 10 s | `studio_get_info` |
| 15 s | `studio_get_scene`, `studio_list_characters`, `studio_list_objects`, `studio_get_camera`, `studio_list_scenes`, `studio_list_outfit`, `studio_list_attach_points`, `studio_list_bones`, `studio_list_overlays`, `studio_set_light`, `studio_set_object_transform`, `studio_delete_object`, `studio_set_object_visible`, `studio_rename_object`, `studio_create_folder`, `studio_set_camera`, expression, hands, state, bone, IK, `studio_attach_object`, scene lighting, et les tools Timeline et NodesConstraints sauf `studio_timeline_single_file` |
| 30 s | `studio_search_catalog`, `studio_set_character_kinematics` |
| 90 s | `studio_screenshot`, `studio_save_scene`, `studio_create_object`, `studio_create_light`, `studio_duplicate_object`, pose, anime, overlay, post-processing, `studio_timeline_single_file` |
| 180 s | `studio_create_character`, `studio_set_character_outfit` |
| 240 s | `studio_list_character_cards`, `studio_list_poses`, `studio_list_coordinates`, `studio_load_scene`, `studio_set_map` |
| 300 s | `studio_execute_batch` |

## execute_batch

`studio_execute_batch` exécute jusqu'à 32 étapes dans l'ordre en un seul appel. Chaque étape est `{method, params}` ; `method` est n'importe quel tool Miraneo sauf `studio_screenshot` et `studio_execute_batch` lui-même.

```json
{"jsonrpc":"2.0","id":2,"method":"studio_execute_batch","params":{"steps":[
  {"method":"studio_create_object","params":{"group":1,"category":1,"no":1,"pos":[0,0,0]}},
  {"method":"studio_set_camera","params":{"distance":5}}
]}}
```

Groupe, catégorie et `no` ci-dessus sont des exemples : lisez les vrais dans `studio_search_catalog`.

**Validé d'avance.** Avant que rien ne s'exécute, chaque étape est vérifiée : `steps` doit être un tableau non vide d'au plus 32 objets, chacun avec une `method`, jamais le batch lui-même (pas d'imbrication), jamais `studio_screenshot` (son image reviendrait en texte base64 qu'aucun client ne sait afficher), la méthode doit exister, et `params` doit être un objet. Une seule étape invalide refuse tout le batch avec `-32602`, et rien n'a été appliqué.

**Pas de rollback.** Une fois l'exécution lancée, une étape qui échoue arrête le batch et tout ce qui précède reste appliqué. Studio n'a pas de transaction, `UndoRedoManager.Do` pousse sa commande *avant* de l'exécuter, et plusieurs tools vident la pile d'un coup : il n'y a rien vers quoi revenir.

**Toujours un résultat.** Un batch démarré renvoie un résultat plutôt qu'une erreur RPC :

```json
{
  "ok": false,
  "executed": 1,
  "stopped_at": 1,
  "error": {"step": 1, "method": "studio_set_camera", "code": -32602, "message": "..."},
  "steps": [{"step": 0, "method": "studio_create_object", "result": {"...": "..."}}]
}
```

| Champ | Signification |
|---|---|
| `ok` | Toutes les étapes ont tourné. |
| `executed` | Combien d'étapes ont tourné ; en cas d'échec, c'est aussi l'index de l'étape en échec. |
| `stopped_at` | Index de l'étape en échec, `null` quand `ok`. |
| `error` | `null` quand `ok`, sinon `{step, method, code, message}` avec les mêmes codes qu'un appel direct. |
| `steps` | Le `result` de chaque étape, exactement tel que le tool l'aurait renvoyé seul. |

**Le batch économise des tours, pas des millisecondes.** Un appel coûte environ une milliseconde de loopback plus une frame, donc un batch vaut le coup quand il supprime des allers-retours du LLM : une scène à construire, un membre à poser, des accessoires à créer et placer. Préférez les tools multi-entrées (`studio_set_character_bone`, `studio_set_character_ik`) à un batch d'écritures unitaires : ils s'appliquent en une seule étape d'annulation, un batch non.

La boucle du batch tourne sur le thread de travail HTTP, pas sur le thread principal. Chaque étape se met elle-même en file ; faire tourner la boucle dans la file du thread principal le ferait attendre une file que lui seul peut vider.
