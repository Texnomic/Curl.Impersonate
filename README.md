# Texnomic.Curl.Impersonate

[![NuGet](https://img.shields.io/nuget/v/Texnomic.Curl.Impersonate.svg)](https://www.nuget.org/packages/Texnomic.Curl.Impersonate/)
[![Downloads](https://img.shields.io/nuget/dt/Texnomic.Curl.Impersonate.svg)](https://www.nuget.org/packages/Texnomic.Curl.Impersonate/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)

Managed .NET 10 wrapper around [**libcurl-impersonate**](https://github.com/lexiforest/curl-impersonate) (the maintained lexiforest fork of lwthiker's original). Sends HTTP requests with TLS, HTTP/2 and HTTP/3 fingerprints identical to real Chrome, Firefox, Safari, Edge and Tor browsers — defeating TLS-based bot detection (Cloudflare, Akamai, DataDome, PerimeterX) without browser automation, cookies, or JavaScript challenges.

## Installation

```sh
dotnet add package Texnomic.Curl.Impersonate
```

The package depends on one native sub-package per supported runtime. Restore
downloads all of them, and at run time .NET loads the binary that matches your
platform:

| Runtime sub-package | Platform |
|---|---|
| `Texnomic.Curl.Impersonate.Runtime.win-x64` | Windows x64 |
| `Texnomic.Curl.Impersonate.Runtime.linux-x64` | Linux x64 (glibc) |

## Usage

```csharp
using Texnomic.Curl.Impersonate;

using var Client = new CurlHttpClient(ImpersonateTarget.Chrome146);

using var Response = await Client.GetAsync("https://tls.peet.ws/api/all");

Response.EnsureSuccessStatusCode();

var Body = await Response.Content.ReadAsStringAsync();

Console.WriteLine(Body);
```

### Configuration

```csharp
using var Client = new CurlHttpClient(ImpersonateTarget.Firefox147)
{
    BaseAddress    = new Uri("https://api.example.com"),
    VerifySsl      = true,
    Timeout        = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(10),
};

Client.DefaultHeaders["Authorization"] = "Bearer xxx";
Client.DefaultHeaders["X-Tenant"]      = "acme";
```

### JSON payloads

```csharp
using var Response = await Client.PostAsJsonAsync(
    "/users",
    new { Name = "Ada", Email = "ada@example.com" });
```

### Cancellation

```csharp
using var Cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

using var Response = await Client.GetAsync("https://slow.example.com", Cts.Token);
```

## Available impersonation targets

`ImpersonateTarget` exposes a static field for every documented fingerprint in the bundled libcurl-impersonate build (v2.2.3):

- Chrome 99 → 146 and 150 (desktop) and Chrome 99 / 131 (Android)
- Firefox 133, 135, 144, 147
- Safari 15.3 → 26.0.1 (desktop) and 17.2 → 26.0 (iOS)
- Edge 99 / 101
- Tor Browser 14.5

Pass any of them to the `CurlHttpClient` constructor. Targets can also be
passed as plain strings — for Safari's dotted aliases, or for a target that a
future native upgrade adds before this wrapper has a field for it:

```csharp
ImpersonateTarget Target = "safari26_0_1"; // alias of safari2601
```

The string must name a target or alias that exists in the bundled build,
otherwise `curl_easy_impersonate` rejects it at request time. Use the lowercase
primary name: primary names are matched case-sensitively, and only aliases fall
back to a case-insensitive match. Note that Chrome and Firefox version numbers
advance independently — `firefox147` is a real target, `chrome147` is not.

## Verifying the fingerprint

Hit any TLS fingerprint inspector while running with a target — the reported JA3/JA4 and HTTP/2 SETTINGS values should match the real browser:

- <https://tls.peet.ws/api/all>
- <https://tlsfingerprint.io>
- <https://browserleaks.com/tls>

## Supported platforms

| OS | Architecture | Status |
|---|---|---|
| Windows | x64 | ✅ Shipped |
| Linux (glibc) | x64 | ✅ Shipped |
| macOS | x64 / arm64 | 🚧 Planned |
| Linux | arm64 | 🚧 Planned |

## How it works

`libcurl-impersonate` is a fork of libcurl built against BoringSSL — for every browser profile, Firefox and Safari included — patched to replay the TLS handshake (ClientHello extensions, cipher order, ALPN, GREASE), HTTP/2 SETTINGS frame, and HTTP/3 transport parameters of a real browser. This package wraps that library behind a familiar `HttpClient`-shaped API.

## Building from source

```sh
git clone https://github.com/texnomic/Curl.Impersonate
cd Curl.Impersonate
dotnet build
dotnet test
dotnet pack -c Release
```

Live HTTP tests are gated behind the `Category=E2E` xUnit trait and are skipped by default:

```sh
dotnet test --filter "Category=E2E"
```

## License

[MIT](LICENSE) © Texnomic. Bundled libcurl-impersonate binaries are distributed under the upstream project's [MIT license](https://github.com/lexiforest/curl-impersonate/blob/main/LICENSE).
