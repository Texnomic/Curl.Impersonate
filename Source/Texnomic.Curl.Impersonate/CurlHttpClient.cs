using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Texnomic.Curl.Impersonate;

/// <summary>
/// HTTP client backed by <c>libcurl-impersonate</c>. Each request is issued
/// through a fresh easy handle so callers can use a single instance from
/// multiple threads. The class is thread-safe for sending requests and
/// reading <see cref="BaseAddress"/>; modify <see cref="DefaultHeaders"/>
/// before issuing requests.
/// </summary>
/// <example>
/// <code>
/// using var Client = new CurlHttpClient(ImpersonateTarget.Chrome146);
/// using var Response = await Client.GetAsync("https://tls.peet.ws/api/all");
/// Response.EnsureSuccessStatusCode();
/// </code>
/// </example>
public sealed class CurlHttpClient : IDisposable
{
    private static int _initialized;
    private static readonly object _initLock = new();

    private readonly ImpersonateTarget Target;
    private bool _disposed;

    /// <summary>
    /// Base URI prepended to relative request URIs. Mirrors
    /// <see cref="HttpClient.BaseAddress"/>.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Headers attached to every outgoing request. Per-request headers on the
    /// <see cref="HttpRequestMessage"/> are appended after these.
    /// </summary>
    public IDictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the TLS peer and host name are verified. Defaults to
    /// <see langword="true"/>. Disable only for trusted hosts with self-signed
    /// certificates.
    /// </summary>
    public bool VerifySsl { get; set; } = true;

    /// <summary>
    /// Total request timeout. Defaults to <see cref="TimeSpan.Zero"/> meaning
    /// libcurl's default (no timeout). Honoured by libcurl's internal timer in
    /// addition to any <see cref="CancellationToken"/> passed to a send call.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Maximum time spent establishing the TCP/TLS connection. Defaults to
    /// <see cref="TimeSpan.Zero"/> (libcurl default, 300 s).
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Initializes a new client with the requested browser fingerprint.
    /// </summary>
    /// <param name="Target">
    /// Browser identity to impersonate. Defaults to
    /// <see cref="ImpersonateTarget.Chrome146"/>.
    /// </param>
    public CurlHttpClient(ImpersonateTarget Target = default)
    {
        this.Target = Target == default ? ImpersonateTarget.Chrome146 : Target;

        EnsureGloballyInitialized();
    }

    private static void EnsureGloballyInitialized()
    {
        if (Volatile.Read(ref _initialized) == 1)
            return;

        lock (_initLock)
        {
            if (_initialized == 1)
                return;

            var Result = CurlNative.GlobalInitialize(CurlNative.GlobalDefault);

            if (Result != 0)
                throw new InvalidOperationException($"curl_global_init failed with code {Result}.");

            Volatile.Write(ref _initialized, 1);
        }
    }

    /// <summary>
    /// Issues a request, returning the response synchronously. Most callers
    /// should prefer <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
    /// </summary>
    public HttpResponseMessage Send(HttpRequestMessage Request, CancellationToken Token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(Request);

        Token.ThrowIfCancellationRequested();

        var RequestUri = ResolveRequestUri(Request);

        using var Handle = new CurlEasyHandle();

        Handle.SetUrl(RequestUri);
        Handle.SetTarget(Target);
        Handle.SetSslVerification(VerifySsl);
        Handle.SetAcceptEncoding();

        if (Timeout > TimeSpan.Zero)
            Handle.SetTimeout(Timeout);

        if (ConnectTimeout > TimeSpan.Zero)
            Handle.SetConnectTimeout(ConnectTimeout);

        Handle.AddHeaders(DefaultHeaders);
        Handle.AddHeaders(Request.Headers);

        string? Body = null;

        if (Request.Content is not null)
        {
            Handle.AddHeaders(Request.Content.Headers);
            Body = Request.Content.ReadAsStringAsync(Token).GetAwaiter().GetResult();
        }

        Handle.SetMethod(Request.Method, Body);

        return Handle.Execute(Request, Token);
    }

    /// <summary>
    /// Asynchronously issues a request. The work runs on the thread pool; the
    /// returned task is cancelled when <paramref name="Token"/> is signalled.
    /// </summary>
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage Request, CancellationToken Token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(Request);

        return Task.Run(() => Send(Request, Token), Token);
    }

    /// <summary>Sends a GET request to <paramref name="RequestUri"/>.</summary>
    public Task<HttpResponseMessage> GetAsync(string RequestUri, CancellationToken Token = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), Token);

    /// <summary>Sends a GET request to <paramref name="RequestUri"/>.</summary>
    public Task<HttpResponseMessage> GetAsync(Uri RequestUri, CancellationToken Token = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), Token);

    /// <summary>Sends a POST request with the supplied content.</summary>
    public Task<HttpResponseMessage> PostAsync(string RequestUri, HttpContent Content, CancellationToken Token = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, RequestUri) { Content = Content }, Token);

    /// <summary>
    /// Serializes <paramref name="Value"/> as JSON and sends it as the body of a
    /// POST request.
    /// </summary>
    public Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string RequestUri,
        T Value,
        JsonSerializerOptions? Options = null,
        CancellationToken Token = default)
    {
        var Request = new HttpRequestMessage(HttpMethod.Post, RequestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(Value, Options), Encoding.UTF8, "application/json"),
        };

        return SendAsync(Request, Token);
    }

    private Uri ResolveRequestUri(HttpRequestMessage Request)
    {
        var RequestUri = Request.RequestUri;

        if (RequestUri is { IsAbsoluteUri: true })
            return RequestUri;

        if (BaseAddress is null)
            throw new InvalidOperationException(
                "Request URI is relative and CurlHttpClient.BaseAddress is null.");

        return new Uri(BaseAddress, RequestUri?.ToString() ?? string.Empty);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Per-instance disposal is a no-op: libcurl's global state is owned
        // process-wide. See CurlGlobalShutdown to release it on shutdown if
        // the host needs deterministic cleanup before AppDomain unload.
    }

    /// <summary>
    /// Releases libcurl's process-wide global state. Call this once at
    /// application shutdown if you need deterministic cleanup. After calling
    /// this, no <see cref="CurlHttpClient"/> instance may be used again.
    /// </summary>
    public static void GlobalShutdown()
    {
        lock (_initLock)
        {
            if (_initialized == 0)
                return;

            CurlNative.GlobalCleanup();
            Volatile.Write(ref _initialized, 0);
        }
    }
}
