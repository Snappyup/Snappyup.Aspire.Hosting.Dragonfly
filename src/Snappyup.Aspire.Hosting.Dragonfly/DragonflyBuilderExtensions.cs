// Licensed under the MIT License.
// Patterns adapted from Aspire.Hosting.Redis (v9.5) for DragonflyDB.

using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Snappyup.Aspire.Hosting.ApplicationModel;

namespace Snappyup.Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Dragonfly resources to the distributed application model.
/// </summary>
public static class DragonflyBuilderExtensions
{
    /// <summary>
    /// Adds a Dragonfly container to the application model.
    /// </summary>
    /// <param name="builder">The app host builder.</param>
    /// <param name="name">The name of the resource (used for service discovery / connection discovery).</param>
    /// <param name="port">Optional host port to bind.</param>
    /// <returns>A resource builder.</returns>
    public static IResourceBuilder<DragonflyResource> AddDragonfly(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? port)
    {
        return builder.AddDragonfly(name, port, password: null);
    }

    /// <summary>
    /// Adds a Dragonfly container to the application model.
    /// </summary>
    /// <param name="builder">The app host builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="port">Optional host port to bind.</param>
    /// <param name="password">Password parameter for Dragonfly authentication. When <see langword="null"/>, a default password parameter is created.</param>
    /// <returns>A resource builder.</returns>
    public static IResourceBuilder<DragonflyResource> AddDragonfly(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? port = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // StackExchange.Redis doesn't support passwords with commas.
        var passwordParameter = password?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password", special: false);

        var dragonfly = new DragonflyResource(name, passwordParameter);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(dragonfly, async (@event, ct) =>
        {
            connectionString = await dragonfly.GetConnectionStringAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException(
                    $"ConnectionStringAvailableEvent was published for the '{dragonfly.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks()
            .AddRedis(_ => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(dragonfly)
            .WithEndpoint(port: port, targetPort: 6379, name: DragonflyResource.PrimaryEndpointName)
            .WithImage(DragonflyContainerImageTags.Image, DragonflyContainerImageTags.Tag)
            .WithImageRegistry(DragonflyContainerImageTags.Registry)
            .WithHealthCheck(healthCheckKey)
            .WithEntrypoint("/bin/sh")
            .WithEnvironment(context =>
            {
                if (dragonfly.PasswordParameter is { } pwd)
                {
                    context.EnvironmentVariables["DRAGONFLY_PASSWORD"] = pwd;
                }
            })
            .WithArgs(context =>
            {
                var sb = new StringBuilder();
                sb.Append("exec dragonfly --dir /data");

                if (dragonfly.PasswordParameter is not null)
                {
                    sb.Append(" --requirepass \"$DRAGONFLY_PASSWORD\"");
                }

                if (dragonfly.TryGetLastAnnotation<DragonflyPersistenceAnnotation>(out var persist))
                {
                    sb.Append(" --dbfilename ");
                    sb.Append(persist.DbFileName);
                    sb.Append(" --snapshot_cron '");
                    sb.Append(persist.SnapshotCron.Replace("'", "'\\''", StringComparison.Ordinal));
                    sb.Append('\'');
                }

                if (dragonfly.TryGetLastAnnotation<DragonflyClusterModeAnnotation>(out _))
                {
                    sb.Append(" --cluster_mode=emulated");
                }

                if (dragonfly.TryGetLastAnnotation<DragonflyTlsAnnotation>(out var tls))
                {
                    sb.Append(" --tls");
                    sb.Append(" --tls_cert_file=");
                    sb.Append(tls.CertPath);
                    sb.Append(" --tls_key_file=");
                    sb.Append(tls.KeyPath);
                    if (tls.CaPath is { } ca)
                    {
                        sb.Append(" --tls_ca_cert_file=");
                        sb.Append(ca);
                    }
                }

                context.Args.Add("-c");
                context.Args.Add(sb.ToString());
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Adds Redis Commander wired to Dragonfly instances in the AppHost model.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithRedisCommander(
        this IResourceBuilder<DragonflyResource> builder,
        Action<IResourceBuilder<RedisCommanderResource>>? configureContainer = null,
        string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<RedisCommanderResource>().SingleOrDefault() is { } existingRedisCommanderResource)
        {
            configureContainer?.Invoke(builder.ApplicationBuilder.CreateResourceBuilder(existingRedisCommanderResource));
            return builder;
        }

        containerName ??= "rediscommander";

        var resource = new RedisCommanderResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
            .WithImage(DragonflyContainerImageTags.RedisCommanderImage, DragonflyContainerImageTags.RedisCommanderTag)
            .WithImageRegistry(DragonflyContainerImageTags.RedisCommanderRegistry)
            .WithHttpEndpoint(targetPort: 8081, name: "http")
            .ExcludeFromManifest();

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(resource, async (_, ct) =>
        {
            var instances = builder.ApplicationBuilder.Resources.OfType<DragonflyResource>().ToList();
            if (instances.Count == 0)
            {
                return;
            }

            var hostsVariableBuilder = new StringBuilder();

            foreach (var instance in instances)
            {
                var hostString =
                    $"{(hostsVariableBuilder.Length > 0 ? "," : string.Empty)}{instance.Name}:{instance.Name}:{instance.PrimaryEndpoint.TargetPort}:0";

                if (instance.PasswordParameter is not null)
                {
                    var password = await instance.PasswordParameter.GetValueAsync(ct).ConfigureAwait(false);
                    hostString += $":{password}";
                }

                hostsVariableBuilder.Append(hostString);
            }

            resourceBuilder.WithEnvironment("REDIS_HOSTS", hostsVariableBuilder.ToString());
        });

        configureContainer?.Invoke(resourceBuilder);

        resourceBuilder.WithRelationship(builder.Resource, "RedisCommander");

        return builder;
    }

    /// <summary>
    /// Adds Redis Insight wired to Dragonfly instances in the AppHost model.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithRedisInsight(
        this IResourceBuilder<DragonflyResource> builder,
        Action<IResourceBuilder<RedisInsightResource>>? configureContainer = null,
        string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<RedisInsightResource>().SingleOrDefault() is { } existingInsight)
        {
            configureContainer?.Invoke(builder.ApplicationBuilder.CreateResourceBuilder(existingInsight));
            return builder;
        }

        containerName ??= "redisinsight";

        var resource = new RedisInsightResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
            .WithImage(DragonflyContainerImageTags.RedisInsightImage, DragonflyContainerImageTags.RedisInsightTag)
            .WithImageRegistry(DragonflyContainerImageTags.RedisInsightRegistry)
            .WithHttpEndpoint(targetPort: 5540, name: "http")
            .WithEnvironment(context =>
            {
                var instances = builder.ApplicationBuilder.Resources.OfType<DragonflyResource>().ToList();
                if (instances.Count == 0)
                {
                    return;
                }

                var counter = 1;
                foreach (var instance in instances)
                {
                    context.EnvironmentVariables.Add($"RI_REDIS_HOST{counter}", instance.Name);
                    context.EnvironmentVariables.Add($"RI_REDIS_PORT{counter}", instance.PrimaryEndpoint.TargetPort!.Value);
                    context.EnvironmentVariables.Add($"RI_REDIS_ALIAS{counter}", instance.Name);
                    if (instance.PasswordParameter is not null)
                    {
                        context.EnvironmentVariables.Add($"RI_REDIS_PASSWORD{counter}", instance.PasswordParameter);
                    }

                    counter++;
                }
            })
            .WithRelationship(builder.Resource, "RedisInsight")
            .ExcludeFromManifest();

        configureContainer?.Invoke(resourceBuilder);

        return builder;
    }

    /// <summary>
    /// Exposes Redis Commander HTTP on the given host port.
    /// </summary>
    public static IResourceBuilder<RedisCommanderResource> WithHostPort(
        this IResourceBuilder<RedisCommanderResource> builder,
        int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Exposes Redis Insight HTTP on the given host port.
    /// </summary>
    public static IResourceBuilder<RedisInsightResource> WithHostPort(
        this IResourceBuilder<RedisInsightResource> builder,
        int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint(RedisInsightResource.PrimaryEndpointName, endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a named volume for Dragonfly data and enables snapshots by default snapshot schedule.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithDataVolume(
        this IResourceBuilder<DragonflyResource> builder,
        string? name = null,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Adds a bind mount for Dragonfly data and enables snapshots by default snapshot schedule when not read-only.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithDataBindMount(
        this IResourceBuilder<DragonflyResource> builder,
        string source,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        builder.WithBindMount(source, "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Enables Dragonfly snapshots using <c>--snapshot_cron</c>.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithPersistence(
        this IResourceBuilder<DragonflyResource> builder,
        string? snapshotCron = null,
        string? dbFilename = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        snapshotCron ??= "*/1 * * * *";
        dbFilename ??= "dump";

        return builder.WithAnnotation(new DragonflyPersistenceAnnotation(snapshotCron, dbFilename),
            ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Enables Dragonfly emulated Redis cluster compatibility mode (<c>--cluster_mode=emulated</c>).
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithClusterMode(this IResourceBuilder<DragonflyResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new DragonflyClusterModeAnnotation(),
            ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Enables server-side TLS using certificate files mounted into the Dragonfly container, and configures client TLS in the Aspire connection string.
    /// </summary>
    /// <param name="builder">The Dragonfly resource builder.</param>
    /// <param name="certPath">Host path to the server certificate PEM.</param>
    /// <param name="keyPath">Host path to the server key PEM.</param>
    /// <param name="caCertPath">Optional host path to a CA certificate PEM.</param>
    public static IResourceBuilder<DragonflyResource> WithTlsCertificate(
        this IResourceBuilder<DragonflyResource> builder,
        string certPath,
        string keyPath,
        string? caCertPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(certPath);
        ArgumentException.ThrowIfNullOrEmpty(keyPath);

        builder
            .WithBindMount(certPath, DragonflyTlsPaths.ServerCert, isReadOnly: true)
            .WithBindMount(keyPath, DragonflyTlsPaths.ServerKey, isReadOnly: true);

        string? caContainer = null;

        if (!string.IsNullOrEmpty(caCertPath))
        {
            builder.WithBindMount(caCertPath, DragonflyTlsPaths.CaCert, isReadOnly: true);
            caContainer = DragonflyTlsPaths.CaCert;
        }

        builder.Resource.UseSslConnectionStringSuffix = true;

        return builder.WithAnnotation(
            new DragonflyTlsAnnotation(DragonflyTlsPaths.ServerCert, DragonflyTlsPaths.ServerKey, caContainer),
            ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Configures Dragonfly authentication using the provided parameter builder.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithPassword(
        this IResourceBuilder<DragonflyResource> builder,
        IResourceBuilder<ParameterResource>? password)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.SetPassword(password?.Resource);
        return builder;
    }

    /// <summary>
    /// Pins the Dragonfly replica port exposed on the host.
    /// </summary>
    public static IResourceBuilder<DragonflyResource> WithHostPort(
        this IResourceBuilder<DragonflyResource> builder,
        int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint(DragonflyResource.PrimaryEndpointName, endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a Redis Insight persistent data volume.
    /// </summary>
    public static IResourceBuilder<RedisInsightResource> WithDataVolume(
        this IResourceBuilder<RedisInsightResource> builder,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/data");
    }

    /// <summary>
    /// Adds a Redis Insight persistent bind mount.
    /// </summary>
    public static IResourceBuilder<RedisInsightResource> WithDataBindMount(
        this IResourceBuilder<RedisInsightResource> builder,
        string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, "/data");
    }
}

internal static class DragonflyTlsPaths
{
    public const string ServerCert = "/certs/server.crt";
    public const string ServerKey = "/certs/server.key";
    public const string CaCert = "/certs/ca.crt";
}

internal sealed record DragonflyPersistenceAnnotation(string SnapshotCron, string DbFileName) : IResourceAnnotation;

internal sealed class DragonflyClusterModeAnnotation : IResourceAnnotation;

internal sealed record DragonflyTlsAnnotation(string CertPath, string KeyPath, string? CaPath) : IResourceAnnotation;
