using System.Diagnostics;
using EasySave.Localization;

namespace EasySave.Backup;

/// <summary>
/// Default process runner for CryptoSoft.
/// </summary>
public sealed class ExternalCryptoProcessRunner : IExternalCryptoProcessRunner
{
    private const int ProcessPollDelayMs = 100;

    /// <inheritdoc />
    public ExternalCryptoProcessRunResult Run(
        string executablePath,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return ExternalCryptoProcessRunResult.Failure(-1, LocalizationKey.error_invalid_argument.ToString());
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ExternalCryptoProcessRunResult.Failure(-1, LocalizationKey.error_invalid_argument.ToString());
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add(filePath);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                return ExternalCryptoProcessRunResult.Failure(-1, LocalizationKey.error_encryption_failed.ToString());
            }
        }
        catch (Exception ex)
        {
            return ExternalCryptoProcessRunResult.Failure(ex.HResult, LocalizationKey.error_encryption_failed.ToString());
        }

        try
        {
            while (!process.WaitForExit(ProcessPollDelayMs))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            TryTerminateProcess(process);
            throw;
        }

        return process.ExitCode == 0
            ? ExternalCryptoProcessRunResult.Success()
            : ExternalCryptoProcessRunResult.Failure(process.ExitCode, LocalizationKey.error_encryption_failed.ToString());
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            process.WaitForExit(milliseconds: 1_000);
        }
        catch
        {
            // Best-effort kill during cancellation.
        }
    }
}
