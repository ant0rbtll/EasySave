namespace EasySave.UI.Services;

internal interface IConsoleAdapter
{
    void Clear();
    void Write(string value);
    void WriteLine();
    void WriteLine(string value);
    ConsoleKeyInfo ReadKey(bool intercept);
    void SetForegroundColor(ConsoleColor color);
    void ResetColor();
    void WriteErrorLine(string value);
}
