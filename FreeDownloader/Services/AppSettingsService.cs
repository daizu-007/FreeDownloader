namespace FreeDownloader.Services;

public class AppSettingsService
{
    public string DownloadDirectory { get; set; } = @"C:\Users\daizu\Downloads\yt-dlp";
    public string YtDlpExecutablePath { get; set; } = @"C:\Users\daizu\AppData\Local\Microsoft\WinGet\Links\yt-dlp.exe";
}