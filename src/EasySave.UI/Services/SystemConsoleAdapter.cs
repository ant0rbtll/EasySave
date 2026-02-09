namespace EasySave.UI.Services;

internal class SystemConsoleAdapter : IConsoleAdapter
{
    public void Clear() => Console.Clear();

    public void Write(string value) => Console.Write(value);

    public void WriteLine() => Console.WriteLine();

    public void WriteLine(string value) => Console.WriteLine(value);

    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public void SetForegroundColor(ConsoleColor color) => Console.ForegroundColor = color;

    public void ResetColor() => Console.ResetColor();

    public void WriteErrorLine(string value) => Console.Error.WriteLine(value);
}
