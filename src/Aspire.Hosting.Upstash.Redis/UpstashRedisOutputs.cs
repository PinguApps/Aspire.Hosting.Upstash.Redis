using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Supplementary app-facing outputs populated from the deployed Upstash Redis database.
/// </summary>
[AspireExport("pinguapps.upstash.redis.outputs", ExposeProperties = true, ExposeMethods = false)]
public sealed class UpstashRedisOutputs
{
    internal UpstashRedisOutputs(RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Endpoint = new(resource, UpstashRedisOutputNames.Endpoint);
        Port = new(resource, UpstashRedisOutputNames.Port);
        Password = new(resource, UpstashRedisOutputNames.Password, secret: true);
        Tls = new(resource, UpstashRedisOutputNames.Tls);
        DatabaseName = new(resource, UpstashRedisOutputNames.DatabaseName);
        Properties =
        [
            Endpoint,
            Port,
            Password,
            Tls,
            DatabaseName,
        ];
    }

    /// <summary>The deployed Upstash Redis host endpoint.</summary>
    public UpstashRedisOutputReference Endpoint { get; }

    /// <summary>The deployed Upstash Redis port.</summary>
    public UpstashRedisOutputReference Port { get; }

    /// <summary>The deployed Upstash Redis password.</summary>
    public UpstashRedisOutputReference Password { get; }

    /// <summary>Whether TLS is enabled for the deployed Upstash Redis endpoint.</summary>
    public UpstashRedisOutputReference Tls { get; }

    /// <summary>The deployed Upstash Redis database name.</summary>
    public UpstashRedisOutputReference DatabaseName { get; }

    /// <summary>The stable supplementary output references.</summary>
    [AspireExportIgnore(Reason = "TypeScript AppHosts consume named output properties directly.")]
    public IReadOnlyList<UpstashRedisOutputReference> Properties { get; }

    /// <summary>Returns whether the named supplementary output contains a secret value.</summary>
    [AspireExportIgnore(Reason = "Output secret classification is implementation metadata.")]
    public static bool IsSecret(string outputName)
    {
        ArgumentNullException.ThrowIfNull(outputName);

        return string.Equals(outputName, UpstashRedisOutputNames.Password, StringComparison.Ordinal);
    }

    internal void Populate(UpstashRedisDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        UpstashRedisConnectionOutput connectionOutput = UpstashRedisConnectionOutput.FromDatabase(database);

        Endpoint.SetValue(connectionOutput.Host);
        Port.SetValue(connectionOutput.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Password.SetValue(connectionOutput.Password);
        Tls.SetValue(connectionOutput.Tls.ToString().ToLowerInvariant());
        DatabaseName.SetValue(database.DatabaseName);
    }
}
