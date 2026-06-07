using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// A supplementary app-facing Upstash Redis output reference.
/// </summary>
public sealed class UpstashRedisOutputReference : IExpressionValue, IValueProvider, IManifestExpressionProvider, IValueWithReferences
{
    private readonly RedisResource _resource;
    private string? _value;

    internal UpstashRedisOutputReference(RedisResource resource, string name, bool secret = false)
    {
        ArgumentNullException.ThrowIfNull(resource);

        _resource = resource;
        Name = name;
        ValueExpression = $"{{{resource.Name}.outputs.{name}}}";
        Secret = secret;
    }

    /// <summary>The stable output name.</summary>
    public string Name { get; }

    /// <summary>Whether the output value is sensitive.</summary>
    public bool Secret { get; }

    /// <inheritdoc />
    public IEnumerable<object> References => [_resource];

    /// <summary>The manifest expression used to reference the output.</summary>
    public string ValueExpression { get; }

    /// <summary>Creates a reference expression for this output.</summary>
    public ReferenceExpression AsReferenceExpression()
    {
        return ReferenceExpression.Create($"{this}");
    }

    /// <inheritdoc />
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _value is null
            ? throw new InvalidOperationException("The Upstash Redis output has not been populated by the deployment pipeline.")
            : ValueTask.FromResult<string?>(_value);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        return GetValueAsync(cancellationToken);
    }

    internal void SetValue(string value)
    {
        _value = value;
    }
}
