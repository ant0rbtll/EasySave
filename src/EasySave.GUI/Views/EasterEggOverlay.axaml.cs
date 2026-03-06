using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace EasySave.GUI.Views;

/// <summary>
/// Overlay that displays an Easter-egg GIF.
/// Call <see cref="Show"/> to make it visible and <see cref="Hide"/> to dismiss it.
/// Clicking anywhere on the overlay or pressing Escape also dismisses it.
/// </summary>
public partial class EasterEggOverlay : UserControl
{
    public EasterEggOverlay()
    {
        InitializeComponent();
        PointerPressed += OnOverlayClicked;
    }

    /// <summary>Shows the overlay.</summary>
    public void Show()
    {
        IsVisible = true;
        Focus();
    }

    /// <summary>Hides the overlay.</summary>
    public void Hide()
    {
        IsVisible = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnOverlayClicked(object? sender, PointerPressedEventArgs e)
    {
        Hide();
        e.Handled = true;
    }
}
