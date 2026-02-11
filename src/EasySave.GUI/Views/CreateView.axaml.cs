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
            Title = vm.BrowseSourceTitle
        };

        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
        {
            vm.SetSourcePathFromDialog(result);
        }
    }

    private async void BrowseDestination_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CreateViewModel vm)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = vm.BrowseDestinationTitle
        };

        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
        {
            vm.SetDestinationPathFromDialog(result);
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
