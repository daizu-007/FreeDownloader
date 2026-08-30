using System.Diagnostics;
using System.Text;

namespace FreeDownloader.Services;

// 任意のCLIツールをバックグラウンドで実行する汎用サービス
public class CliProcessService
{
    // プロセスからの出力を外部に通知するイベント
    public event Action<string>? OutputReceived;

    // CLIを裏で実行する汎用メソッド
    public async Task<int> RunCliAsync(string fileName, string arguments, CancellationToken ct = default)
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

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // WaitForExitAsyncは待機を止めるだけでプロセスを殺さないため、明示的に終了する
            try { process.Kill(entireProcessTree: true); } catch { }
            // ファイルロックが解放されるよう、終了を待ってから投げ直す
            try { await process.WaitForExitAsync(); } catch { }
            throw;
        }

        return process.ExitCode;
    }
}