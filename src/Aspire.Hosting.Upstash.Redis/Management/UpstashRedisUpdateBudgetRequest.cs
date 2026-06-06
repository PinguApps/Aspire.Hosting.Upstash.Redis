using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisUpdateBudgetRequest
{
    [JsonPropertyName("budget")]
    public required int Budget
    {
        get;
        init;
    }
}
