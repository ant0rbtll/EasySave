using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Configuration;
using EasySave.Localization;
using EasySave.Persistence;

namespace EasySave.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SidebarViewModel Sidebar { get; }

    [ObservableProperty]
    private ViewModelBase currentPage;

    private readonly HomeViewModel _homeViewModel;
    private readonly CreateViewModel _createViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly ProgressViewModel _progressViewModel;
    private readonly LogViewModel _logViewModel;
    private readonly ConfigViewModel _configViewModel;

    public MainWindowViewModel(
        IUserPreferencesRepository preferencesRepository,
        ILocalizationService localizationService,
        IPathProvider pathProvider)
    {
        _homeViewModel = new HomeViewModel(localizationService);
        _createViewModel = new CreateViewModel();
        _manageViewModel = new ManageViewModel();
        _progressViewModel = new ProgressViewModel();
        _logViewModel = new LogViewModel();

        Sidebar = new SidebarViewModel(Navigate, localizationService);

        _configViewModel = new ConfigViewModel(
            preferencesRepository,
            localizationService,
            pathProvider,
            OnLanguageChanged);

        currentPage = _homeViewModel;
    }

    private void OnLanguageChanged()
    {
        Sidebar.RefreshTranslations();
        _configViewModel.RefreshTranslations();
        _homeViewModel.RefreshTranslations();
    }

    public void Navigate(string page)
    {
        CurrentPage = page switch
        {
            "creation" => _createViewModel,
            "manage" => _manageViewModel,
            "progress" => _progressViewModel,
            "log" => _logViewModel,
            "conf" => _configViewModel,
            _ => CurrentPage
        };
    }
}
