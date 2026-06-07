using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisConnectionOutputAnnotation : IResourceAnnotation
{
    public UpstashRedisConnectionOutputAnnotation(UpstashRedisConnectionOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        Output = output;
    }

    public UpstashRedisConnectionOutput Output { get; }
}
