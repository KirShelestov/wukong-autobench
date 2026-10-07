using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WukongAutoBench;

static class Steam
{
    public const int BenchmarkAppId = 3132990;

    public static string? GetSteamDir()
    {
        var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string
                   ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;

        if (string.IsNullOrEmpty(path))
            return null;
        return Path.GetFullPath(path.Replace('/', '\\'));
    }

    public static string? FindBenchmarkDir()
    {
        var steamDir = GetSteamDir();
        if (steamDir == null)
            return null;

        var libraries = new List<string> { steamDir };
        var vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
        }

        foreach (var lib in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var manifest = Path.Combine(lib, "steamapps", $"appmanifest_{BenchmarkAppId}.acf");
            if (!File.Exists(manifest))
                continue;

            var m = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\"");
            if (!m.Success)
                continue;

            var dir = Path.Combine(lib, "steamapps", "common", m.Groups[1].Value);
            if (Directory.Exists(dir))
                return dir;
        }

        return null;
    }
}
