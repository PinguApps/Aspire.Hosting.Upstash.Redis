using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Supplementary app-facing outputs populated from the deployed Upstash Redis database.
/// </summary>
public sealed class UpstashRedisOutputs
{
    private readonly UpstashRedisOutputValue _endpoint;
    private readonly UpstashRedisOutputValue _port;
    private readonly UpstashRedisOutputValue _password;
    private readonly UpstashRedisOutputValue _tls;
    private readonly UpstashRedisOutputValue _databaseName;

    internal UpstashRedisOutputs(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        _endpoint = new(resourceName, UpstashRedisOutputNames.Endpoint);
        _port = new(resourceName, UpstashRedisOutputNames.Port);
        _password = new(resourceName, UpstashRedisOutputNames.Password, secret: true);
        _tls = new(resourceName, UpstashRedisOutputNames.Tls);
        _databaseName = new(resourceName, UpstashRedisOutputNames.DatabaseName);

        Endpoint = CreateReference(_endpoint);
        Port = CreateReference(_port);
        Password = CreateReference(_password);
        Tls = CreateReference(_tls);
        DatabaseName = CreateReference(_databaseName);
        Properties =
        [
            new(UpstashRedisOutputNames.Endpoint, Endpoint),
            new(UpstashRedisOutputNames.Port, Port),
            new(UpstashRedisOutputNames.Password, Password),
            new(UpstashRedisOutputNames.Tls, Tls),
            new(UpstashRedisOutputNames.DatabaseName, DatabaseName),
        ];
    }

    /// <summary>The deployed Upstash Redis host endpoint.</summary>
    public ReferenceExpression Endpoint { get; }

    /// <summary>The deployed Upstash Redis port.</summary>
    public ReferenceExpression Port { get; }

    /// <summary>The deployed Upstash Redis password.</summary>
    public ReferenceExpression Password { get; }

    /// <summary>Whether TLS is enabled for the deployed Upstash Redis endpoint.</summary>
    public ReferenceExpression Tls { get; }

    /// <summary>The deployed Upstash Redis database name.</summary>
    public ReferenceExpression DatabaseName { get; }

    /// <summary>The stable supplementary output names and references.</summary>
    public IReadOnlyList<KeyValuePair<string, ReferenceExpression>> Properties { get; }

    /// <summary>Returns whether the named supplementary output contains a secret value.</summary>
    public static bool IsSecret(string outputName)
    {
        ArgumentNullException.ThrowIfNull(outputName);

        return string.Equals(outputName, UpstashRedisOutputNames.Password, StringComparison.Ordinal);
    }

    internal void Populate(UpstashRedisDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(database);

        _endpoint.SetValue(database.Endpoint);
        _port.SetValue(database.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _password.SetValue(database.Password ?? string.Empty);
        _tls.SetValue(database.Tls.ToString().ToLowerInvariant());
        _databaseName.SetValue(database.DatabaseName);
    }

    private static ReferenceExpression CreateReference(UpstashRedisOutputValue value)
    {
        return ReferenceExpression.Create($"{value}");
    }

    private sealed class UpstashRedisOutputValue : IValueProvider, IManifestExpressionProvider
    {
        private string? _value;

        public UpstashRedisOutputValue(string resourceName, string name, bool secret = false)
        {
            ValueExpression = $"{{{resourceName}.outputs.{name}}}";
            Secret = secret;
        }

        public bool Secret { get; }

        public string ValueExpression { get; }

        public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _value is null
                ? throw new InvalidOperationException("The Upstash Redis output has not been populated by the deployment pipeline.")
                : ValueTask.FromResult<string?>(_value);
        }

        public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
        {
            return GetValueAsync(cancellationToken);
        }

        public void SetValue(string value)
        {
            _value = value;
        }
    }
}
