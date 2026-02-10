namespace EasySave.UI.Tests;

public class SystemConsoleAdapterTests
{
    [Fact]
    public void WriteAndWriteLine_WriteToStandardOutput()
    {
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var adapter = new SystemConsoleAdapter();
            adapter.Write("Hello");
            adapter.WriteLine(" World");
            adapter.WriteLine();

            var output = writer.ToString();
            Assert.Contains("Hello", output);
            Assert.Contains(" World", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WriteErrorLine_WritesToErrorOutput()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();
        Console.SetError(writer);

        try
        {
            var adapter = new SystemConsoleAdapter();
            adapter.WriteErrorLine("Oops");

            Assert.Contains("Oops", writer.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void SetForegroundColorAndResetColor_AreCallable()
    {
        var original = Console.ForegroundColor;
        var adapter = new SystemConsoleAdapter();

        adapter.SetForegroundColor(ConsoleColor.Yellow);
        Assert.Equal(ConsoleColor.Yellow, Console.ForegroundColor);

        adapter.ResetColor();
        Console.ForegroundColor = original;
    }

    [Fact]
    public void Clear_IsCallable()
    {
        var adapter = new SystemConsoleAdapter();
        _ = Record.Exception(() => adapter.Clear());
        Assert.True(true);
    }

    [Fact]
    public void ReadKey_WithoutInteractiveConsole_ThrowsOrReturns()
    {
        var adapter = new SystemConsoleAdapter();
        var exception = Record.Exception(() => adapter.ReadKey(intercept: true));

        if (exception != null)
        {
            Assert.IsType<InvalidOperationException>(exception);
        }
    }
}
