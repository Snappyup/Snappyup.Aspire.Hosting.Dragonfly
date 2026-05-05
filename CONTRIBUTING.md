# Contributing

Thank you for helping improve Snappyup.Aspire.Hosting.Dragonfly. This document describes how we work on the repository day to day.

## Getting started

1. **Fork** the repository and clone your fork.
2. Ensure you have a **.NET SDK** that supports **net8.0**, **net9.0**, and **net10.0** (matching the shipped TFMs).
3. From the repository root:

   ```bash
   dotnet restore Snappyup.Aspire.Hosting.Dragonfly.slnx
   dotnet build Snappyup.Aspire.Hosting.Dragonfly.slnx -c Release
   ```

4. Optionally produce a NuGet package locally:

   ```bash
   dotnet pack Snappyup.Aspire.Hosting.Dragonfly.slnx -c Release -o ./artifacts
   ```

Central package versions live in **`Directory.Packages.props`**. A **`nuget.config`** in the repo root pins package source mapping so restores stay deterministic even if your machine has additional NuGet feeds.

## Code style

- Match existing formatting; the repo includes **`.editorconfig`**.
- Prefer **small, focused** changes with a clear commit message.
- Public API additions should include **XML documentation** on the shipped surface (the project builds with `GenerateDocumentationFile`).

## Design alignment

This package intentionally tracks the **shape and behavior** of Microsoft’s **`Aspire.Hosting.Redis`** (for the same Aspire major line) while swapping the container/runtime to **Dragonfly**. When proposing behavior changes, call out:

- Parity vs `Aspire.Hosting.Redis`
- Dragonfly-specific flags (for example `--snapshot_cron`, `--cluster_mode`)

## Pull requests

1. Open an issue first for **large** features or breaking changes, unless the fix is trivial.
2. Use a **descriptive PR title** and explain **what** changed and **why** in the description.
3. Ensure CI passes locally: **`dotnet restore`**, **`dotnet format … --verify-no-changes`**, **`dotnet build … -c Release`** (see [`.github/workflows/ci.yml`](.github/workflows/ci.yml)).
4. If you change user-facing behavior, update **`PackageReadme.md`** and/or the root **`README.md`** as appropriate.

There is a short [pull request template](.github/pull_request_template.md) to help structure reviews.

## Branch policy (`main`)

The default branch is protected for a typical **fork → feature branch → PR** flow:

- **Direct pushes to `main` are not allowed.** Changes land via merged pull requests (or equivalent merge queue, if enabled later).
- **Pull requests are required** before merging (`required_approving_review_count` is **0**, so collaborators can merge once checks pass—maintainers may raise this if they want mandatory human review).
- **Status check `build`** (GitHub Actions job from the [**CI**](.github/workflows/ci.yml) workflow) is **required** and **strict** (branch must be up to date with the merge base logic GitHub applies for required checks).
- **Include administrators:** rules apply to admins too (**`enforce_admins: true`**), so bypassing protection needs an explicit exemption in GitHub (avoid for day-to-day work).
- **Force-push and deletion** of `main` are disabled.

Repository conveniences contributors expect:

- **Delete branch on merge** and **Update branch** (sync PR head with base) are enabled.
- **Forking** remains allowed for public contribution.

Releases still work by pushing **version tags** (`v*`) after `main` contains the release commit; tags are not covered by branch protection on `main`.

Maintainers can adjust protection under **Settings → Branches** in the GitHub UI or via the [Branches API](https://docs.github.com/rest/branches/branch-protection).

## CI expectations

CI runs format verification (`dotnet format --verify-no-changes`), a **NuGet vulnerability audit**, Release **build**, and **pack**. All of that lives in [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

If formatting fails locally after `dotnet restore`:

```bash
dotnet format Snappyup.Aspire.Hosting.Dragonfly.slnx --verify-no-changes --no-restore
```

If the vulnerability step fails, bump the affected version in **`Directory.Packages.props`** (or adopt a **[Dependabot](.github/dependabot.yml)** PR). Other automation (CodeQL, Dependency Review, OpenSSF Scorecard, stale workflow) runs from **`.github/workflows/`**.

## Versioning and releases

- **NuGet package version** is driven by the **`<Version>`** element in the project file and by **release tags** in CI (`v*` → pack with that version).
- Maintainers may use **`UpdateVersion.ps1`** / **`UpdateVersion.sh`** for local bumps; packs land in **`./artifacts`** (same as CI). See script comments for pack/push options.

## Community standards

Follow the **[Code of Conduct](CODE_OF_CONDUCT.md)** in all interactions.

## Questions

Open a **discussion** or **issue** on GitHub if something is unclear. For security-sensitive reports, see **[SECURITY.md](SECURITY.md)**.
