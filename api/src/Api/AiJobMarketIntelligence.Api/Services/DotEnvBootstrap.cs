using DotNetEnv;

namespace AiJobMarketIntelligence.Api.Services;

internal static class DotEnvBootstrap
{
    private static int _loaded;

    public static void LoadFromRepoRoot(string basePath)
    {
        if (Interlocked.Exchange(ref _loaded, 1) == 1)
            return;

        // basePath is typically .../bin/Debug/net8.0. Walk upward until we find .env.
        var dir = new DirectoryInfo(basePath);
        if (!dir.Exists)
            dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                try { Env.Load(candidate); } catch { }
                return;
            }
            dir = dir.Parent;
        }
    }
}
