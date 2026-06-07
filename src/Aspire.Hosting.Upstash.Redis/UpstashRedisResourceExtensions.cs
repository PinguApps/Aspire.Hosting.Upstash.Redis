using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisResourceExtensions
{
    public static UpstashRedisDeploymentState? GetUpstashRedisDeploymentState(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<UpstashRedisDeploymentAnnotation>()
            .SingleOrDefault()
            ?.State;
    }

    public static UpstashRedisConnectionOutput ApplyUpstashRedisConnectionOutput(
        this RedisResource resource,
        UpstashRedisDatabaseDetails database)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(database);

        UpstashRedisConnectionOutput output = UpstashRedisConnectionOutput.FromDatabase(database);

        RemoveExistingUpstashConnectionOutput(resource);

        resource.Annotations.Add(new UpstashRedisConnectionOutputAnnotation(output));
        resource.Annotations.Add(new ConnectionStringRedirectAnnotation(output));

        foreach (KeyValuePair<string, ReferenceExpression> property in output.GetConnectionProperties())
        {
            resource.Annotations.Add(new ConnectionPropertyAnnotation(property.Key, property.Value));
        }

        return output;
    }

    private static void RemoveExistingUpstashConnectionOutput(RedisResource resource)
    {
        for (int annotationIndex = 0; annotationIndex < resource.Annotations.Count; annotationIndex++)
        {
            if (resource.Annotations[annotationIndex] is not UpstashRedisConnectionOutputAnnotation)
            {
                continue;
            }

            resource.Annotations.RemoveAt(annotationIndex);
            RemoveFollowingAnnotation<ConnectionStringRedirectAnnotation>(resource, annotationIndex);
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Host");
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Port");
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Password");
            RemoveFollowingConnectionProperty(resource, annotationIndex, "Uri");

            return;
        }
    }

    private static void RemoveFollowingAnnotation<TAnnotation>(RedisResource resource, int annotationIndex)
    {
        if (annotationIndex < resource.Annotations.Count
            && resource.Annotations[annotationIndex] is TAnnotation)
        {
            resource.Annotations.RemoveAt(annotationIndex);
        }
    }

    private static void RemoveFollowingConnectionProperty(
        RedisResource resource,
        int annotationIndex,
        string propertyName)
    {
        if (annotationIndex >= resource.Annotations.Count
            || resource.Annotations[annotationIndex] is not ConnectionPropertyAnnotation propertyAnnotation
            || !string.Equals(propertyAnnotation.Name, propertyName, StringComparison.Ordinal))
        {
            return;
        }

        resource.Annotations.RemoveAt(annotationIndex);
    }
}
