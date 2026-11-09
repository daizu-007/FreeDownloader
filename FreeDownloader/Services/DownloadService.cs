using System.Globalization;

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
    public Task<int> DownloadAsync(string url, DownloadOptions? options = null, CancellationToken ct = default, string? tempDir = null)
        => DownloadAsync(url, options, _settings.YtDlp, ct, tempDir);

    internal async Task<int> DownloadAsync(string url, DownloadOptions? options, YtDlpOptions ytDlp, CancellationToken ct, string? tempDir)
    {
        LastError = null;
        var browserArgument = BrowserCookieSource.GetBrowserArgument(_settings);
        var cookieFile = BrowserCookieSource.GetCookieFile(_settings);

        await _tools.EnsureAllToolsAsync(ct: ct);
        var exePath = await _tools.EnsureYtDlpAsync(ct: ct);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"yt-dlpが見つかりません: {exePath}");

        var outputDir = _settings.DownloadDirectory;
        Directory.CreateDirectory(outputDir);
        var outputTemplate = $"{ytDlp.FileNameTemplate}.%(ext)s";
        List<string> arguments = ["-o", outputTemplate, "-P", $"home:{outputDir}"];
        if (tempDir is not null)
        {
            Directory.CreateDirectory(tempDir);
            arguments.AddRange(["-P", $"temp:{tempDir}"]);
        }

        var ffmpegPath = _tools.ResolveFfmpegPath();
        if (File.Exists(ffmpegPath))
            arguments.AddRange(["--ffmpeg-location", Path.GetDirectoryName(ffmpegPath)!]);

        // 画質・形式・音声抽出のオプションを yt-dlp の引数に変換する
        var opts = options ?? new DownloadOptions();
        if (opts.AudioOnly)
        {
            // -x で音声ストリームだけを抽出。ffmpeg による再エンコードが必要
            arguments.AddRange(["-x", "--audio-format", "mp3"]);
        }
        else
        {
            // 画質指定: best は yt-dlp のデフォルト挙動(最高画質の動画+音声)。
            // 数値指定は「その高さ以下の最高」を選ばせる。bv* だけでは音声がないため
            // +ba で音声と結合し、結合できない場合のフォールバックとして best も併記するのが定石
            var format = opts.Quality == "best"
                ? "bv*+ba/b"
                : $"bv*[height<={opts.Quality}]+ba/b[height<={opts.Quality}]";
            arguments.AddRange(["-f", format, "--merge-output-format", opts.Format]);
        }

        var embedThumbnail = ytDlp.EmbedThumbnail && (opts.AudioOnly || opts.Format != "webm");
        var embedSubtitles = ytDlp.EmbedSubtitles && !opts.AudioOnly;
        arguments.AddRange([
            "-N", ytDlp.ConcurrentFragments.ToString(CultureInfo.InvariantCulture),
            embedThumbnail ? "--embed-thumbnail" : "--no-embed-thumbnail",
            embedSubtitles ? "--embed-subs" : "--no-embed-subs",
            ytDlp.EmbedChapters ? "--embed-chapters" : "--no-embed-chapters",
            ytDlp.EmbedMetadata ? "--embed-metadata" : "--no-embed-metadata"
        ]);

        // --merge-output-format は結合したときだけ効くため、結合しない動画もサムネイル対応の形式にそろえる。
        if (embedThumbnail && !opts.AudioOnly)
            arguments.AddRange(["--remux-video", opts.Format]);

        if (embedSubtitles)
        {
            arguments.AddRange(["--sub-langs", "all,-live_chat"]);
            if (opts.Format == "webm")
                arguments.AddRange(["--sub-format", "vtt/best"]);
        }

        if (ytDlp.RemoveSponsorSegments)
            arguments.AddRange(["--sponsorblock-remove", "sponsor"]);
        else
            arguments.Add("--no-sponsorblock");

        if (browserArgument is not null)
            arguments.AddRange(["--cookies-from-browser", browserArgument]);

        if (cookieFile is null)
        {
            arguments.AddRange(["--", url]);
            return await Cli.RunCliAsync(exePath, arguments, ct);
        }

        // yt-dlpは--cookiesで渡したファイルに書き戻すため、ダウンロードごとのコピーを渡す。
        var workingDirectory = Directory.CreateTempSubdirectory("FreeDownloader-cookies-");
        try
        {
            var workingCopy = Path.Combine(workingDirectory.FullName, "cookies.txt");
            File.Copy(cookieFile, workingCopy);
            File.SetAttributes(workingCopy, FileAttributes.Normal);
            arguments.AddRange(["--cookies", workingCopy, "--", url]);
            return await Cli.RunCliAsync(exePath, arguments, ct);
        }
        finally
        {
            try { workingDirectory.Delete(recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
