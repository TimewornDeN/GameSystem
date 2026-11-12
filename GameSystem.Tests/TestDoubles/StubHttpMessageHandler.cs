using System.Net;
using System.Text;

namespace GameSystem.Tests.TestDoubles;

/// <summary>
/// Routes outgoing requests to canned JSON responses by exact relative path (including query string),
/// so HttpClient-based clients can be tested without touching the network.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responsesByPath = new(StringComparer.Ordinal);

    public StubHttpMessageHandler RespondWith(string pathAndQuery, string json)
    {
        _responsesByPath[pathAndQuery] = json;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string key = request.RequestUri!.PathAndQuery.TrimStart('/');

        if (!_responsesByPath.TryGetValue(key, out var json))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"No stub registered for '{key}'.")
            });
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
