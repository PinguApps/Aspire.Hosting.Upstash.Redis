namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisProviderDomain
{
    private static readonly IReadOnlyDictionary<UpstashRedisRegion, RegionMetadata> _regions =
        new Dictionary<UpstashRedisRegion, RegionMetadata>
        {
            [UpstashRedisRegion.AwsUsEast1] = new("us-east-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsUsEast2] = new("us-east-2", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsUsWest1] = new("us-west-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsUsWest2] = new("us-west-2", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsCaCentral1] = new("ca-central-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsEuCentral1] = new("eu-central-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsEuWest1] = new("eu-west-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsEuWest2] = new("eu-west-2", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsSaEast1] = new("sa-east-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsApSouth1] = new("ap-south-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsApNortheast1] = new("ap-northeast-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsApSoutheast1] = new("ap-southeast-1", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsApSoutheast2] = new("ap-southeast-2", UpstashRedisCloudPlatform.Aws, true),
            [UpstashRedisRegion.AwsAfSouth1] = new("af-south-1", UpstashRedisCloudPlatform.Aws, false),
            [UpstashRedisRegion.GcpUsCentral1] = new("us-central1", UpstashRedisCloudPlatform.Gcp, false),
            [UpstashRedisRegion.GcpUsEast4] = new("us-east4", UpstashRedisCloudPlatform.Gcp, false),
            [UpstashRedisRegion.GcpEuropeWest1] = new("europe-west1", UpstashRedisCloudPlatform.Gcp, false),
            [UpstashRedisRegion.GcpAsiaNortheast1] = new("asia-northeast1", UpstashRedisCloudPlatform.Gcp, false)
        };

    public static string MapCloudPlatform(UpstashRedisCloudPlatform platform)
    {
        return platform switch
        {
            UpstashRedisCloudPlatform.Aws => "aws",
            UpstashRedisCloudPlatform.Gcp => "gcp",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "The Upstash Redis cloud platform is not supported.")
        };
    }

    public static string MapRegion(UpstashRedisRegion region)
    {
        return GetRegion(region).ProviderValue;
    }

    public static string MapPlan(UpstashRedisPlan plan)
    {
        return plan switch
        {
            UpstashRedisPlan.Free => "free",
            UpstashRedisPlan.PayAsYouGo => "payg",
            UpstashRedisPlan.Fixed250Mb => "fixed_250mb",
            UpstashRedisPlan.Fixed1Gb => "fixed_1gb",
            UpstashRedisPlan.Fixed5Gb => "fixed_5gb",
            UpstashRedisPlan.Fixed10Gb => "fixed_10gb",
            UpstashRedisPlan.Fixed50Gb => "fixed_50gb",
            UpstashRedisPlan.Fixed100Gb => "fixed_100gb",
            UpstashRedisPlan.Fixed500Gb => "fixed_500gb",
            _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "The Upstash Redis plan is not supported.")
        };
    }

    public static UpstashRedisCloudPlatform ParseCloudPlatform(string value, string settingName)
    {
        return Normalize(value) switch
        {
            "aws" => UpstashRedisCloudPlatform.Aws,
            "gcp" => UpstashRedisCloudPlatform.Gcp,
            _ => throw new InvalidOperationException(
                $"Upstash Redis {settingName} '{value}' is not supported. Supported values: aws, gcp.")
        };
    }

    public static UpstashRedisRegion ParsePrimaryRegion(string value, string settingName)
    {
        string normalizedValue = Normalize(value);

        foreach ((UpstashRedisRegion region, RegionMetadata metadata) in _regions)
        {
            if (StringComparer.Ordinal.Equals(metadata.ProviderValue, normalizedValue))
            {
                return region;
            }
        }

        throw new InvalidOperationException(
            $"Upstash Redis {settingName} '{value}' is not a supported primary region.");
    }

    public static UpstashRedisRegion ParseReadRegion(string value, string settingName)
    {
        UpstashRedisRegion region = ParsePrimaryRegion(value, settingName);
        RegionMetadata metadata = GetRegion(region);

        return metadata.SupportsReadReplica
            ? region
            : throw new InvalidOperationException(
                $"Upstash Redis {settingName} '{metadata.ProviderValue}' is not supported as a read region.");
    }

    public static UpstashRedisPlan ParsePlan(string value, string settingName)
    {
        return Normalize(value) switch
        {
            "free" => UpstashRedisPlan.Free,
            "payg" => UpstashRedisPlan.PayAsYouGo,
            "fixed_250mb" => UpstashRedisPlan.Fixed250Mb,
            "fixed_1gb" => UpstashRedisPlan.Fixed1Gb,
            "fixed_5gb" => UpstashRedisPlan.Fixed5Gb,
            "fixed_10gb" => UpstashRedisPlan.Fixed10Gb,
            "fixed_50gb" => UpstashRedisPlan.Fixed50Gb,
            "fixed_100gb" => UpstashRedisPlan.Fixed100Gb,
            "fixed_500gb" => UpstashRedisPlan.Fixed500Gb,
            _ => throw new InvalidOperationException(
                $"Upstash Redis {settingName} '{value}' is not supported. Supported values: free, payg, fixed_250mb, fixed_1gb, fixed_5gb, fixed_10gb, fixed_50gb, fixed_100gb, fixed_500gb.")
        };
    }

    public static int ParseBudget(string value, string settingName)
    {
        return int.TryParse(value, out int budget) && budget > 0
            ? budget
            : throw new InvalidOperationException($"Upstash Redis {settingName} must be a positive integer.");
    }

    public static UpstashRedisCloudPlatform GetCloudPlatform(UpstashRedisRegion region)
    {
        return GetRegion(region).Platform;
    }

    public static bool SupportsReadReplica(UpstashRedisRegion region)
    {
        return GetRegion(region).SupportsReadReplica;
    }

    private static RegionMetadata GetRegion(UpstashRedisRegion region)
    {
        return _regions.TryGetValue(region, out RegionMetadata? metadata)
            ? metadata
            : throw new ArgumentOutOfRangeException(nameof(region), region, "The Upstash Redis region is not supported.");
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed class RegionMetadata
    {
        public RegionMetadata(
            string providerValue,
            UpstashRedisCloudPlatform platform,
            bool supportsReadReplica)
        {
            ProviderValue = providerValue;
            Platform = platform;
            SupportsReadReplica = supportsReadReplica;
        }

        public string ProviderValue { get; }

        public UpstashRedisCloudPlatform Platform { get; }

        public bool SupportsReadReplica { get; }
    }
}
