# Guide Avalonia UI - EasySave v2.0

## Qu'est-ce qu'Avalonia ?

Avalonia est un framework UI **cross-platform** pour .NET. Il remplace WPF (qui est Windows-only) et permet de construire des interfaces graphiques qui tournent sur **Windows, macOS et Linux** avec le même code.

Le XAML d'Avalonia est très similaire à celui de WPF, avec quelques différences.

## Setup de l'environnement de développement

### IDE recommandé par OS

| OS | IDE | Extension/Plugin |
|----|-----|-----------------|
| Windows | Visual Studio 2022 | [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS) |
| Windows | JetBrains Rider | Plugin Avalonia intégré |
| macOS | JetBrains Rider | Plugin Avalonia intégré |
| Linux | JetBrains Rider | Plugin Avalonia intégré |
| Tous | VS Code | Extension [Avalonia for VSCode](https://marketplace.visualstudio.com/items?itemName=avalaboratories.avalonia-vscode) |

> **Note** : Rider est gratuit avec une licence étudiante JetBrains.

### Prérequis

- .NET SDK 8.0 (vérifié dans `global.json`)
- Templates Avalonia installés :

```bash
dotnet new install Avalonia.Templates
```

### Lancer l'application

```bash
# Depuis la racine du projet
dotnet run --project src/EasySave.GUI
```

> **Important** : L'application GUI ne peut pas se lancer dans Docker (pas d'écran). Lancez-la toujours directement sur votre machine.

## Architecture MVVM

Le projet GUI suit le pattern **Model-View-ViewModel** :

```
src/EasySave.GUI/
├── Models/              # Modèles spécifiques à la vue (si besoin)
├── ViewModels/          # Logique de présentation (pas d'UI ici)
│   ├── ViewModelBase.cs # Classe de base (ObservableObject)
│   └── MainWindowViewModel.cs
├── Views/               # Fichiers XAML + code-behind
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── App.axaml            # Configuration globale (thème, styles)
├── App.axaml.cs         # Initialisation + injection de dépendances
├── ViewLocator.cs       # Résolution automatique View <-> ViewModel
└── Program.cs           # Point d'entrée
```

### Principe

```
View (.axaml)  ──binding──>  ViewModel (.cs)  ──appel──>  Services existants
   (ce qu'on voit)           (logique UI)                (EasySave.Application, etc.)
```

- **View** : Décrit l'interface en AXAML (équivalent XAML). Ne contient **aucune logique**.
- **ViewModel** : Expose des propriétés et des commandes. Notifie la vue quand les données changent.
- **Model** : Les objets métier existants (`BackupJob`, etc.) dans `EasySave.Core`.

### Règles

1. Une View **ne doit jamais** appeler un service directement
2. Un ViewModel **ne doit jamais** manipuler des contrôles UI
3. La communication View <-> ViewModel passe uniquement par le **data binding**

## CommunityToolkit.Mvvm - Aide-mémoire

Le projet utilise [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) pour simplifier le code MVVM.

### Propriété observable

```csharp
// Le source generator crée automatiquement la propriété publique "Name"
// avec notification de changement (INotifyPropertyChanged)
public partial class MyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = "";
}
```

Equivalent sans le toolkit (à titre informatif, ne pas faire) :
```csharp
private string _name = "";
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}
```

### Commande

```csharp
public partial class MyViewModel : ViewModelBase
{
    // Le source generator crée automatiquement "SaveCommand"
    // utilisable en AXAML avec Command="{Binding SaveCommand}"
    [RelayCommand]
    private void Save()
    {
        // logique ici
    }

    // Commande asynchrone
    [RelayCommand]
    private async Task LoadAsync()
    {
        // logique async ici
    }
}
```

## AXAML - Syntaxe de base

### Extension des fichiers

Les fichiers de vue utilisent l'extension **`.axaml`** (pas `.xaml`).

### Binding de données

```xml
<!-- Binding simple (identique à WPF) -->
<TextBlock Text="{Binding Name}"/>

<!-- Binding vers une commande -->
<Button Content="Sauvegarder" Command="{Binding SaveCommand}"/>

<!-- Binding avec paramètre -->
<Button Content="Lancer"
        Command="{Binding RunJobCommand}"
        CommandParameter="{Binding}"/>
```

### Accéder au DataContext parent

```xml
<!-- WPF : RelativeSource FindAncestor -->
<!-- Avalonia : $parent[Type] (plus simple) -->
<Button Command="{Binding $parent[Window].DataContext.DeleteCommand}"
        CommandParameter="{Binding}"/>
```

### Liste d'éléments

```xml
<ListBox ItemsSource="{Binding Jobs}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="10">
                <TextBlock Text="{Binding Name}"/>
                <TextBlock Text="{Binding Type}"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Contrôles courants

| Contrôle | Usage |
|----------|-------|
| `TextBlock` | Afficher du texte (lecture seule) |
| `TextBox` | Champ de saisie |
| `Button` | Bouton cliquable |
| `ListBox` | Liste avec sélection |
| `ComboBox` | Liste déroulante |
| `StackPanel` | Empile des éléments (vertical/horizontal) |
| `DockPanel` | Disposition en dock (Top, Bottom, Left, Right) |
| `Grid` | Grille avec lignes et colonnes |
| `CheckBox` | Case à cocher |

## Différences clés avec WPF

| Concept | WPF | Avalonia |
|---------|-----|----------|
| Extension fichiers | `.xaml` | `.axaml` |
| Binding parent | `RelativeSource FindAncestor` | `$parent[Type]` |
| Styles | `<Style TargetType="Button">` | `<Style Selector="Button">` (syntaxe CSS-like) |
| Thème | Intégré Windows | `<FluentTheme />` (dans `App.axaml`) |
| Plateforme | Windows uniquement | Windows + macOS + Linux |
| Designer | Intégré Visual Studio | Rider ou extension VS/VSCode |

> **Astuce** : Si vous trouvez un exemple WPF en ligne, il fonctionnera souvent en Avalonia en changeant `.xaml` -> `.axaml` et en adaptant les styles. Pour les cas complexes, consultez la [doc Avalonia](https://docs.avaloniaui.net/).

## Ajouter une nouvelle vue

### 1. Créer le ViewModel

Créer `ViewModels/MyNewViewModel.cs` :

```csharp
namespace EasySave.GUI.ViewModels;

public partial class MyNewViewModel : ViewModelBase
{
    // Propriétés et commandes ici
}
```

### 2. Créer la View

Créer `Views/MyNewView.axaml` :

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:EasySave.GUI.ViewModels"
             x:Class="EasySave.GUI.Views.MyNewView"
             x:DataType="vm:MyNewViewModel">

    <!-- Contenu ici -->

</UserControl>
```

Et son code-behind `Views/MyNewView.axaml.cs` :

```csharp
using Avalonia.Controls;

namespace EasySave.GUI.Views;

public partial class MyNewView : UserControl
{
    public MyNewView()
    {
        InitializeComponent();
    }
}
```

### 3. Convention de nommage

Le `ViewLocator` résout automatiquement les vues en remplaçant `ViewModel` par `View` dans le nom :

- `MainWindowViewModel` -> `MainWindowView` (View)
- `JobListViewModel` -> `JobListView`
- `JobDetailViewModel` -> `JobDetailView`

**Respectez cette convention** pour que la résolution automatique fonctionne.

## Ressources

- [Documentation officielle Avalonia](https://docs.avaloniaui.net/)
- [Tutoriel Avalonia "To Do List"](https://docs.avaloniaui.net/docs/tutorials/todo-list-app/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Avalonia GitHub](https://github.com/AvaloniaUI/Avalonia)
