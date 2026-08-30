using FreeDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FreeDownloader.Pages;

public sealed partial class QueuePage : Page
{
    public QueuePage()
    {
        InitializeComponent();
        QueueList.ItemsSource = App.Queue.Items;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetItem(sender) is { } item)
            App.Queue.Cancel(item);
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetItem(sender) is { } item)
            App.Queue.Pause(item);
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetItem(sender) is { } item)
            App.Queue.Resume(item);
    }

    // ボタンの親要素(DataContext = DownloadItem)から対象の項目を取り出す
    private static DownloadItem? GetItem(object sender)
        => (sender as FrameworkElement)?.DataContext as DownloadItem;
}
