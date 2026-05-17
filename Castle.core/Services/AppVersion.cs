using System.Reflection;

namespace Castle.Services;

public static class AppVersion
{
    public static readonly string Current = GetCurrentVersion();

    public const string GitHubOwner = "ozzydotjs";
    public const string GitHubRepo = "Castle";
    public const string InstallerAssetName = "CastleSetup.exe";

    private static string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            // Strip commit hash if present (e.g. "1.0.1+abc123" -> "1.0.1")
            var plusIndex = informationalVersion.IndexOf('+');
            if (plusIndex > 0)
                informationalVersion = informationalVersion[..plusIndex];

            return $"v{informationalVersion}";
        }

        // Fallback to assembly version
        var version = assembly.GetName().Version;
        if (version != null)
            return $"v{version.Major}.{version.Minor}.{version.Build}";

        return "v1.0.0"; // Ultimate fallback
    }
}