# Changelog EasySave

## Version 1.0 - [05/02/2026]

### Architecture & Infrastructure

#### Clean Architecture
- **Architecture en couches** avec séparation stricte des responsabilités :
  - `Core` : Entités métier (BackupJob, BackupType, BackupStatus)
  - `Application` : Logique applicative (BackupApplicationService)
  - `Infrastructure` : Implémentations concrètes (Persistence, System, State, Backup)
  - `UI` : Interface utilisateur (ConsoleUI)
  - `Localization` : Gestion de l'internationalisation
  - `Configuration` : Configuration de l'application

#### Dependency Injection
- **Injection de dépendances** avec Microsoft.Extensions.DependencyInjection
- Configuration centralisée des services dans `Program.cs`
- Interfaces pour tous les services majeurs permettant la testabilité et l'extensibilité

#### Docker
- **Dockerfile.dev** : Environnement de développement avec build et debug
- **Dockerfile.test** : Environnement de test automatisé
- **compose.yaml** : Orchestration des conteneurs
- Support multi-plateforme (Windows/Linux/macOS)

#### CI/CD
- **Pipeline GitHub Actions** :
  - Build automatique sur push
  - Exécution des tests unitaires
  - Génération du code coverage (rapport de couverture de tests)
  - Création automatique des releases avec artifacts
  - Publication des exécutables pour Windows/Linux/macOS

---

### Fonctionnalités Principales

#### 1. Gestion des Travaux de Sauvegarde

##### Création et Configuration
- **Création de travaux** avec paramètres personnalisables :
  - Nom du travail
  - Chemin source (dossier à sauvegarder)
  - Chemin destination (dossier de sauvegarde)
  - Type de sauvegarde (Complète ou Différentielle)
- **Limite de 5 travaux simultanés** configurables
- **Validation des entrées** avec gestion d'erreurs détaillée

##### Gestion
- **Liste des travaux** : Affichage de tous les travaux créés avec leurs détails
- **Modification** : Mise à jour des propriétés des travaux existants
- **Suppression** : Retrait des travaux avec confirmation
- **Exécution** : 
  - Exécution d'un travail unique par son ID
  - Exécution multiple de travaux (liste d'IDs)
  - Exécution de tous les travaux
  - Exécution depuis la ligne de commande

#### 2. Moteur de Sauvegarde (BackupEngine)

##### Sauvegarde Complète
- **Copie intégrale** de tous les fichiers et sous-dossiers
- **Création automatique** de l'arborescence de destination
- **Transfert récursif** des fichiers avec préservation de la structure

##### Sauvegarde Différentielle
- **Comparaison intelligente** basée sur la date de dernière modification
- **Transfert optimisé** : Copie uniquement des fichiers modifiés ou nouveaux
- **Économie d'espace disque** et de temps de sauvegarde

##### Fonctionnalités Avancées
- **Gestion des erreurs** : Continuité en cas d'erreur sur un fichier
- **Suivi de progression** en temps réel (fichiers traités, taille, pourcentage)
- **Support des chemins longs** et caractères spéciaux
- **Normalisation des chemins** multi-plateforme

---

### Interface Utilisateur CLI

#### Menu Interactif
- **Navigation intuitive** avec flèches haut/bas et validation par Entrée
- **Menu principal dynamique** :
  - Créer un travail de sauvegarde
  - Lancer un travail de sauvegarde
  - Supprimer un travail
  - Modifier un travail
  - Afficher la liste des travaux
  - Paramètres
  - Quitter
- **Menus contextuels** pour chaque travail avec actions spécifiques
- **Système de formulaires** en ligne de commande avec validation

#### Internationalisation (i18n)
- **Support multilingue** : Français et Anglais
- **Traduction automatique** de toute l'interface
- **Changement de langue à chaud** depuis le menu paramètres
- **Fichiers YAML** de traduction extensibles
- **Gestion des paramètres** : 366 clés de traduction
- **Persistance de la langue** sélectionnée

#### Ligne de Commande (CLI Parsing)
- **Exécution directe** sans interface interactive
- **Formats supportés** :
  - `EasySave 1-3` : Exécute les jobs 1, 2, 3
  - `EasySave 1;3;5` : Exécute les jobs 1, 3, 5
  - `EasySave 2` : Exécute le job 2
- **Parsing flexible** avec gestion des espaces et séparateurs

---

### Logs et États

#### Logs Journaliers (EasyLog)
- **Fichiers JSON quotidiens** nommés par date (`log-YYYY-MM-DD.json`)
- **Structure détaillée** pour chaque entrée :
  - Timestamp (horodatage précis)
  - Nom du travail (BackupName)
  - Type d'événement (TransferFile, CreateDirectory)
  - Chemin source et destination
  - Taille du fichier transféré
  - Durée de l'opération (en millisecondes)
- **Événements loggés** :
  - Transfert de fichier (TransferFile)
  - Création de répertoire (CreateDirectory)
- **Rotation automatique** : Un fichier par jour
- **Format standardisé** pour analyse ultérieure

#### État en Temps Réel (Real-Time State)
- **Fichier JSON unique** (`state.json`) mis à jour en continu
- **État global** contenant tous les travaux en cours
- **Informations par travail** :
  - Nom et timestamp
  - Statut (Inactive, Active, Completed, Error)
  - Progression détaillée :
    - Nombre total de fichiers
    - Taille totale (en octets)
    - Fichiers restants
    - Taille restante
    - Pourcentage de complétion
  - Fichier en cours de traitement
  - Chemin source et destination actuels
- **Mise à jour synchronisée** lors de chaque opération
- **Support multi-processus** avec gestion des accès concurrents

#### EasyLog.dll - Bibliothèque de Logs
- **DLL réutilisable** intégrée au projet
- **Abstraction du système de logs** via interfaces
- **Implémentations** :
  - `DailyFileLogger` : Logs journaliers sur fichier
  - `JsonLogFormatter` : Formatage JSON standardisé
  - `NoOpLogger` : Logger silencieux pour tests
- **Configuration flexible** :
  - Chemin personnalisable pour les logs
  - Fallback automatique en cas d'erreur
- **Thread-safe** : Gestion des écritures concurrentes avec mutex

---

### Persistance

#### Repositories
- **IBackupJobRepository** : Gestion des travaux de sauvegarde
  - `InMemoryBackupJobRepository` : Stockage en mémoire (développement)
  - `JsonBackupJobRepository` : Stockage JSON persistant (production)
- **IUserPreferencesRepository** : Préférences utilisateur
  - `JsonUserPreferencesRepository` : Sauvegarde en JSON
- **Persistance automatique** des modifications
- **Rechargement au démarrage** de l'application

#### Configuration
- **UserPreferences** :
  - Langue de l'interface
  - Répertoire personnalisé pour les logs
- **DefaultPathProvider** : Centralisation des chemins système
  - Répertoire de configuration : `%APPDATA%/ProSoft/EasySave` (Windows) ou `~/.config/ProSoft/EasySave` (Linux/macOS)
  - Chemins des logs personnalisables
  - Chemins des états et préférences
- **Portabilité multi-plateforme**

#### Sequential Job ID Provider
- **Génération d'IDs** séquentiels et uniques
- **Persistence des compteurs** pour éviter les collisions
- **Thread-safe**

---

### Tests Unitaires

#### Couverture Complète
- **Plus de 100 tests unitaires** couvrant toutes les couches :
  - `EasySave.Application.Tests` : Tests du service applicatif
  - `EasySave.Backup.Tests` : Tests du moteur de sauvegarde
  - `EasySave.Configuration.Tests` : Tests de configuration
  - `EasySave.Localization.Tests` : Tests d'internationalisation
  - `EasySave.Persistence.Tests` : Tests des repositories
  - `EasySave.State.Tests` : Tests de gestion d'état
  - `EasySave.System.Tests` : Tests des abstractions système
  - `EasyLog.Tests` : Tests de la bibliothèque de logs

#### Mocking et Isolation
- **Moq Framework** : Création de mocks pour les dépendances
- **Tests isolés** : Chaque composant testé indépendamment
- **Patterns de test** :
  - Arrange-Act-Assert
  - Fixtures de test réutilisables
  - TempDirectory pour tests avec fichiers

#### Frameworks et Outils
- **xUnit** : Framework de tests moderne
- **Code Coverage** : Rapport de couverture généré automatiquement
- **Tests paramétrés** : [Theory] et [InlineData]

---

### Gestion des Erreurs

#### Error Manager
- **Gestion centralisée** des erreurs
- **Messages personnalisés** par type d'exception :
  - `ArgumentException` : Erreurs de paramètres
  - `DirectoryNotFoundException` : Dossiers introuvables
  - `UnauthorizedAccessException` : Erreurs de permissions
  - `IOException` : Erreurs d'entrées/sorties
  - `NotSupportedException` : Opérations non supportées
- **Affichage en couleur** : Messages d'erreur en rouge
- **Traduction des erreurs** selon la langue de l'interface
- **Stack traces** pour le débogage

#### Validation
- **Validation des chemins** : Vérification d'existence des dossiers
- **Validation des entrées** : Format, limites, contraintes
- **Gestion des cas limites** : Chemins vides, caractères spéciaux, etc.

---

### Documentation

#### Diagrammes UML (PlantUML)
- **Diagramme de classes** (`classes.puml`) : Architecture complète
- **Diagrammes de séquence** (`sequence.puml`) : Flux d'exécution
- **Diagrammes d'activité** (`activity.puml`) : Processus métier
- **Diagrammes de cas d'utilisation** (`usecase.puml`) : Interactions utilisateur

#### Annotations Code
- **XML Documentation** sur toutes les classes publiques
- **Commentaires détaillés** pour les méthodes complexes
- **Exemples d'utilisation** dans les commentaires
- **Annotations en anglais** pour cohérence internationale

---

### Sécurité et Robustesse

#### Gestion Mémoire
- **IDisposable** correctement implémenté pour les ressources
- **using statements** pour libération automatique
- **Gestion des mutex** pour synchronisation inter-processus

#### Stabilité
- **Try-catch** aux points critiques
- **Continuité en cas d'erreur** : Un fichier en erreur n'arrête pas la sauvegarde
- **Validation stricte** des entrées utilisateur
- **Tests de stress** dans la suite de tests

---

### Outils de Développement

#### Scripts Utilitaires
- **clean.sh** : Nettoyage des dossiers bin/obj et restauration NuGet
- **Script NuGet** : Résolution automatique des problèmes de dépendances locales

#### Configuration Projet
- **global.json** : Version SDK .NET 8.0.100 fixée
- **Solution .sln** : Organisation multi-projets
- **NuGet packages** :
  - YamlDotNet : Parsing des fichiers de traduction
  - Moq : Framework de mocking
  - xUnit : Tests unitaires
  - Microsoft.Extensions.DependencyInjection : IoC Container

---

### Qualité de Code

#### Principes Appliqués
- **SOLID** : Respect des principes de conception objet
- **DRY** (Don't Repeat Yourself) : Factorisation du code
- **KISS** (Keep It Simple, Stupid) : Simplicité et clarté
- **Separation of Concerns** : Découpage par responsabilités
- **Dependency Inversion** : Dépendance sur abstractions

#### Standards C#
- **C# 12** : Utilisation des dernières fonctionnalités
- **Primary Constructors** : Syntaxe moderne
- **Nullable Reference Types** : Sécurité contre les null
- **Pattern Matching** : Code expressif
- **File-scoped Namespaces** : Lisibilité améliorée

---

### Performance

#### Optimisations
- **Lazy Loading** : Chargement à la demande
- **Buffering** : Transferts de fichiers optimisés
- **Énumération paresseuse** : LINQ pour parcours efficace
- **Sérialisation JSON** : System.Text.Json haute performance

#### Scalabilité
- **Support grands volumes** : Milliers de fichiers
- **Mémoire optimisée** : Streaming des opérations
- **Paths longs** : Support Windows et Unix

---

### Expérience Utilisateur

#### Feedback Visuel
- **Messages colorés** : Erreurs en rouge, succès normaux
- **Confirmation** : Demandes de validation pour actions critiques

#### Ergonomie
- **Navigation fluide** : Retour au menu à tout moment
- **Annulation** : Possibilité d'annuler la saisie
- **Labels clairs** : Titres explicites pour chaque menu
- **Aide contextuelle** : Instructions à chaque étape

---

### Extensibilité

#### Points d'Extension
- **Interfaces** : Tous les composants majeurs sont abstraits
- **Factories** : MenuFactory pour création dynamique
- **Strategy Pattern** : BackupType pour différents types de sauvegarde
- **Repository Pattern** : Abstraction du stockage

#### Évolutions Futures Facilitées
- Ajout de nouveaux types de sauvegarde
- Intégration de nouveaux systèmes de stockage (cloud, FTP, etc.)
- Support de nouveaux formats de logs
- Ajout de langues supplémentaires
- Interface graphique (WPF, Avalonia)
- API REST pour contrôle distant

---

### Caractéristiques Techniques

#### Environnement
- **.NET 8.0** : Version LTS avec support long terme
- **C# 12** : Dernière version du langage
- **Multi-plateforme** : Windows, Linux, macOS

#### Dépendances
- `Microsoft.Extensions.DependencyInjection` : IoC Container
- `YamlDotNet` : Parsing YAML pour traductions
- `xUnit` : Tests unitaires
- `Moq` : Mocking framework
- `System.Text.Json` : Sérialisation JSON native

---

## Réalisations Techniques Notables

1. **Architecture Clean** complète avec injection de dépendances
2. **Couverture de tests >95%** avec plus de 100 tests unitaires
3. **Internationalisation** complète avec 366 clés de traduction
4. **Pipeline CI/CD** automatisé avec GitHub Actions
5. **Docker** multi-environnement (dev/test/prod)
6. **Logs structurés** avec rotation automatique
7. **État temps réel** avec synchronisation multi-processus
8. **CLI parsing** flexible et robuste
9. **Gestion d'erreurs** exhaustive et localisée
10. **Documentation UML** professionnelle avec PlantUML

---

## Contributeurs

- **Antonin RABATEL** (@ant0rbtll) - Architecture, Persistence, Docker, Tests
- **Romain TOUZE** (@RomainTouze) - UI, Localization, Error Management
- **Alexandre RIVET** (@Gosyfrone) - Architecture, Core, Backup Engine, State Management, CI/CD
- **Youcef AFANE** (@RezeGH) - EasyLog.dll, Tests, Documentation
- **Lisa ACHOUR** - Application Service, Tests
- **Thaïs VAINES** - ETR (État Temps Réel), State Writer

---

**Note** : Cette version 1.0 représente une base solide et extensible pour le logiciel EasySave, avec toutes les fonctionnalités fondamentales implémentées, testées et documentées.
