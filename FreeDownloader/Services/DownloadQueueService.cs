using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace FreeDownloader.Services;

// キューの1件分。状態が変わるので INotifyPropertyChanged を実装する
public class DownloadItem : INotifyPropertyChanged
{
    public string Url { get; }

    // この項目専用のキャンセルトークン(再開時に作り直す)
    internal CancellationTokenSource Cts { get; set; } = new();

    // 一時停止のためのキャンセルかどうかを区別するフラグ
    internal bool PauseRequested { get; set; }

    // この項目専用の一時ファイル置き場(キャンセル時に削除する)
    internal string TempDir { get; }

    // ユーザーが選択したダウンロードオプション
    public DownloadOptions Options { get; }

    public DownloadItem(string url, string tempDir, DownloadOptions? options = null)
    {
        Url = url;
        TempDir = tempDir;
        Options = options ?? new DownloadOptions();
    }

    private string _status = "待機中";
    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            // Statusから導出されるUI用プロパティにも通知する
            OnPropertyChanged(nameof(IsDownloading));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsActive));
        }
    }

    // ダウンロード中なら「一時停止」を押せる
    public bool IsDownloading => Status == "ダウンロード中";

    // 一時停止中なら「再開」を押せる
    public bool IsPaused => Status == "一時停止中";

    // 待機中・ダウンロード中・一時停止中はキャンセルできる
    public bool IsActive => Status is "待機中" or "ダウンロード中" or "一時停止中";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DownloadQueueService
{
    public ObservableCollection<DownloadItem> Items { get; } = new();

    // キュー本体。FIFO(先に入れた順)で処理される
    private readonly Channel<DownloadItem> _channel = Channel.CreateUnbounded<DownloadItem>();
    private readonly DownloadService _downloader;

    public DownloadQueueService(DownloadService downloader)
    {
        _downloader = downloader;
        // バックグラウンドで1件ずつ処理するループを開始
        _ = ProcessLoopAsync();
    }

    public void Enqueue(string url, DownloadOptions? options = null)
    {
        var item = new DownloadItem(url, CreateTempDir(), options);
        Items.Add(item);
        _channel.Writer.TryWrite(item);
    }

    public void Cancel(DownloadItem item)
    {
        item.Cts.Cancel();
        // まだ始まっていない項目は、すぐに状態へ反映して一時ファイルを削除する
        if (item.Status != "ダウンロード中")
        {
            item.Status = "キャンセル";
            DeleteDirectory(item.TempDir);
        }
    }

    public void Pause(DownloadItem item)
    {
        item.PauseRequested = true;
        item.Cts.Cancel(); // プロセスを止める(部分ファイルは残る)
    }

    public void Resume(DownloadItem item)
    {
        item.PauseRequested = false;
        item.Cts = new CancellationTokenSource(); // 新しいトークンでやり直す
        item.Status = "待機中";
        _channel.Writer.TryWrite(item); // 再びキューに並べる
    }

    private async Task ProcessLoopAsync()
    {
        await foreach (var item in _channel.Reader.ReadAllAsync())
        {
            try
            {
                // 待機中にキャンセルされていたら、ここで即座に検出する
                item.Cts.Token.ThrowIfCancellationRequested();

                item.Status = "ダウンロード中";
                int exitCode = await _downloader.DownloadAsync(item.Url, item.Options, item.Cts.Token, item.TempDir);
                item.Status = exitCode == 0
                    ? "完了"
                    : $"エラー(終了コード {exitCode}): {_downloader.LastError ?? "詳細不明"}";
                DeleteDirectory(item.TempDir); // 完了・失敗時は一時ディレクトリを掃除
            }
            catch (OperationCanceledException)
            {
                if (item.PauseRequested)
                {
                    item.Status = "一時停止中"; // 部分ファイルは残す(再開のため)
                }
                else
                {
                    item.Status = "キャンセル";
                    DeleteDirectory(item.TempDir); // キャンセル時は部分ファイルを削除
                }
            }
            catch (Exception ex)
            {
                item.Status = "エラー: " + ex.Message;
                DeleteDirectory(item.TempDir);
            }
        }
    }

    // 項目ごとの一時ディレクトリを生成する
    private static string CreateTempDir()
    {
        var baseDir = Path.Combine(App.Settings.DownloadDirectory, ".free-downloader-tmp");
        return Path.Combine(baseDir, Guid.NewGuid().ToString("N"));
    }

    // 一時ディレクトリを削除する(ベストエフォート)
    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch { }
    }
}
