# Tools — les 56 tools MCP

Chaque tool, par groupe : ce qu'il prend, ce qu'il fait, et quel tool de lecture le confirme.

Les contrats d'entrée et de sortie vivent dans [`shared/schemas/`](../../shared/schemas/), un fichier JSON par tool (`description`, `input`, `output`). Le serveur MCP les importe directement et le plugin les implémente. Les paramètres requis sont en **gras**.

## Conventions

| Règle | Signification |
|---|---|
| Valeurs absolues | Les écritures prennent des valeurs absolues, jamais des deltas. |
| Patch partiel | Seuls les champs présents sont appliqués ; un champ omis garde sa valeur. Rejouer un appel est idempotent. |
| Ids | Les `id` d'objet et de personnage sont des entiers lus dans `studio_list_objects` / `studio_list_characters`. Les ids d'assets viennent de `studio_search_catalog` ; les chemins de fichiers des tools `studio_list_*`. |
| Lire avant d'écrire | Plusieurs tools refusent des valeurs que les tools de lecture n'annoncent pas (`available`, `states`, `weighted`, ...). Lisez d'abord. |
| Relecture | Chaque écriture a un tool de lecture qui montre ce que Studio contient maintenant ; il est nommé dans les notes. |
| Erreurs | Codes JSON-RPC, voir [protocol.md](./protocol.md). Un batch ne lève jamais d'erreur, voir [Batch](#batch). |
| Échelle de scène | Les scènes Studio ne sont pas toujours à 1 unité = 1 m. Lisez le `transform` d'un objet avant de choisir des distances de caméra ou des portées de lumière. |

## Lecture

| Tool | Params | Notes |
|---|---|---|
| `studio_get_info` | aucun | Infos runtime : versions du produit, d'Unity, du jeu, du plugin et de BepInEx, scène active, `studio_loaded`, nombre d'objets, `plugins`. Fonctionne avant que la scène soit prête. `plugins.timeline` et `plugins.nodes_constraints` disent si ces tools fonctionneront. |
| `studio_get_scene` | aucun | Nombre d'objets par type, fond/cadre, la map, la lumière de scène et tout l'état du post-processing. Erreur `-32000` tant que Studio n'est pas prêt. Relecture des quatre tools d'écriture de l'environnement. |
| `studio_list_characters` | aucun | Tous les personnages : id, nom, nom complet de la carte, sexe, visible, transform, anime, kinematic, visage, mains, expression. Relecture de toutes les écritures de personnage. |
| `studio_list_objects` | aucun | Tous les objets de la scène : id, type, nom, visible, `parent_id` (renseigné pour les objets attachés), transform, état de la lumière. Relecture des tools du workspace. |
| `studio_get_camera` | aucun | Caméra de la vue : position de la cible, angle, distance, fov, position et rotation monde. Les quatre premiers se renvoient tels quels dans `studio_set_camera`. |
| `studio_screenshot` | `format` `jpg`\|`png` (défaut `jpg`), `quality` 1–100 (défaut 85) | Capture de la fenêtre Studio, interface comprise, renvoyée comme contenu image MCP. |
| `studio_search_catalog` | `catalog` `item`\|`light`\|`anime`\|`map`\|`color_grading`\|`reflection_probe`\|`sky` (défaut `item`), `query`, `limit` 1–500 (défaut 50) | **Requis** avant de créer quoi que ce soit : les ids ne se devinent pas. `query` cherche dans le nom et, pour les items et animations, dans les noms de groupe et de catégorie. |
| `studio_list_scenes` | `query`, `limit` 1–1000 (défaut 100) | Scènes sauvegardées sous `UserData/Studio/scene`. Les chemins alimentent `studio_load_scene`. |
| `studio_list_character_cards` | `sex` `female`\|`male`, `query`, `limit` 1–1000 (défaut 100) | Cartes sous `UserData/chara/{female,male}`. Les chemins alimentent `studio_create_character`. Le premier appel lit toutes les cartes, puis le résultat est mis en cache. |
| `studio_list_poses` | `sex`, `query`, `limit` 1–1000 (défaut 100) | Fichiers de pose sous `UserData/studio/pose`. Les chemins alimentent `studio_set_character_pose`. Chaque pose est propre à un sexe. |
| `studio_list_coordinates` | `sex`, `query`, `limit` ≥ 1 (défaut 100) | Fichiers de tenue sous `UserData/coordinate/{female,male}`. Les chemins alimentent `studio_set_character_outfit`. Lecture de l'en-tête seul : le balayage d'environ 2000 fichiers prend moins d'une seconde. |
| `studio_list_outfit` | **`id`** | Les huit emplacements de vêtements d'un personnage (pièce portée, état, états acceptés, couplage) et ses vingt emplacements d'accessoires. **Requis** avant `studio_set_character_outfit` : les états qu'un emplacement accepte dépendent du vêtement. |
| `studio_list_attach_points` | **`id`** | Les 54 ancres d'os d'un personnage : index, nom, `available`, ids déjà attachés. **Requis** avant `studio_attach_object`. |
| `studio_list_bones` | **`id`** | Le squelette posable d'un personnage : chaque os FK (nom, groupe, `weighted`, rotation) et les 13 cibles IK (nom, groupe, `rotatable`, position/rotation). **Requis** avant de poser : l'ensemble des os dépend du rig, des cheveux et des vêtements. |
| `studio_list_overlays` | `kind` `background`\|`frame`, `query`, `limit` 1–500 (défaut 100) | Les images 2D de fond et de cadre sur disque. Les chemins alimentent `studio_set_overlay`. |

## Scène et objets

| Tool | Params | Notes |
|---|---|---|
| `studio_load_scene` | **`path`** | Charge une scène sauvegardée en remplaçant le workspace. Destructif et non annulable. Attend la fin de la coroutine de la map et reflète lui-même la lumière de scène, pour que la vue corresponde à la scène qui vient d'être lue. |
| `studio_save_scene` | `name`, `overwrite` | Sauvegarde le workspace sous `UserData/Studio/scene`, miniature comprise. Le seul moyen de persister le travail d'un agent. Refuse de remplacer un fichier sans `overwrite`. `name` peut être un chemin relatif avec des `/` (`agent/run17`) ; omis, le nom horodaté de Studio est utilisé. Renvoie `relative_path`, la valeur à passer à `studio_load_scene`. |
| `studio_set_camera` | `target_pos`, `angle`, `distance`, `fov` (1–160) | Déplace la caméra de la vue. La caméra orbite autour d'un point cible. Partiel et absolu ; relit après une frame pour que `world_pos` soit à jour. |
| `studio_create_object` | **`group`**, **`category`**, **`no`**, `pos`, `rot`, `scale` | Crée un item à partir du triplet du catalogue. Annulable dans Studio. |
| `studio_create_light` | **`no`**, `pos`, `rot`, `color`, `intensity`, `range`, `spot_angle` (0–180), `enabled`, `shadow` | Ajoute une lumière à partir de son `no` du catalogue. Annulable dans Studio. |
| `studio_set_light` | **`id`**, `color`, `intensity`, `range`, `spot_angle`, `enabled`, `shadow` | Modifie une lumière existante. Partiel et absolu. |
| `studio_set_object_transform` | **`id`**, `pos`, `rot`, `scale` | Déplace, tourne ou met à l'échelle n'importe quel objet. Idempotent ; annulable dans Studio. |
| `studio_delete_object` | **`id`** | Supprime un objet et ses enfants. **Non** annulable, et vide la pile d'annulation de Studio. |
| `studio_attach_object` | **`id`**, `parent_id`, `attach_point` (index ou nom) | Attache un objet à un os de personnage, un item ou un dossier, ou le détache (omettre `parent_id`). **Non** annulable et vide la pile d'annulation. |

**Sauvegarder ne perturbe pas la scène vivante.** `studio_save_scene` est le seul tool d'écriture qui ne change rien dans le workspace : aucun objet n'est touché, la pile d'annulation reste intacte et la vue ne bouge pas. Sa miniature est rendue depuis les caméras telles qu'elles sont au moment de la sauvegarde, donc tout ce qui est visible dans `studio_screenshot`, gizmos de sélection et poignées IK/FK compris, se retrouve dans l'aperçu. Le tool attend d'abord la fin de la frame, si bien qu'une sauvegarde lancée juste après `studio_set_camera` ou `studio_create_character` capture bien le nouvel état.

**Attacher change le sens d'un transform.** Tant qu'un objet est attaché, son `transform`, aussi bien ce que rapporte `studio_list_objects` que ce qu'écrit `studio_set_object_transform`, est dans l'espace local du parent, pas dans l'espace monde. `pos: [0,0,0]` place l'objet exactement sur l'os. Détacher reconvertit vers l'espace monde. La conversion se fait sur les coordonnées stockées de l'objet : tout transform encore présent sur la pile d'annulation de Studio serait rejoué dans le mauvais espace, c'est pourquoi `studio_attach_object` vide la pile.

Les points d'attache sont les mêmes 54 ancres pour tous les personnages, mais l'os derrière chacune dépend du rig : un point inutilisable ressemble à un point utilisable jusqu'à ce que l'objet finisse silencieusement à la racine de la scène. `studio_list_attach_points` indique `available` par point et `studio_attach_object` refuse les autres. Un point se donne par son index ou son nom (`44` ou `"N_Hand_L"`). Les objets peuvent aussi être rattachés à des items et des dossiers, sans point d'attache.

## Workspace

L'arbre du workspace est modifiable, dans les limites de Studio. `studio_list_objects` est la relecture des quatre.

| Tool | Params | Notes |
|---|---|---|
| `studio_set_object_visible` | **`id`**, **`visible`** | Affiche ou masque un objet et sa branche. Refusé sur les lumières et les points de route. Non annulable mais laisse la pile d'annulation intacte. |
| `studio_rename_object` | **`id`**, **`name`** | Renomme un dossier, une caméra de la scène ou une route. Refusé sur tous les autres types : leur libellé n'est pas sauvegardé. |
| `studio_create_folder` | `name`, `pos`, `rot`, `scale` | Ajoute un dossier vide pour regrouper des objets. **Non** annulable, et vide la pile d'annulation, exactement comme le bouton de Studio. |
| `studio_duplicate_object` | **`id`** (entier ou tableau) | Copie un ou plusieurs objets avec tout ce qu'ils contiennent. Renvoie chaque copie avec le `source_id` d'où elle vient. **Non** annulable. |

Masquer agit par branche : masquer un parent masque tout ce qu'il contient, tandis que chaque enfant garde son propre drapeau, donc ré-afficher le parent restaure la branche telle qu'elle était. Le drapeau ne persiste que parce que chaque chemin de sauvegarde le resynchronise d'abord depuis le nœud de l'arbre ; la valeur stockée sur l'objet lui-même est périmée entre deux sauvegardes, c'est pourquoi `visible` est lu depuis le nœud. Les lumières sont refusées plutôt qu'ignorées en silence : Studio ne donne jamais de bascule de visibilité à une lumière et son handler est vide, le drapeau serait stocké et inerte (utilisez `enabled` dans `studio_set_light`). Les points de route sont refusés pour la même raison, la leur suit leur route.

Renommer est plus étroit : seuls les dossiers, les caméras de la scène et les routes stockent un nom qui leur est propre dans le fichier de sauvegarde. Tous les autres types prennent leur libellé de workspace depuis leur carte ou depuis le catalogue d'assets au chargement, donc l'écrire aurait l'air correct jusqu'au prochain rechargement.

La duplication passe par le sérialiseur de scène de Studio. Une copie emporte la pose, la tenue, les couleurs **et** les données de plugins tiers de sa source, atterrit sous le même parent, et revient appariée à son `source_id` : Studio alloue de nouveaux ids, rien d'autre ne relie les deux. Les données de plugin restaurées (les contraintes de nœuds en particulier) arrivent quelques frames plus tard, donc relisez-les plutôt que de les interroger dans le même batch.

## Personnages

| Tool | Params | Notes |
|---|---|---|
| `studio_create_character` | **`card`**, `sex` `female`\|`male`, `pos`, `rot`, `scale` | Ajoute un personnage depuis un chemin de carte. **Non** annulable, et vide la pile d'annulation, comme le menu Add de Studio. |
| `studio_set_character_pose` | **`id`**, **`path`** | Applique un fichier de pose : anime, toutes les cibles IK/FK et les drapeaux d'expression. Sexe vérifié par rapport au personnage. |
| `studio_set_character_anime` | **`id`**, `group`, `category`, `no`, `normalized_time`, `speed` (0–3), `pattern` (0–1), `option_param1`, `option_param2` (0–1) | Animation de base `(group, category, no)` et/ou réglages de playback. Ids depuis `studio_search_catalog` avec `catalog: "anime"`. Les champs de playback sont annulables. |
| `studio_set_character_expression` | **`id`**, `eyebrows`, `eyes`, `eyes_open`, `blink`, `mouth`, `mouth_open`, `lip_sync`, `tears`, `blush`, `look_eyes`, `look_neck`, `expression_categories` | Patterns du visage, ouvertures, clignement, larmes, rougeur, regard, catégories d'expression. Partiel et absolu. |
| `studio_set_character_hands` | **`id`**, `left`, `right` | Formes des mains, chaque côté indépendamment. `0` désactive la forme, `1..hands.count` en choisit une. |
| `studio_set_character_outfit` | **`id`**, `coordinate`, `clothes_all` `worn`\|`half`\|`off`, `clothes`, `accessories` | Charge une tenue, habille ou déshabille les huit emplacements, affiche ou masque les accessoires. Partiel et absolu. |
| `studio_set_character_state` | **`id`**, `skin_gloss`, `wet`, `simple`, `simple_color`, `son`, `son_length`, `nipple`, `siru` | Le reste du panneau d'état de Studio. Relu depuis `state` dans `studio_list_characters`. |
| `studio_set_character_kinematics` | **`id`**, `mode` `fk`\|`ik`\|`none`, `fk`, `ik`, `reflect` `fk`\|`ik`\|`both` | Change de mode de pose, active des groupes d'os, et `reflect` la pose en cours dans les valeurs de changement. Conditionne les deux tools suivants. Non annulable mais laisse la pile intacte. |
| `studio_set_character_bone` | **`id`**, **`bones`** | Fait tourner des os FK, plusieurs à la fois, en degrés d'Euler absolus. Un appel = une étape d'annulation. |
| `studio_set_character_ik` | **`id`**, **`targets`** | Déplace, et sur les mains et pieds fait tourner, des cibles IK, plusieurs à la fois, en offsets absolus. Un appel = une étape d'annulation. |

`studio_list_characters` est la relecture de tous ces tools.

**Pose contre anime.** Ce sont deux concepts distincts de Studio. Une *anime* est un clip du catalogue (`OCIChar.LoadAnime`) et ne règle que l'animation. Un *fichier de pose* (`PauseCtrl`, `UserData/studio/pose/*.dat`) restaure en plus chaque cible d'os IK et FK et les drapeaux de catégories d'expression, et ne s'applique qu'à un personnage du sexe pour lequel il a été sauvegardé. Le catalogue d'anime lui-même n'est **pas** filtré par sexe ; Studio ne le filtre pas non plus.

**Playback de l'anime.** `speed`, `pattern` et `option_param1|2` ajustent le clip déjà chargé et peuvent être envoyés sans `(group, category, no)`. `pattern` et les paramètres d'option n'atteignent l'animator que pour les clips qui les déclarent : `anime.is_anime_motion` et `anime.is_h_anime` indiquent si c'est le cas, sinon la valeur est stockée mais ne change rien. Les plages sont les bornes du panneau de Studio, pour qu'une scène sauvegardée reste reproductible depuis l'interface.

**Les mains se règlent séparément du reste.** Ni un fichier de pose ni un clip ne touche aux formes de mains, donc `studio_set_character_hands` est le seul moyen de les changer et son effet survit à une pose ou une anime ultérieure. Les formes n'ont de nom nulle part dans Studio, son panneau affiche un simple numéro, donc le tool prend un index et le moyen d'identifier une forme est `studio_screenshot`. `0` veut dire « pas de forme », c'est-à-dire que l'animation pilote les doigts ; `1..hands.count` en sélectionne une, `hands.count` venant de `studio_list_characters` (il dépend des mods installés, et vaut `-1` tant que la liste des formes charge encore).

**Les patterns d'yeux et de sourcils sont des positions de liste, pas des ids d'assets.** Le panneau de visage de Studio affiche l'index dans la liste de patterns du personnage propre à son sexe et le passe par le tableau de clés, parce que les mods ajoutent des ids non contigus. `studio_set_character_expression` utilise ce même espace d'index ; lisez `face.eyebrows_count` et `face.eyes_count` dans `studio_list_characters` pour la plage valide.

### Tenues

**L'habillage se fait par emplacement, et Studio ne dit jamais non.** `studio_set_character_outfit` couvre les trois choses que font les onglets costume et état de Studio : charger une coordinate, régler comment chacun des huit emplacements est porté (`worn`, `half`, `off`) et basculer les vingt emplacements d'accessoires. L'ordre dans un même appel est fixe (`coordinate`, puis `clothes_all`, puis `clothes`, puis `accessories`) parce qu'une coordinate remplace à la fois les vêtements et les accessoires et change donc le sens du reste du patch.

Les états qu'un emplacement accepte dépendent du vêtement porté. `ChaControl.SetClothesState` parcourt `(state + 1) % 3` jusqu'à en trouver un que l'emplacement accepte et applique *celui-là* sans rien dire ; un état non pris en charge est donc refusé ici. `states` dans `studio_list_outfit` fait foi, et une liste vide signifie que l'emplacement n'a aucun état d'habillage. Même chose pour un emplacement d'accessoire vide : le drapeau serait stocké et rien n'apparaîtrait. `clothes_all` est l'exception et n'est volontairement pas validé, puisque ce sont les trois boutons d'habillage de Studio, qui retombent emplacement par emplacement exactement comme eux.

**Un vêtement peut occuper deux emplacements.** Un haut intégral laisse `bot` vide et une robe d'une pièce laisse `inner_b` vide ; dans cet état Studio reflète `off` entre les deux, donc écrire un emplacement déplace l'autre. `linked_to` dans `studio_list_outfit` nomme le voisin, et les tools de lecture renvoient tous les emplacements, pas seulement ceux demandés. Deux autres conséquences d'un changement de tenue : les os FK de la jupe sont repondérés contre le nouveau vêtement (relisez `studio_list_bones` ; avec une jupe, 6 des 48 sont pondérés ici, avec un short aucun), et les états d'habillage ne sont *pas* réinitialisés, donc un emplacement laissé `off` par la tenue précédente reste `off` sous la nouvelle sauf si le même appel passe `clothes_all`.

**Le panneau d'état est coupé en deux.** L'onglet d'état par personnage de Studio contient les curseurs du visage, larmes et rougeur, qui relèvent de `studio_set_character_expression`, à côté de ceux du corps, qui sont `studio_set_character_state` : brillance de la peau, humidité, la silhouette plate d'une seule couleur et sa couleur, la partie masculine et sa longueur, plus `nipple` et `siru`, refusés sur un homme parce que leurs applicateurs n'y font rien. Les niveaux de `siru` sont plafonnés à 2 pour une raison qui n'a rien de cosmétique : le niveau indexe une table de trois entrées dans la boucle de mise à jour de Studio à la frame suivante, où une valeur hors plage fait tomber le processus au lieu de renvoyer une erreur.

### Poses

**D'abord le mode, ensuite les os.** FK et IK sont des chaînes mutuellement exclusives et toutes deux désactivées sur un personnage neuf : l'animation pilote le squelette. `studio_set_character_bone` et `studio_set_character_ik` refusent d'écrire tant que leur chaîne est désactivée, parce que Studio stockerait la valeur sans jamais l'appliquer : `ActiveFK` n'atteint les os que si `enableFK` est déjà vrai, et `FKCtrl` n'écrit un os que si `enable & boneWeight`. Le même non-effet silencieux existe un niveau plus bas, par groupe et par os, donc les deux tools refusent aussi un os dont le groupe FK est désactivé, une cible IK dont le groupe est désactivé, et un os non pondéré (l'état normal des 48 os de jupe quand aucune jupe n'est portée : `weighted: false` dans `studio_list_bones`). Les drapeaux de groupe sont stockés quel que soit le mode, donc préconfigurer des groupes `fk` en mode IK est permis et simplement invisible jusqu'au changement.

```json
{"id": 4, "mode": "fk", "reflect": "fk"}
```

Les noms d'os sont les noms de transform propres au rig (`cf_J_ArmUp00_L`, `cf_J_Hand_Thumb01_R`) et sont uniques par personnage ; le champ `label` est la légende japonaise de Studio et est partagé par plusieurs os, il n'est donc que décoratif. Le groupe FK `body` couvre tout le squelette sauf les parties listées à part : bras, jambes et colonne sont tous des os de corps, parce que Studio stocke le groupe d'un os comme un masque où chaque membre porte aussi le bit `Body`. Les os ne font que tourner (Studio ne donne à leurs guides ni poignée de position ni d'échelle) ; les cibles IK se déplacent, et seules les quatre cibles de mains et de pieds tournent aussi. Les deux tools prennent une map de plusieurs entrées et poussent un seul `GuideCommand` pour le lot, donc la modification d'un agent est une seule étape d'annulation dans l'interface de Studio.

**`reflect` capture la pose que le personnage montre réellement.** Activer FK ou IK cale le squelette sur les valeurs de changement stockées en dernier, qui après un changement d'animation sont périmées. `reflect: "fk" | "ik" | "both"` copie le squelette vivant dans ces valeurs une frame après l'appel et **avant** l'application de `mode`. Cet ordre est tout l'intérêt : l'appel ci-dessus fige l'animation en cours au lieu de revenir à une ancienne pose. L'attente est nécessaire parce que les valeurs ne deviennent correctes qu'une fois que l'Animator a écrit la frame, c'est pourquoi Studio diffère ses propres boutons reflect d'un `LateUpdate`. Capturer dans une chaîne déjà activée ne fait rien, puisque cette chaîne est ce qui pilote les os. La capture FK couvre le corps, le cou et les deux mains (cheveux, poitrine et jupe sont pilotés par des dynamic bones) ; la capture IK couvre les cinq groupes. La capture elle-même n'est pas annulable.

## Environnement

`studio_get_scene` est la relecture des quatre.

| Tool | Params | Notes |
|---|---|---|
| `studio_set_map` | `no`, `pos`, `rot`, `option`, `light` | Choisit le décor 3D, le déplace, bascule ses objets optionnels (`option`) et ses propres lumières (`light`). Un chargement de scène complet : **non** annulable, et vide la pile d'annulation quand `no` est fourni. |
| `studio_set_scene_lighting` | `color`, `intensity` (0.1–2), `rot`, `shadow` | La lumière principale des personnages de la scène. Partiel et absolu ; les plages sont les bornes de Studio. |
| `studio_set_overlay` | `background`, `frame` | Les images 2D derrière et par-dessus la scène. `""` en efface une ; une image illisible l'efface aussi et signale une erreur. |
| `studio_set_post_processing` | `color_grading`, `ambient_occlusion`, `bloom`, `depth_of_field`, `vignette`, `screen_space_reflections`, `reflection_probe`, `fog`, `sun_shafts`, `self_shadow`, `environment_lighting`, `sky` | Les douze effets d'écran. Partiel et absolu par section ; non annulable. |

**Le décor, la lumière et l'étalonnage sont trois choses différentes.** `studio_set_map` charge le décor 3D dans lequel la scène est construite, une scène Unity entière, donc l'appel prend un moment et, comme Studio réinitialise le transform de la map à chaque changement de map sans commande d'annulation, il vide la pile d'annulation dès que `no` est fourni. Il réinitialise ce transform même quand la map demandée est celle déjà chargée, donc `pos` et `rot` sont appliqués une fois le chargement stabilisé plutôt qu'avant. `option` et `light` affichent les objets optionnels et les lumières propres à la map et sont refusés sur une map qui n'en a pas : `map.option_available` et `map.light_available` l'indiquent. `studio_set_overlay` règle des images 2D plates à la place, `background` derrière tout et `frame` par-dessus, et aucune ne remplace la map.

**Il y a une lumière de scène, pas deux.** `studio_set_scene_lighting` pilote la lumière des personnages que contrôle le panneau de lumière de Studio, distincte des objets `OCILight` que crée `studio_create_light`. Sa voisine `mapLight` existe dans le fichier de scène et est volontairement absente de l'API : rien dans Studio ne l'applique jamais. Charger une scène n'applique pas non plus la lumière des personnages, puisque Studio ne la reflète qu'à l'ouverture de son panneau, donc `studio_load_scene` la reflète lui-même.

**Le post-processing, c'est un appel pour douze effets.** Chaque section et chaque champ de `studio_set_post_processing` est optionnel et absolu, et tout l'état des effets est réappliqué en une passe ensuite, donc un champ rejeté signifie que rien n'a été écrit. Les plages numériques sont les bornes des curseurs du panneau, qui vivent dans le prefab d'interface de Studio et non dans son code : elles sont documentées par champ mais non appliquées, et l'état renvoyé est ce que la scène contient réellement. Trois champs sont des ids qui doivent exister, `color_grading.lookup_texture`, `reflection_probe.cubemap` et `sky.pattern`, tous issus de `studio_search_catalog` (`color_grading`, `reflection_probe`, `sky`). Deux sont des ids d'objets de la scène qui doivent être des **items** : `depth_of_field.focus_object_id` et `sun_shafts.caster_object_id` (`-1` pour aucun). Les setters de Studio acceptent n'importe quoi et retombent discrètement sur `-1`, ils sont donc refusés ici. Un champ n'est pas du tout un état de scène : `self_shadow` déplace d'un cran le niveau de qualité global et survit à la scène dans laquelle il a été réglé.

## Batch

| Tool | Params | Notes |
|---|---|---|
| `studio_execute_batch` | **`steps`** (1 à 32 `{method, params}`) | Exécute des tools dans l'ordre en un seul appel. Validé en entier avant que rien ne s'exécute, mais **pas de rollback** une fois lancé. |

Le batch économise des tours, pas des millisecondes. Deux tools groupent déjà en interne et sont à préférer à un batch d'écritures unitaires : `studio_set_character_bone` et `studio_set_character_ik` s'appliquent en une seule étape d'annulation, un batch d'appels individuels non. Règles, forme du résultat et comportement en cas d'échec : [protocol.md](./protocol.md#execute_batch).

## Plugins optionnels

Treize tools font le pont avec des plugins tiers. Ils sont toujours enregistrés ; le détail, les workflows et le modèle d'échec sont dans [optional-plugins.md](./optional-plugins.md).

| Tool | Params | Notes |
|---|---|---|
| `studio_timeline_get_state` | `object_id`, `include_keyframes` (défaut `true`) | Le transport plus chaque interpolable et ses keyframes. `available` dit si Timeline est présent. |
| `studio_timeline_playback` | **`action`** `play`\|`pause`\|`stop`\|`seek`\|`next_frame`\|`previous_frame`, `time` | Pilote le transport. |
| `studio_timeline_set_settings` | `duration`, `frame_rate` | Durée de Timeline et fréquence d'images utilisée par les pas. |
| `studio_timeline_list_models` | `object_id`, `query`, `limit` (défaut 100) | Les propriétés animables qu'un objet donné accepte. |
| `studio_timeline_add_interpolable` | **`object_id`**, **`model`**, `owner`, `name`, `target`, `alias` | Lie une propriété à un objet, en créant la piste. |
| `studio_timeline_remove_interpolable` | **`id`** | Supprime une ou plusieurs pistes, keyframes comprises. |
| `studio_timeline_set_keyframe` | **`interpolable_id`**, **`time`**, `value`, `curve` | Pose ou écrase une keyframe. |
| `studio_timeline_delete_keyframe` | **`interpolable_id`**, **`time`** | Retire une keyframe à son temps exact. |
| `studio_timeline_single_file` | **`action`** `list`\|`load`\|`save`\|`delete`, `name`, `object_id`, `overwrite` | L'animation d'un objet comme fichier réutilisable. |
| `studio_constraint_list` | `object_id` | Chaque contrainte de la scène, entièrement développée. `available` dit si NodesConstraints est présent. |
| `studio_constraint_create` | **`parent`**, **`child`** (`{object_id, target}`), plus des options par canal | Fait suivre un nœud par un autre. |
| `studio_constraint_set` | **`id`**, plus les mêmes options | Modifie une contrainte. Les deux nœuds ne peuvent pas être changés. |
| `studio_constraint_delete` | **`id`** (entier ou tableau) | Supprime une ou plusieurs contraintes en restaurant leurs enfants. |

## Comportement d'annulation

| Comportement | Tools |
|---|---|
| Annulable depuis Studio | `studio_create_object`, `studio_create_light`, `studio_set_object_transform`, les champs de playback de `studio_set_character_anime` ; `studio_set_character_bone` et `studio_set_character_ik` à raison d'une étape par appel |
| Non annulable, et **vide** la pile d'annulation | `studio_delete_object`, `studio_create_folder`, `studio_create_character`, `studio_attach_object`, `studio_set_map` (quand `no` est fourni) |
| Non annulable, pile intacte | `studio_set_object_visible`, `studio_rename_object`, `studio_set_character_kinematics`, `studio_save_scene`, et le reste des écritures de personnage (pose, chargement de clip, visage, mains), pour lesquelles Studio n'a pas de commande |
| Historique propre | Les tools Timeline et NodesConstraints ne touchent jamais l'`UndoRedoManager` de Studio |

La suppression et la création de personnage ne sont pas annulables parce que Studio NEO V2 n'a ni commande de suppression ni commande d'ajout de personnage, et vide lui-même la pile pour cette dernière.

## Limites connues

- Pas de routes ni de caméras dans la scène.
- Pas de son : les voix et les réglages `bgmCtrl`, `envCtrl` et `outsideSoundCtrl` du fichier de scène ne sont pas couverts.
- Changer la pièce de vêtement d'un emplacement (`ChangeClothes`, `ChangeAccessory`) et ses couleurs est hors périmètre : c'est le travail du character maker et cela demande tout le catalogue de vêtements par sexe, que les panneaux côté Studio de Studio n'exposent pas non plus.
