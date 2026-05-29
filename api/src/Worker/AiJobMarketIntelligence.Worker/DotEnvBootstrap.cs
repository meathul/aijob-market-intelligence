using DotNetEnv;

namespace AiJobMarketIntelligence.Worker;

internal static class DotEnvBootstrap
{
    private static int _loaded;

    /// <summary>
    /// Walk upward from <paramref name="basePath"/> to find a repo-root `.env` file and load it.
    /// 
    /// Notes:
    /// - DotNetEnv v3.2.0 doesn't support an "overwriteExistingVars" parameter.
    ///   To avoid overriding existing OS env vars, we snapshot relevant values before load,
    ///   then restore them after loading the `.env` file.
    /// </summary>
    public static void LoadFromRepoRoot(string basePath)
    {
        if (Interlocked.Exchange(ref _loaded, 1) == 1)
            return;

        var dir = new DirectoryInfo(basePath);
        if (!dir.Exists)
            dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                // Snapshot a few high-value variables so OS env vars can win.
                // (If you want more, add them here.)
                var snapshot = new Dictionary<string, string?>
                {
                    ["OPENAI_API_KEY"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
                    ["DB_CONNECTION_STRING"] = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"),
                    ["ConnectionStrings__DefaultConnection"] = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
                    ["JWT_KEY"] = Environment.GetEnvironmentVariable("JWT_KEY"),
                    ["JOB_PROVIDER"] = Environment.GetEnvironmentVariable("JOB_PROVIDER"),
                    ["ADZUNA_APP_ID"] = Environment.GetEnvironmentVariable("ADZUNA_APP_ID"),
                    ["ADZUNA_APP_KEY"] = Environment.GetEnvironmentVariable("ADZUNA_APP_KEY")
                };

                Env.Load(candidate);

                foreach (var kvp in snapshot)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                        Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }

                return;
            }

            dir = dir.Parent;
        }

        Environment.SetEnvironmentVariable("AIJOB_DOTENV_NOT_FOUND", "1");
    }
}
