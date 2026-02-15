namespace EasySave.GUI.Helpers;

/// <summary>
/// Shared path validation utilities for form ViewModels.
/// </summary>
public static class PathValidation
{
    public static bool IsValidPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
            if (!Path.IsPathRooted(path)) return false;
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string ExamplePath(string folder) =>
        OperatingSystem.IsWindows()
            ? $"C:\\Users\\user\\{folder}"
            : $"/home/user/{folder}";
}
