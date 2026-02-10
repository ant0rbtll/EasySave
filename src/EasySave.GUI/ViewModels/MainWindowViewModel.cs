using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySave.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SidebarViewModel Sidebar { get; }

    [ObservableProperty]
    private ViewModelBase currentPage;

    private readonly HomeViewModel _homeViewModel;
    private readonly CreateViewModel _createViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly LogViewModel _logViewModel;
    private readonly ConfigViewModel _configViewModel;
    private readonly IBackupJobRepository _backupJobRepository;

    public MainWindowViewModel(
        CreateViewModel createViewModel,
        ManageViewModel manageViewModel,
        LogViewModel logViewModel,
        ConfigViewModel configViewModel,
        SidebarViewModel sidebarViewModel,
        HomeViewModel homeViewModel
        )
    {
        _createViewModel = createViewModel;
        _manageViewModel = manageViewModel;
        _logViewModel = logViewModel;
        _configViewModel = configViewModel;
        _configViewModel.SetOnLanguageChanged(OnLanguageChanged);
        _homeViewModel = homeViewModel;

        Sidebar = sidebarViewModel;
        Sidebar.SetNavigateAction(Navigate);
        CurrentPage = _homeViewModel;
    }

    private void OnLanguageChanged()
    {
        Sidebar.RefreshTranslations();
        _configViewModel.RefreshTranslations();
        _homeViewModel.RefreshTranslations();
        _manageViewModel.RefreshTranslations();
    }
    public void Navigate(string page)
    {
        CurrentPage = page switch
        {
            "creation" => _createViewModel,
            "manage" => _manageViewModel,
            "log" => _logViewModel,
            "conf" => _configViewModel,
            _ => CurrentPage
        };
    }
}
