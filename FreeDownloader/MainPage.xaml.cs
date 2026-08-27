using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Text;

namespace FreeDownloader;

public sealed partial class MainPage : Page
{
    private CancellationTokenSource? _cts;

    public MainPage()
    {
        InitializeComponent();
    }

    // ダウンロードボタン
    private async void DownloadButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();
        string downloadPath = "C:\\Users\\daizu\\Downloads\\yt-dlp";
        
        if (string.IsNullOrWhiteSpace(url))
        {
            ResultText.Text = "URLを入力してください";
            return;
        }

        // UIをロック
        DownloadButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        LoadingRing.IsActive = true;
        ResultText.Text = $"実行開始: {url}\n";

        _cts = new CancellationTokenSource();

        try
        {
            var exePath = "C:\\Users\\daizu\\AppData\\Local\\Microsoft\\WinGet\\Links\\yt-dlp.exe";
            
            if (!System.IO.File.Exists(exePath))
            {
                ResultText.Text += $"エラー: {exePath} が見つかりません\nwingetで入れたならフルパスを指定してください";
                return;
            }
            
            var args = $"\"{url}\" -o \"{downloadPath}\\%(title)s.%(ext)s\"";

            int exitCode = await RunCliAsync(exePath, args, _cts.Token);
            ResultText.Text += $"\n完了！終了コード: {exitCode}";
        }
        catch (OperationCanceledException)
        {
            ResultText.Text += "\nキャンセルされました";
        }
        catch (Exception ex)
        {
            ResultText.Text += $"\nエラー: {ex.Message}";
        }
        finally
        {
            DownloadButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            LoadingRing.IsActive = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    // ★CLIを裏で実行する汎用メソッド（コピペで使える）
    private async Task<int> RunCliAsync(string fileName, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false, // 必須: 出力を取るにはfalse
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.Start();

        // 出力をリアルタイムでResultTextに追記
        // UIスレッド以外からUIを触るので DispatcherQueue を使う
        process.OutputDataReceived += (s, args) =>
        {
            if (args.Data != null)
                DispatcherQueue.TryEnqueue(() => ResultText.Text += args.Data + "\n");
        };
        process.ErrorDataReceived += (s, args) =>
        {
            if (args.Data != null)
                DispatcherQueue.TryEnqueue(() => ResultText.Text += "[ERR] " + args.Data + "\n");
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // 終了まで非同期で待つ（ここでUIは固まらない）
        await process.WaitForExitAsync(ct);

        return process.ExitCode;
    }
}