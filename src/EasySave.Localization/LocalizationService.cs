using System.Security.Cryptography.X509Certificates;

namespace EasySave.Localization;

public class LocalizationService : ILocalizationService
{

    private string culture;
    public string getCulture()
    {
        return culture;
    }

    public void  setCulture(string culture)
    {
        this.culture = culture;
    }
}
