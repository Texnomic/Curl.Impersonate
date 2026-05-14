using System.Net.Http;
using Xunit;

namespace Texnomic.Curl.Impersonate.Tests;

public class CurlHttpClientTests
{
    [Fact]
    public void Constructor_DefaultsToChrome146()
    {
        using var Client = new CurlHttpClient();

        Assert.True(Client.VerifySsl);
        Assert.Equal(TimeSpan.Zero, Client.Timeout);
    }

    [Fact]
    public void Send_NullRequest_Throws()
    {
        using var Client = new CurlHttpClient();

        Assert.Throws<ArgumentNullException>(() => Client.Send(null!));
    }

    [Fact]
    public void Send_AfterDispose_Throws()
    {
        var Client = new CurlHttpClient();
        Client.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            Client.Send(new HttpRequestMessage(HttpMethod.Get, "https://example.com")));
    }

    [Fact]
    public void Send_RelativeUriWithoutBaseAddress_Throws()
    {
        using var Client = new CurlHttpClient();

        Assert.Throws<InvalidOperationException>(() =>
            Client.Send(new HttpRequestMessage(HttpMethod.Get, "/relative")));
    }

    [Fact]
    public async Task SendAsync_PrecancelledToken_Throws()
    {
        using var Client = new CurlHttpClient();
        using var Cts = new CancellationTokenSource();
        Cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), Cts.Token));
    }
}
