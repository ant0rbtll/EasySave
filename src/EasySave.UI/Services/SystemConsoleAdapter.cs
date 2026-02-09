namespace EasySave.UI.Services;

/// <summary>
/// Production console adapter delegating directly to <see cref="Console"/>.
/// </summary>
internal class SystemConsoleAdapter : IConsoleAdapter
{
    /// <inheritdoc />
    public void Clear() => Console.Clear();

    /// <inheritdoc />
    public void Write(string value) => Console.Write(value);

    /// <inheritdoc />
    public void WriteLine() => Console.WriteLine();

    /// <inheritdoc />
    public void WriteLine(string value) => Console.WriteLine(value);

    /// <inheritdoc />
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    /// <inheritdoc />
    public void SetForegroundColor(ConsoleColor color) => Console.ForegroundColor = color;

    /// <inheritdoc />
    public void ResetColor() => Console.ResetColor();

    /// <inheritdoc />
    public void WriteErrorLine(string value) => Console.Error.WriteLine(value);
}
