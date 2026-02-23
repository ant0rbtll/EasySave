using EasySave.Exceptions;

namespace EasySave.UI.Tests;

public class ConsoleMessageServiceTests
{
    [Fact]
    public void Write_WritesLocalizedLine()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.menu] = "Menu";
        var console = new FakeConsoleAdapter();
        var service = new ConsoleMessageService(localization, new ErrorManager(), console);

        service.Write(LocalizationKey.menu);

        Assert.Contains("WL:Menu", console.Events);
    }

    [Fact]
    public void ShowError_UsesMappedLocalizationKeyAndSortedParameters()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.error] = "Error";
        var console = new FakeConsoleAdapter();
        var service = new ConsoleMessageService(localization, new ErrorManager(), console);

        var exception = new EasysaveDefaultException(LocalizationKey.error_invalid_argument, new List<string>());
        exception.Options.Add("1");
        exception.Options.Add("2");

        service.ShowError(exception);

        Assert.Contains("WL:Error", console.Events);
        Assert.Contains("WL:error_invalid_argument", console.Events);
        Assert.Contains("FG:Red", console.Events);
        Assert.Contains("RESET", console.Events);
    }

    [Fact]
    public void ShowError_FallsBackToExceptionMessageWhenUnmapped()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.error] = "Error";
        var console = new FakeConsoleAdapter();
        var service = new ConsoleMessageService(localization, new ErrorManager(), console);

        service.ShowError(new Exception("raw failure"));

        Assert.Contains("WL:raw failure", console.Events);
    }
}
