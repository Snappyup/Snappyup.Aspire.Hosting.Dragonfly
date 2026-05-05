## Summary

<!-- What does this PR change and why? -->

## Testing

<!-- How did you verify the change? e.g. dotnet format … --verify-no-changes ; dotnet build … -c Release -->

## Checklist

- [ ] `dotnet format Snappyup.Aspire.Hosting.Dragonfly.slnx --verify-no-changes` passes (after `dotnet restore`)
- [ ] `dotnet build` succeeds for `Snappyup.Aspire.Hosting.Dragonfly.slnx` (Release); `dotnet list … package --vulnerable` is clean when applicable
- [ ] User-facing changes are reflected in `README.md` and/or `PackageReadme.md` when needed
- [ ] No unrelated refactors or formatting-only churn outside touched files
