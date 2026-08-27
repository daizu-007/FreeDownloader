using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FreeDownloader;

/// <summary>
/// The application window. This hosts a NavigationView that displays pages.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // 初期選択: ダウンロードページ
        NavView.SelectedItem = NavView.MenuItems[0];
        NavigateTo("Download");
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        Type pageType = tag switch
        {
            "Queue" => typeof(Pages.QueuePage),
            "Settings" => typeof(Pages.SettingsPage),
            _ => typeof(Pages.DownloadPage),
        };

        if (RootFrame.CurrentSourcePageType != pageType)
        {
            RootFrame.Navigate(pageType);
        }
    }
}
