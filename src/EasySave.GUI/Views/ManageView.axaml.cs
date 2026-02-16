using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI.Views;

public partial class ManageView : UserControl
{
    private ManageViewModel? _activeViewModel;

    public ManageView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        SwitchPollingViewModel(DataContext as ManageViewModel);
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        SwitchPollingViewModel(null);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (VisualRoot is not null)
            SwitchPollingViewModel(DataContext as ManageViewModel);
    }

    private void SwitchPollingViewModel(ManageViewModel? next)
    {
        if (ReferenceEquals(_activeViewModel, next))
            return;

        _activeViewModel?.StopLiveRefresh();
        _activeViewModel = next;
        _activeViewModel?.StartLiveRefresh();
    }

    private async void EditBrowseSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ManageViewModel mvm || mvm.EditingJob is not { } vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = vm.BrowseSourceTitle,
            AllowMultiple = false
        });

        if (folders.Count > 0)
            vm.SetSourcePathFromDialog(folders[0].Path.LocalPath);
    }

    private async void EditBrowseDestination_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ManageViewModel mvm || mvm.EditingJob is not { } vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = vm.BrowseDestinationTitle,
            AllowMultiple = false
        });

        if (folders.Count > 0)
            vm.SetDestinationPathFromDialog(folders[0].Path.LocalPath);
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
