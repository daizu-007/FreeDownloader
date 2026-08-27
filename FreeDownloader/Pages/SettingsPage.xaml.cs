using FreeDownloader.Services;
using Microsoft.UI.Xaml.Controls;

namespace FreeDownloader.Pages;

public sealed partial class SettingsPage : Page
{
    public AppSettingsService Settings => App.Settings;
    
    public SettingsPage()
    {
        InitializeComponent();
    }
}
