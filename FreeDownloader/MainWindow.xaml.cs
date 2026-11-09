using System.ComponentModel;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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

        ApplyWindowBackdrop();
        App.Settings.PropertyChanged += Settings_PropertyChanged;
        Closed += (_, _) => App.Settings.PropertyChanged -= Settings_PropertyChanged;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(App.Settings.WindowBackdrop))
        {
            ApplyWindowBackdrop();
        }
    }

    private void ApplyWindowBackdrop()
    {
        SystemBackdrop = App.Settings.WindowBackdrop switch
        {
            "MicaAlt" => new MicaBackdrop { Kind = MicaKind.BaseAlt },
            "DesktopAcrylic" => new DesktopAcrylicBackdrop(),
            "Solid" => null,
            _ => new MicaBackdrop(),
        };

        SolidBackground.Visibility = SystemBackdrop is null ? Visibility.Visible : Visibility.Collapsed;
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
