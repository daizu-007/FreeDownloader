using FreeDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace FreeDownloader.Pages;

public sealed partial class SettingsPage : Page
{
    public AppSettingsService Settings => App.Settings;
    
    public SettingsPage()
    {
        InitializeComponent();
    }

    private Visibility CustomCookieSettingsVisibility(string browser)
        => browser == "custom" ? Visibility.Visible : Visibility.Collapsed;

    private Visibility CookieProfileVisibility(string source)
        => source is "firefox" or "chromium" ? Visibility.Visible : Visibility.Collapsed;

    private Visibility CookieFileVisibility(string source)
        => source == "file" ? Visibility.Visible : Visibility.Collapsed;

    private async void BrowseCookieProfile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
            Settings.CustomCookieProfilePath = folder.Path;
    }

    private async void BrowseCookieFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
            Settings.CustomCookieFilePath = file.Path;
    }
}
