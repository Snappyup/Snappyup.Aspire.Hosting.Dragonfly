// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted from Aspire.Hosting.Redis for Snappyup Dragonfly hosting.

using Aspire.Hosting.ApplicationModel;

namespace Snappyup.Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Redis Commander container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisCommanderResource(string name) : ContainerResource(name)
{
}
