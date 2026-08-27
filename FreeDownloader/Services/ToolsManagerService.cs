using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace FreeDownloader.Services;

public class ToolsManagerService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static ToolsManagerService()
    {
        var v = typeof(ToolsManagerService).Assembly.GetName().Version;
        var version = v is null ? "1.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
        Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FreeDownloader", version));
    }

    private const string YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    private const string FfmpegZipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-lgpl.zip";
    private const string DenoUrl = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

    private readonly AppSettingsService _settings;

    public ToolsManagerService() : this(App.Settings) { }

    public ToolsManagerService(AppSettingsService settings)
    {
        _settings = settings;
    }

    public static string ManagedToolsDirectory
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "FreeDownloader", "tools");
        }
    }

    public static string ManagedYtDlpPath => Path.Combine(ManagedToolsDirectory, "yt-dlp.exe");
    public static string ManagedFfmpegPath => Path.Combine(ManagedToolsDirectory, "ffmpeg.exe");
    public static string ManagedFfprobePath => Path.Combine(ManagedToolsDirectory, "ffprobe.exe");
    public static string ManagedDenoPath => Path.Combine(ManagedToolsDirectory, "deno.exe");

    public string ResolveYtDlpPath()
    {
        if (_settings.UseManagedTools)
        {
            return ManagedYtDlpPath;
        } else if (!string.IsNullOrWhiteSpace(_settings.YtDlpExecutablePath))
        {
            return _settings.YtDlpExecutablePath;
        }
        // 設定が不正なら管理されたパスを返す
        else
        {
            return ManagedYtDlpPath;
        }
    }

    public string ResolveFfmpegPath()
    {
        if (_settings.UseManagedTools)
        {
            return ManagedFfmpegPath;
        } else if (!string.IsNullOrWhiteSpace(_settings.FfmpegExecutablePath))
        {
            return _settings.FfmpegExecutablePath;
        }
        // 設定が不正なら管理されたパスを返す
        else
        {
            return ManagedFfmpegPath;
        }
    }
    
    public string ResolveDenoPath()
    {
        if (_settings.UseManagedTools)
        {
            return ManagedDenoPath;
        } else if (!string.IsNullOrWhiteSpace(_settings.DenoExecutablePath))
        {
            return _settings.DenoExecutablePath;
        }
        // 設定が不正なら管理されたパスを返す
        else
        {
            return ManagedDenoPath;
        }
    }

    public bool IsManagedYtDlpInstalled => File.Exists(ManagedYtDlpPath);
    public bool IsManagedFfmpegInstalled => File.Exists(ManagedFfmpegPath) && File.Exists(ManagedFfprobePath);
    public bool IsManagedDenoInstalled => File.Exists(ManagedDenoPath);

    public async Task<string> EnsureYtDlpAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (!_settings.UseManagedTools)
            return ResolveYtDlpPath();

        if (IsManagedYtDlpInstalled)
            return ManagedYtDlpPath;

        return await InstallYtDlpAsync(progress, ct);
    }

    public async Task<string> EnsureFfmpegAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (!_settings.UseManagedTools)
            return ResolveFfmpegPath();

        if (IsManagedFfmpegInstalled)
            return ManagedFfmpegPath;

        return await InstallFfmpegAsync(progress, ct);
    }
    
    public async Task<string> EnsureDenoAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (!_settings.UseManagedTools)
            return ResolveDenoPath();

        if (IsManagedDenoInstalled)
            return ManagedDenoPath;

        return await InstallDenoAsync(progress, ct);
    }

    public async Task EnsureAllToolsAsync(IProgress<(string tool, double progress)>? progress = null, CancellationToken ct = default)
    {
        if (!_settings.UseManagedTools) return;

        var ytProgress = new Progress<double>(p => progress?.Report(("yt-dlp", p)));
        var ffProgress = new Progress<double>(p => progress?.Report(("ffmpeg", p)));
        var denoProgress = new Progress<double>(p => progress?.Report(("deno", p)));
        await EnsureYtDlpAsync(ytProgress, ct);
        await EnsureFfmpegAsync(ffProgress, ct);
        await EnsureDenoAsync(denoProgress, ct);
    }

    public async Task<string> InstallYtDlpAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(ManagedToolsDirectory);
        await DownloadFileAsync(YtDlpUrl, ManagedYtDlpPath, progress, ct);
        return ManagedYtDlpPath;
    }

    public async Task<string> InstallFfmpegAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(ManagedToolsDirectory);

        var tempZip = Path.Combine(ManagedToolsDirectory, "_ffmpeg.zip");
        try
        {
            await DownloadFileAsync(FfmpegZipUrl, tempZip, progress, ct);
            await ExtractFfmpegAsync(tempZip, ManagedToolsDirectory, ct);
            return ManagedFfmpegPath;
        }
        finally
        {
            TryDelete(tempZip);
        }
    }

    public async Task<string> InstallDenoAsync(IProgress<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(ManagedToolsDirectory);

        var tempZip = Path.Combine(ManagedToolsDirectory, "_deno.zip");
        try
        {
            await DownloadFileAsync(DenoUrl, tempZip, progress, ct);
            await ExtractDenoAsync(tempZip, ManagedToolsDirectory, ct);
            return ManagedDenoPath;
        }
        finally
        {
            TryDelete(tempZip);
        }
    }
    

    public async Task<string?> GetYtDlpVersionAsync(CancellationToken ct = default)
    {
        var exe = ResolveYtDlpPath();
        if (!File.Exists(exe)) return null;
        return await GetToolVersionAsync(exe, "--version", ct);
    }

    public async Task<string?> GetFfmpegVersionAsync(CancellationToken ct = default)
    {
        var exe = ResolveFfmpegPath();
        if (!File.Exists(exe)) return null;
        return await GetToolVersionAsync(exe, "-version", ct);
    }

    public async Task<string?> GetDenoVersionAsync(CancellationToken ct = default)
    {
        var exe = ResolveDenoPath();
        if (!File.Exists(exe)) return null;
        return await GetToolVersionAsync(exe, "--version", ct);
    }

    private static async Task<string?> GetToolVersionAsync(string exePath, string args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo(exePath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return null;
            var output = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            return output.Split('\n')[0].Trim();
        }
        catch
        {
            return null;
        }
    }

    private static async Task DownloadFileAsync(string url, string destPath, IProgress<double>? progress, CancellationToken ct)
    {
        var tempPath = destPath + ".tmp";

        using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength;
        await using var netStream = await resp.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long readTotal = 0;
        int read;
        while ((read = await netStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
            if (total.HasValue && progress != null)
                progress.Report((double)readTotal / total.Value * 100);
        }

        fileStream.Close();
        File.Move(tempPath, destPath, overwrite: true);
    }

    private static async Task ExtractFfmpegAsync(string zipPath, string destDir, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();

                if (!entry.FullName.EndsWith("bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase) &&
                    !entry.FullName.EndsWith("bin/ffprobe.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileName(entry.FullName);
                var destPath = Path.Combine(destDir, fileName);
                entry.ExtractToFile(destPath, overwrite: true);
            }
        }, ct);

        if (!File.Exists(Path.Combine(destDir, "ffmpeg.exe")) || !File.Exists(Path.Combine(destDir, "ffprobe.exe")))
            throw new FileNotFoundException("ffmpegの展開に失敗しました。zipの構造が変わった可能性があります。");
    }

    private static async Task ExtractDenoAsync(string zipPath, string destDir, CancellationToken ct)
    {
        // deno の zip は https://github.com/denoland/deno/releases から取得する
        // 中身は deno.exe が直接入っている(フォルダ階層なし)
        await Task.Run(() =>
        {
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();

                if (!entry.FullName.EndsWith("deno.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                entry.ExtractToFile(Path.Combine(destDir, "deno.exe"), overwrite: true);
                break; // deno.exe は1つだけなので見つけたら終了
            }
        }, ct);

        if (!File.Exists(Path.Combine(destDir, "deno.exe")))
            throw new FileNotFoundException("denoの展開に失敗しました。zipの構造が変わった可能性があります。");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
