namespace EasySave.UI.Services;

/// <summary>
/// Abstracts console I/O to simplify testing of interactive flows.
/// </summary>
internal interface IConsoleAdapter
{
    /// <summary>
    /// Clears the console buffer and viewport.
    /// </summary>
    void Clear();

    /// <summary>
    /// Writes text without line termination.
    /// </summary>
    /// <param name="value">Text to write.</param>
    void Write(string value);

    /// <summary>
    /// Writes a line terminator.
    /// </summary>
    void WriteLine();

    /// <summary>
    /// Writes text followed by a line terminator.
    /// </summary>
    /// <param name="value">Text to write.</param>
    void WriteLine(string value);

    /// <summary>
    /// Reads one key from input.
    /// </summary>
    /// <param name="intercept">Whether the key should be hidden from output.</param>
    /// <returns>The read key information.</returns>
    ConsoleKeyInfo ReadKey(bool intercept);

    /// <summary>
    /// Sets the current foreground color.
    /// </summary>
    /// <param name="color">Foreground color to apply.</param>
    void SetForegroundColor(ConsoleColor color);

    /// <summary>
    /// Restores default console colors.
    /// </summary>
    void ResetColor();

    /// <summary>
    /// Writes one line to the error output stream.
    /// </summary>
    /// <param name="value">Text to write.</param>
    void WriteErrorLine(string value);
}
