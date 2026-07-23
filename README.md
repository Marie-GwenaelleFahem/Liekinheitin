# Liekinheitin

Système de pilotage d'un spectacle son et lumière : conception d'animations lumineuses et diffusion en temps réel vers des contrôleurs DMX/ArtNet via le réseau.

Le nom *Liekinheitin* ("lance flammes" en finnois) sert d'identifiant technique à la solution.

## Vue d'ensemble

Le projet gère l'adressage et le pilotage d'un parc de plusieurs milliers d'entités lumineuses (LED RGB/RGBW individuellement adressables, lyres motorisées) réparties sur plusieurs contrôleurs physiques et univers DMX, via le protocole ArtNet (UDP).

Il se compose de deux applications complémentaires :

- **CreativeTool** — l'outil de création : conçoit et joue les animations (grille de pixels, timeline, couleurs, audio), puis diffuse l'état lumineux courant (~40 fois/seconde) au routeur.
- **RoutingHost** — le routeur / serveur d'exécution : reçoit l'état lumineux, le traduit en trames ArtNet selon le plan d'adressage (patch), les envoie aux contrôleurs, et supervise leur bon fonctionnement (santé réseau, logs, monitoring des univers).

Les deux applications communiquent entre elles en UDP (état lumineux, liste des entités, heartbeat de présence) et sont indépendantes l'une de l'autre : le CreativeTool ne connaît pas le réseau physique, seul le RoutingHost route vers les contrôleurs.

## Architecture

La solution suit une architecture en couches (clean architecture) :

```
Liekinheitin.Domain          Entités métier pures, sans dépendance
Liekinheitin.Application     Interfaces + logique métier (routage, patch)
Liekinheitin.Infrastructure   Implémentations concrètes (ArtNet, UDP, JSON, supervision)
Liekinheitin.CreativeTool     Application WPF de création (Domain + Application)
Liekinheitin.RoutingHost      Application WPF de routage (toutes les couches)
```

### Domain

Modèles de données purs : `Controller`, `Entity` (une LED/fixture et ses canaux), `PatchRange` / `PatchData` (plan d'adressage), `DmxFrame` (trame protocole-agnostique), `State` (photo de l'ensemble des entités à un instant donné).

### Application

- `PatchService` — charge et interroge le plan d'adressage (`patch.json`), retrouve l'adresse réseau d'une entité, construit l'état initial.
- `RoutingEngine` — cœur du routage : convertit chaque `State` reçu en trames DMX par contrôleur/univers et les transmet à l'envoyeur de paquets.
- Interfaces d'abstraction : `IPatchLoader`, `IPacketSender`, `IStatePublisher` / `IStateSource`, `IEntityListPublisher` / `IEntityListSource`.

### Infrastructure

- `JsonPatchLoader` — lecture/écriture du plan d'adressage (`Path.json`).
- `ArtNetSender` — construction et envoi des paquets ArtDMX en UDP (port 6454).
- `UdpStatePublisher` / `UdpStateReceiver` — diffusion de l'état lumineux en JSON sur UDP.
- `UdpEntityListPublisher` / `UdpEntityListReceiver` — diffusion de la liste des entités au démarrage / rechargement du patch.
- `HeartbeatService` — ping de présence bidirectionnel entre CreativeTool et RoutingHost.
- `LogService` — journalisation centralisée.
- `ControllerHealthChecker` — supervision des contrôleurs par ping ICMP.
- `StateFaker` / `UniverseSnapshotStore` — génération d'états de test et monitoring des univers envoyés.

## État d'avancement

**Projet terminé et livré.** Les deux applications communiquent en continu et pilotent réellement le mur de LED physique.

Les couches Domain, Application et Infrastructure sont implémentées et documentées.

**RoutingHost** :
- ✅ `PatchVisualizationView` — visualisation du patch avec navigation par drill-down (contrôleurs → univers → LED), statut de santé en direct (ping ICMP via `ControllerHealthChecker`), et envoi manuel de couleurs de test en ArtNet réel pour vérifier le câblage sur le terrain.
- ✅ Réception réelle du flux de CreativeTool (`UdpStateReceiver` relié à `RoutingEngine`), routé vers les contrôleurs via `ArtNetSender`.
- ✅ `LogView` — journal des événements en temps réel (`LogService.Instance`, partagé par toute l'application).
- ⬜ `UniverseMonitorView` / `MonitorViewModel` — suivi des univers actifs, dernières valeurs DMX envoyées, déclenchement de `StateFaker` : non implémenté, hors périmètre final (pas nécessaire au fonctionnement du spectacle).

**CreativeTool** : application complète — preview LED 128×128, timeline avec key frames, formes et effets de mouvement, panneau de contrôle des lyres motorisées, intégration musique et médias (image/vidéo/gif), animations finales du spectacle, et publication UDP de l'état courant vers RoutingHost (~40 fois/seconde).

**Communication réseau** : le flux `State`/liste d'entités entre CreativeTool et RoutingHost est sérialisé en **MessagePack** (plus compact que JSON) et **découpé en morceaux UDP** (`UdpChunkSender`/`UdpChunkReassembler`) pour rester sous la limite de fragmentation réseau, indispensable vu la taille du mur (16 384 LED adressables).

**Correspondance avec le matériel réel** : le mur visible fait 128×128 LED, câblé physiquement en 64 bandes de LED pliées en U (chaque bande forme 2 colonnes de 128 LED, avec quelques LED de fixation/pli invisibles). CreativeTool reconstruit cette correspondance à partir de `patch.json` pour que chaque pixel de l'aperçu logiciel corresponde à la bonne LED physique sur le mur.

## Prérequis

- .NET 10 SDK
- Windows (les applications CreativeTool et RoutingHost sont des applications WPF)

## Compilation

```
dotnet build Liekinheitin/Liekinheitin.slnx
```


## Lancement

Depuis la racine du dépôt :

```powershell
dotnet build Liekinheitin\Liekinheitin.slnx
```

Lancer CreativeTool :

```powershell
dotnet run --project Liekinheitin\CreativeTool\Liekinheitin.CreativeTool.csproj
```

Lancer RoutingHost dans un autre terminal :

```powershell
dotnet run --project Liekinheitin\RoutingHost\Liekinheitin.RoutingHost.csproj
```

CreativeTool publie actuellement les `State` en UDP vers `127.0.0.1:5000` pendant la lecture. RoutingHost doit donc écouter ce port pour recevoir les états.

Si un build échoue avec un message du type `Liekinheitin.CreativeTool.exe est en cours d'utilisation`, fermer la fenêtre CreativeTool déjà ouverte puis relancer le build. C'est un verrou Windows sur l'exécutable, pas une erreur de compilation.

## Configuration

Le fichier `Liekinheitin/Path.json` décrit le plan d'adressage : contrôleurs, univers DMX et plages d'entités associées.

## Notes Git

Le dossier `.vs/` est un dossier local de Visual Studio et ne doit pas être versionné. Il est ignoré par `.gitignore`.

Si des fichiers `.vs` apparaissent encore dans `git status` après une ancienne indexation, les retirer du suivi sans les supprimer du disque :

```powershell
git rm --cached -r .vs
```

Avant commit, vérifier que seuls les fichiers source utiles sont stagés :

```powershell
git status --short
git diff --cached --name-only
```
