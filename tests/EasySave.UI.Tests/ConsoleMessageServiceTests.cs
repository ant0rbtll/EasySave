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

        var exception = new InvalidOperationException("ignored");
        exception.Data["errorKey"] = "error_add_max";
        exception.Data["b"] = "2";
        exception.Data["a"] = "1";

        service.ShowError(exception);

        Assert.Contains("WL:Error", console.Events);
        Assert.Contains("WL:error_add_max:1,2", console.Events);
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
