using Avalonia;
using Avalonia.Controls;

namespace EasySave.GUI.Views;

public partial class MainWindow : Window
{
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
    }
}
