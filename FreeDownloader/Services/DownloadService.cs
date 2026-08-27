using System.Diagnostics;
using System.Text;

namespace FreeDownloader.Services;

public class DownloadService
{
    // ログを外部に通知するイベント
    public event Action<string>? OutputReceived;
    public async Task<int> DownloadAsync(string url)
    {
        var exePath = "C:\\Users\\daizu\\AppData\\Local\\Microsoft\\WinGet\\Links\\yt-dlp.exe";
        var arguments = $"\"{url}\" -o \"C:\\Users\\daizu\\Downloads\\yt-dlp\\%(title)s.%(ext)s\"";

        return await RunCliAsync(exePath, arguments, CancellationToken.None);
    }
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