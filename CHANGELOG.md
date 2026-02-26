# Changelog

All notable changes to EasySave will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [3.0] - 2026-02-25

Version majeure: execution parallele, du pilotage runtime, de la supervision multi-jobs et de la centralisation des logs.

### Added

#### Execution / Runtime
- Execution parallele de plusieurs jobs (`RunJobs`, `RunAllJobs`, `Task.WhenAll`) avec anti-duplication par identifiant via `IBackupRunCoordinator` / `InMemoryBackupRunCoordinator`. (`1cb9646`)
- Ajout du pilotage runtime pause/reprise/arret (par job et global) via `IBackupExecutionController` / `BackupExecutionController` avec prise en compte dans le moteur et la GUI. (`b8ba6e9`)
- Ajout de la vue de progression live multi-jobs (`ProgressViewModel`) et du modele `BackupJobLiveProgressState`. (`7fd8939`, `2ccc588`)
- Ajout de l'estimation de temps restant (ETA) via `IBackupEtaEstimator` / `BackupEtaEstimator`. (`00b9c39`)

#### Orchestration de copie
- Ajout des extensions prioritaires avec barriere globale inter-jobs (`PriorityExtensions`, `IPriorityFilesBarrier`, `InMemoryPriorityFilesBarrier`) et statut `Waiting`. (`1959b71`)
- Ajout d'un limiteur global pour les gros transferts en parallele (`ILargeFileTransferBarrier`) avec seuil configurable (Ko/Mo/Go). (`756b388`)
- Ajout des metadonnees runtime `RemainingFiles` et `RemainingSizeBytes` dans l'etat temps reel pour la supervision fine et l'ETA.

#### Logs centralises
- Ajout du mode de logs centralises/hybrides via `LogMode` (`Local`, `Centralized`, `LocalAndCentralized`). (`93e9571`)
- Ajout de `HttpLogSender`, `ResilientHttpLogSender`, `CompositeLogger`, `LogServerStatusNotifier` et integration dans `ReloadableLogger`.
- Ajout du projet `EasySave.LogServer` (API HTTP de collecte des logs, registre clients, ecriture journaliere enrichie JSON/XML). (`93e9571`)
- Ajout des artefacts Docker dedies au serveur de logs: `Dockerfile.logserver`, `compose.logserver.yaml`. (`93e9571`)

#### Chiffrement externe
- Ajout de `ExternalCryptoProcessRunner` et enforcement mono-instance de CryptoSoft via mutex nomme et timeouts de securite dans `ExternalEncryptionProvider`. (`6da2781`)

#### GUI / I18N
- Ajout des filtres par statut/type dans `ManageViewModel` avec reset des filtres. (`4284959`)
- Ajout de la langue italienne (`translations.it.yaml`) et prise en charge de la culture `it`. (`4284959`)
- Ajout d'ameliorations UX de configuration (messages de statut auto-nettoyes, validation URL serveur, etc.). (`56244d9`)

### Changed

#### Architecture
- Refonte du moteur en composants specialises: `BackupRuntimeGate`, `BackupExecutionReporter`, `BackupFileExecutionService`, `DefaultBackupFilePlanner`. (`816a070`)
- Reorganisation de la couche application en `Readers/` et `Services/`, avec socles communs de lecture/traitement des logs. (`93caa02`)
- Decoupage massif de `ManageViewModel` (commands/live-refresh/translations) et introduction de services GUI dedies (`BackupRunningStateTracker`, `BackupJobDisplayService`). (`b5bdd4e`)

#### Robustesse technique
- Introduction du projet `EasySave.Exception` pour centraliser les exceptions metier partagees et leur traduction.
- Uniformisation des contrats de status runtime (`Inactive`, `Active`, `Waiting`, `Done`, `Error`, `Paused`, `Blocked`) entre coeur, state writer et GUI.

### Fixed

- Correction de l'actualisation de `LastExecutionDate` et de la reconciliation d'etats actifs. (`4d5519a`)
- Correction du calcul/remontee des durees de transfert dans les logs de fin d'execution. (`ae9d2e2`)
- Correction de la coherence des timestamps (GUI / logs serveur). (`26918e6`)
- Correctifs de revue et stabilisation des parcours de supervision/execution (`6a77f9d`, `b5bdd4e`).

### Tests

- Extension de la couverture avec des suites dediees `EasySave.AppCommon.Tests` et `EasySave.LogServer.Tests`.
- Validation de la suite complete (`dotnet test` le 2026-02-25):
  - 12 projets de tests
  - 688 tests passes, 0 echec, 0 ignores

## [2.0] - 2026-02-15

Version majeure: passage a une architecture multi-host (GUI + console) et extension des capacites de supervision/execution.

### Added

#### Application / Hosting
- Ajout de `EasySave.AppCommon` comme point d'entree unique (`EasySave`) avec selection dynamique du host:
  - GUI par defaut (sans argument)
  - Console/CLI si arguments, ou `EASYSAVE_HOST=console`
- Ajout de `ApplicationManager` pour centraliser l'injection de dependances partagees.

#### GUI (Avalonia)
- Ajout d'un host GUI complet (`EasySave.GUI`) avec navigation par ViewModels:
  - `CreateViewModel`: creation de jobs
  - `ManageViewModel`: recherche, tri, pagination, execution, edition, suppression
  - `LogViewModel`: exploration des logs par date/job/run avec details
  - `ConfigViewModel`: preferences et configuration avancee
- Ajout de la synchronisation de langue a chaud entre les ecrans GUI.

#### Chiffrement
- Ajout de `IEncryptionPolicyProvider` alimente par les preferences utilisateur.
- Ajout de `DotNetAesEncryptionProvider` (chiffrement AES en flux, post-transfert).
- Ajout de `ExternalEncryptionProvider` (contrat present, implementation reservee pour version future).
- Ajout de `EncryptionProviderResolver` pour selectionner le fournisseur de chiffrement par nom.
- Ajout des extensions chiffrables et du fournisseur dans `UserPreferences`.

#### Protection logiciel metier
- Ajout de `BusinessSoftwareBackupExecutionGuard`:
  - blocage de la copie si un processus metier configure est detecte
  - integration dans `BackupApplicationService` et `BackupEngine`
- Ajout de l'evenement de log `BusinessSoftwareDetected`.

#### Logs / Navigation
- Ajout de `ILogQueryService` et `ILogNavigationService` pour navigation hierarchique des logs.
- Ajout des lecteurs multi-format (`JsonLogReader`, `XmlLogReader`) avec index/cache journalier par date.
- Ajout de `ReloadableLogger` (`ILoggerRuntimeReloader`) pour appliquer les changements de format de log sans redemarrage.

### Changed

#### Runtime et UX
- `README.md` passe en version 2.0 et documente explicitement les modes GUI, console et CLI.
- Le flux de configuration GUI permet:
  - langue FR/EN
  - format de logs JSON/XML
  - dossier de logs
  - extensions a chiffrer
  - logiciels metier surveilles

#### Domain model
- `BackupJob` expose l'etat d'execution enrichi en runtime:
  - `LastExecutionDate`
  - `IsActive`

### Fixed

- Robustesse accrue lors du rechargement du logger (fallback `NoOpLogger` en cas d'erreur).
- Compatibilite ascendante des preferences pour l'ancien champ unique `businessSoftwareProcessName`.

### Tests

- Suite complete validee avec `dotnet test`:
  - 10 projets de tests
  - 505 tests passes, 0 echec, 0 ignores

## [1.1] - 2026-02-09

Evolution majeure de l'architecture UI/Logs et des preferences utilisateur.

### Added

#### UI / Navigation
- Introduction de `JobsFlowService` et `SettingsFlowService` pour separer les workflows metier de `ConsoleUI`
- Ajout de `JobEditSessionService` pour gerer un snapshot d'edition et detecter les changements non sauvegardes
- Ajout d'un menu de resolution des changements non sauvegardes (sauvegarder, annuler, retour)
- Ajout de l'enum `JobEditableField` pour typer les champs modifiables des travaux

#### Preferences / Parametres
- Ajout de la preference `LogFormat` dans `UserPreferences` avec support `Json` et `Xml`
- Ajout de la configuration du dossier de logs via `IPathProvider.SetLogDirectoryOverride(...)`
- Ajout du menu de changement du format de logs (JSON/XML) dans les parametres
- Ajout d'affichages de contexte dans les menus de parametres (langue active, format actif/pending, dossier actif)

#### Logging
- Ajout de `EasyLog.XmlLogFormatter` en plus de `JsonLogFormatter`
- Ajout de `ILogFileLayout` pour separer le format d'entree et la structure du fichier journal
- Renforcement de `DailyFileLogger` (mutex inter-process, insertion robuste avant footer, normalisation des chemins)
- Ajout des evenements `StartBackup` et `EndBackup` dans `LogEventType`

#### Execution / Transfert
- Ajout de `TransferResult.ErrorCode` et de codes standards (`InvalidSourcePath`, `InvalidDestinationPath`, `SourceNotFound`)
- Gestion explicite des echecs de transfert dans `BackupEngine` avec erreur metier localisable (`error_file_transfer_failed`)

### Changed

#### Architecture
- `ConsoleUI` devient un orchestrateur leger (composition des services UI au lieu d'embarquer toute la logique)
- `Program.CreateLogger(...)` selectionne dynamiquement le formatter en fonction des preferences utilisateur
- `DefaultPathProvider.GetDailyLogPath(...)` prend en compte le format de log et la creation/fallback des fichiers

#### Coherence fonctionnelle
- Le menu principal n'utilise plus une limite hardcodee pour la creation de jobs, mais `IBackupJobRepository.DefaultMaxJobs`
- Le menu d'edition de job n'utilise plus de chaines magiques (`"name"`, `"source"`, etc.), mais des identifiants types

### Fixed

- Normalisation (`Trim`) du dossier de logs avant persistence pour eviter les valeurs incoherentes en preferences
- Nettoyage de plusieurs divergences UI/persistence sur les contraintes et identifiants de champs d'edition

### Documentation

- Mise a jour complete des diagrammes UML en version 1.1 (`docs/classes.puml`, `docs/sequence.puml`, `docs/activity.puml`, `docs/usecase.puml`)

## [1.0] - 2026-02-05

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

[1.0]: https://github.com/ant0rbtll/easysave/releases/tag/v1.0
[1.1]: https://github.com/ant0rbtll/easysave/compare/v1.0...v1.1
[2.0]: https://github.com/ant0rbtll/easysave/compare/v1.1...v2.0
[3.0]: https://github.com/ant0rbtll/easysave/compare/v2.0...v3.0
