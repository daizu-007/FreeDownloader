using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FreeDownloader.Services;

public class AppSettingsService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _downloadDirectory = @"C:\Users\daizu\Downloads\yt-dlp";
    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set
        {
            if (_downloadDirectory == value) return;
            _downloadDirectory = value;
            OnPropertyChanged(); // ここでUIに通知
        }
    }

    private bool _useManagedTools = true;
    public bool UseManagedTools
    {
        get => _useManagedTools;
        set
        {
            if (_useManagedTools == value) return;
            _useManagedTools = value;
            OnPropertyChanged();
        }
    }

    private string _ytDlpExecutablePath = string.Empty;
    public string YtDlpExecutablePath
    {
        get => _ytDlpExecutablePath;
        set
        {
            if (_ytDlpExecutablePath == value) return;
            _ytDlpExecutablePath = value;
            OnPropertyChanged();
        }
    }

    private string _ffmpegExecutablePath = string.Empty;
    public string FfmpegExecutablePath
    {
        get => _ffmpegExecutablePath;
        set
        {
            if (_ffmpegExecutablePath == value) return;
            _ffmpegExecutablePath = value;
            OnPropertyChanged();
        }
    }
    
    private string _denoExecutablePath = string.Empty;
    public string DenoExecutablePath
    {
        get => _denoExecutablePath;
        set
        {
            if (_denoExecutablePath == value) return;
            _denoExecutablePath = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}