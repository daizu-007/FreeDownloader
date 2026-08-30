using FreeDownloader.Services;
using Microsoft.UI.Xaml.Controls;

namespace FreeDownloader.Pages;

public sealed partial class DownloadPage : Page
{
    public DownloadPage()
    {
        InitializeComponent();
    }

    // キューに追加ボタン
    private void DownloadButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            ResultText.Text = "URLを入力してください";
            return;
        }

        var options = new DownloadOptions
        {
            // ComboBoxItem の Tag に入れた値を取り出す(未選択なら既定値)
            Quality = (QualityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "best",
            Format = (FormatComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "mp4",
            AudioOnly = AudioOnlyToggle.IsOn
        };

        // await しない = 追加したらすぐにボタンを解放する
        App.Queue.Enqueue(url, options);

        ResultText.Text = $"キューに追加しました: {url}";
        UrlTextBox.Text = "";
    }

    // 音声だけ取り出すときは画質・コンテナ形式が無意味なのでグレーアウトする
    private void AudioOnlyToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        QualityComboBox.IsEnabled = !AudioOnlyToggle.IsOn;
        FormatComboBox.IsEnabled = !AudioOnlyToggle.IsOn;
    }
}
