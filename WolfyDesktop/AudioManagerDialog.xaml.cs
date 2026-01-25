using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace WolfyDesktop
{
    public sealed partial class AudioManagerDialog : UserControl
    {
        private const string GOOGLE_DRIVE_FILE = "https://drive.usercontent.google.com/download?id=1x2JiSEDsj-MRPA6fhBjqf8jL12QSAPu4&export=download&confirm=t";
        private const string AUDIO_FILENAME = "lofi_rain.m4a";
        private static string AudioFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics", AUDIO_FILENAME);

        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isDownloading;

        public AudioManagerDialog()
        {
            this.InitializeComponent();
            _ = UpdateStatusAsync();
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

        private async Task UpdateStatusAsync()
        {
            bool fileExists = File.Exists(AudioFilePath);

            if (!fileExists)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
                StatusText.Text = "Audio file not found";
                DownloadButton.Content = "Download Audio";
                return;
            }

            // File exists, now check if it's valid
            bool isValid = await IsAudioFileValid();

            if (isValid)
            {
                StatusIcon.Glyph = "\uE930";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                StatusText.Text = "Audio file exists";
                DownloadButton.Content = "Re-download Audio";
            }
            else
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                StatusText.Text = "Audio file is corrupted or cannot be played";
                DownloadButton.Content = "Re-download Audio";
            }
        }

        private async Task<bool> IsAudioFileValid()
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
            var musicFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics");
            Directory.CreateDirectory(musicFolder);
            Process.Start("explorer.exe", musicFolder);
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
