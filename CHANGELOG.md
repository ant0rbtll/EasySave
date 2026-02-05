# Changelog

All notable changes to EasySave will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [1.0.0] - 2026-02-05

Version initiale de production d'EasySave.

### Added

#### Architecture
- Implémentation Clean Architecture avec séparation stricte des couches (Core, Application, Infrastructure, UI)
- Injection de dépendances avec Microsoft.Extensions.DependencyInjection
- Configuration centralisée des services dans Program.cs
- Interfaces pour tous les services majeurs (IBackupEngine, IFileSystem, ILogger, etc.)

#### Core Domain
- Entité `BackupJob` (Id, Name, Source, Destination, Type)
- Enum `BackupType` (Complete, Differential)
- Enum `BackupStatus` (Inactive, Active, Completed, Error)

#### Backup Engine
- Moteur de sauvegarde complète avec traversée récursive
- Moteur de sauvegarde différentielle basée sur date de modification
- Gestion des erreurs avec continuité (un fichier en erreur n'arrête pas la sauvegarde)
- Suivi de progression en temps réel (fichiers traités, taille, pourcentage)
- Support des chemins longs et normalisation multi-plateforme

#### Persistence Layer
- Repository pattern avec `IBackupJobRepository`
  - `InMemoryBackupJobRepository` pour développement
  - `JsonBackupJobRepository` pour production
- `IUserPreferencesRepository` avec implémentation JSON
- `IJobIdProvider` avec `SequentialJobIdProvider` pour génération d'IDs uniques
- Limite configurable de 5 travaux maximum
- Sérialisation avec System.Text.Json

#### State Management
- `GlobalState` pour état partagé entre composants
- `RealTimeStateWriter` avec fichier JSON unique mis à jour en continu
- `StateSerializer` pour sérialisation des états
- Support multi-processus avec gestion des accès concurrents
- Informations par travail : statut, progression, fichier courant

#### Logging (EasyLog.dll)
- Bibliothèque de logs réutilisable séparée du projet principal
- Abstraction via `ILogger` interface
- `DailyFileLogger` avec rotation automatique (un fichier par jour)
- `JsonLogFormatter` pour structure standardisée
- `NoOpLogger` pour tests sans I/O
- Mutex global pour synchronisation inter-processus
- Format de log : timestamp, backup name, event type, source/dest, size, duration
- Types d'événements : TransferFile, CreateDirectory
- Projet `EasySave.Log` comme couche d'abstraction (fonctionne sans EasyLog.dll si nécessaire)

#### UI Layer
- Interface CLI avec navigation clavier (flèches haut/bas, Entrée)
- `ConsoleUI` avec menus dynamiques
- `MenuService` pour gestion des menus
- `MenuFactory` avec pattern Factory pour création dynamique
- Formulaires en ligne de commande avec validation
- Gestion d'erreurs avec `ErrorManager` et messages localisés
- Affichage en couleur (erreurs en rouge)

#### Localization
- Support multilingue : Français et Anglais
- 366 clés de traduction
- `LocalizationService` avec fichiers YAML
- Changement de langue à chaud
- Persistence de la langue sélectionnée dans préférences
- Enum `LocalizationKey` pour typage fort

#### CLI Parsing
- `CommandLineParser` pour exécution sans interaction
- Support plages : `1-3` exécute jobs 1, 2, 3
- Support listes : `1;3;5` exécute jobs 1, 3, 5
- Support unique : `1` exécute job 1
- Parsing flexible avec espaces et séparateurs multiples

#### Configuration
- `IPathProvider` avec `DefaultPathProvider`
- Répertoires configurables par OS :
  - Windows : `%APPDATA%/ProSoft/EasySave/`
  - Linux/macOS : `~/.config/ProSoft/EasySave/`
- Chemins personnalisables pour logs
- Fallback automatique en cas d'erreur

#### System Abstractions
- `IFileSystem` pour abstraction système de fichiers (testabilité)
- `DefaultFileSystem` avec implémentation réelle
- `ITransferService` pour transferts de fichiers
- `DefaultTransferService` avec buffer optimisé
- `TransferResult` pour résultats de transfert
- Support chemins longs et caractères spéciaux

#### Testing
- 100+ tests unitaires avec xUnit
- Couverture >95% du code
- Mocking avec Moq pour isolation des tests
- Tests paramétrés avec [Theory] et [InlineData]
- TempDirectory pour tests avec fichiers temporaires
- Suites de tests :
  - EasySave.Application.Tests
  - EasySave.Backup.Tests
  - EasySave.Configuration.Tests
  - EasySave.Localization.Tests
  - EasySave.Persistence.Tests
  - EasySave.State.Tests
  - EasySave.System.Tests
  - EasyLog.Tests

#### Docker
- Dockerfile.dev pour environnement de développement
- Dockerfile.test pour exécution des tests
- compose.yaml pour orchestration
- Support multi-plateforme

#### CI/CD
- Pipeline GitHub Actions (.github/workflows/)
- Build automatique sur push
- Exécution des tests unitaires
- Génération du code coverage
- Création automatique des releases avec artifacts
- Publication des exécutables pour Windows/Linux/macOS

#### Documentation
- Diagrammes UML PlantUML (classes, séquence, activité, use cases)
- XML Documentation sur toutes les classes publiques
- Manuels utilisateur (FR/EN)
- Manuels support (FR/EN)
- README pour guide utilisateur
- CHANGELOG pour historique technique

### Changed
- Renommage `SaveWork` en `BackupJob` pour cohérence terminologique
- Migration vers C# 12 avec primary constructors
- Utilisation de file-scoped namespaces
- Migration vers .NET 8.0 LTS

### Fixed
- Correction dépendances circulaires entre modules
- Fix gestion des espaces dans chemins de fichiers
- Correction sérialisation JSON avec caractères spéciaux
- Fix mutex pour logs multi-processus
- Correction gestion des fichiers vides dans logs

### Technical Details

#### Architecture Patterns
- SOLID principles appliqués strictement
- Dependency Inversion : dépendances sur abstractions
- Repository Pattern pour persistance
- Strategy Pattern pour types de sauvegarde
- Factory Pattern pour création dynamique de menus
- Observer Pattern pour état temps réel

#### Technologies Stack
- .NET 8.0 LTS
- C# 12
- Microsoft.Extensions.DependencyInjection 8.0.0
- YamlDotNet pour parsing fichiers de traduction
- System.Text.Json pour sérialisation
- xUnit pour tests
- Moq pour mocking

#### Performance
- Lazy loading des services
- Buffering pour transferts de fichiers
- Énumération paresseuse avec LINQ
- Streaming pour grandes quantités de fichiers
- Optimisation mémoire avec IDisposable

#### Security & Robustness
- Validation stricte des entrées utilisateur
- Gestion exhaustive des exceptions
- Try-catch aux points critiques
- IDisposable pour ressources
- Mutex pour synchronisation inter-processus
- Tests de stress inclus

### Contributors

- **Antonin RABATEL** (@ant0rbtll) - Architecture, Persistence, Docker, Tests
- **Romain TOUZE** (@RomainTouze) - UI, Localization, Error Management
- **Alexandre RIVET** (@Gosyfrone) - Architecture, Core, Backup Engine, State Management, CI/CD
- **Youcef AFANE** (@RezeGH) - EasyLog.dll, Tests, Documentation
- **Lisa ACHOUR** (@achourl14) - Application Service, UI, Tests
- **Thaïs VIANES** (@thedarknessqueen) - ETR (État Temps Réel), State Writer

[1.0.0]: https://github.com/ant0rbtll/easysave/releases/tag/v1.0.0
