using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisOutputsAnnotation : IResourceAnnotation
{
    public UpstashRedisOutputsAnnotation(UpstashRedisOutputs outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        Outputs = outputs;
    }

    public UpstashRedisOutputs Outputs { get; }
}
