namespace EasySave.UI.Tests;

public class ConsoleInputServiceTests
{
    [Fact]
    public void AskString_ReturnsTypedValueOnEnter()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.A, 'a');
        console.EnqueueKey(ConsoleKey.B, 'b');
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new ConsoleInputService(message, console);

        var result = service.AskString(LocalizationKey.user_choice);

        Assert.Equal("ab", result);
    }

    [Fact]
    public void AskInt_ReturnsParsedNumber()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D4, '4');
        console.EnqueueKey(ConsoleKey.D2, '2');
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new ConsoleInputService(message, console);

        var result = service.AskInt(LocalizationKey.user_choice);

        Assert.Equal(42, result);
    }

    [Fact]
    public void AskInt_ReturnsNullOnEscape()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Escape);

        var service = new ConsoleInputService(message, console);

        var result = service.AskInt(LocalizationKey.user_choice);

        Assert.Null(result);
    }

    [Fact]
    public void AskBackupType_ReturnsSelectedBackupType()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D2, '2');
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new ConsoleInputService(message, console);

        var result = service.AskBackupType(LocalizationKey.user_choice);

        Assert.Equal(BackupType.Differential, result);
    }
}
