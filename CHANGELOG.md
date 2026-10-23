# Changelog

All notable changes to this project are documented in this file. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this
project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

Initial release, planned as 1.0.0. Nothing has been published to NuGet yet.

### Added
- `CurlHttpClient` with `Send` / `SendAsync` / `GetAsync` / `PostAsync` / `PostAsJsonAsync`.
- `ImpersonateTarget` covering Chrome 99 → 146 and 150 → 152 (desktop), 151 / 152 (macOS) and 99 / 131 (Android), Firefox 133–155, Safari 15.3 → 27.0 (desktop) and 17.2 → 27.0 (iOS), Edge 99 / 101, Tor 14.5. A test checks every field against the bundled native target table.
- Cancellation token support via libcurl's transfer-info progress callback.
- Configurable SSL verification, total timeout, connect timeout, default headers and base address.
- Native runtime sub-packages: `Texnomic.Curl.Impersonate.Runtime.win-x64`, `Texnomic.Curl.Impersonate.Runtime.linux-x64`, bundling [lexiforest/curl-impersonate](https://github.com/lexiforest/curl-impersonate) v2.3.0 (curl 8.23.0, which fixes the 21 curl advisories affecting 8.22.0, including High CVE-2026-92392, plus the proxy DNS leak from upstream issue #320). zlib, zstd and brotli are linked statically, so no `zlib.dll` ships on Windows.

[Unreleased]: https://github.com/Texnomic/Curl.Impersonate/commits/main
