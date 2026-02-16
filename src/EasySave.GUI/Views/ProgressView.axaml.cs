using System;
using Avalonia.Controls;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI.Views;

public partial class ProgressView : UserControl
{
    private ProgressViewModel? _activeViewModel;

    public ProgressView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        SwitchPollingViewModel(DataContext as ProgressViewModel);
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        SwitchPollingViewModel(null);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (VisualRoot is not null)
            SwitchPollingViewModel(DataContext as ProgressViewModel);
    }

    private void SwitchPollingViewModel(ProgressViewModel? next)
    {
        if (ReferenceEquals(_activeViewModel, next))
            return;

        _activeViewModel?.StopLiveRefresh();
        _activeViewModel = next;
        _activeViewModel?.StartLiveRefresh();
    }
}
