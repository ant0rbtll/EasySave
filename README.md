# EasySave v3.0

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![License](https://img.shields.io/badge/license-ProSoft-blue)

EasySave est un logiciel professionnel de sauvegarde développé par ProSoft.  
La version 3.0 finalise l'exécution parallèle, le pilotage runtime (pause/reprise/arrêt), la supervision temps réel multi-jobs, la priorisation des fichiers, la limitation des gros transferts et la journalisation centralisée.

> Historique technique: [CHANGELOG.md](CHANGELOG.md)

## Table des matières

- [Démarrage rapide](#démarrage-rapide)
- [Fonctionnalités](#fonctionnalités)
- [Installation](#installation)
- [Utilisation](#utilisation)
- [Serveur de logs centralisé](#serveur-de-logs-centralisé)
- [Fichiers générés](#fichiers-générés)
- [Documentation](#documentation)
- [Développement](#développement)
- [Équipe](#équipe)

## Démarrage rapide

```bash
# Build
dotnet build

# GUI (par défaut)
dotnet run --project src/EasySave.AppCommon

# CLI (exécution directe)
dotnet run --project src/EasySave.AppCommon -- 1
dotnet run --project src/EasySave.AppCommon -- "1;3;5"
dotnet run --project src/EasySave.AppCommon -- 1-3
```

## Fonctionnalités

- Double interface: GUI (Avalonia) et console/CLI sur le même coeur applicatif
- Exécution parallèle de plusieurs jobs, avec anti-duplication par ID
- Contrôle runtime: pause/reprise/arrêt (un job, sélection, ou global)
- Suivi temps réel multi-jobs avec progression, fichiers/taille restants et ETA
- Gestion des priorités de fichiers (extensions prioritaires globales)
- Limiteur global des gros transferts parallèles (seuil configurable en Ko/Mo/Go)
- Sauvegardes complètes et différentielles
- Chiffrement post-transfert:
  - `DotNet` (AES)
  - `External` (CryptoSoft, mono-instance via mutex)
- Blocage dynamique par logiciel métier détecté (`Blocked`)
- Logs locaux JSON/XML et/ou centralisés via HTTP (`Local`, `Centralized`, `LocalAndCentralized`)
- Fallback résilient en local si serveur de logs indisponible
- Consultation des logs par date/job/run + détails durées/taille
- Internationalisation FR/EN/IT
- Suite de tests validée: `dotnet test` (688 tests passés le 2026-02-25)

## Installation

### Prérequis

- .NET 8 SDK
- Windows, Linux ou macOS

### Local

```bash
git clone https://github.com/ant0rbtll/easysave.git
cd easysave
dotnet restore
dotnet build
```

### Docker

```bash
# Environnement dev
docker compose up easysave-dev

# Exécution de la suite de tests
docker compose run --rm easysave-test

# Serveur de logs centralisé
docker compose -f compose.logserver.yaml up -d
```

## Utilisation

### GUI (par défaut)

```bash
dotnet run --project src/EasySave.AppCommon
```

Écrans principaux:

- `Home`
- `Create`: création de jobs
- `Manage`: recherche, tri, pagination, filtres (statut/type), exécution/édition/suppression
- `Progress`: supervision live des jobs actifs + actions runtime
- `Log`: navigation des logs par date/job/run
- `Config`: langue, logs, chiffrement, priorités, logiciels métier, seuil gros fichiers

### Console interactive

```bash
EASYSAVE_HOST=console dotnet run --project src/EasySave.AppCommon
```

### CLI (one-shot)

```bash
dotnet run --project src/EasySave.AppCommon -- 1
dotnet run --project src/EasySave.AppCommon -- "1;3;5"
dotnet run --project src/EasySave.AppCommon -- 1-3
```

## Serveur de logs centralisé

### Lancement local

```bash
dotnet run --project src/EasySave.LogServer --urls http://localhost:5080
```

### Configuration client EasySave

Dans `Config`:

- `Log mode`: `Centralized` ou `LocalAndCentralized`
- `Log server URL`: ex. `http://localhost:5080`

### API exposée

- `POST /api/logs`: réception d'une entrée de log
- `GET /api/config`: format de log serveur
- `GET /api/clients`: liste des clients connus
- `PUT /api/clients/{macAddress}`: renommage d'un client

## Fichiers générés

Les fichiers applicatifs sont créés dans `AppContext.BaseDirectory` (répertoire de l'exécutable lancé).

- `jobs.json`: jobs configurés
- `state.json`: état runtime
- `user-preferences.json`: préférences utilisateur
- `logs/YYYY-MM-DD.json|xml`: logs journaliers locaux

Pour le serveur de logs Docker:

- `/app/logs`: logs centralisés (volume `logserver-logs`)
- `/app/data/clients.json`: registre clients (volume `logserver-data`)

## Documentation

### Manuels

- [Manuel Utilisateur (FR)](docs/manuals/Manuel_Utilisateur_EasySave.pdf)
- [User Manual (EN)](docs/manuals/User_Manual_EasySave.pdf)
- [Manuel Support (FR)](docs/manuals/Manuel_Support_EasySave.pdf)
- [Support Manual (EN)](docs/manuals/Support_Manual_EasySave.pdf)

### UML

- [Classes](docs/classes.puml)
- [Séquence](docs/sequence.puml)
- [Activité](docs/activity.puml)
- [Cas d'utilisation](docs/usecase.puml)

### Changelog

- [CHANGELOG.md](CHANGELOG.md)

## Développement

### Structure

```text
src/
├── EasySave.AppCommon/     # Entrée unique + DI + sélection host GUI/console
├── EasySave.GUI/           # Interface Avalonia (MVVM)
├── EasySave.UI/            # Interface console + parser CLI
├── EasySave.Application/   # Services applicatifs + readers + ETA + coordination
├── EasySave.Backup/        # Moteur backup + runtime gate + chiffrement
├── EasySave.Persistence/   # Repositories JSON (jobs + préférences)
├── EasySave.State/         # État temps réel
├── EasySave.Log/           # Contrats de logs
├── EasySave.LogServer/     # API de centralisation des logs
├── EasyLog/                # Logger local journalier JSON/XML
├── EasySave.Configuration/ # Résolution des chemins
├── EasySave.Localization/  # Traductions FR/EN/IT
├── EasySave.Exception/     # Exceptions métier partagées
└── EasySave.System/        # Abstractions système/transfert

tests/                      # 12 projets de tests
```

### Commandes utiles

```bash
./clean.sh
dotnet restore
dotnet build
dotnet test
```

## Équipe

Développé par l'équipe ProSoft - CESI:

- **Antonin RABATEL** ([@ant0rbtll](https://github.com/ant0rbtll))
- **Romain TOUZE** ([@RomainTouze](https://github.com/RomainTouze))
- **Alexandre RIVET** ([@Gosyfrone](https://github.com/Gosyfrone))
- **Youcef AFANE** ([@RezeGH](https://github.com/RezeGH))
- **Lisa ACHOUR** ([@achourl14](https://github.com/achourl14))
- **Thaïs VIANES** ([@thedarknessqueen](https://github.com/thedarknessqueen))

## Support

1. Consultez les [manuels](docs/manuals/)
2. Vérifiez les [issues existantes](../../issues)
3. Créez une [nouvelle issue](../../issues/new)

© 2026 ProSoft - Tous droits réservés
