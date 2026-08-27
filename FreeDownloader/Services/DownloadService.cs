using System.Diagnostics;
using System.Text;

namespace FreeDownloader.Services;

public class DownloadService
{
    // ログを外部に通知するイベント
    public event Action<string>? OutputReceived;
    private readonly ToolsManagerService _tools;

    public DownloadService() : this(App.Settings) { }

    public DownloadService(AppSettingsService settings)
    {
        _tools = new ToolsManagerService(settings);
    }

    public async Task<int> DownloadAsync(string url, CancellationToken ct = default)
    {
        var exePath = await _tools.EnsureYtDlpAsync(ct: ct);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"yt-dlpが見つかりません: {exePath}");

        var outputDir = App.Settings.DownloadDirectory;
        Directory.CreateDirectory(outputDir);

        var ffmpegPath = _tools.ResolveFfmpegPath();
        var ffmpegArg = File.Exists(ffmpegPath) ? $" --ffmpeg-location \"{Path.GetDirectoryName(ffmpegPath)}\"" : "";
        var arguments = $"\"{url}\" -o \"{Path.Combine(outputDir, "%(title)s.%(ext)s")}\"{ffmpegArg}";

        return await RunCliAsync(exePath, arguments, ct);
    }
    // wingetを実行するメソッド
    // CLIを裏で実行する汎用メソッド
    private async Task<int> RunCliAsync(string fileName, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.Start();

        process.OutputDataReceived += (s, args) =>
        {
            if (args.Data != null)
                OutputReceived?.Invoke(args.Data);
        };
        process.ErrorDataReceived += (s, args) =>
        {
            if (args.Data != null)
                OutputReceived?.Invoke("[ERR] " + args.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        return process.ExitCode;
    }
}