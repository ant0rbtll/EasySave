namespace EasySave.GUI.Helpers;

/// <summary>
/// Converts raw log values (durations and sizes) into compact human-readable strings.
/// </summary>
public static class LogValueFormatter
{
    public static string FormatDuration(long milliseconds)
    {
        if (milliseconds < 0)
        {
            return "Error";
        }

        if (milliseconds < 1000)
        {
            return $"{milliseconds} ms";
        }

        if (milliseconds < 60_000)
        {
            return $"{milliseconds / 1000d:0.##} s";
        }

        if (milliseconds < 3_600_000)
        {
            return $"{milliseconds / 60_000d:0.##} min";
        }

        if (milliseconds < 86_400_000)
        {
            return $"{milliseconds / 3_600_000d:0.##} h";
        }

        return $"{milliseconds / 86_400_000d:0.##} d";
    }

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 0)
        {
            return "Error";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
