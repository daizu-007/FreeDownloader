using System.Collections.ObjectModel;

namespace FreeDownloader.Services;

public class DownloadQueueService
{
    public ObservableCollection<DownloadItem> Items { get; } = new();
}
public record DownloadItem(string Url, string Status);