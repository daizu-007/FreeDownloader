using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Text;

namespace FreeDownloader.Pages;

public sealed partial class DownloadPage : Page
{
    private CancellationTokenSource? _cts;

    public DownloadPage()
    {
        InitializeComponent();
        App.Downloader.OutputReceived += OnLogReceived;
    }
    
    private void OnLogReceived(string log)
    {
        // UIスレッドでResultTextにログを追加
        DispatcherQueue.TryEnqueue(() =>
        {
            ResultText.Text += log + "\n";
        });
    }
    
    // YT-DLPダウンロードボタン
    private async void InstallYtDlpButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string exePath = "C:\\Users\\daizu\\AppData\\Local\\Microsoft\\WinGet\\Links\\yt-dlp.exe";
        string wingetArgs = "install yt-dlp";

        // wingetでインストール
        int exitCode = await App.Downloader.DownloadAsync("UrlTextBox.Text");

        if (exitCode == 0)
        {
            ResultText.Text = $"YT-DLPが正常にインストールされました: {exePath}";
        }
        else
        {
            ResultText.Text = $"YT-DLPのインストールに失敗しました。終了コード: {exitCode}";
        }
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

            int exitCode = await App.Downloader.DownloadAsync(url);
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
}
