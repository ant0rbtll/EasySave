using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI.Views;

public partial class CreateView : UserControl
{
    public CreateView()
    {
        InitializeComponent();
    }

    private async void BrowseSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CreateViewModel vm)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = "Choisir le dossier source"
        };

        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
        {
            vm.SetSourcePath(result);
        }
    }

    private async void BrowseDestination_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CreateViewModel vm)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = "Choisir le dossier de destination"
        };

        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
        {
            vm.SetDestinationPath(result);
        }
    }
}

