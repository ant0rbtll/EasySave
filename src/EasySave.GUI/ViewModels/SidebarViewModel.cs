using CommunityToolkit.Mvvm.Input;

namespace EasySave.GUI.ViewModels;

public partial class SidebarViewModel(Action<string> navigate) : ViewModelBase
{
    private readonly Action<string> _navigate = navigate;

    [RelayCommand]
    private void GoToCreate() => _navigate("creation");

    [RelayCommand]
    private void GoToManage() => _navigate("manage");

    [RelayCommand]
    private void GoToProgress() => _navigate("progress");

    [RelayCommand]
    private void GoToLog() => _navigate("log");

    [RelayCommand]
    private void GoToConfig() => _navigate("conf");
}
