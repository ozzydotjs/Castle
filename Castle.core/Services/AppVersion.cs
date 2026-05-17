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
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var plusIndex = informationalVersion.IndexOf('+');
            if (plusIndex > 0)
                informationalVersion = informationalVersion[..plusIndex];

            return $"v{informationalVersion}";
        }

        var version = assembly.GetName().Version;
        if (version != null)
            return $"v{version.Major}.{version.Minor}.{version.Build}";

        return "v1.0.0";
    }
}