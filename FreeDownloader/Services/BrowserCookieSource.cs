namespace FreeDownloader.Services;

internal static class BrowserCookieSource
{
    public static string? GetBrowserArgument(AppSettingsService settings) => settings.CookieBrowser switch
    {
        "none" => null,
        "zen" => GetFirefoxForkArgument("zen", "Zen Browser"),
        "floorp" => GetFirefoxForkArgument("Floorp", "Floorp"),
        "helium" => GetHeliumArgument(),
        "custom" => settings.CustomCookieSource switch
        {
            "firefox" or "chromium" => GetCustomProfileArgument(settings),
            "file" => null,
            _ => throw new InvalidOperationException("設定でカスタムCookieの読み取り方法を選択してください。")
        },
        var browser => string.IsNullOrWhiteSpace(browser) ? null : browser
    };

    public static string? GetCookieFile(AppSettingsService settings)
    {
        if (settings.CookieBrowser != "custom" || settings.CustomCookieSource != "file")
            return null;

        var path = ExpandPath(settings.CustomCookieFilePath);
        if (!Path.IsPathFullyQualified(path) || !File.Exists(path))
            throw new InvalidOperationException("設定で既存のCookieファイルを指定してください。");

        return Path.GetFullPath(path);
    }

    private static string GetFirefoxForkArgument(string directoryName, string browserName)
    {
        var roamingData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var profilesPath = Path.Combine(roamingData, directoryName, "Profiles");
        if (!Directory.Exists(profilesPath))
            throw new InvalidOperationException($"{browserName}の標準プロファイルが見つかりません。別の場所にある場合は設定の「カスタム」で指定してください。");

        return $"firefox:{profilesPath}";
    }

    private static string GetHeliumArgument()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userDataPath = Path.Combine(localData, "imput", "Helium", "User Data");
        if (!Directory.Exists(userDataPath))
            throw new InvalidOperationException("Heliumの標準プロファイルが見つかりません。別の場所にある場合は設定の「カスタム」で指定してください。");

        // yt-dlpは指定したChromiumプロファイルの親フォルダーから復号用のLocal Stateを探す。
        var profile = new DirectoryInfo(userDataPath).EnumerateDirectories()
            .Where(directory => directory.Name is not ("System Profile" or "Guest Profile"))
            .SelectMany(directory => new[] { "Network\\Cookies", "Cookies" }
                .Select(name => (ProfilePath: directory.FullName, Database: new FileInfo(Path.Combine(directory.FullName, name)))))
            .Where(candidate => candidate.Database.Exists)
            .OrderByDescending(candidate => candidate.Database.LastWriteTimeUtc)
            .FirstOrDefault();

        if (profile.ProfilePath is null)
            throw new InvalidOperationException("HeliumのCookieが見つかりません。Heliumで対象サイトを開いてから、ブラウザを完全に終了してください。");

        return $"chromium:{profile.ProfilePath}";
    }

    private static string GetCustomProfileArgument(AppSettingsService settings)
    {
        var path = ExpandPath(settings.CustomCookieProfilePath);
        if (!Path.IsPathFullyQualified(path) || !Directory.Exists(path))
            throw new InvalidOperationException("設定で既存のブラウザプロファイルのフォルダーを指定してください。");

        return $"{settings.CustomCookieSource}:{Path.TrimEndingDirectorySeparator(Path.GetFullPath(path))}";
    }

    private static string ExpandPath(string path) => Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
}
