using System.Net;
using System.Text.Json;
using Xunit;

namespace Texnomic.Curl.Impersonate.Tests;

/// <summary>
/// Live HTTP tests, opt-in via the <c>E2E</c> trait so CI does not depend on
/// external services by default. Run them with
/// <c>dotnet test --filter "Category=E2E"</c>.
/// </summary>
[Trait("Category", "E2E")]
public class CurlHttpClientE2ETests
{
    [Fact]
    public async Task GetAsync_PublicEndpoint_Returns200()
    {
        using var Client = new CurlHttpClient(ImpersonateTarget.Chrome146);

        using var Response = await Client.GetAsync("https://www.example.com/");

        Assert.Equal(HttpStatusCode.OK, Response.StatusCode);
    }

    [Fact]
    public async Task GetAsync_TlsFingerprintEndpoint_ReturnsBrowserUserAgent()
    {
        using var Client = new CurlHttpClient(ImpersonateTarget.Chrome146);

        using var Response = await Client.GetAsync("https://tls.peet.ws/api/all");

        Response.EnsureSuccessStatusCode();

        await using var Stream = await Response.Content.ReadAsStreamAsync();
        using var Document = await JsonDocument.ParseAsync(Stream);

        var UserAgent = Document.RootElement.GetProperty("user_agent").GetString();
        Assert.NotNull(UserAgent);
        Assert.Contains("Chrome", UserAgent);
    }
}