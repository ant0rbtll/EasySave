# EasySave v1.0

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![License](https://img.shields.io/badge/license-ProSoft-blue)

Logiciel professionnel de sauvegarde développé par ProSoft. Créez et gérez des sauvegardes complètes et différentielles avec une interface CLI multilingue.

> Pour les détails techniques complets : [CHANGELOG.md](changeLog.md)

---

## Table des Matières

- [Démarrage Rapide](#démarrage-rapide)
- [Fonctionnalités](#fonctionnalités)
- [Installation](#installation)
- [Utilisation](#utilisation)
- [Documentation](#documentation)
- [Développement](#développement)
- [Équipe](#équipe)

---

## Démarrage Rapide

```bash
# Compilation
dotnet build
dotnet run --project src/EasySave.UI

# Docker
docker compose up dev
```

---

## Fonctionnalités

- Sauvegardes Complètes et Différentielles
- Interface CLI multilingue (Français/Anglais)
- Logs journaliers JSON avec état temps réel
- Gestion jusqu'à 5 travaux de sauvegarde
- Multi-plateforme : Windows, Linux, macOS
- Clean Architecture avec injection de dépendances
- 100+ tests unitaires (>95% de couverture)

---

## Installation

### Prérequis
- .NET 8.0 SDK ou runtime
- Windows 10/11, Linux, ou macOS

### Depuis les Releases
1. Téléchargez depuis [Releases](../../releases)
2. Lancez `EasySave.exe` (Windows) ou `./EasySave` (Linux/macOS)

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
dotnet run --project src/EasySave.UI
```

---

## Utilisation

### Mode Interactif
```bash
./EasySave
```
Navigation au clavier pour créer, exécuter et gérer les travaux.

### Mode Ligne de Commande
```bash
./EasySave 1        # Travail 1
./EasySave 1;3;5    # Travaux 1, 3, 5
./EasySave 1-3      # Travaux 1, 2, 3
```

### Fichiers de Configuration
- **Windows** : `%APPDATA%/ProSoft/EasySave/`
- **Linux/macOS** : `~/.config/ProSoft/EasySave/`

---

## Documentation

### Manuels
- [Manuel Utilisateur (FR)](docs/manuals/Manuel_Utilisateur_EasySave.pdf) • [User Manual (EN)](docs/manuals/User_Manual_EasySave.pdf)
- [Manuel Support (FR)](docs/manuals/Manuel_Support_EasySave.pdf) • [Support Manual (EN)](docs/manuals/Support_Manual_EasySave.pdf)

### Diagrammes UML
- [Classes](docs/classes.puml) • [Séquence](docs/sequence.puml) • [Activité](docs/activity.puml) • [Cas d'utilisation](docs/usecase.puml)

### Changelog
[CHANGELOG.md](changeLog.md) - Historique complet avec architecture et détails techniques

---

## Développement

### Structure du Projet
```
src/
├── EasySave.Core/          # Entités métier
├── EasySave.Application/   # Services applicatifs
├── EasySave.Backup/        # Moteur de sauvegarde
├── EasySave.Persistence/   # Repositories
├── EasySave.State/         # Gestion d'état temps réel
├── EasySave.Localization/  # Internationalisation
├── EasySave.Log/           # Abstraction logging (implémente EasyLog si présent)
├── EasySave.UI/            # Interface CLI
└── EasyLog/                # Bibliothèque de logs (DLL)

tests/                      # Tests unitaires
docs/                       # Documentation et UML
```

### Technologies
- .NET 8.0 avec C# 12
- Microsoft.Extensions.DependencyInjection
- YamlDotNet, System.Text.Json
- xUnit, Moq

### Commandes
```bash
./clean.sh                  # Nettoyer
dotnet restore              # Restaurer dépendances
dotnet build                # Compiler
dotnet test                 # Lancer tests
```

---

## Équipe

Développé par l'équipe ProSoft - CESI :

- **Antonin RABATEL** ([@ant0rbtll](https://github.com/ant0rbtll)) - Architecture, Persistence, Docker, Tests
- **Romain TOUZE** ([@RomainTouze](https://github.com/RomainTouze)) - UI, Localization, Error Management
- **Alexandre RIVET** ([@Gosyfrone](https://github.com/Gosyfrone)) - Architecture, Core, Backup Engine, State Management, CI/CD
- **Youcef AFANE** ([@RezeGH](https://github.com/RezeGH)) - EasyLog.dll, Tests, Documentation
- **Lisa ACHOUR** ([@achourl14](https://github.com/achourl14)) - Application Service, UI, Tests
- **Thaïs VIANES** ([@thedarknessqueen](https://github.com/thedarknessqueen)) - ETR (État Temps Réel), State Writer

---

## Support

1. Consultez les [manuels](docs/manuals/)
2. Vérifiez les [issues existantes](../../issues)
3. Créez une [nouvelle issue](../../issues/new)

---

© 2026 ProSoft - Tous droits réservés
