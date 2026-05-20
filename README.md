# Texnomic.Curl.Impersonate

[![NuGet](https://img.shields.io/nuget/v/Texnomic.Curl.Impersonate.svg)](https://www.nuget.org/packages/Texnomic.Curl.Impersonate/)
[![Downloads](https://img.shields.io/nuget/dt/Texnomic.Curl.Impersonate.svg)](https://www.nuget.org/packages/Texnomic.Curl.Impersonate/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)

Managed .NET 10 wrapper around [**libcurl-impersonate**](https://github.com/lwthiker/curl-impersonate). Sends HTTP requests with TLS, HTTP/2 and HTTP/3 fingerprints identical to real Chrome, Firefox, Safari, Edge and Tor browsers — defeating TLS-based bot detection (Cloudflare, Akamai, DataDome, PerimeterX) without browser automation, cookies, or JavaScript challenges.

## Installation

```sh
dotnet add package Texnomic.Curl.Impersonate
```

The package automatically pulls the native binaries for your runtime:

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

`ImpersonateTarget` exposes static fields for every fingerprint shipped with libcurl-impersonate, including:

- Chrome 99 → 146 (desktop) and Chrome 99 / 131 (Android)
- Firefox 133, 135, 144, 147
- Safari 15.3 → 26.0 (desktop) and 17.2 → 26.0 (iOS)
- Edge 99 / 101
- Tor Browser 14.5

Pass any of them to the `CurlHttpClient` constructor, or use an arbitrary string for targets added in upstream releases:

```csharp
ImpersonateTarget Target = "chrome147";
```

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

`libcurl-impersonate` is a fork of libcurl built against BoringSSL (Chrome) or NSS (Firefox), patched to replay the TLS handshake (ClientHello extensions, cipher order, ALPN, GREASE), HTTP/2 SETTINGS frame, and HTTP/3 transport parameters of a real browser. This package wraps that library behind a familiar `HttpClient`-shaped API.

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

[MIT](LICENSE) © Texnomic. Bundled libcurl-impersonate binaries are distributed under the upstream project's [MIT license](https://github.com/lwthiker/curl-impersonate/blob/main/LICENSE).
