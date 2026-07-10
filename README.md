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

Les couches Domain, Application et Infrastructure sont implémentées et documentées.

**RoutingHost** :
- ✅ `PatchVisualizationView` — visualisation du patch avec navigation par drill-down (contrôleurs → univers → LED), statut de santé en direct (ping ICMP via `ControllerHealthChecker`), et envoi manuel de couleurs de test en ArtNet réel pour vérifier le câblage sur le terrain.
- ⬜ `UniverseMonitorView` / `MonitorViewModel` — suivi des univers actifs, dernières valeurs DMX envoyées, déclenchement de `StateFaker` (squelette de projet, pas encore implémenté).
- ⬜ `LogView` / `LogViewModel` — affichage du journal alimenté par `LogService` (squelette de projet, pas encore implémenté).

**CreativeTool** : existe pour l'instant sous forme de squelette de projet (ViewModels et vues WPF encore à construire — grille de pixels, timeline, sélecteur de couleur, lecture audio).

## Prérequis

- .NET 10 SDK
- Windows (les applications CreativeTool et RoutingHost sont des applications WPF)

## Compilation

```
dotnet build Liekinheitin/Liekinheitin.slnx
```

## Configuration

Le fichier `Liekinheitin/Path.json` décrit le plan d'adressage : contrôleurs, univers DMX et plages d'entités associées.
