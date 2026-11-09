using FreeDownloader.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using Windows.System;

namespace FreeDownloader.Pages;

public sealed partial class SettingsPage : Page
{
    public AppSettingsService Settings => App.Settings;
    
    public SettingsPage()
    {
        InitializeComponent();

        ConcurrentFragmentsBox.Maximum = YtDlpOptions.MaxConcurrentFragments;
        ConcurrentFragmentsBox.Value = Settings.ConcurrentFragments;
        ConcurrentFragmentsBox.ValueChanged += ConcurrentFragmentsBox_ValueChanged;

        FileNameTemplateBox.Text = Settings.FileNameTemplate;
        FileNameTemplateBox.TextChanged += FileNameTemplateBox_TextChanged;
    }

    private void ConcurrentFragmentsBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        Settings.ConcurrentFragments = double.IsNaN(args.NewValue)
            ? 1
            : (int)Math.Clamp(Math.Round(args.NewValue, MidpointRounding.AwayFromZero), sender.Minimum, sender.Maximum);
        sender.Value = Settings.ConcurrentFragments;
    }

    private void FileNameTemplateBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FileNameTemplateError.Visibility = YtDlpOptions.IsValidFileNameTemplate(FileNameTemplateBox.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void FileNameTemplateBox_LostFocus(object sender, RoutedEventArgs e)
        => Settings.FileNameTemplate = FileNameTemplateBox.Text;

    private void FileNameTemplateBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
            Settings.FileNameTemplate = FileNameTemplateBox.Text;
    }

    private void InsertFileNameVariable_Click(object sender, RoutedEventArgs e)
    {
        var variable = (string)((Button)sender).Tag;
        var caret = FileNameTemplateBox.SelectionStart + variable.Length;
        FileNameTemplateBox.SelectedText = variable;
        FileNameTemplateBox.Focus(FocusState.Programmatic);
        FileNameTemplateBox.Select(caret, 0);
        Settings.FileNameTemplate = FileNameTemplateBox.Text;
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
