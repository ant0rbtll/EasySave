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
            ExtendClientAreaTitleBarHeightHint = 32;
            MainLayoutGrid.RowDefinitions = new RowDefinitions("32,*");
            TitleBarLogo.IsVisible = false;
            TitleBarBrand.Spacing = 0;
            TitleBarBrand.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            TitleBarBrand.Margin = new Thickness(0, -2, 0, 0);
            TitleBarBrand.SetValue(Grid.ColumnProperty, 0);
            TitleBarBrand.SetValue(Grid.ColumnSpanProperty, 4);
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
            if (e.Property == WindowStateProperty && e.NewValue is WindowState state)
                UpdateMaximizeIcon(state);
        };

        UpdateMaximizeIcon(WindowState);
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
}
