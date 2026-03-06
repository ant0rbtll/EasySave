using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace EasySave.GUI.Views;

/// <summary>
/// Overlay that displays an Easter-egg GIF.
/// Call <see cref="Show"/> to make it visible and <see cref="Hide"/> to dismiss it.
/// Clicking anywhere on the overlay or pressing Escape also dismisses it.
/// </summary>
public partial class EasterEggOverlay : UserControl
{
    private const string GifResourceUri = "avares://EasySave.GUI/Assets/EasterEgg/easteregg.gif";
    private bool _sourceLoaded;

    public EasterEggOverlay()
    {
        InitializeComponent();
        PointerPressed += OnOverlayClicked;
    }

    /// <summary>Shows the overlay.</summary>
    public void Show()
    {
        try
        {
            if (!_sourceLoaded)
            {
                EasterEggImage.Source = new Uri(GifResourceUri);
                _sourceLoaded = true;
            }

            IsVisible = true;
            Focus();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EasterEgg] Failed to show overlay: {ex.Message}");
        }
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
