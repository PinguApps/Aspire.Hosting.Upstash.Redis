namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Describes an Upstash Redis global database region.
/// </summary>
public enum UpstashRedisRegion
{
    /// <summary>
    /// AWS US East, N. Virginia.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsUsEast1")]
    AwsUsEast1,

    /// <summary>
    /// AWS US East, Ohio.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsUsEast2")]
    AwsUsEast2,

    /// <summary>
    /// AWS US West, N. California.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsUsWest1")]
    AwsUsWest1,

    /// <summary>
    /// AWS US West, Oregon.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsUsWest2")]
    AwsUsWest2,

    /// <summary>
    /// AWS Canada Central.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsCaCentral1")]
    AwsCaCentral1,

    /// <summary>
    /// AWS Europe, Frankfurt.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsEuCentral1")]
    AwsEuCentral1,

    /// <summary>
    /// AWS Europe, Ireland.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsEuWest1")]
    AwsEuWest1,

    /// <summary>
    /// AWS Europe, London.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsEuWest2")]
    AwsEuWest2,

    /// <summary>
    /// AWS South America, Sao Paulo.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsSaEast1")]
    AwsSaEast1,

    /// <summary>
    /// AWS Asia Pacific, Mumbai.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsApSouth1")]
    AwsApSouth1,

    /// <summary>
    /// AWS Asia Pacific, Tokyo.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsApNortheast1")]
    AwsApNortheast1,

    /// <summary>
    /// AWS Asia Pacific, Singapore.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsApSoutheast1")]
    AwsApSoutheast1,

    /// <summary>
    /// AWS Asia Pacific, Sydney.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsApSoutheast2")]
    AwsApSoutheast2,

    /// <summary>
    /// AWS Africa, Cape Town.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "awsAfSouth1")]
    AwsAfSouth1,

    /// <summary>
    /// Google Cloud Iowa.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "gcpUsCentral1")]
    GcpUsCentral1,

    /// <summary>
    /// Google Cloud Virginia.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "gcpUsEast4")]
    GcpUsEast4,

    /// <summary>
    /// Google Cloud Belgium.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "gcpEuropeWest1")]
    GcpEuropeWest1,

    /// <summary>
    /// Google Cloud Tokyo.
    /// </summary>
    [AspireValue("upstashRedisRegion", Name = "gcpAsiaNortheast1")]
    GcpAsiaNortheast1
}
