using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace EasySave.GUI.Views;

public partial class MainWindow : Window
{
    private const string MaximizePathData = "M4 4h16v16H4V4m2 2v12h12V6H6z";
    private const string RestorePathData = "M4 8h12v12H4V8m2 2v8h8v-8H6m4-6h10v10h-2V4H10V2z";

    public MainWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsMacOS())
        {
            TitleBarLogo.IsVisible = false;
            TitleBarBrand.Spacing = 0;
            TitleBarBrand.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            TitleBarBrand.Margin = new Thickness(0, -2, 0, 0);
        }
        ConfigurePlatformTitleBar();
    }

    private void ConfigurePlatformTitleBar()
    {
        if (OperatingSystem.IsLinux())
        {
            ConfigureLinux();
        }
    }

    private void ConfigureLinux()
    {
        SystemDecorations = SystemDecorations.BorderOnly;
        LinuxWindowButtons.IsVisible = true;

        MinimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        MaximizeButton.Click += (_, _) => ToggleMaximize();
        CloseButton.Click += (_, _) => Close();

        TitleBarGrid.PointerPressed += OnTitleBarPointerPressed;

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                UpdateMaximizeIcon((WindowState)e.NewValue!);
        };
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateMaximizeIcon(WindowState state)
    {
        var pathData = state == WindowState.Maximized ? RestorePathData : MaximizePathData;
        MaximizeIcon.Data = StreamGeometry.Parse(pathData);
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
}

