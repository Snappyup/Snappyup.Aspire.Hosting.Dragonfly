# Security policy

## Supported versions

We treat the **latest published** [Snappyup.Aspire.Hosting.Dragonfly](https://www.nuget.org/packages/Snappyup.Aspire.Hosting.Dragonfly) package on NuGet as the primary line for security updates. Older versions may not receive backports unless agreed by maintainers.

## Reporting a vulnerability

If you believe you have found a security vulnerability in **this repository** (for example, unsafe defaults in how the AppHost configures containers or connection strings), please report it **privately** rather than opening a public issue.

**Preferred:** use [GitHub Security Advisories](https://docs.github.com/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) for this repository (if enabled by the org), or contact the maintainers through a private channel they provide in the repo.

Include:

- A short description of the issue and its impact
- Steps to reproduce (sample AppHost code, versions of .NET / Aspire / this package)
- Any suggested fix (optional)

We will aim to acknowledge receipt and coordinate a fix and release timeline with you.

## Scope note

This package orchestrates **third-party container images** (DragonflyDB, optional Redis tooling images). Vulnerabilities in upstream images or in **Microsoft Aspire** itself should be reported to the respective projects; we will still consider PRs that bump pinned tags or document safer defaults when appropriate.

## Automated scanning

The repository enables **GitHub Code scanning** (CodeQL), **OpenSSF Scorecard**, CI **NuGet advisory checks**, and **Dependency Review** on pull requests. These help catch issues early but **do not replace** coordinated disclosure—please still report vulnerabilities privately as above.
