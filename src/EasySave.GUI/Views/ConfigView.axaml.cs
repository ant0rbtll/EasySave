using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EasySave.GUI.ViewModels;

namespace EasySave.GUI.Views;

public partial class ConfigView : UserControl
{
    public ConfigView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ConfigViewModel vm)
        {
            vm.BrowseFolder = BrowseFolderAsync;
        }
    }

    private async Task<string?> BrowseFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false
            });

#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections
        return folders.FirstOrDefault()?.Path.LocalPath;
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections
    }
}
