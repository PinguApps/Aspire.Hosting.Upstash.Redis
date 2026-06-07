using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisConnectionOutput : IResourceWithConnectionString
{
    private const string TlsSuffix = ",ssl=true";

    public UpstashRedisConnectionOutput(
        string host,
        int port,
        string password,
        bool tls)
    {
        Host = host;
        Port = port;
        Password = password;
        Tls = tls;

        ConnectionString = BuildConnectionString(host, port, password, tls);
        Uri = BuildUri(host, port, password, tls);
        ConnectionStringExpression = ReferenceExpression.Create($"{ConnectionString}");
    }

    public string Name => "upstash-redis-connection-output";

    public ResourceAnnotationCollection Annotations { get; } = [];

    public string Host { get; }

    public int Port { get; }

    public string Password { get; }

    public bool Tls { get; }

    public string ConnectionString { get; }

    public string Uri { get; }

    public ReferenceExpression ConnectionStringExpression { get; }

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(ConnectionString);
    }

    public IEnumerable<KeyValuePair<string, ReferenceExpression>> GetConnectionProperties()
    {
        yield return new("Host", ReferenceExpression.Create($"{Host}"));
        yield return new("Port", ReferenceExpression.Create($"{Port.ToString(CultureInfo.InvariantCulture)}"));
        yield return new("Password", ReferenceExpression.Create($"{Password}"));
        yield return new("Uri", ReferenceExpression.Create($"{Uri}"));
    }

    public static UpstashRedisConnectionOutput FromDatabase(UpstashRedisDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        string host = NormalizeHost(database.DatabaseId, database.Endpoint);
        string password = NormalizePassword(database.DatabaseId, database.Password);

        if (database.Port <= 0)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{database.DatabaseId}' without a valid port.");
        }

        if (!database.Tls)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{database.DatabaseId}' with TLS disabled.");
        }

        return new UpstashRedisConnectionOutput(host, database.Port, password, tls: true);
    }

    private static string BuildConnectionString(string host, int port, string password, bool tls)
    {
        string tlsSuffix = tls ? TlsSuffix : string.Empty;

        return $"{host}:{port.ToString(CultureInfo.InvariantCulture)},password={password}{tlsSuffix}";
    }

    private static string BuildUri(string host, int port, string password, bool tls)
    {
        string scheme = tls ? "rediss" : "redis";
        string escapedPassword = System.Uri.EscapeDataString(password);

        return $"{scheme}://:{escapedPassword}@{host}:{port.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string NormalizeHost(string databaseId, string endpoint)
    {
        string host = endpoint.Trim();

        if (host.Length == 0)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' without an endpoint.");
        }

        if (host.Contains("://", StringComparison.Ordinal)
            || host.Contains('/', StringComparison.Ordinal)
            || host.Contains('?', StringComparison.Ordinal)
            || host.Contains('#', StringComparison.Ordinal)
            || host.Contains(':', StringComparison.Ordinal)
            || host.Any(char.IsWhiteSpace))
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' with endpoint '{endpoint}', which is not a host name.");
        }

        UriHostNameType hostNameType = System.Uri.CheckHostName(host);

        if (hostNameType == UriHostNameType.IPv4)
        {
            return host;
        }

        if (hostNameType == UriHostNameType.Dns && host.Contains('.', StringComparison.Ordinal))
        {
            return host;
        }

        throw new UpstashRedisProviderException(
            UpstashRedisProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Upstash Redis returned database '{databaseId}' with endpoint '{endpoint}', which is not a complete host name.");
    }

    private static string NormalizePassword(string databaseId, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' without credentials.");
        }

        if (password.Contains(',', StringComparison.Ordinal) || password.Any(char.IsControl))
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' with a password that cannot be represented in the Aspire Redis connection string format.");
        }

        return password;
    }
}
