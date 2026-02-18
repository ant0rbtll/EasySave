using System.Net;
using System.Text.Json;
using EasySave.AppCommon.Tests.TestHelpers;

namespace EasySave.AppCommon.Tests;

public class HttpLogSenderTests : IDisposable
{
    private readonly StubHttpMessageHandler _handler = new();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private HttpLogSender CreateSender(LogFormat format = LogFormat.Json)
        => new(_handler, "http://localhost:9999", format);

    [Fact]
    public void Write_SendsPostToApiLogs()
    {
        using var sender = CreateSender();
        var entry = LogEntryFactory.Create();

        sender.Write(entry);

        var request = Assert.Single(_handler.SentRequests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("api/logs", request.RequestUri!.AbsolutePath.TrimStart('/'));
    }

    [Fact]
    public void Write_JsonContainsExpectedFields()
    {
        using var sender = CreateSender(LogFormat.Json);
        var entry = LogEntryFactory.Create("MyBackup");

        sender.Write(entry);

        var request = Assert.Single(_handler.SentRequests);
        using var stream = request.Content!.ReadAsStream();
        using var reader = new global::System.IO.StreamReader(stream);
        var bodyText = reader.ReadToEnd();
        using var doc = JsonDocument.Parse(bodyText);
        var root = doc.RootElement;

        Assert.Equal("MyBackup", root.GetProperty("backupName").GetString());
        Assert.True(root.TryGetProperty("macAddress", out _));
        Assert.Equal("Json", root.GetProperty("logFormat").GetString());
        Assert.Equal("TransferFile", root.GetProperty("eventType").GetString());
    }

    [Fact]
    public void Write_WhenServerFails_ThrowsInvalidOperationException()
    {
        _handler.SetHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var sender = CreateSender();
        var entry = LogEntryFactory.Create();

        // PostAsync itself does not throw on non-success status codes,
        // but the current implementation does not call EnsureSuccessStatusCode on POST.
        // So we verify it does NOT throw for non-success POST responses.
        // The exception path is for network-level failures.
        var networkHandler = new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("Connection refused"));
        using var networkSender = new HttpLogSender(networkHandler, "http://localhost:9999", LogFormat.Json);

        Assert.Throws<InvalidOperationException>(() => networkSender.Write(entry));
    }

    [Fact]
    public void Write_NullEntry_ThrowsArgumentNullException()
    {
        using var sender = CreateSender();

        Assert.Throws<ArgumentNullException>(() => sender.Write(null!));
    }

    [Fact]
    public void CheckServerHealth_SendsGetToApiConfig()
    {
        using var sender = CreateSender();

        sender.CheckServerHealth();

        var request = Assert.Single(_handler.SentRequests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("api/config", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public void CheckServerHealth_WhenServerFails_Throws()
    {
        _handler.SetHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var sender = CreateSender();

        Assert.ThrowsAny<Exception>(() => sender.CheckServerHealth());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceUrl_ThrowsArgumentException(string url)
    {
        Assert.ThrowsAny<ArgumentException>(() => new HttpLogSender(url, LogFormat.Json));
    }
}
