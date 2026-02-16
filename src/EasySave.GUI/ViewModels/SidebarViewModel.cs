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

    [ObservableProperty] private string createLabel = "";
    [ObservableProperty] private string manageLabel = "";
    [ObservableProperty] private string progressLabel = "";
    [ObservableProperty] private string logLabel = "";
    [ObservableProperty] private string configLabel = "";

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
    }

    [RelayCommand] private void GoToCreate() => _navigate?.Invoke("creation");
    [RelayCommand] private void GoToManage() => _navigate?.Invoke("manage");
    [RelayCommand] private void GoToProgress() => _navigate?.Invoke("progress");
    [RelayCommand] private void GoToLog() => _navigate?.Invoke("log");
    [RelayCommand] private void GoToConfig() => _navigate?.Invoke("conf");
}
