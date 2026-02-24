using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class SidebarViewModel: ViewModelBase
{
    private Action<string>? _navigate;
    private readonly ILocalizationService _localizationService;

    public void SetNavigateAction(Action<string> navigate)
    {
        _navigate = navigate;
    }

    public void Navigate(string page)
    {
        _navigate?.Invoke(page);
    }

    private bool _isCollapsed;
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (_isCollapsed == value) return;
            _isCollapsed = value;
            OnPropertyChanged(nameof(IsCollapsed));
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    public bool IsExpanded => !_isCollapsed;

    private string _activePage = "home";
    public string ActivePage
    {
        get => _activePage;
        set
        {
            if (string.Equals(_activePage, value, StringComparison.Ordinal)) return;
            _activePage = value;
            OnPropertyChanged(nameof(ActivePage));
            OnPropertyChanged(nameof(IsCreateActive));
            OnPropertyChanged(nameof(IsManageActive));
            OnPropertyChanged(nameof(IsProgressActive));
            OnPropertyChanged(nameof(IsLogActive));
            OnPropertyChanged(nameof(IsConfigActive));
        }
    }

    public bool IsCreateActive => string.Equals(_activePage, "creation", StringComparison.Ordinal);
    public bool IsManageActive => string.Equals(_activePage, "manage", StringComparison.Ordinal);
    public bool IsProgressActive => string.Equals(_activePage, "progress", StringComparison.Ordinal);
    public bool IsLogActive => string.Equals(_activePage, "log", StringComparison.Ordinal);
    public bool IsConfigActive => string.Equals(_activePage, "conf", StringComparison.Ordinal);

    [ObservableProperty] private string createLabel = "";
    [ObservableProperty] private string manageLabel = "";
    [ObservableProperty] private string progressLabel = "";
    [ObservableProperty] private string logLabel = "";
    [ObservableProperty] private string configLabel = "";
    [ObservableProperty] private string toggleTooltip = "";

    public SidebarViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        CreateLabel = _localizationService.TranslateText(LocalizationKey.gui_sidebar_create);
        ManageLabel = _localizationService.TranslateText(LocalizationKey.gui_sidebar_manage);
        ProgressLabel = _localizationService.TranslateText(LocalizationKey.gui_sidebar_progress);
        LogLabel = _localizationService.TranslateText(LocalizationKey.gui_sidebar_log);
        ConfigLabel = _localizationService.TranslateText(LocalizationKey.gui_sidebar_config);
        ToggleTooltip = _localizationService.TranslateText(LocalizationKey.gui_sidebar_toggle);
    }

    [RelayCommand] private void GoToCreate() => _navigate?.Invoke("creation");
    [RelayCommand] private void GoToManage() => _navigate?.Invoke("manage");
    [RelayCommand] private void GoToProgress() => _navigate?.Invoke("progress");
    [RelayCommand] private void GoToLog() => _navigate?.Invoke("log");
    [RelayCommand] private void GoToConfig() => _navigate?.Invoke("conf");
}
