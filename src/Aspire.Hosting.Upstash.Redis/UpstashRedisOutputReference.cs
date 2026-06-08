using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// A supplementary app-facing Upstash Redis output reference.
/// </summary>
[AspireExport("pinguapps.upstash.redis.outputReference", ExposeProperties = false, ExposeMethods = false)]
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
    [AspireExportIgnore(Reason = "Output metadata is not part of the TypeScript authoring surface.")]
    public string Name { get; }

    /// <summary>Whether the output value is sensitive.</summary>
    [AspireExportIgnore(Reason = "Output metadata is not part of the TypeScript authoring surface.")]
    public bool Secret { get; }

    /// <inheritdoc />
    [AspireExportIgnore(Reason = "Reference mechanics are consumed by Aspire, not TypeScript authors.")]
    public IEnumerable<object> References => [_resource];

    /// <summary>The manifest expression used to reference the output.</summary>
    [AspireExportIgnore(Reason = "Reference mechanics are consumed by Aspire, not TypeScript authors.")]
    public string ValueExpression { get; }

    /// <summary>Creates a reference expression for this output.</summary>
    [AspireExportIgnore(Reason = "Reference mechanics are consumed by Aspire, not TypeScript authors.")]
    public ReferenceExpression AsReferenceExpression()
    {
        return ReferenceExpression.Create($"{this}");
    }

    /// <inheritdoc />
    [AspireExportIgnore(Reason = "Reference values are resolved by Aspire.")]
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _value is null
            ? throw new InvalidOperationException("The Upstash Redis output has not been populated by the deployment pipeline.")
            : ValueTask.FromResult<string?>(_value);
    }

    /// <inheritdoc />
    [AspireExportIgnore(Reason = "Reference values are resolved by Aspire.")]
    public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        return GetValueAsync(cancellationToken);
    }

    internal void SetValue(string value)
    {
        _value = value;
    }
}
