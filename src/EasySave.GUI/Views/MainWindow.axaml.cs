using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using EasySave.GUI.Services;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI.Views;

public partial class MainWindow : Window
{
    private readonly EasterEggKeyDetector _easterEggKeyDetector = new();
    private const string MaximizePathData = "M4 4h16v16H4V4m2 2v12h12V6H6z";
    private const string RestorePathData = "M4 8h12v12H4V8m2 2v8h8v-8H6m4-6h10v10h-2V4H10V2z";

    /// <summary>Thickness in pixels of the resize area along the window edges.</summary>
    private const int ResizeBorder = 6;
    private bool _isCloseConfirmed;
    private MainWindowViewModel? _subscribedViewModel;

    public MainWindow()
    {
        InitializeComponent();

        ConfigurePlatformTitleBar();
        ConfigureEasterEgg();
        Closing += OnWindowClosing;
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Wires the Easter-egg key sequence detector.
    /// Pressing 6 then 7 anywhere in the window shows the overlay.
    /// </summary>
    private void ConfigureEasterEgg()
    {
        _easterEggKeyDetector.SequenceDetected += () =>
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => EasterEggOverlay.Show());

        AddHandler(KeyDownEvent, (_, e) => _easterEggKeyDetector.OnKeyDown(e.Key),
            RoutingStrategies.Tunnel);
    }

    private void ConfigurePlatformTitleBar()
    {
        if (OperatingSystem.IsLinux())
        {
            ConfigureLinux();
            return;
        }
        else if (OperatingSystem.IsMacOS())
        {
            ConfigureMacOS();
            return;
        }

        // Keep native title bar on non-Unix platforms.
        ExtendClientAreaToDecorationsHint = false;
        SystemDecorations = SystemDecorations.Full;
        TitleBarGrid.IsVisible = false;
        MainLayoutGrid.RowDefinitions = new RowDefinitions("0,Auto,*");
    }

    private void ConfigureLinux()
    {
        ExtendClientAreaToDecorationsHint = false;
        SystemDecorations = SystemDecorations.BorderOnly;
        LinuxWindowButtons.IsVisible = true;

        MinimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        MaximizeButton.Click += (_, _) => ToggleMaximize();
        CloseButton.Click += (_, _) => Close();

        // Tunnel pointer event interception for custom edge resizing
        AddHandler(PointerPressedEvent, OnEdgePointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnEdgePointerMoved, RoutingStrategies.Tunnel);

        TitleBarGrid.PointerPressed += OnTitleBarPointerPressed;

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty && e.NewValue is WindowState state)
                UpdateMaximizeIcon(state);
        };

        UpdateMaximizeIcon(WindowState);
    }

    private void ConfigureMacOS()
    {
        ExtendClientAreaTitleBarHeightHint = 32;
        MainLayoutGrid.RowDefinitions = new RowDefinitions("32,Auto,*");
        TitleBarLogo.IsVisible = false;
        TitleBarBrand.Spacing = 0;
        TitleBarBrand.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        TitleBarBrand.Margin = new Thickness(0, -2, 0, 0);
        TitleBarBrand.SetValue(Grid.ColumnProperty, 0);
        TitleBarBrand.SetValue(Grid.ColumnSpanProperty, 4);
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateMaximizeIcon(WindowState state)
    {
        var isMaximized = state == WindowState.Maximized;
        MaximizeIcon.Data = StreamGeometry.Parse(isMaximized ? RestorePathData : MaximizePathData);
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.UpdateMaximizeTooltip(isMaximized);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        BeginMoveDrag(e);
    }

    /// <summary>
    /// Determines the window edge that matches the cursor position.
    /// Returns <c>null</c> if the cursor is not in the resize area
    /// or if the window is maximized.
    /// </summary>
    private WindowEdge? GetEdgeAtPosition(Point pos)
    {
        if (WindowState == WindowState.Maximized) return null;

        var w = ClientSize.Width;
        var h = ClientSize.Height;
        var top = pos.Y < ResizeBorder;
        var bottom = pos.Y > h - ResizeBorder;
        var left = pos.X < ResizeBorder;
        var right = pos.X > w - ResizeBorder;

        if (top && left) return WindowEdge.NorthWest;
        if (top && right) return WindowEdge.NorthEast;
        if (bottom && left) return WindowEdge.SouthWest;
        if (bottom && right) return WindowEdge.SouthEast;
        if (top) return WindowEdge.North;
        if (bottom) return WindowEdge.South;
        if (left) return WindowEdge.West;
        if (right) return WindowEdge.East;
        return null;
    }

    /// <summary>
    /// Starts native resizing when the user clicks a window edge.
    /// </summary>
    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var edge = GetEdgeAtPosition(e.GetPosition(this));
        if (edge is null) return;

        BeginResizeDrag(edge.Value, e);
        e.Handled = true;
    }

    /// <summary>
    /// Updates the cursor based on the hovered edge to visually indicate
    /// the possible resize direction.
    /// </summary>
    private void OnEdgePointerMoved(object? sender, PointerEventArgs e)
    {
        var edge = GetEdgeAtPosition(e.GetPosition(this));
        Cursor = edge switch
        {
            WindowEdge.North or WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
            WindowEdge.West or WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
            WindowEdge.NorthWest or WindowEdge.SouthEast => new Cursor(StandardCursorType.TopLeftCorner),
            WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(StandardCursorType.TopRightCorner),
            _ => Cursor.Default
        };
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isCloseConfirmed)
            return;

        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (!viewModel.HasRunningOrPausedBackups())
            return;

        e.Cancel = true;

        viewModel.OpenCloseConfirmationDialog();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(_subscribedViewModel, DataContext))
            return;

        if (_subscribedViewModel is not null)
            _subscribedViewModel.CloseConfirmed -= OnCloseConfirmed;

        _subscribedViewModel = DataContext as MainWindowViewModel;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.CloseConfirmed += OnCloseConfirmed;
    }

    private void OnCloseConfirmed(object? sender, EventArgs e)
    {
        _isCloseConfirmed = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_subscribedViewModel is not null)
            _subscribedViewModel.CloseConfirmed -= OnCloseConfirmed;
    }
}
