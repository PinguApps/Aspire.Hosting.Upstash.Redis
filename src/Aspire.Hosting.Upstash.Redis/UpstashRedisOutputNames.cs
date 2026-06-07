namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Stable names for supplementary Upstash Redis app-facing outputs.
/// </summary>
public static class UpstashRedisOutputNames
{
    /// <summary>The Upstash Redis host endpoint.</summary>
    public const string Endpoint = "Endpoint";

    /// <summary>The Upstash Redis port.</summary>
    public const string Port = "Port";

    /// <summary>The Upstash Redis password.</summary>
    public const string Password = "Password";

    /// <summary>Whether TLS is enabled for the Upstash Redis endpoint.</summary>
    public const string Tls = "Tls";

    /// <summary>The Upstash Redis database name.</summary>
    public const string DatabaseName = "DatabaseName";
}
