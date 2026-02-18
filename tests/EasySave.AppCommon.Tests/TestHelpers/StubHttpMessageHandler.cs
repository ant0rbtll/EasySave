using System.Net;

namespace EasySave.AppCommon.Tests.TestHelpers;

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? handler = null) : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _handler = handler ?? (_ => new HttpResponseMessage(HttpStatusCode.OK));
    public List<HttpRequestMessage> SentRequests { get; } = [];

    public void SetHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        return Task.FromResult(_handler(request));
    }
}
