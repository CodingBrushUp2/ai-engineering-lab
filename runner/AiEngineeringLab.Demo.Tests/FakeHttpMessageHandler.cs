using System.Net;
using System.Text;

namespace AiEngineeringLab.Demo.Tests;

/// <summary>Records the outgoing request and returns a canned response. No network access.</summary>
internal sealed class FakeHttpMessageHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string? RequestBody { get; private set; }
    public CancellationToken ObservedToken { get; private set; }

    public static FakeHttpMessageHandler Returning(HttpStatusCode status, string body) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        }));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        ObservedToken = cancellationToken;
        return await respond(cancellationToken);
    }
}
