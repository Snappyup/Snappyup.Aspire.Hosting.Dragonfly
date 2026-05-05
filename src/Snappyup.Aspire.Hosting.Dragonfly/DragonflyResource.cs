// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;

namespace Snappyup.Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Dragonfly container independent of hosting model (Redis-wire compatible).
/// </summary>
public class DragonflyResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DragonflyResource"/> class with a password parameter.
    /// </summary>
    public DragonflyResource(string name, ParameterResource password) : this(name)
    {
        PasswordParameter = password;
    }

    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Dragonfly server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the parameter that contains the Dragonfly server password.
    /// </summary>
    public ParameterResource? PasswordParameter { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, appends <c>,ssl=true</c> to the connection string (client TLS to Dragonfly).
    /// </summary>
    public bool UseSslConnectionStringSuffix { get; internal set; }

    private ReferenceExpression BuildConnectionString()
    {
        var builder = new ReferenceExpressionBuilder();
        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");

        if (PasswordParameter is not null)
        {
            builder.Append($",password={PasswordParameter}");
        }

        if (UseSslConnectionStringSuffix)
        {
            builder.Append($",ssl=true");
        }

        return builder.Build();
    }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return BuildConnectionString();
        }
    }

    /// <inheritdoc />
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return BuildConnectionString().GetValueAsync(cancellationToken);
    }

    internal void SetPassword(ParameterResource? password) => PasswordParameter = password;
}
