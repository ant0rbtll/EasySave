using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class SidebarViewModel : ViewModelBase
{
    private readonly Action<string> _navigate;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty] private string createLabel = "";
    [ObservableProperty] private string manageLabel = "";
    [ObservableProperty] private string progressLabel = "";
    [ObservableProperty] private string logLabel = "";
    [ObservableProperty] private string configLabel = "";

    public SidebarViewModel(Action<string> navigate, ILocalizationService localizationService)
    {
        _navigate = navigate;
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

    [RelayCommand] private void GoToCreate() => _navigate("creation");
    [RelayCommand] private void GoToManage() => _navigate("manage");
    [RelayCommand] private void GoToProgress() => _navigate("progress");
    [RelayCommand] private void GoToLog() => _navigate("log");
    [RelayCommand] private void GoToConfig() => _navigate("conf");
}
