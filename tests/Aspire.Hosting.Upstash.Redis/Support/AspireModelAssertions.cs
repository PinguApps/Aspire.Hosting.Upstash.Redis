using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
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

    public static void AssertRedisConnectionPropertiesDoNotContain(RedisResource resource, string unexpectedValue)
    {
        IResourceWithConnectionString connectionResource = Assert.IsAssignableFrom<IResourceWithConnectionString>(resource);

        foreach (KeyValuePair<string, ReferenceExpression> property in connectionResource.GetConnectionProperties())
        {
            Assert.DoesNotContain(unexpectedValue, property.Key, StringComparison.Ordinal);
            Assert.DoesNotContain(unexpectedValue, property.Value.ValueExpression, StringComparison.Ordinal);
        }
    }

    public static async Task AssertContainerEnvironmentDoesNotContainAsync(ContainerResource resource, string unexpectedValue)
    {
        Dictionary<string, object> environmentVariables = [];
        DistributedApplicationExecutionContext executionContext = new(DistributedApplicationOperation.Run);
        EnvironmentCallbackContext callbackContext = new(executionContext, resource, environmentVariables, CancellationToken.None);

        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(callbackContext);
        }

        foreach (KeyValuePair<string, object> variable in environmentVariables)
        {
            Assert.DoesNotContain(unexpectedValue, variable.Key, StringComparison.Ordinal);
            AssertEnvironmentValueDoesNotContain(variable.Value, unexpectedValue);
        }
    }

    private static void AssertEnvironmentValueDoesNotContain(object? value, string unexpectedValue)
    {
        switch (value)
        {
            case null:
                return;

            case ReferenceExpression expression:
                Assert.DoesNotContain(unexpectedValue, expression.ValueExpression, StringComparison.Ordinal);
                return;

            case string text:
                Assert.DoesNotContain(unexpectedValue, text, StringComparison.Ordinal);
                return;

            default:
                Assert.DoesNotContain(unexpectedValue, value.ToString(), StringComparison.Ordinal);
                return;
        }
    }
}
