namespace FreeDownloader.Services;

// ダウンロードオプションのまとめ。UI → キュー → DownloadService へと渡される
public class DownloadOptions
{
    // "best" または高さの上限(px)を表す数値文字列
    public string Quality { get; init; } = "best";

    // コンテナ形式: mp4 / mkv / webm
    public string Format { get; init; } = "mp4";

    // 音声だけ抽出するかどうか
    public bool AudioOnly { get; init; }
}

public class DownloadService
{
    // CLI実行と出力ログを担当するサービス
    public CliProcessService Cli { get; }
    private readonly ToolsManagerService _tools;
    private readonly AppSettingsService _settings;

    // 直近の実行で yt-dlp が出力したエラーメッセージ(キュー画面の表示用)
    public string? LastError { get; private set; }

    // 引数なしで初期化されたときにApp.Settingsを引数に渡して初期化する
    public DownloadService() : this(App.Settings) { }
    
    // 初期化関数
    public DownloadService(AppSettingsService settings)
    {
        _tools = new ToolsManagerService(settings);
        _settings = settings;
        Cli = new CliProcessService();

        // yt-dlp はエラーを標準エラー出力に流す。CliProcessService はそこに
        // "[ERR] " 接頭辞を付けて通知するので、ERROR 行だけ取り出して保持する
        Cli.OutputReceived += line =>
        {
            var text = line.StartsWith("[ERR] ", StringComparison.Ordinal) ? line[6..] : line;
            if (text.StartsWith("ERROR:", StringComparison.Ordinal))
                LastError = text;
        };
    }

    // Task<>は値を返すことを約束する。TSで言うところのPromiseに近い
    public async Task<int> DownloadAsync(string url, DownloadOptions? options = null, CancellationToken ct = default, string? tempDir = null)
    {
        LastError = null;
        await _tools.EnsureAllToolsAsync(ct: ct);
        var exePath = await _tools.EnsureYtDlpAsync(ct: ct);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"yt-dlpが見つかりません: {exePath}");

        var outputDir = App.Settings.DownloadDirectory;
        Directory.CreateDirectory(outputDir);
        if (tempDir is not null)
            Directory.CreateDirectory(tempDir);

        var ffmpegPath = _tools.ResolveFfmpegPath();
        var ffmpegArg = File.Exists(ffmpegPath) ? $" --ffmpeg-location \"{Path.GetDirectoryName(ffmpegPath)}\"" : "";

        // 画質・形式・音声抽出のオプションを yt-dlp の引数に変換する
        var opts = options ?? new DownloadOptions();
        string optionArgs;
        if (opts.AudioOnly)
        {
            // -x で音声ストリームだけを抽出。ffmpeg による再エンコードが必要
            optionArgs = " -x --audio-format mp3";
        }
        else
        {
            // 画質指定: best は yt-dlp のデフォルト挙動(最高画質の動画+音声)。
            // 数値指定は「その高さ以下の最高」を選ばせる。bv* だけでは音声がないため
            // +ba で音声と結合し、結合できない場合のフォールバックとして best も併記するのが定石
            optionArgs = opts.Quality == "best"
                ? " -f \"bv*+ba/b\""
                : $" -f \"bv*[height<={opts.Quality}]+ba/b[height<={opts.Quality}]\"";
            // 映像と音声をマージするときのコンテナ形式を指定
            optionArgs += $" --merge-output-format {opts.Format}";
        }

        // tempDir指定時は一時ファイルを専用ディレクトリに隔離し、完了時にhomeへ移動させる
        var arguments = tempDir is not null
            ? $"\"{url}\" -o \"%(title)s.%(ext)s\" -P \"home:{outputDir}\" -P \"temp:{tempDir}\"{optionArgs}{ffmpegArg}"
            : $"\"{url}\" -o \"{Path.Combine(outputDir, "%(title)s.%(ext)s")}\"{optionArgs}{ffmpegArg}";

        // 設定で指定された場合、ブラウザからCookieを借りて認証する
        // (YouTubeの「Sign in to confirm you're not a bot」対策)
        var cookieBrowser = _settings.CookieBrowser;
        if (!string.IsNullOrWhiteSpace(cookieBrowser) && cookieBrowser != "none")
            arguments += $" --cookies-from-browser {cookieBrowser}";

        return await Cli.RunCliAsync(exePath, arguments, ct);
    }
}