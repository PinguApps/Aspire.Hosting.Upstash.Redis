using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

internal static class AspireModelAssertions
{
    public static void AssertStandardRedisResource(IResource resource)
    {
        Assert.IsType<RedisResource>(resource);
    }

    public static void AssertRedisConnectionProperties(RedisResource resource)
    {
        IResourceWithConnectionString connectionResource = Assert.IsAssignableFrom<IResourceWithConnectionString>(resource);
        string[] propertyNames = [.. connectionResource.GetConnectionProperties().Select(property => property.Key)];

        Assert.Contains("Host", propertyNames);
        Assert.Contains("Port", propertyNames);
        Assert.Contains("Password", propertyNames);
        Assert.Contains("Uri", propertyNames);
    }

    public static void AssertContainerHasEnvironmentCallback(ContainerResource resource)
    {
        Assert.Contains(
            resource.Annotations,
            annotation => annotation is EnvironmentCallbackAnnotation);
    }
}
