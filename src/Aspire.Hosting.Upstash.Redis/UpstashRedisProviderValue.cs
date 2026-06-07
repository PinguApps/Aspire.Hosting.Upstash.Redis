namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisProviderValue
{
    public UpstashRedisProviderValue(UpstashRedisValue source, object? literalValue)
    {
        Source = source;
        LiteralValue = literalValue;
    }

    public UpstashRedisValue Source { get; }

    public object? LiteralValue { get; }

    public bool IsParameter => Source.IsParameter;
}
