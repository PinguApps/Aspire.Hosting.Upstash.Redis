using System.Text.RegularExpressions;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed partial class SourceStyleStepDefinitions
{
    [Then("the package source does not contain tuple switch expressions")]
    public static void ThenThePackageSourceDoesNotContainTupleSwitchExpressions()
    {
        string repositoryRoot = FindRepositoryRoot();
        string sourceRoot = Path.Combine(repositoryRoot, "src", "Aspire.Hosting.Upstash.Redis");
        List<string> violations = [];

        foreach (string filePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(filePath);

            if (TupleSwitchExpressionPattern().IsMatch(source))
            {
                violations.Add(Path.GetRelativePath(repositoryRoot, filePath));
            }
        }

        Assert.Empty(violations);
    }

    private static string FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (Directory.Exists(Path.Combine(directory, ".git"))
                || File.Exists(Path.Combine(directory, ".git"))
                || File.Exists(Path.Combine(directory, "Aspire.Hosting.Upstash.Redis.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not find the repository root.");
    }

    [GeneratedRegex("""(?:return|=>|=)\s*\([^;{}]*,[^;{}]*\)\s*switch""", RegexOptions.Singleline)]
    private static partial Regex TupleSwitchExpressionPattern();
}
