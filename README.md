# Snappyup.Aspire.Hosting.Dragonfly

[![CI](https://github.com/snappyup/Snappyup.Aspire.Hosting.Dragonfly/actions/workflows/ci.yml/badge.svg)](https://github.com/snappyup/Snappyup.Aspire.Hosting.Dragonfly/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Snappyup.Aspire.Hosting.Dragonfly.svg)](https://www.nuget.org/packages/Snappyup.Aspire.Hosting.Dragonfly)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Community-maintained **.NET Aspire App Hosting** integration for **[DragonflyDB](https://www.dragonflydb.io/)**, a Redis®-wire-compatible in-memory datastore.

Use this package to model **Dragonfly as a first-class resource** in your Aspire `AppHost`: container image, endpoints, health checks (via Redis-compatible checks), optional persistence and TLS, and optional Redis Insight / Redis Commander sidecars for local development.

> This project follows the same extension patterns as Microsoft’s [`Aspire.Hosting.Redis`](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview), adapted for the official **`dragonflydb/dragonfly`** image. It is **not** maintained by Microsoft.

## Why Dragonfly in Aspire?

| Goal | How this package helps |
|------|-------------------------|
| Redis-compatible clients | StackExchange.Redis-style connection expressions (`host:port`, `password`, `ssl`) |
| Aspire wait / health | Uses `AspNetCore.HealthChecks.Redis` against the resolved connection string |
| Local dev UX | Optional Redis Insight (port **5540**) and Redis Commander (**8081**) |
| Data & snapshots | Bind mount or volume on `/data` with Dragonfly `--snapshot_cron` |

## Requirements

- **.NET SDK** supporting **net8.0**, **net9.0**, and **net10.0** (the package multi-targets all three).
- **Aspire.Hosting** in the **9.x** line (see [`Directory.Packages.props`](Directory.Packages.props) for the pinned version).
- **Docker** (or a compatible container runtime) when you run the AppHost and start the Dragonfly resource.

## Installation

In your **AppHost** project:

```bash
dotnet add package Snappyup.Aspire.Hosting.Dragonfly
```

Then add the hosting namespace and register the resource:

```csharp
using Snappyup.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddDragonfly("cache")
                   .WithDataVolume();

builder.AddProject<Projects.Api>("api")
       .WithReference(cache);
```

Further examples (TLS, cluster mode, tooling) live in [`src/Snappyup.Aspire.Hosting.Dragonfly/PackageReadme.md`](src/Snappyup.Aspire.Hosting.Dragonfly/PackageReadme.md) (also published on NuGet).

## Build from source

The solution uses the modern **`.slnx`** format:

```bash
git clone https://github.com/snappyup/Snappyup.Aspire.Hosting.Dragonfly.git
cd Snappyup.Aspire.Hosting.Dragonfly

dotnet restore Snappyup.Aspire.Hosting.Dragonfly.slnx
dotnet build Snappyup.Aspire.Hosting.Dragonfly.slnx -c Release
dotnet pack Snappyup.Aspire.Hosting.Dragonfly.slnx -c Release -o ./artifacts
```

A repo-local [`nuget.config`](nuget.config) enables **central package management** (`Directory.Packages.props`) without conflicting extra feeds on your machine.

## Release & NuGet

Releases are automated with Git tags:

1. Maintainer sets the GitHub **`NUGET_API_KEY`** repository secret.
2. Push a tag such as **`v0.2.0`** — the [Release workflow](.github/workflows/release.yml) packs `Version=0.2.0` and publishes to [nuget.org](https://www.nuget.org/).

## Local version bump (optional)

Maintainers can use the bundled scripts to bump **`<Version>`** / assembly metadata, then optionally pack and push (see [CONTRIBUTING.md](CONTRIBUTING.md)):

- **PowerShell:** `./UpdateVersion.ps1` (optional `-NewVersion "1.2.0"`).
- **Bash:** `./UpdateVersion.sh` (optional first argument = version).

Environment: **`NUGET_SOURCE`**, **`NUGET_API_KEY`** (see script headers). Pack output is written to **`./artifacts`**, same as CI workflows.

## Contributing

We welcome issues and pull requests. Please read [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## CI, dependency, and security tooling

This repo uses **free GitHub-native and OSS** automation common to public .NET projects:

| Area | What runs |
|------|-----------|
| Build & pack | [`.github/workflows/ci.yml`](.github/workflows/ci.yml) — `dotnet format --verify-no-changes`, vulnerable-package check, build, pack |
| Releases | [`.github/workflows/release.yml`](.github/workflows/release.yml) — tag `v*` → NuGet.org |
| Static analysis | [`.github/workflows/codeql.yml`](.github/workflows/codeql.yml) — GitHub **CodeQL** for C# ([code scanning](https://docs.github.com/code-security/code-scanning)) |
| Supply-chain score | [`.github/workflows/scorecard.yml`](.github/workflows/scorecard.yml) — **OpenSSF Scorecard** → SARIF uploads to the Security tab |
| PR dependencies | [`.github/workflows/dependency-review.yml`](.github/workflows/dependency-review.yml) — **Dependency review** on pull requests |
| Version bump PRs | [`.github/dependabot.yml`](.github/dependabot.yml) — **Dependabot** for NuGet + GitHub Actions weekly |
| Issue hygiene | [`.github/workflows/stale.yml`](.github/workflows/stale.yml) — stale issues/PRs (with safe labels exempt) |

You can disable or tune any workflow via a follow-up PR.

## Security

Report sensitive issues as described in [SECURITY.md](SECURITY.md). Automated scans (CodeQL, Scorecard, dependency review) complement but do not replace responsible disclosure.

## Related links

- [.NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Aspire Redis hosting (reference design)](https://github.com/dotnet/aspire/tree/main/src/Aspire.Hosting.Redis)
- [Dragonfly documentation](https://www.dragonflydb.io/docs)

## License

Licensed under the [MIT License](LICENSE).

_Redis® is a registered trademark of Redis Ltd._
