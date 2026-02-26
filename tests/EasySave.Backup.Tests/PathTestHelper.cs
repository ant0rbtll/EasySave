namespace EasySave.Backup.Tests;

internal static class PathTestHelper
{
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }

    public static bool Equal(string? actual, string expected)
    {
        return actual is not null
            && string.Equals(Normalize(actual), Normalize(expected), StringComparison.OrdinalIgnoreCase);
    }
}
