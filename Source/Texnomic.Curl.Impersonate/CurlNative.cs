using System.Runtime.InteropServices;

namespace Texnomic.Curl.Impersonate;

internal static partial class CurlNative
{
    private const string LibCurl = "libcurl-impersonate";

    [LibraryImport(LibCurl, EntryPoint = "curl_global_init")]
    internal static partial int GlobalInitialize(long Flags);

    [LibraryImport(LibCurl, EntryPoint = "curl_global_cleanup")]
    internal static partial void GlobalCleanup();

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_init")]
    internal static partial IntPtr CreateHandle();

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_cleanup")]
    internal static partial void DestroyHandle(IntPtr Handle);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_perform")]
    internal static partial int Execute(IntPtr Handle);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_reset")]
    internal static partial void Reset(IntPtr Handle);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_setopt", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SetOption(IntPtr Handle, int Option, string Value);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_setopt")]
    internal static partial int SetOption(IntPtr Handle, int Option, long Value);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_setopt")]
    internal static partial int SetOption(IntPtr Handle, int Option, IntPtr Value);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_getinfo")]
    internal static partial int GetInfo(IntPtr Handle, int Info, out long Value);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_getinfo")]
    internal static partial int GetInfo(IntPtr Handle, int Info, out IntPtr Value);

    [LibraryImport(LibCurl, EntryPoint = "curl_slist_append", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr AppendToList(IntPtr List, string Value);

    [LibraryImport(LibCurl, EntryPoint = "curl_slist_free_all")]
    internal static partial void FreeList(IntPtr List);

    [LibraryImport(LibCurl, EntryPoint = "curl_easy_strerror")]
    internal static partial IntPtr GetErrorMessage(int Code);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate UIntPtr DataCallback(IntPtr Data, UIntPtr Size, UIntPtr Nmemb, IntPtr Userdata);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ProgressCallback(IntPtr Userdata, long DownloadTotal, long DownloadNow, long UploadTotal, long UploadNow);

    internal readonly record struct Option(int Value)
    {
        public static implicit operator int(Option Option) => Option.Value;

        internal static readonly Option Url = new(10002);
        internal static readonly Option Post = new(47);
        internal static readonly Option HttpHeader = new(10023);
        internal static readonly Option PostFieldSize = new(60);
        internal static readonly Option CopyPostFields = new(10165);
        internal static readonly Option WriteFunction = new(20011);
        internal static readonly Option HeaderFunction = new(20079);
        internal static readonly Option SslVerifyPeer = new(64);
        internal static readonly Option SslVerifyHost = new(81);
        internal static readonly Option AcceptEncoding = new(10102);
        internal static readonly Option CustomRequest = new(10036);
        internal static readonly Option NoBody = new(44);
        internal static readonly Option Impersonate = new(10999);
        internal static readonly Option NoProgress = new(43);
        internal static readonly Option XferInfoFunction = new(20219);
        internal static readonly Option XferInfoData = new(10220);
        internal static readonly Option TimeoutMs = new(155);
        internal static readonly Option ConnectTimeoutMs = new(156);
    }

    internal readonly record struct Info(int Value)
    {
        public static implicit operator int(Info Info) => Info.Value;

        internal static readonly Info ResponseCode = new(0x200002);
        internal static readonly Info ContentType = new(0x100012);
    }

    internal const long GlobalDefault = 3;
    internal const int AbortedByCallback = 42;
}
