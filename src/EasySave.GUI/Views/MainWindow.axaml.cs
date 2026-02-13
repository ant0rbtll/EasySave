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
            TitleBarBrand.Margin = new Thickness(70, -2, 0, 0);
        }
    }
}
