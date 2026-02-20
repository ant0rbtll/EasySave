using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private void PlayJob_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button
            && TryGetJobId(button.CommandParameter, out var jobId)
            && ResolveViewModel() is { } viewModel)
        {
            viewModel.PlayJobCommand.Execute(jobId);
        }
    }

    private void PauseJob_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button
            && TryGetJobId(button.CommandParameter, out var jobId)
            && ResolveViewModel() is { } viewModel)
        {
            viewModel.PauseJobCommand.Execute(jobId);
        }
    }

    private void ItemCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        if (ResolveViewModel() is { } viewModel)
            viewModel.OnItemSelectionChanged();
    }

    private void StopJob_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button
            && TryGetJobId(button.CommandParameter, out var jobId)
            && ResolveViewModel() is { } viewModel)
        {
            viewModel.AskStopJobCommand.Execute(jobId);
        }
    }

    private ProgressViewModel? ResolveViewModel()
    {
        return _activeViewModel ?? DataContext as ProgressViewModel;
    }

    private static bool TryGetJobId(object? rawValue, out int jobId)
    {
        switch (rawValue)
        {
            case int intId:
                jobId = intId;
                return true;
            case long longId when longId >= int.MinValue && longId <= int.MaxValue:
                jobId = (int)longId;
                return true;
            case string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                jobId = parsed;
                return true;
            default:
                jobId = default;
                return false;
        }
    }
}
