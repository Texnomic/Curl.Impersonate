# Changelog

All notable changes to this project are documented in this file. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this
project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.0] - 2026-05-14

### Added
- Initial public release.
- `CurlHttpClient` with `Send` / `SendAsync` / `GetAsync` / `PostAsync` / `PostAsJsonAsync`.
- `ImpersonateTarget` covering Chrome 99 → 146, Firefox 133–147, Safari 15.3 → 26.0 (desktop and iOS), Edge 99 / 101, Tor 14.5.
- Cancellation token support via libcurl's transfer-info progress callback.
- Configurable SSL verification, total timeout, connect timeout, default headers and base address.
- Native runtime sub-packages: `Texnomic.Curl.Impersonate.Runtime.win-x64`, `Texnomic.Curl.Impersonate.Runtime.linux-x64`.

[Unreleased]: https://github.com/texnomic/Curl.Impersonate/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/texnomic/Curl.Impersonate/releases/tag/v1.0.0
