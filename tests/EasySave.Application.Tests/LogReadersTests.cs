namespace EasySave.Application.Tests;

public class LogReadersTests
{
    [Fact]
    public void JsonLogReader_WhenEventTypeIsNumeric_ThrowsInvalidDataException()
    {
        using var temp = new TempDirectory();
        string path = Path.Combine(temp.RootPath, "2026-02-11.json");
        File.WriteAllText(
            path,
            """
            [
              {
                "timestamp": "2026-02-11T13:12:50Z",
                "backupName": "job-json",
                "eventType": 1,
                "sourcePathUNC": "\\\\src",
                "destinationPathUNC": "\\\\dst",
                "fileSizeBytes": 10,
                "transferTimeMs": 20,
                "encryptionTimeMs": 0
              }
            ]
            """);

        var reader = new JsonLogReader();

        Assert.Throws<InvalidDataException>(() => reader.ReadEntries(path));
    }

    [Fact]
    public void JsonLogReader_WhenFileExceedsSizeLimit_ThrowsInvalidDataException()
    {
        using var temp = new TempDirectory();
        string path = Path.Combine(temp.RootPath, "2026-02-11.json");
        CreateLargeFile(path, 50L * 1024 * 1024 + 1);

        var reader = new JsonLogReader();

        Assert.Throws<InvalidDataException>(() => reader.ReadEntries(path));
    }

    [Fact]
    public void XmlLogReader_WhenFileExceedsSizeLimit_ThrowsInvalidDataException()
    {
        using var temp = new TempDirectory();
        string path = Path.Combine(temp.RootPath, "2026-02-11.xml");
        CreateLargeFile(path, 50L * 1024 * 1024 + 1);

        var reader = new XmlLogReader();

        Assert.Throws<InvalidDataException>(() => reader.ReadEntries(path));
    }

    private static void CreateLargeFile(string path, long length)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(length);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "easysave-log-reader-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Directory.Delete(RootPath, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
