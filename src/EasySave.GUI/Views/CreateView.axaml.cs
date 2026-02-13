using Avalonia.Controls;
using Avalonia.Platform.Storage;
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

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = vm.BrowseSourceTitle,
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            vm.SetSourcePathFromDialog(folders[0].Path.LocalPath);
        }
    }

    private async void BrowseDestination_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CreateViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = vm.BrowseDestinationTitle,
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            vm.SetDestinationPathFromDialog(folders[0].Path.LocalPath);
        }
    }

    private void Source_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CreateViewModel vm)
            vm.ValidateSourcePathOnLostFocus();
    }

    private void Destination_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CreateViewModel vm)
            vm.ValidateDestinationPathOnLostFocus();
    }

    private void Name_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CreateViewModel vm)
            vm.ValidateNameOnLostFocus();
    }
}
