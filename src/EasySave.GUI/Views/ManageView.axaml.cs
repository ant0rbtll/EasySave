using Avalonia.Controls;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI.Views;

public partial class ManageView : UserControl
{
    public ManageView()
    {
        InitializeComponent();
    }

    private async void EditBrowseSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ManageViewModel mvm || mvm.EditingJob is not { } vm)
            return;

        var dialog = new OpenFolderDialog { Title = vm.BrowseSourceTitle };
        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
            vm.SetSourcePathFromDialog(result);
    }

    private async void EditBrowseDestination_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ManageViewModel mvm || mvm.EditingJob is not { } vm)
            return;

        var dialog = new OpenFolderDialog { Title = vm.BrowseDestinationTitle };
        var result = await dialog.ShowAsync((Window)this.VisualRoot!);

        if (!string.IsNullOrWhiteSpace(result))
            vm.SetDestinationPathFromDialog(result);
    }

    private void EditName_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ManageViewModel mvm && mvm.EditingJob is { } vm)
            vm.ValidateNameOnLostFocus();
    }

    private void EditSource_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ManageViewModel mvm && mvm.EditingJob is { } vm)
            vm.ValidateSourcePathOnLostFocus();
    }

    private void EditDestination_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ManageViewModel mvm && mvm.EditingJob is { } vm)
            vm.ValidateDestinationPathOnLostFocus();
    }

    private void JobCheckBox_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ManageViewModel mvm)
            mvm.OnJobSelectionChanged();
    }
}
