using Aspire.Hosting;

namespace Projects;

internal sealed class Api : IProjectMetadata
{
    public string ProjectPath => "../Api/Api.csproj";

    public LaunchSettings LaunchSettings { get; } = new();

    public bool SuppressBuild => true;

    public bool IsFileBasedApp => false;
}
