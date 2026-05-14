using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace Texnomic.Curl.Impersonate;

/// <summary>
/// Managed wrapper over a single <c>libcurl-impersonate</c> easy handle. One
/// handle services exactly one request and is disposed afterwards. The wrapper
/// owns the unmanaged handle, the curl <c>slist</c> for headers, and the GC
/// roots of the marshalled callback delegates.
/// </summary>
internal sealed class CurlEasyHandle : IDisposable
{
    private IntPtr Handle;
    private IntPtr HeaderList;

    // Held as fields so the GC does not collect them while libcurl holds the
    // function pointer. Releasing them at Dispose-time is sufficient.
    private CurlNative.DataCallback? WriteDelegate;
    private CurlNative.DataCallback? HeaderDelegate;
    private CurlNative.ProgressCallback? ProgressDelegate;

    private readonly MemoryStream ResponseBody = new();
    private readonly List<string> ResponseHeaders = [];

    public CurlEasyHandle()
    {
        Handle = CurlNative.CreateHandle();

        if (Handle == IntPtr.Zero)
            throw new InvalidOperationException("curl_easy_init returned null.");
    }

    private void Set(CurlNative.Option Option, string Value)
    {
        var Result = CurlNative.SetOption(Handle, Option, Value);
        ThrowIfFailed(Result, Option);
    }

    private void Set(CurlNative.Option Option, long Value)
    {
        var Result = CurlNative.SetOption(Handle, Option, Value);
        ThrowIfFailed(Result, Option);
    }

    private void Set(CurlNative.Option Option, IntPtr Value)
    {
        var Result = CurlNative.SetOption(Handle, Option, Value);
        ThrowIfFailed(Result, Option);
    }

    private static void ThrowIfFailed(int Result, CurlNative.Option Option)
    {
        if (Result == 0) return;

        var ErrorPtr = CurlNative.GetErrorMessage(Result);
        var ErrorMessage = Marshal.PtrToStringAnsi(ErrorPtr) ?? $"code {Result}";
        throw new InvalidOperationException(
            $"curl_easy_setopt({(int)Option}) failed: {ErrorMessage}");
    }

    public void SetUrl(Uri Url) => Set(CurlNative.Option.Url, Url.AbsoluteUri);

    public void SetTarget(ImpersonateTarget Target) => Set(CurlNative.Option.Impersonate, (string)Target);

    public void SetSslVerification(bool Enabled)
    {
        Set(CurlNative.Option.SslVerifyPeer, Enabled ? 1L : 0L);
        Set(CurlNative.Option.SslVerifyHost, Enabled ? 2L : 0L);
    }

    public void SetAcceptEncoding(string Encoding = "") =>
        Set(CurlNative.Option.AcceptEncoding, Encoding);

    public void SetTimeout(TimeSpan Timeout) =>
        Set(CurlNative.Option.TimeoutMs, (long)Timeout.TotalMilliseconds);

    public void SetConnectTimeout(TimeSpan Timeout) =>
        Set(CurlNative.Option.ConnectTimeoutMs, (long)Timeout.TotalMilliseconds);

    public void SetMethod(HttpMethod Method, string? Body = null)
    {
        if (Method == HttpMethod.Get)
            return;

        if (Method == HttpMethod.Post)
        {
            Set(CurlNative.Option.Post, 1L);
            Set(CurlNative.Option.CopyPostFields, Body ?? string.Empty);
        }
        else if (Method == HttpMethod.Put)
        {
            Set(CurlNative.Option.CustomRequest, "PUT");
            Set(CurlNative.Option.CopyPostFields, Body ?? string.Empty);
        }
        else if (Method == HttpMethod.Patch)
        {
            Set(CurlNative.Option.CustomRequest, "PATCH");
            Set(CurlNative.Option.CopyPostFields, Body ?? string.Empty);
        }
        else if (Method == HttpMethod.Delete)
        {
            Set(CurlNative.Option.CustomRequest, "DELETE");
            if (!string.IsNullOrEmpty(Body))
                Set(CurlNative.Option.CopyPostFields, Body);
        }
        else if (Method == HttpMethod.Head)
        {
            Set(CurlNative.Option.NoBody, 1L);
        }
        else
        {
            Set(CurlNative.Option.CustomRequest, Method.Method);
            if (!string.IsNullOrEmpty(Body))
                Set(CurlNative.Option.CopyPostFields, Body);
        }
    }

    public void AddHeader(string Name, string Value) =>
        HeaderList = CurlNative.AppendToList(HeaderList, $"{Name}: {Value}");

    public void AddHeaders(IEnumerable<KeyValuePair<string, string>> Headers)
    {
        foreach (var Header in Headers)
            AddHeader(Header.Key, Header.Value);
    }

    public void AddHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers)
    {
        foreach (var Header in Headers)
            AddHeader(Header.Key, string.Join(", ", Header.Value));
    }

    public HttpResponseMessage Execute(HttpRequestMessage Request, CancellationToken Token = default)
    {
        if (HeaderList != IntPtr.Zero)
            Set(CurlNative.Option.HttpHeader, HeaderList);

        WriteDelegate = (Data, Size, Nmemb, _) =>
        {
            var Length = (int)(Size * Nmemb);
            var Buffer = new byte[Length];
            Marshal.Copy(Data, Buffer, 0, Length);
            ResponseBody.Write(Buffer, 0, Length);
            return (UIntPtr)Length;
        };
        Set(CurlNative.Option.WriteFunction, Marshal.GetFunctionPointerForDelegate(WriteDelegate));

        HeaderDelegate = (Data, Size, Nmemb, _) =>
        {
            var Length = (int)(Size * Nmemb);
            var Line = Marshal.PtrToStringUTF8(Data, Length).Trim();

            if (!string.IsNullOrEmpty(Line))
                ResponseHeaders.Add(Line);

            return (UIntPtr)Length;
        };
        Set(CurlNative.Option.HeaderFunction, Marshal.GetFunctionPointerForDelegate(HeaderDelegate));

        // CancellationToken support: libcurl polls the progress callback during
        // transfers. Returning non-zero aborts with CURLE_ABORTED_BY_CALLBACK.
        if (Token.CanBeCanceled)
        {
            ProgressDelegate = (_, _, _, _, _) => Token.IsCancellationRequested ? 1 : 0;
            Set(CurlNative.Option.NoProgress, 0L);
            Set(CurlNative.Option.XferInfoFunction, Marshal.GetFunctionPointerForDelegate(ProgressDelegate));
        }

        var Result = CurlNative.Execute(Handle);

        if (Result == CurlNative.AbortedByCallback && Token.IsCancellationRequested)
            throw new OperationCanceledException(Token);

        if (Result != 0)
        {
            var ErrorPtr = CurlNative.GetErrorMessage(Result);
            var ErrorMessage = Marshal.PtrToStringAnsi(ErrorPtr) ?? $"Unknown error ({Result})";
            throw new HttpRequestException($"libcurl-impersonate error: {ErrorMessage}");
        }

        CurlNative.GetInfo(Handle, CurlNative.Info.ResponseCode, out long StatusCode);

        ResponseBody.Position = 0;

        var Response = new HttpResponseMessage((HttpStatusCode)StatusCode)
        {
            Content = new StreamContent(ResponseBody),
            RequestMessage = Request,
        };

        foreach (var HeaderLine in ResponseHeaders)
        {
            if (HeaderLine.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                continue;

            var ColonIndex = HeaderLine.IndexOf(':');

            if (ColonIndex <= 0)
                continue;

            var Name = HeaderLine[..ColonIndex].Trim();
            var Value = HeaderLine[(ColonIndex + 1)..].Trim();

            if (!Response.Headers.TryAddWithoutValidation(Name, Value))
                Response.Content.Headers.TryAddWithoutValidation(Name, Value);
        }

        return Response;
    }

    public void Dispose()
    {
        if (HeaderList != IntPtr.Zero)
        {
            CurlNative.FreeList(HeaderList);
            HeaderList = IntPtr.Zero;
        }

        if (Handle != IntPtr.Zero)
        {
            CurlNative.DestroyHandle(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
