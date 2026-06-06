using System.Runtime.ExceptionServices;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

internal sealed class LiveUpstashTestSession
{
    private readonly Stack<Func<Task>> _cleanupActions = [];

    public string? AccountEmail => Environment.GetEnvironmentVariable("UPSTASH_EMAIL");

    public string? ApiKey => Environment.GetEnvironmentVariable("UPSTASH_API_KEY");

    public bool HasCredentials => !string.IsNullOrWhiteSpace(AccountEmail) && !string.IsNullOrWhiteSpace(ApiKey);

    public int CleanupActionCount => _cleanupActions.Count;

    public void RegisterCleanup(Func<Task> cleanup)
    {
        _cleanupActions.Push(cleanup);
    }

    public async Task CleanupAsync()
    {
        List<Exception>? failures = null;

        while (_cleanupActions.TryPop(out Func<Task>? cleanup))
        {
            try
            {
                await cleanup().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }

        if (failures is null)
        {
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        throw new AggregateException("One or more live Upstash cleanup actions failed.", failures);
    }
}
