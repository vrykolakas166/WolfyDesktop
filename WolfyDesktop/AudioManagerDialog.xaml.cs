using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using WolfyDesktop.Services;
using WolfyDesktop.ViewModels;

namespace WolfyDesktop;

public sealed partial class AudioManagerDialog : UserControl
{
    public AudioManagerDialogViewModel? ViewModel { get; private set; }
    
    public AudioManagerDialog()
    {
        this.InitializeComponent();
        InitializeWithServices();
    }

    private void InitializeWithServices()
    {
        // Get services from DI container
        var fileService = App.Current.Services.GetRequiredService<IFileService>();
        var downloadService = App.Current.Services.GetRequiredService<IDownloadService>();
        var settingsService = App.Current.Services.GetRequiredService<ISettingsService>();

        ViewModel = new AudioManagerDialogViewModel(fileService, downloadService, settingsService);

        // Set up data binding
        MusicListView.ItemsSource = ViewModel.MusicItems;

        // Subscribe to ViewModel property changes for UI updates
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Initialize asynchronously
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (ViewModel != null)
        {
            await ViewModel.InitializeAsync();
            UpdateUIFromViewModel();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update UI elements that aren't data-bound
        switch (e.PropertyName)
        {
            case nameof(ViewModel.StatusIconGlyph):
                StatusIcon.Glyph = ViewModel?.StatusIconGlyph ?? "\uE7BA";
                break;
            case nameof(ViewModel.StatusIconForeground):
                StatusIcon.Foreground = ViewModel?.StatusIconForeground;
                break;
            case nameof(ViewModel.StatusText):
                StatusText.Text = ViewModel?.StatusText ?? "";
                break;
            case nameof(ViewModel.DownloadButtonText):
                DownloadButton.Content = ViewModel?.DownloadButtonText ?? "Download";
                break;
            case nameof(ViewModel.DownloadButtonEnabled):
                DownloadButton.IsEnabled = ViewModel?.DownloadButtonEnabled ?? true;
                break;
            case nameof(ViewModel.CancelButtonVisible):
                CancelButton.Visibility = ViewModel?.CancelButtonVisible == true ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(ViewModel.ProgressBarVisible):
                ProgressBar.Visibility = ViewModel?.ProgressBarVisible == true ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(ViewModel.ProgressBarIndeterminate):
                ProgressBar.IsIndeterminate = ViewModel?.ProgressBarIndeterminate ?? false;
                break;
            case nameof(ViewModel.ProgressValue):
                ProgressBar.Value = ViewModel?.ProgressValue ?? 0;
                break;
            case nameof(ViewModel.ProgressText):
                ProgressText.Text = ViewModel?.ProgressText ?? "";
                break;
            case nameof(ViewModel.ProgressTextVisible):
                ProgressText.Visibility = ViewModel?.ProgressTextVisible == true ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(ViewModel.ProgressTextForeground):
                ProgressText.Foreground = ViewModel?.ProgressTextForeground;
                break;
            case nameof(ViewModel.MessageVisible):
                MessageText.Visibility = ViewModel?.MessageVisible == true ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(ViewModel.Message):
                MessageText.Text = ViewModel?.Message ?? "";
                break;
        }
    }

    private void UpdateUIFromViewModel()
    {
        if (ViewModel == null) return;

        StatusIcon.Glyph = ViewModel.StatusIconGlyph;
        StatusIcon.Foreground = ViewModel.StatusIconForeground;
        StatusText.Text = ViewModel.StatusText;
        DownloadButton.Content = ViewModel.DownloadButtonText;
        DownloadButton.IsEnabled = ViewModel.DownloadButtonEnabled;
        CancelButton.Visibility = ViewModel.CancelButtonVisible ? Visibility.Visible : Visibility.Collapsed;
        ProgressBar.Visibility = ViewModel.ProgressBarVisible ? Visibility.Visible : Visibility.Collapsed;
        ProgressBar.IsIndeterminate = ViewModel.ProgressBarIndeterminate;
        ProgressText.Visibility = ViewModel.ProgressTextVisible ? Visibility.Visible : Visibility.Collapsed;
        ProgressText.Text = ViewModel.ProgressText;
        MessageText.Visibility = ViewModel.MessageVisible ? Visibility.Visible : Visibility.Collapsed;
        MessageText.Text = ViewModel.Message ?? "";
    }

    public void SetMessage(string? message)
    {
        ViewModel?.SetMessage(message);
    }

    public void CancelDownload()
    {
        ViewModel?.CancelDownload();
    }

    #region Button Click Handlers

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.DownloadCommand.ExecuteAsync(null);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CancelCommand.Execute(null);
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.OpenFolderCommand.Execute(null);
    }

    private async void AddMusicButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.BrowseCommand.ExecuteAsync(null);
        }
    }

    private async void SetDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.SetDefaultCommand.ExecuteAsync(null);
        }
    }

    private async void RemoveMusicButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.DeleteCommand.ExecuteAsync(null);
        }
    }

    private void MusicListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && MusicListView.SelectedItem is Models.MusicItem selectedItem)
        {
            ViewModel.SelectedMusicItem = selectedItem;
        }
        else if (ViewModel != null)
        {
            ViewModel.SelectedMusicItem = null;
        }

        // Update button states
        var hasSelection = MusicListView.SelectedItem != null;
        SetDefaultButton.IsEnabled = hasSelection;
        RemoveMusicButton.IsEnabled = hasSelection;
    }

    #endregion
}
