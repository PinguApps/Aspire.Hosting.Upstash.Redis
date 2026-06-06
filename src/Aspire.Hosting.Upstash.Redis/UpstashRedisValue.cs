using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Represents an Upstash Redis deployment value supplied either as a literal string or as an Aspire parameter.
/// </summary>
public sealed class UpstashRedisValue
{
    private UpstashRedisValue(string literalValue)
    {
        LiteralValue = literalValue;
    }

    private UpstashRedisValue(ParameterResource parameter)
    {
        Parameter = parameter;
    }

    /// <summary>
    /// Gets the literal string value when this value was created from a string.
    /// </summary>
    public string? LiteralValue
    {
        get;
    }

    /// <summary>
    /// Gets the Aspire parameter when this value was created from a parameter resource.
    /// </summary>
    public ParameterResource? Parameter
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether this value is backed by an Aspire parameter.
    /// </summary>
    public bool IsParameter => Parameter is not null;

    /// <summary>
    /// Creates an Upstash Redis deployment value from a literal string.
    /// </summary>
    /// <param name="value">The literal deployment value.</param>
    /// <returns>An Upstash Redis deployment value backed by the supplied literal string.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is null, empty, or whitespace.</exception>
    public static UpstashRedisValue FromString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new UpstashRedisValue(value);
    }

    /// <summary>
    /// Creates an Upstash Redis deployment value from an Aspire parameter resource.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource.</param>
    /// <returns>An Upstash Redis deployment value backed by the supplied Aspire parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
    public static UpstashRedisValue FromParameter(ParameterResource parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return new UpstashRedisValue(parameter);
    }

    /// <summary>
    /// Creates an Upstash Redis deployment value from an Aspire parameter resource.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource.</param>
    /// <returns>An Upstash Redis deployment value backed by the supplied Aspire parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
    public static UpstashRedisValue FromParameterResource(ParameterResource parameter) => FromParameter(parameter);

    /// <summary>
    /// Creates an Upstash Redis deployment value from an Aspire parameter resource builder.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource builder.</param>
    /// <returns>An Upstash Redis deployment value backed by the supplied Aspire parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
    public static UpstashRedisValue FromParameter(IResourceBuilder<ParameterResource> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return FromParameter(parameter.Resource);
    }

    /// <summary>
    /// Converts a literal string to an Upstash Redis deployment value.
    /// </summary>
    /// <param name="value">The literal deployment value.</param>
    public static implicit operator UpstashRedisValue(string value) => FromString(value);

    /// <summary>
    /// Converts an Aspire parameter resource to an Upstash Redis deployment value.
    /// </summary>
    /// <param name="parameter">The Aspire parameter resource.</param>
    public static implicit operator UpstashRedisValue(ParameterResource parameter) => FromParameter(parameter);
}
