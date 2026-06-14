# STRANDED - Projet Unity

STRANDED est un jeu d'aventure et de survie développé avec Unity 6. Le joueur commence dans un labyrinthe, s'échappe vers une île, récupère le carburant dans la tour, collecte les pièces du bateau autour de l'île, résout un puzzle de coloration de graphes, puis répare le bateau pour quitter l'île.

## Scènes Principales

Les scènes importantes du jeu sont :

- `Assets/Scenes/TapToPlay.unity` - écran de démarrage.
- `Assets/Scenes/level1_maze.unity` - niveau du labyrinthe.
- `Assets/Scenes/youescaped.unity` - écran de transition après le labyrinthe.
- `Assets/Island_V2.unity` - scène principale de l'île.
- `Assets/tower/Assets/Scenes/PipePuzzle.unity` - puzzle de tuyaux de la tour, chargé en mode additif.
- `Assets/tower/Assets/Towers/safetowers.unity` - scène/assets originaux de la tour.

La majorité du gameplay se trouve dans `Island_V2` et dans les assets/scènes de la partie `tower`.

## Configuration Requise Avant de Lancer le Jeu

### 1. Installer glTFast pour les assets Sketchfab/GLB

Certains objets 3D préconstruits, notamment les modèles de type Sketchfab en `.glb` ou `.gltf`, nécessitent le package glTFast.

Dans Unity :

1. Ouvrir `Window > Package Manager`.
2. Cliquer sur le bouton `+`.
3. Choisir `Install package by name...`.
4. Entrer le nom suivant :

```text
com.unity.cloud.gltfast
```

5. Cliquer sur `Install`.

### 2. Activer les deux systèmes d'input

Le projet peut utiliser à la fois l'ancien Input Manager et le nouveau Input System de Unity.

Dans Unity :

1. Ouvrir `Edit > Project Settings`.
2. Aller dans `Player`.
3. Ouvrir la section `Other Settings`.
4. Chercher `Active Input Handling`.
5. Choisir :

```text
Both
```

6. Si Unity demande un redémarrage, cliquer sur `Apply` ou redémarrer l'éditeur.

### 3. Ajouter les scènes dans Build Profiles

L'ordre des scènes est important. Le jeu commence par `TapToPlay`, passe ensuite au labyrinthe, puis à l'écran de transition, puis à l'île. Le puzzle de la tour doit aussi être ajouté, car il est chargé en mode additif depuis `Island_V2`.

Dans Unity 6 :

1. Ouvrir `File > Build Profiles`.
2. Sélectionner la plateforme, par exemple `Windows`.
3. Ouvrir la `Scene List`.
4. Ajouter les scènes dans cet ordre :

```text
0 - Assets/Scenes/TapToPlay.unity
1 - Assets/Scenes/level1_maze.unity
2 - Assets/Scenes/youescaped.unity
3 - Assets/Island_V2.unity
4 - Assets/tower/Assets/Scenes/PipePuzzle.unity
```

Si une version du projet saute l'écran `youescaped`, l'ordre principal devient :

```text
TapToPlay -> level1_maze -> Island_V2
```

Mais pour le fonctionnement actuel du projet, il faut garder `youescaped` dans la liste des scènes.

## Comment Lancer le Jeu

Ouvrir d'abord cette scène :

```text
Assets/Scenes/TapToPlay.unity
```

Ensuite, cliquer sur `Play` dans Unity.

## Notes Importantes

- Ne pas lancer directement `PipePuzzle`, car cette scène est un puzzle chargé en additif depuis `Island_V2`.
- Si l'interaction avec la tour ne fonctionne pas, vérifier que `PipePuzzle.unity` est bien ajoutée dans `Build Profiles > Scene List`.
- Si les objets Sketchfab/GLB sont absents ou cassés, installer `com.unity.cloud.gltfast`.
- Si les contrôles du joueur ne répondent pas correctement, mettre `Active Input Handling` sur `Both`.
- Les scènes principales à vérifier sont surtout `Island_V2` et les scènes/assets de la partie `tower`.
