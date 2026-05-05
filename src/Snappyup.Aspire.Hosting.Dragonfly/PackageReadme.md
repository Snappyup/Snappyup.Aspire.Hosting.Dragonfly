## Snappyup.Aspire.Hosting.Dragonfly

Provides extension methods for an Aspire AppHost to run **[Dragonfly](https://www.dragonflydb.io/)**, a Redis®-compatible in-memory datastore, as a container resource alongside your distributed application.

Dragonfly listens on Redis port **6379** and exposes **StackExchange.Redis-compatible** Aspire connection strings (for example `host:port`, with optional `password=...`, `ssl=...`), so you can reuse `Aspire.StackExchange.Redis*` components or standard Redis clients.

### Install

In your AppHost project:

```bash
dotnet add package Snappyup.Aspire.Hosting.Dragonfly
```

### Usage

```csharp
using Snappyup.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddDragonfly("cache")
                   .WithDataVolume();

var api = builder.AddProject<Projects.Api>("api")
                 .WithReference(cache);
```

### Optional tooling

```csharp
var cache = builder.AddDragonfly("cache")
                   .WithRedisInsight()
                   .WithRedisCommander();
```

Redis Insight listens on HTTP target port **5540** inside the container; Redis Commander on **8081**.

### Persistence

Dragonfly uses `--snapshot_cron` (not Redis `SAVE` intervals). Defaults when using `WithDataVolume` / `WithDataBindMount` call `WithPersistence()` with:

- Cron: `*/1 * * * *` (every minute)
- DB filename stem: `dump`

Override:

```csharp
builder.AddDragonfly("cache")
       .WithDataVolume()
       .WithPersistence(snapshotCron: "*/5 * * * *", dbFilename: "mydb");
```

### TLS

Mount PEM files from the host and enable server TLS + client `ssl=true` in the Aspire connection string:

```csharp
builder.AddDragonfly("cache")
       .WithTlsCertificate(
           certPath: "/path/to/server.crt",
           keyPath: "/path/to/server.key",
           caCertPath: "/path/to/ca.crt"); // optional
```

### Connection metadata

By default, **`AddDragonfly`** creates a managed password parameter (matching **`Aspire.Hosting.Redis`** behavior), so typical connection strings look like:

- `host:port,password=<value>`
- **`WithPassword(null)`** can remove the password entirely → `host:port`
- **`WithTlsCertificate`** appends **`ssl=true`** → e.g. `host:port,password=<value>,ssl=true`

See Microsoft's Aspire Redis docs for analogous consumer wiring patterns (`WithReference`, health checks, `WaitFor`, etc.).

### Feedback

Issues and contributions: repository URL in the NuGet metadata / project README.

_Trademarks: Redis is a registered trademark of Redis Ltd._
