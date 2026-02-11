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
            Title = "Source..."
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
            Title = "Destination..."
        };

        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
        {
            vm.SetDestinationPathFromDialog(result);
        }
    }

    private void Name_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && DataContext is CreateViewModel vm)
            vm.Name = tb.Text ?? string.Empty;
    }

    private void Source_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && DataContext is CreateViewModel vm)
            vm.SourcePath = tb.Text ?? string.Empty;
    }

    private void Destination_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && DataContext is CreateViewModel vm)
            vm.DestinationPath = tb.Text ?? string.Empty;
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
