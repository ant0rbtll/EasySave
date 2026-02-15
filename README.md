# EasySave v2.0

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![License](https://img.shields.io/badge/license-ProSoft-blue)

Logiciel professionnel de sauvegarde développé par ProSoft. EasySave 2.0 introduit une interface graphique Avalonia, conserve le mode console/CLI, et ajoute un pilotage avancé des logs, de l'état temps réel et du chiffrement.

> Historique technique : [CHANGELOG.md](CHANGELOG.md)

## Table des matières

- [Démarrage rapide](#démarrage-rapide)
- [Fonctionnalités](#fonctionnalités)
- [Installation](#installation)
- [Utilisation](#utilisation)
- [Documentation](#documentation)
- [Développement](#développement)
- [Équipe](#équipe)

## Démarrage rapide

```bash
# Build
dotnet build

# Lancement GUI (par défaut)
dotnet run --project src/EasySave.AppCommon

# Lancement console/CLI
dotnet run --project src/EasySave.AppCommon -- 1
```

## Fonctionnalités

- Double interface : GUI (Avalonia) et console/CLI (même cœur applicatif)
- Sauvegardes complètes et différentielles
- Exécution mono ou multi-jobs (`1`, `1;3;5`, `1-3`)
- Logs journaliers JSON/XML + rechargement runtime du logger
- Consultation des logs par date/job/run dans l'interface GUI
- État temps réel (`state.json`) avec progression et statut
- Chiffrement post-transfert par extensions configurables (`DotNet` intégré, `External` préparé)
- Blocage de l'exécution si un logiciel métier surveillé est détecté
- Internationalisation FR/EN (console + GUI)
- 505 tests unitaires passants (`dotnet test`)

## Installation

### Prérequis

- .NET 8.0 SDK ou runtime
- Windows 10/11, Linux, ou macOS

### Avec Docker

```bash
docker compose up dev  # Développement
docker compose up test # Tests
```

### Compilation

```bash
git clone https://github.com/ant0rbtll/easysave.git
cd easysave
dotnet restore
dotnet build
dotnet run --project src/EasySave.AppCommon
```

## Utilisation

### Mode GUI (par défaut)

```bash
dotnet run --project src/EasySave.AppCommon
```

Sections principales :

- Création de jobs
- Gestion (recherche, tri, pagination, exécution, suppression, édition)
- Historique des logs (calendrier, runs, détails)
- Configuration (langue, format de log, dossier de logs, extensions chiffrées, logiciels métier)

### Mode console interactif

```bash
EASYSAVE_HOST=console dotnet run --project src/EasySave.AppCommon
```

### Mode ligne de commande

```bash
dotnet run --project src/EasySave.AppCommon -- 1
dotnet run --project src/EasySave.AppCommon -- "1;3;5"
dotnet run --project src/EasySave.AppCommon -- 1-3
```

### Fichiers générés

- `jobs.json` : configuration des travaux
- `state.json` : état temps réel
- `user-preferences.json` : préférences utilisateur
- `logs/YYYY-MM-DD.json|xml` : historique journalier

### Emplacements par OS

- Windows : `%APPDATA%/ProSoft/EasySave/`
- Linux/macOS : `~/.config/ProSoft/EasySave/`

## Documentation

### Manuels

- [Manuel Utilisateur (FR)](docs/manuals/Manuel_Utilisateur_EasySave.pdf) • [User Manual (EN)](docs/manuals/User_Manual_EasySave.pdf)
- [Manuel Support (FR)](docs/manuals/Manuel_Support_EasySave.pdf) • [Support Manual (EN)](docs/manuals/Support_Manual_EasySave.pdf)

### Diagrammes UML

- [Classes](docs/classes.puml) • [Séquence](docs/sequence.puml) • [Activité](docs/activity.puml) • [Cas d'utilisation](docs/usecase.puml)

### Changelog

- [CHANGELOG.md](CHANGELOG.md)

## Développement

### Structure du projet

```text
src/
├── EasySave.AppCommon/     # Entrée unique + DI + sélection host GUI/console
├── EasySave.GUI/           # Interface graphique Avalonia (MVVM)
├── EasySave.UI/            # Interface console + parser CLI
├── EasySave.Application/   # Cas d'usage (jobs, état, logs)
├── EasySave.Backup/        # Moteur de backup + chiffrement + garde d'exécution
├── EasySave.Persistence/   # Repositories JSON (jobs + préférences)
├── EasySave.State/         # Écriture état temps réel
├── EasySave.Log/           # Contrats de log (abstraction)
├── EasyLog/                # Logger journalier JSON/XML
├── EasySave.Configuration/ # Résolution des chemins
├── EasySave.Localization/  # Traductions FR/EN
└── EasySave.System/        # Abstractions filesystem/transfert

tests/                      # 10 projets de tests
docs/                       # UML + guides + manuels
```

### Technologies

- .NET 8.0 avec C# 12
- Microsoft.Extensions.DependencyInjection
- Avalonia 11, CommunityToolkit.Mvvm
- YamlDotNet, System.Text.Json
- xUnit, Moq

### Commandes

```bash
./clean.sh
dotnet restore
dotnet build
dotnet test
dotnet run --project src/EasySave.AppCommon
```

## Équipe

Développé par l'équipe ProSoft - CESI :

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
