using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SidebarViewModel Sidebar { get; }

    [ObservableProperty]
    private ViewModelBase currentPage;

    [ObservableProperty]
    private bool useGlobalScroll;

    public ViewModelBase? ScrollablePage => UseGlobalScroll ? CurrentPage : null;

    public ViewModelBase? FixedPage => UseGlobalScroll ? null : CurrentPage;

    [ObservableProperty]
    private string tooltipMinimize = string.Empty;

    [ObservableProperty]
    private string tooltipMaximize = string.Empty;

    [ObservableProperty]
    private string tooltipClose = string.Empty;

    private readonly ILocalizationService _localizationService;
    private readonly HomeViewModel _homeViewModel;
    private readonly CreateViewModel _createViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly ProgressViewModel _progressViewModel;
    private readonly LogViewModel _logViewModel;
    private readonly ConfigViewModel _configViewModel;

    private string _tooltipMaximizeText = string.Empty;
    private string _tooltipRestoreText = string.Empty;

    public MainWindowViewModel(
        CreateViewModel createViewModel,
        ManageViewModel manageViewModel,
        ProgressViewModel progressViewModel,
        LogViewModel logViewModel,
        ConfigViewModel configViewModel,
        SidebarViewModel sidebarViewModel,
        HomeViewModel homeViewModel,
        ILocalizationService localizationService
        )
    {
        _createViewModel = createViewModel;
        _manageViewModel = manageViewModel;
        _progressViewModel = progressViewModel;
        _logViewModel = logViewModel;
        _logViewModel.SetOnLanguageChanged(OnLanguageChanged);
        _configViewModel = configViewModel;
        _configViewModel.SetOnLanguageChanged(OnLanguageChanged);
        _homeViewModel = homeViewModel;
        _localizationService = localizationService;

        _createViewModel.OnJobCreated = () => _manageViewModel.LoadJobsCommand.Execute(null);

        Sidebar = sidebarViewModel;
        Sidebar.SetNavigateAction(Navigate);
        CurrentPage = _homeViewModel;

        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        TooltipMinimize = _localizationService.TranslateText(LocalizationKey.gui_window_minimize);
        _tooltipMaximizeText = _localizationService.TranslateText(LocalizationKey.gui_window_maximize);
        _tooltipRestoreText = _localizationService.TranslateText(LocalizationKey.gui_window_restore);
        TooltipClose = _localizationService.TranslateText(LocalizationKey.gui_window_close);
        TooltipMaximize = _tooltipMaximizeText;
    }

    public void UpdateMaximizeTooltip(bool isMaximized)
    {
        TooltipMaximize = isMaximized ? _tooltipRestoreText : _tooltipMaximizeText;
    }

    private void OnLanguageChanged()
    {
        RefreshTranslations();
        Sidebar.RefreshTranslations();
        _configViewModel.RefreshTranslations();
        _logViewModel.RefreshTranslations();
        _homeViewModel.RefreshTranslations();
        _manageViewModel.RefreshTranslations();
        _progressViewModel.RefreshTranslations();
        _createViewModel.RefreshTranslations();
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

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        // Keep global scroll only on long form pages.
        // Other pages keep their own centered layout without a scroll container.
        var shouldUseGlobalScroll = value is CreateViewModel or ConfigViewModel;
        if (UseGlobalScroll != shouldUseGlobalScroll)
        {
            UseGlobalScroll = shouldUseGlobalScroll;
            return;
        }

        NotifyPageTargetsChanged();
    }

    partial void OnUseGlobalScrollChanged(bool value)
    {
        NotifyPageTargetsChanged();
    }

    private void NotifyPageTargetsChanged()
    {
        OnPropertyChanged(nameof(ScrollablePage));
        OnPropertyChanged(nameof(FixedPage));
    }
}
