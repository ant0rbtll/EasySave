namespace EasySave.UI.Tests;

public class ConsoleInputServiceAdditionalTests
{
    [Fact]
    public void AskString_WithWhitespaceThenValid_RePromptsAndReturnsValidValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Spacebar, ' ');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        console.EnqueueKey(ConsoleKey.A, 'a');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskString(LocalizationKey.user_choice);

        Assert.Equal("a", result);
        Assert.Contains(message.Writes, call => call.Key == LocalizationKey.input_string_invalid);
    }

    [Fact]
    public void AskString_WithBackspace_EditsTypedValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.A, 'a');
        console.EnqueueKey(ConsoleKey.B, 'b');
        console.EnqueueKey(ConsoleKey.Backspace);
        console.EnqueueKey(ConsoleKey.C, 'c');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskString(LocalizationKey.user_choice);

        Assert.Equal("ac", result);
    }

    [Fact]
    public void AskStringWithCurrentValue_OnEnter_ReturnsCurrentValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskStringWithCurrentValue(LocalizationKey.user_choice, "current");

        Assert.Equal("current", result);
    }

    [Fact]
    public void AskStringWithCurrentValue_WithWhitespaceThenEscape_ReturnsNull()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Spacebar, ' ');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        console.EnqueueKey(ConsoleKey.Escape);
        var service = new ConsoleInputService(message, console);

        var result = service.AskStringWithCurrentValue(LocalizationKey.user_choice, "current");

        Assert.Null(result);
        Assert.Contains(message.Writes, call => call.Key == LocalizationKey.input_string_invalid);
    }

    [Fact]
    public void AskInt_WithInvalidThenValid_ReturnsParsedValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        console.EnqueueKey(ConsoleKey.D4, '4');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskInt(LocalizationKey.user_choice);

        Assert.Equal(4, result);
        Assert.Contains(message.Writes, call => call.Key == LocalizationKey.input_number_invalid);
    }

    [Fact]
    public void AskIntWithCurrentValue_OnEnter_ReturnsCurrentValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskIntWithCurrentValue(LocalizationKey.user_choice, 12);

        Assert.Equal(12, result);
    }

    [Fact]
    public void AskIntWithCurrentValue_WithInvalidThenValid_ReturnsParsedValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.OemMinus, '-');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        console.EnqueueKey(ConsoleKey.D7, '7');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskIntWithCurrentValue(LocalizationKey.user_choice, 3);

        Assert.Equal(7, result);
        Assert.Contains(message.Writes, call => call.Key == LocalizationKey.input_number_invalid);
    }

    [Fact]
    public void AskIntWithCurrentValue_WithEscape_ReturnsNull()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Escape);
        var service = new ConsoleInputService(message, console);

        var result = service.AskIntWithCurrentValue(LocalizationKey.user_choice, 10);

        Assert.Null(result);
    }

    [Fact]
    public void AskBackupType_WithInvalidChoiceThenValid_ReturnsSelectedType()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D9, '9');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        console.EnqueueKey(ConsoleKey.D1, '1');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskBackupType(LocalizationKey.user_choice);

        Assert.Equal(BackupType.Complete, result);
        Assert.Contains(message.Writes, call => call.Key == LocalizationKey.input_backuptype_invalid);
    }

    [Fact]
    public void AskBackupTypeWithCurrentValue_WithEscape_ReturnsNull()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Escape);
        var service = new ConsoleInputService(message, console);

        var result = service.AskBackupTypeWithCurrentValue(LocalizationKey.user_choice, BackupType.Differential);

        Assert.Null(result);
    }

    [Fact]
    public void AskBackupTypeWithCurrentValue_OnEnterKeepsCurrentValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskBackupTypeWithCurrentValue(LocalizationKey.user_choice, BackupType.Differential);

        Assert.Equal(BackupType.Differential, result);
    }

    [Fact]
    public void AskBackupTypeWithCurrentValue_WithInvalidThenValid_ReturnsSelectedValue()
    {
        var message = new FakeConsoleMessageService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D0, '0');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        console.EnqueueKey(ConsoleKey.D1, '1');
        console.EnqueueKey(ConsoleKey.Enter, '\n');
        var service = new ConsoleInputService(message, console);

        var result = service.AskBackupTypeWithCurrentValue(LocalizationKey.user_choice, BackupType.Differential);

        Assert.Equal(BackupType.Complete, result);
        Assert.Contains(message.Writes, call => call.Key == LocalizationKey.input_backuptype_invalid);
    }
}
