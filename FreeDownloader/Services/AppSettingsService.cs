using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreeDownloader.Services;

public class AppSettingsService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set => Set(ref _downloadDirectory, value);
    }

    private bool _useManagedTools = true;
    public bool UseManagedTools
    {
        get => _useManagedTools;
        set => Set(ref _useManagedTools, value);
    }

    private string _ytDlpExecutablePath = string.Empty;
    public string YtDlpExecutablePath
    {
        get => _ytDlpExecutablePath;
        set => Set(ref _ytDlpExecutablePath, value);
    }

    private string _ffmpegExecutablePath = string.Empty;
    public string FfmpegExecutablePath
    {
        get => _ffmpegExecutablePath;
        set => Set(ref _ffmpegExecutablePath, value);
    }

    private string _denoExecutablePath = string.Empty;
    public string DenoExecutablePath
    {
        get => _denoExecutablePath;
        set => Set(ref _denoExecutablePath, value);
    }

    // クッキーを借りるブラウザ("none" または chrome / edge / firefox など)
    private string _cookieBrowser = "none";
    public string CookieBrowser
    {
        get => _cookieBrowser;
        set => Set(ref _cookieBrowser, value);
    }

    // ===== 永続化 =====

    // %LOCALAPPDATA%\FreeDownloader\settings.json に保存する
    public static string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FreeDownloader",
        "settings.json");

    // Load() 中にプロパティを書き換えても Save() が走らないようにするフラグ
    private bool _suppressSave;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    // 起動時に保存済みのJSONから設定を読み込む(ファイルがなければ既定値のまま)
    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) return;

            var json = File.ReadAllText(SettingsFilePath);
            var dto = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.SettingsDto);
            if (dto is null) return;

            _suppressSave = true;
            try
            {
                if (dto.DownloadDirectory is not null) _downloadDirectory = dto.DownloadDirectory;
                if (dto.UseManagedTools is not null) _useManagedTools = dto.UseManagedTools.Value;
                if (dto.YtDlpExecutablePath is not null) _ytDlpExecutablePath = dto.YtDlpExecutablePath;
                if (dto.FfmpegExecutablePath is not null) _ffmpegExecutablePath = dto.FfmpegExecutablePath;
                if (dto.DenoExecutablePath is not null) _denoExecutablePath = dto.DenoExecutablePath;
                if (dto.CookieBrowser is not null) _cookieBrowser = dto.CookieBrowser;
            }
            finally
            {
                _suppressSave = false;
            }
        }
        catch (Exception)
        {
            // ファイルが破損している場合もアプリが起動できるよう、既定値のまま続行する
        }

        // 読み込んだ値をUIに反映させる
        OnPropertyChanged(nameof(DownloadDirectory));
        OnPropertyChanged(nameof(UseManagedTools));
        OnPropertyChanged(nameof(YtDlpExecutablePath));
        OnPropertyChanged(nameof(FfmpegExecutablePath));
        OnPropertyChanged(nameof(DenoExecutablePath));
        OnPropertyChanged(nameof(CookieBrowser));
    }

    public void Save()
    {
        var dto = new SettingsDto(
            DownloadDirectory,
            UseManagedTools,
            YtDlpExecutablePath,
            FfmpegExecutablePath,
            DenoExecutablePath,
            CookieBrowser);

        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(dto, AppSettingsJsonContext.Default.SettingsDto));
    }

    // プロパティ共通の変更処理:値更新 → UI通知 → ファイル保存
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
        if (!_suppressSave) Save();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// JSONとして保存するデータの入れ物
public sealed record SettingsDto(
    string? DownloadDirectory,
    bool? UseManagedTools,
    string? YtDlpExecutablePath,
    string? FfmpegExecutablePath,
    string? DenoExecutablePath,
    string? CookieBrowser);

// PublishTrimmed が有効な Release ビルドでも正しく動くように、
// シリアライザーをソース生成する(JsonSerializerContext)
[JsonSerializable(typeof(SettingsDto))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;
