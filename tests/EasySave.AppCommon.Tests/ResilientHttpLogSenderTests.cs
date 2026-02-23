using System.Net;
using EasySave.AppCommon.Tests.TestHelpers;
using EasySave.Exceptions;

namespace EasySave.AppCommon.Tests;

public class ResilientHttpLogSenderTests
{
    private static HttpLogSender CreateHttpSender(StubHttpMessageHandler handler)
        => new(handler, "http://localhost:9999", LogFormat.Json);

    /// <summary>
    /// Creates a ResilientHttpLogSender, waits for the initial health check tick to complete,
    /// then returns the instance. The caller is responsible for disposing.
    /// </summary>
    private static ResilientHttpLogSender CreateResilient(
        StubHttpMessageHandler handler,
        LogServerStatusNotifier notifier,
        ILogger? fallback = null)
    {
        var httpSender = CreateHttpSender(handler);
        var resilient = new ResilientHttpLogSender(httpSender, notifier, fallback);
        // Let the initial health check tick (TimeSpan.Zero) complete on the ThreadPool.
        Thread.Sleep(300);
        return resilient;
    }

    [Fact]
    public void Write_WhenServerUp_ReportsStatusTrue()
    {
        var handler = new StubHttpMessageHandler();
        var notifier = new LogServerStatusNotifier();
        using var resilient = CreateResilient(handler, notifier);

        resilient.Write(LogEntryFactory.Create());

        Assert.True(notifier.IsServerReachable);
    }

    [Fact]
    public void Write_WhenServerDown_FallsBackAndReportsStatusFalse()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            // Health check GET succeeds, but POST fails
            if (request.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK);
            throw new HttpRequestException("Connection refused");
        });
        var notifier = new LogServerStatusNotifier();
        var fallback = new SpyLogger();
        using var resilient = CreateResilient(handler, notifier, fallback);
        var entry = LogEntryFactory.Create();

        resilient.Write(entry);

        Assert.False(notifier.IsServerReachable);
        Assert.Single(fallback.WrittenEntries);
        Assert.Same(entry, fallback.WrittenEntries[0]);
    }

    [Fact]
    public void Write_WhenServerDownAndNoFallback_DoesNotThrow()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK);
            throw new HttpRequestException("Connection refused");
        });
        var notifier = new LogServerStatusNotifier();
        using var resilient = CreateResilient(handler, notifier, fallback: null);

        var exception = Record.Exception(() => resilient.Write(LogEntryFactory.Create()));

        Assert.Null(exception);
        Assert.False(notifier.IsServerReachable);
    }

    [Fact]
    public void Write_NullEntry_ThrowsArgumentNullException()
    {
        var handler = new StubHttpMessageHandler();
        var notifier = new LogServerStatusNotifier();
        using var resilient = CreateResilient(handler, notifier);

        Assert.Throws<InvalidArgumentException>(() => resilient.Write(null!));
    }

    [Fact]
    public void HealthCheck_InitialTick_CallsApiConfig()
    {
        var handler = new StubHttpMessageHandler();
        var notifier = new LogServerStatusNotifier();
        using var resilient = CreateResilient(handler, notifier);

        // The initial tick (TimeSpan.Zero) already fired during CreateResilient
        Assert.Contains(handler.SentRequests, r =>
            r.Method == HttpMethod.Get &&
            r.RequestUri!.AbsolutePath.Contains("api/config"));
    }

    [Fact]
    public void HealthCheck_WhenServerDown_ReportsStatusFalse()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("Connection refused"));
        var notifier = new LogServerStatusNotifier();
        using var resilient = CreateResilient(handler, notifier);

        // Initial tick failed → status is false
        Assert.False(notifier.IsServerReachable);
    }

    [Fact]
    public void HealthCheck_WhenServerRecovers_ReportsStatusTrue()
    {
        // Start with server down
        var handler = new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("Connection refused"));
        var notifier = new LogServerStatusNotifier();
        using var resilient = CreateResilient(handler, notifier);
        Assert.False(notifier.IsServerReachable);

        // Server comes back up — next Write succeeds
        handler.SetHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        resilient.Write(LogEntryFactory.Create());

        Assert.True(notifier.IsServerReachable);
    }

    [Fact]
    public void Dispose_DisposesInnerResources()
    {
        var handler = new StubHttpMessageHandler();
        var notifier = new LogServerStatusNotifier();
        var fallback = new SpyLogger();
        var resilient = CreateResilient(handler, notifier, fallback);

        resilient.Dispose();

        Assert.True(fallback.Disposed);
    }
}
