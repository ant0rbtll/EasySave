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
    /// <summary>
    /// Opening a file explorer to select the source folder
    /// </summary>
    #region BrowseSource_Click
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
            vm.ValidateSourcePathOnLostFocus();
        }
    }
    #endregion

    /// <summary>
    /// Opening a file explorer to select the destination folder 
    /// </summary>
    #region BrowseDestination_Click
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
            vm.ValidateDestinationPathOnLostFocus();
        }
    }
    #endregion

    /// <summary>
    /// Loss of focus on the target area
    /// </summary>
    #region Source_LostFocus
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
    #endregion
}

