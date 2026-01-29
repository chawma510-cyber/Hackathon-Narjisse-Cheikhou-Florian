# Hackathon VR : Restauration & Narrative Update
*Janvier 2026*

---

## 1. Objectifs de la Session
- **Restaurer** les scripts et scènes perdus (notamment Scène 3).
- **Améliorer** les contrôles VR (Déplacement/Rotation).
- **Implémenter** la narration de la Scène 1 (Intro).

---

## 2. Restauration du Projet
### Scripts & Systèmes
- **Récupération** des scripts `FlashlightController`, `BeeChase`, `HideSpot`.
- **Nettoyage** du `VRTeleporter` (cause de crashs) au profit d'une locomotion fluide.

### Scène 3 : "Attaque des Abeilles"
- **Problème** : La scène sur le `main` était vide/corrompue.
- **Solution** : Extraction de `attaque abeille.unity` depuis la branche `Florian`.
- **Résultat** : La Scène 3 est maintenant complète et fonctionnelle sur le `main`.

---

## 3. Nouveaux Contrôles VR
Nous avons écouté les retours pour standardiser les contrôles :

- **Joystick GAUCHE** : Déplacement Fluide (Marche / Pas de côté).
- **Joystick DROIT** : Rotation par à-coups (Snap Turn).
- **Main GAUCHE** : Lampe Torche (Bouton Y/B).
- **Main DROITE** : Interaction Dialogue (Bouton A).

---

## 4. Scène 1 : Narration Implémentée
Une séquence narrative complète a été codée :

1. **Dialogue d'Intro** : Se lance automatiquement.
2. **Le Livre Mystérieux** :
   - Apparaît en lévitation une fois le dialogue fini.
   - **Interaction** : "Lire (A)" affiche l'histoire du grand-père.
   - **Fermeture** : Déclenche l'événement suivant.
3. **L'Incident Narjisse** :
   - Elle vous invite à regarder le télescope.
   - Elle regarde, crie et disparaît (Mise en scène).

---

## 5. Transition & Climax
- **Le Télescope** : Devenu un objet interactif.
- **L'Action** : Le joueur s'approche pour regarder.
- **La Conséquence** :
   - Flash Blanc (Canvas UI).
   - Son "Flash/Choc".
   - **Transition Automatique** vers la Scène 2.

---

## 6. Architecture Technique
- **`SceneDecorator.cs`** : Outil Editor pour configurer les scènes et spawner les objets (Livre, Télescope) automatiquement.
- **`StoryManager.cs`** : Chef d'orchestre qui gère la chronologie des événements (Dialogue -> Livre -> Télescope -> Fin).
- **`VRLocomotion.cs`** : Script centralisé pour la gestion des déplacements sans conflit.

---

## Conclusion
Le projet est **stable**, **à jour sur le main**, et la **Scène 1** offre maintenant une véritable introduction narrative jouable.

*Prêt pour la suite du Hackathon !*
