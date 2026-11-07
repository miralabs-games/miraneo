# Plugins optionnels — Timeline et NodesConstraints

Deux plugins tiers de Studio sont reliés quand ils sont installés : Timeline (animation par keyframes, neuf tools) et NodesConstraints (quatre tools). Tout le reste fonctionne sans eux.

## Modèle d'échec

Les deux ponts suivent la même règle : **enregistrement inconditionnel, échec explicite.**

| Situation | Ce qui se passe |
|---|---|
| Plugin installé | Les tools fonctionnent. |
| Plugin absent | Chaque tool du groupe échoue avec `-32001` et la raison, sauf le tool de lecture du groupe, qui répond normalement avec `available: false`. |
| Le plugin a déplacé un membre | Le pont résout les membres du plugin par réflexion en une seule passe tout-ou-rien : une version qui a changé quelque chose se déclare indisponible au lieu de fonctionner à moitié. |

La liste des tools MCP est statique : un plugin absent doit répondre par une erreur lisible plutôt que par `unknown method`. Consultez d'abord `studio_get_info` : `plugins.timeline` et `plugins.nodes_constraints` contiennent la version du plugin, ou `null` quand il est absent.

Côté C#, les deux plugins sont déclarés comme dépendances souples de BepInEx : BepInEx les charge en premier quand ils sont installés ; les ponts se lient par réflexion dans les deux cas.

| Plugin | Version testée | Tool de lecture |
|---|---|---|
| Timeline (joan6694) | 1.4.1 | `studio_timeline_get_state` |
| NodesConstraints (joan6694) | 1.3.3 | `studio_constraint_list` |

Comment fonctionne le découpage par réflexion (membres publics contre privés, l'astuce de la sélection) : [internals/studio-api.md](./internals/studio-api.md).

## Timeline

| Tool | Params | Notes |
|---|---|---|
| `studio_timeline_get_state` | `object_id`, `include_keyframes` (défaut `true`) | Le transport plus chaque interpolable et ses keyframes. La relecture de tous les tools ci-dessous ; `available` dit si Timeline est présent. |
| `studio_timeline_playback` | **`action`** `play`\|`pause`\|`stop`\|`seek`\|`next_frame`\|`previous_frame`, `time` | `seek` applique la pose immédiatement, donc `seek` plus `studio_screenshot` rend un instant donné. |
| `studio_timeline_set_settings` | `duration`, `frame_rate` (≥ 1) | Durée de Timeline et fréquence d'images utilisée par les pas. Partiel et absolu. |
| `studio_timeline_list_models` | `object_id`, `query`, `limit` (défaut 100) | Les propriétés animables, filtrées sur ce qu'un objet donné accepte. **Requis** avant d'ajouter : 95 modèles sur un personnage ici, dont 38 seulement propres à Timeline. |
| `studio_timeline_add_interpolable` | **`object_id`**, **`model`**, `owner`, `name`, `target`, `alias` | Lie une propriété à un objet, en créant la piste. Idempotent : ré-ajouter renvoie la piste existante. |
| `studio_timeline_remove_interpolable` | **`id`** | Supprime une ou plusieurs pistes, keyframes comprises. Chaque id est validé avant que rien ne soit retiré. |
| `studio_timeline_set_keyframe` | **`interpolable_id`**, **`time`**, `value`, `curve` | Pose ou écrase une keyframe, avec une courbe d'interpolation. Omettez `value` pour capturer la scène telle qu'elle est. |
| `studio_timeline_delete_keyframe` | **`interpolable_id`**, **`time`** | Retire une keyframe à son temps exact. Les temps sont des flottants : relisez-les plutôt que de les recalculer. |
| `studio_timeline_single_file` | **`action`** `list`\|`load`\|`save`\|`delete`, `name`, `object_id`, `overwrite` | L'animation d'un objet comme fichier réutilisable. C'est ainsi qu'une animation passe d'un personnage à un autre. |

`play` est idempotent ici : il ne rebascule jamais en pause, contrairement au bouton de Timeline. `seek` demande un `time` entre 0 et la durée de la timeline ; `stop` rembobine à 0 et applique cette pose. Pour associer un seek à une capture d'écran, faites la capture dans son propre appel, pas dans le même batch, car la frame capturée est la précédente.

### Poser, puis enregistrer

Ajoutez l'interpolable, placez-vous à un temps, posez la scène avec les tools ordinaires (`studio_set_character_bone`, `studio_set_character_ik`, `studio_set_object_transform`, ...), puis appelez `studio_timeline_set_keyframe` **sans** `value`. La keyframe capture la propriété exactement telle qu'elle est, comme le fait le bouton de Timeline, et c'est la seule option pour les types de valeur sans forme JSON.

```json
{"steps": [
  {"method": "studio_timeline_add_interpolable", "params": {"object_id": 4, "model": "guideObjectRot", "target": "cf_J_ArmUp00_L"}},
  {"method": "studio_timeline_playback", "params": {"action": "seek", "time": 0}},
  {"method": "studio_set_character_bone", "params": {"id": 4, "bones": {"cf_J_ArmUp00_L": [0, 0, 0]}}},
  {"method": "studio_timeline_set_keyframe", "params": {"interpolable_id": 0, "time": 0}}
]}
```

Les ids et valeurs ci-dessus sont des exemples : lisez les vrais dans `studio_timeline_list_models`, `studio_list_bones` et le résultat de l'ajout. Passer `value` explicitement fonctionne pour les types ordinaires (nombres, booléens, `[x,y,z]`, degrés d'Euler, `[r,g,b,a]`) et est typé d'après la valeur actuelle de la propriété, donc une mauvaise forme est refusée plutôt que convertie.

`studio_execute_batch` est le bon véhicule : les paires poser/enregistrer s'alternent correctement dans un même batch, parce que chaque écriture atteint les valeurs de changement de façon synchrone et que la capture qui suit les voit.

### Animer un os demande toujours FK ou IK activé

`guideObjectPos`, `guideObjectRot` et `guideObjectScale` sont les modèles par cible, et ils écrivent les mêmes valeurs de changement que `studio_set_character_bone` et `studio_set_character_ik`. `target` prend `root` pour l'objet lui-même ou n'importe quel nom d'os ou de cible IK de `studio_list_bones` ; le pont résout le guide object depuis les `BoneInfo` et `IKInfo` de Studio, donc rien ne touche à la sélection de Studio. Mais `FKCtrl` n'applique un os que si sa chaîne est activée et que l'os est pondéré : cinématique désactivée, l'animation est stockée, relue correctement, et n'atteint jamais le mesh.

### Courbes

Les cinq préréglages de Timeline : `line`, `top` (rapide puis lent), `bottom` (lent puis rapide), `hermite` (adouci aux deux bouts) et `stairs` (une marche franche), avec les tangentes exactes qu'applique la fenêtre de keyframe de Timeline. Ou des points explicites `[time, value, in_tangent, out_tangent]`. Une courbe omise hérite de celle de la keyframe précédente, avec un repli sur linéaire, la même héritance qu'applique `AddKeyframe` de Timeline. `stairs` porte une tangente infinie, que JSON ne sait pas écrire, donc elle fait l'aller-retour par `null`.

### Ids qui partagent un nom

`charClothes` et `juice` enregistrent un modèle par emplacement sous un seul id, distingués seulement par leur libellé (`Bra State`, `Chest Juice State`). `studio_timeline_add_interpolable` prend `name` pour cela, et l'erreur liste les choix. `owner` départage des modèles que différents plugins publient sous le même id : HS2PE, Flow Control et PregnancyPlus publient chacun les leurs ici.

### Fichiers uniques et historique

Les fichiers uniques vivent hors de `UserData` : ils sont écrits à côté de la DLL de Timeline, et portent les pistes d'un objet plus, pour un personnage, le clip qu'il joue. Charger fusionne dans la timeline courante au lieu de la remplacer, et relie les pistes à l'objet donné. C'est exactement ainsi qu'une animation construite sur un personnage est rejouée sur un autre.

Timeline garde son propre historique. Aucun de ces tools ne touche à l'`UndoRedoManager` de Studio : une édition de timeline n'est donc pas annulable depuis l'interface de Studio et ne perturbe pas la pile d'annulation des modifications de scène autour d'elle.

## NodesConstraints

| Tool | Params | Notes |
|---|---|---|
| `studio_constraint_list` | `object_id` | Chaque contrainte de la scène, entièrement développée. La relecture des trois suivants ; `available` dit si NodesConstraints est présent. |
| `studio_constraint_create` | **`parent`**, **`child`**, `enabled`, `alias`, `position`, `rotation`, `scale`, `look_at`, `mirror_*`, `*_offset`, `*_factor`, `*_damp`, `*_locks`, `reset_*`, `fix_dynamic_bone` | Fait suivre un nœud par un autre. Les offsets omis sont capturés d'après la position actuelle des nœuds. |
| `studio_constraint_set` | **`id`**, les mêmes options | Modifie une contrainte. Partiel et absolu ; les deux nœuds ne peuvent pas être changés. |
| `studio_constraint_delete` | **`id`** (entier ou tableau) | Supprime une ou plusieurs contraintes en restaurant leurs enfants. Chaque id est validé avant que rien ne soit retiré. |

### Une contrainte n'est pas un attachement

`studio_attach_object` rattache dans l'arbre propre à Studio : un lien dur, un offset fixe, et le transform de l'objet passe dans l'espace du parent. Une contrainte ne rattache rien, l'arbre du workspace reste plat, et elle sait faire ce qu'un attachement ne peut pas :

- piloter position, rotation et échelle indépendamment, ou seulement certains axes ;
- refléter le mouvement du parent en miroir ;
- tourner l'enfant vers le parent (`look_at`) ;
- amplifier ou atténuer le mouvement du parent (`*_factor`) ou traîner derrière lui (`*_damp`) ;
- relier **deux nœuds de deux personnages différents**.

Les deux tools ne s'excluent pas : un objet attaché peut aussi être parent ou enfant d'une contrainte.

### Un nœud est `{object_id, target}`

`target` omis ou `"root"` désigne l'objet lui-même. Sinon c'est un nom d'os ou de cible IK de `studio_list_bones`, un nom d'ancre de `studio_list_attach_points`, ou n'importe quel nom de transform ou chemin avec des `/` sous la racine de l'objet : les contraintes atteignent des transforms qu'aucun autre tool n'expose. La résolution essaie ces cas dans cet ordre et un nom seul prend la première correspondance du sous-arbre, donc `path` dans la relecture est la forme non ambiguë et est accepté aussi comme `target`. Les deux nœuds font l'identité de la contrainte : `studio_constraint_set` refuse de recibler, supprimez puis recréez.

```json
{
  "parent": {"object_id": 4, "target": "N_Hand_L"},
  "child":  {"object_id": 9},
  "position_offset": [0, 0, 0]
}
```

### Créer une contrainte ne déplace rien

Chaque offset omis est capturé d'après la relation actuelle des nœuds, comme le calculent les boutons *Use current* de NodesConstraints, donc une création fige la pose et l'enfant ne bouge que lorsque le parent bouge. C'est l'unique écart volontaire avec les défauts du plugin, dont l'offset de position nul et l'offset de rotation identité plaqueraient l'enfant sur le parent. Envoyez `position_offset: [0,0,0]` quand c'est ce que vous voulez, par exemple pour mettre un accessoire dans une main (comme ci-dessus). L'offset de position est dans l'espace local du parent, l'offset de rotation s'applique après la rotation du parent, et l'offset d'échelle multiplie l'échelle monde du parent. `studio_constraint_set` ne recapture pas : lisez les valeurs actuelles et renvoyez-les.

### `*_locks` disent quels axes sont pilotés

`[true, true, true]`, la valeur par défaut, signifie que la contrainte possède les trois. `false` sur un axe laisse la valeur propre de l'enfant à cet endroit, ce qui donne « suivre à l'horizontale mais garder sa propre hauteur ». Le champ garde le nom de NodesConstraints pour qu'une scène sauvegardée se lise de la même façon.

### `enabled` n'est pas un simple drapeau

Désactiver une contrainte restaure l'enfant au transform capturé à la création de la contrainte (selon `reset_position`, `reset_rotation`, `reset_scale`) ; l'activer recapture le transform actuel de l'enfant comme nouvel état de repos. `studio_constraint_set` applique donc `enabled` après tous les autres champs du même appel, pour que la restauration suive la contrainte telle que cet appel la laisse. Supprimer restaure de la même façon.

### Une contrainte par paire

NodesConstraints parcourt sa liste avant d'ajouter et ne renvoie rien en silence sur un doublon : la paire est donc vérifiée ici d'abord, dans les deux sens, et l'erreur nomme l'id existant.

### La frame qui applique une contrainte dépend de Timeline

Avec Timeline installé, la passe tourne depuis un postfix Harmony sur son `Update` ; sans lui, depuis celui de NodesConstraints. Ce n'est pas la même passe : celle de base saute un canal dont le guide object est désactivé et ignore `fix_dynamic_bone`, celle de Timeline respecte `fix_dynamic_bone` et ignore les drapeaux de guide. `fix_dynamic_bone` n'a donc de sens que lorsque Timeline est là, ce qui est aussi la seule configuration testée ici.

### Les contraintes vivent dans le fichier de scène

`ExtendedSave` transpile `SceneInfo.Save`, `Load` et `Import`, ce par quoi passent exactement `studio_save_scene` et `studio_load_scene`, donc les contraintes font l'aller-retour avec la scène (vérifié en direct). Elles sont restaurées environ huit frames après le chargement, donc un `studio_constraint_list` émis juste après `studio_load_scene` peut encore revenir vide : relisez. Comme les tools Timeline, rien ici ne touche l'`UndoRedoManager` : les modifications de contraintes ne sont pas annulables depuis l'interface de Studio et laissent intacte la pile d'annulation de la scène.
