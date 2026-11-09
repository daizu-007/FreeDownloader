using System.Text.Json;

namespace FreeDownloader.Services;

public sealed record YtDlpProgress(string Stage, string? Title, int? PlaylistPosition, int? PlaylistCount, JsonElement Percent)
{
    private const string Prefix = "[FreeDownloader progress] ";

    internal static YtDlpProgress Preparing { get; } = new(string.Empty, null, null, null, default);

    public double? FilePercent { get; } = Percent.ValueKind == JsonValueKind.Number
        && Percent.TryGetDouble(out var value) && double.IsFinite(value)
            ? Math.Clamp(value, 0, 100)
            : null;

    public double FileValue => FilePercent ?? 0;
    public bool ShowFileBar => Stage != "after_video";
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasPlaylistPosition => PlaylistPosition is > 0;
    public bool HasPlaylistTotal => HasPlaylistPosition && PlaylistCount >= PlaylistPosition;
    public double PlaylistValue => PlaylistPosition ?? 0;
    public double PlaylistMaximum => PlaylistCount ?? 1;
    public string PlaylistText => HasPlaylistTotal
        ? $"プレイリストの {PlaylistPosition} / {PlaylistCount} 件目"
        : $"プレイリストの {PlaylistPosition} 件目";

    public string FileText => Stage switch
    {
        "download" when FilePercent is double percent => $"ファイルの転送 {percent:0.0}%",
        "download" => "ファイルを転送中",
        "post_process" => "結合・変換などの処理中",
        "after_video" => "項目の処理が完了",
        _ => "ダウンロードを準備中"
    };

    internal static void AddArguments(List<string> arguments)
    {
        arguments.AddRange([
            "--progress", "--newline", "--progress-delta", "0.25", "--no-quiet",
            "--print", Template("before_dl"),
            "--progress-template", Template("download", "info.", "%(progress._percent|null)j"),
            "--print", Template("post_process"),
            "--print", Template("after_video")
        ]);
    }

    private static string Template(string stage, string info = "", string percent = "null") =>
        $$"""{{stage}}:{{Prefix}}{"Stage":"{{stage}}","Title":%({{info}}title|null)j,""" +
        $$""" "PlaylistPosition":%({{info}}playlist_autonumber|null)j,"PlaylistCount":%({{info}}n_entries|null)j,""" +
        $$""" "Percent":{{percent}}}""";

    internal static YtDlpProgress? Parse(string line)
    {
        if (!line.StartsWith(Prefix, StringComparison.Ordinal))
            return null;

        try { return JsonSerializer.Deserialize<YtDlpProgress>(line.AsSpan(Prefix.Length)); }
        catch (JsonException) { return null; }
    }
}
