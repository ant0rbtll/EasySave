using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ILocalizationService _localizationService;

    [ObservableProperty] private string descriptionText = "";

    public HomeViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        DescriptionText = _localizationService.TranslateText(LocalizationKey.gui_home_description);
    }
}
