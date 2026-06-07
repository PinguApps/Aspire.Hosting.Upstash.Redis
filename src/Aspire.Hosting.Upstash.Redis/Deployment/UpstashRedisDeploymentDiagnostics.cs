using System.Text.RegularExpressions;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal static partial class UpstashRedisDeploymentDiagnostics
{
    public const string Redacted = "[redacted]";

    public static string Redact(string value, UpstashRedisResolvedDeployment? deployment = null, UpstashRedisDatabaseDetails? database = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        string redacted = RedisConnectionStringPattern().Replace(value, Redacted);

        if (deployment is not null)
        {
            redacted = RedactKnownSecret(redacted, deployment.ManagementCredentials.ApiKey);
        }

        if (database is not null)
        {
            redacted = RedactKnownSecret(redacted, database.Password);
        }

        return redacted;
    }

    public static string FormatProviderDatabaseId(string? providerDatabaseId)
    {
        return string.IsNullOrWhiteSpace(providerDatabaseId)
            ? "<unknown>"
            : providerDatabaseId;
    }

    public static UpstashRedisDeploymentProgress CreateProgress(
        UpstashRedisDeploymentPhase phase,
        string message,
        string? resourceName,
        string? databaseName,
        string? providerDatabaseId,
        UpstashRedisResolvedDeployment? deployment = null,
        UpstashRedisDatabaseDetails? database = null)
    {
        return new UpstashRedisDeploymentProgress(
            phase,
            Redact(message, deployment, database),
            resourceName,
            databaseName,
            providerDatabaseId);
    }

    private static string RedactKnownSecret(string value, string? secret)
    {
        return string.IsNullOrEmpty(secret)
            ? value
            : value.Replace(secret, Redacted, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"rediss?://\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RedisConnectionStringPattern();
}
