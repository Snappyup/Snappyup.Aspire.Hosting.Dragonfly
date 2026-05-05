// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted from Aspire.Hosting.Redis for Snappyup Dragonfly hosting.

using Aspire.Hosting.ApplicationModel;

namespace Snappyup.Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis Insight container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisInsightResource(string name) : ContainerResource(name)
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary HTTP endpoint for Redis Insight.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);
}
