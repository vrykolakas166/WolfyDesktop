using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WolfyDesktop.Models;

namespace WolfyDesktop
{
    public sealed partial class AudioManagerDialog : UserControl
    {
        private const string GOOGLE_DRIVE_FILE = "https://drive.usercontent.google.com/download?id=1jzex-vuQrpHUZXI_VhV34AyY97DkUc5c&export=download";
        private const string AUDIO_FILENAME = "lofi_rain.mp3";
        private static string MusicsFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics");
        private static string AudioFilePath => Path.Combine(MusicsFolderPath, AUDIO_FILENAME);

        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isDownloading;
        public readonly ObservableCollection<MusicItem> MusicItems = [];

        public AudioManagerDialog()
        {
            this.InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            MusicListView.ItemsSource = MusicItems;
            await LoadMusicListAsync();
            await UpdateStatusAsync();
        }

        public void SetMessage(string? message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                MessageText.Text = message;
                MessageText.Visibility = Visibility.Visible;
            }
            else
            {
                MessageText.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadMusicListAsync()
        {
            MusicItems.Clear();

            Directory.CreateDirectory(MusicsFolderPath);

            var supportedExtensions = new[] { ".mp3", ".m4a", ".wav", ".flac", ".aac", ".wma" };
            var musicFiles = Directory.GetFiles(MusicsFolderPath)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            var defaultMusic = GetDefaultMusicPath();

            foreach (var file in musicFiles)
            {
                var fileInfo = new FileInfo(file);
                var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);

                MusicItems.Add(new MusicItem
                {
                    FileName = Path.GetFileName(file),
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    FullPath = file,
                    FileSize = fileInfo.Length,
                    SizeText = $"{sizeInMB:F2} MB",
                    IsDefault = file.Equals(defaultMusic, StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        public static string? GetDefaultMusicPath()
        {
            try
            {
                var settingsFile = Path.Combine(MusicsFolderPath, "settings.txt");
                if (File.Exists(settingsFile))
                {
                    var defaultFileName = File.ReadAllText(settingsFile).Trim();
                    var fullPath = Path.Combine(MusicsFolderPath, defaultFileName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            catch { }

            // Fallback to default audio file
            return AudioFilePath;
        }

        private static void SaveDefaultMusicPath(string fileName)
        {
            try
            {
                var settingsFile = Path.Combine(MusicsFolderPath, "settings.txt");
                File.WriteAllText(settingsFile, fileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save default music: {ex.Message}");
            }
        }

        private async Task UpdateStatusAsync()
        {
            if (MusicItems.Count == 0)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
                StatusText.Text = "No music files found in library";
                DownloadButton.Content = "Download Default Audio";
                return;
            }

            // Check if default audio file exists
            bool defaultExists = File.Exists(AudioFilePath);

            if (defaultExists)
            {
                // Check if the default file is valid
                bool isValid = await IsAudioFileValid();

                if (isValid)
                {
                    StatusIcon.Glyph = "\uE930";
                    StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                    StatusText.Text = $"Music library: {MusicItems.Count} file(s)";
                    DownloadButton.Content = "Re-download Default Audio";
                }
                else
                {
                    StatusIcon.Glyph = "\uE7BA";
                    StatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                    StatusText.Text = "Default audio is corrupted";
                    DownloadButton.Content = "Re-download Default Audio";
                }
            }
            else
            {
                // Default doesn't exist, but we have other music
                StatusIcon.Glyph = "\uE930";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                StatusText.Text = $"Music library: {MusicItems.Count} file(s)";
                DownloadButton.Content = "Download Default Audio";
            }
        }

        private static async Task<bool> IsAudioFileValid()
        {
            try
            {
                // Check if file has minimum size (e.g., 1KB)
                var fileInfo = new FileInfo(AudioFilePath);
                if (fileInfo.Length < 1024)
                {
                    return false;
                }

                // Try to open the file as a media source
                var file = await StorageFile.GetFileFromPathAsync(AudioFilePath);
                var mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);

                // Create a temporary media player to validate the source
                using var testPlayer = new Windows.Media.Playback.MediaPlayer
                {
                    Source = mediaSource
                };

                // Wait a bit to see if it loads without errors
                await Task.Delay(100);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio validation failed: {ex.Message}");
                return false;
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
                return;

            _isDownloading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            DownloadButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressText.Visibility = Visibility.Visible;
            ProgressText.Text = "Downloading...";

            try
            {
                await DownloadAudioFile(_cancellationTokenSource.Token);

                StatusIcon.Glyph = "\uE930";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                StatusText.Text = $"Audio file downloaded successfully!\nLocation: {AudioFilePath}";
                DownloadButton.Content = "Re-download Audio";
                ProgressText.Foreground = new SolidColorBrush(Colors.Green);
                await LoadMusicListAsync();
            }
            catch (OperationCanceledException)
            {
                ProgressText.Text = "Download canceled";
                ProgressText.Foreground = new SolidColorBrush(Colors.Orange);
            }
            catch (Exception ex)
            {
                ProgressText.Text = $"Error: {ex.Message}";
                ProgressText.Foreground = new SolidColorBrush(Colors.Red);
            }
            finally
            {
                _isDownloading = false;
                DownloadButton.IsEnabled = true;
                CancelButton.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Visibility = Visibility.Collapsed;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                await UpdateStatusAsync();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            CancelButton.IsEnabled = false;
        }

        public void CancelDownload()
        {
            if (_isDownloading)
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(MusicsFolderPath);
            Process.Start("explorer.exe", MusicsFolderPath);
        }

        private async void AddMusicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainHandle);

                picker.FileTypeFilter.Add(".mp3");
                picker.FileTypeFilter.Add(".m4a");
                picker.FileTypeFilter.Add(".wav");
                picker.FileTypeFilter.Add(".flac");
                picker.FileTypeFilter.Add(".aac");
                picker.FileTypeFilter.Add(".wma");
                picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

                var files = await picker.PickMultipleFilesAsync();
                if (files != null && files.Count > 0)
                {
                    Directory.CreateDirectory(MusicsFolderPath);

                    foreach (var file in files)
                    {
                        var destPath = Path.Combine(MusicsFolderPath, file.Name);

                        // Check if file already exists
                        if (File.Exists(destPath))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file.Name);
                            var extension = Path.GetExtension(file.Name);
                            var counter = 1;

                            while (File.Exists(destPath))
                            {
                                destPath = Path.Combine(MusicsFolderPath, $"{fileName}_{counter}{extension}");
                                counter++;
                            }
                        }

                        await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(MusicsFolderPath), Path.GetFileName(destPath), NameCollisionOption.GenerateUniqueName);
                    }

                    await LoadMusicListAsync();

                    StatusIcon.Glyph = "\uE930";
                    StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                    StatusText.Text = $"Added {files.Count} music file(s)";
                }
            }
            catch (Exception ex)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                StatusText.Text = $"Error adding music: {ex.Message}";
            }
        }

        private async void SetDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (MusicListView.SelectedItem is MusicItem selectedItem)
            {
                SaveDefaultMusicPath(selectedItem.FileName);
                await LoadMusicListAsync();

                StatusIcon.Glyph = "\uE930";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                StatusText.Text = $"Set '{selectedItem.DisplayName}' as default";
            }
        }

        private async void RemoveMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (MusicListView.SelectedItem is MusicItem selectedItem)
            {
                try
                {
                    File.Delete(selectedItem.FullPath);

                    // If it was the default, reset to lofi_rain.m4a
                    if (selectedItem.IsDefault)
                    {
                        SaveDefaultMusicPath(AUDIO_FILENAME);
                    }

                    await LoadMusicListAsync();

                    StatusIcon.Glyph = "\uE930";
                    StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                    StatusText.Text = $"Removed '{selectedItem.DisplayName}'";
                }
                catch (Exception ex)
                {
                    StatusIcon.Glyph = "\uE7BA";
                    StatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                    StatusText.Text = $"Error removing music: {ex.Message}";
                }
            }
        }

        private void MusicListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = MusicListView.SelectedItem != null;
            SetDefaultButton.IsEnabled = hasSelection;
            RemoveMusicButton.IsEnabled = hasSelection;
        }

        private async Task DownloadAudioFile(CancellationToken ct)
        {
            var musicFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics");
            Directory.CreateDirectory(musicFolder);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromHours(2);

            await DispatcherQueue.EnqueueAsync(() =>
            {
                ProgressText.Text = "Connecting to Google Drive...";
            });

            var response = await httpClient.GetAsync(GOOGLE_DRIVE_FILE, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            
            await DispatcherQueue.EnqueueAsync(() =>
            {
                ProgressBar.IsIndeterminate = totalBytes == -1;
                if (totalBytes > 0)
                {
                    ProgressBar.Maximum = totalBytes;
                }
                ProgressText.Text = "Downloading audio file...";
            });

            var downloadedBytes = 0L;
            var lastUpdateTime = DateTime.Now;

            using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            using var fileStream = new FileStream(AudioFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                downloadedBytes += bytesRead;

                // Update UI every 100ms to avoid overwhelming the UI thread
                if ((DateTime.Now - lastUpdateTime).TotalMilliseconds >= 100)
                {
                    var currentDownloaded = downloadedBytes;
                    var currentTotal = totalBytes;

                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        if (currentTotal > 0)
                        {
                            ProgressBar.Value = currentDownloaded;
                            var percentage = (double)currentDownloaded / currentTotal * 100;
                            ProgressText.Text = $"Downloading: {percentage:F1}% ({currentDownloaded / 1024 / 1024:F2} MB / {currentTotal / 1024 / 1024:F2} MB)";
                        }
                        else
                        {
                            ProgressText.Text = $"Downloading: {currentDownloaded / 1024 / 1024:F2} MB";
                        }
                    });

                    lastUpdateTime = DateTime.Now;
                }
            }

            await DispatcherQueue.EnqueueAsync(() =>
            {
                ProgressText.Text = "Download complete!";
            });
        }
    }
}

// Extension method for DispatcherQueue
public static class DispatcherQueueExtensions
{
    public static Task EnqueueAsync(this Microsoft.UI.Dispatching.DispatcherQueue dispatcher, Microsoft.UI.Dispatching.DispatcherQueueHandler callback)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        if (!dispatcher.TryEnqueue(() =>
        {
            try
            {
                callback();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            tcs.SetException(new InvalidOperationException("Failed to enqueue operation"));
        }

        return tcs.Task;
    }
}
