using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySave.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SidebarViewModel Sidebar { get; }

    [ObservableProperty]
    private ViewModelBase currentPage;

    [ObservableProperty]
    private string pageVerticalScrollBarVisibility = "Auto";

    private readonly HomeViewModel _homeViewModel;
    private readonly CreateViewModel _createViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly LogViewModel _logViewModel;
    private readonly ConfigViewModel _configViewModel;

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

        _createViewModel.OnJobCreated = () => _manageViewModel.LoadJobsCommand.Execute(null);

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
        _createViewModel.RefreshTranslations();
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

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        // Only ManageViewModel owns internal scroll regions (jobs list + editor modal),
        // so we disable outer scrolling there to avoid nested-scroll conflicts.
        // Other pages currently rely on the global page scroll.
        PageVerticalScrollBarVisibility = value is ManageViewModel ? "Disabled" : "Auto";
    }
}
