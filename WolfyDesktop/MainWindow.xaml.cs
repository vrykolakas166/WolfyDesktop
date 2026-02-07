using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace WolfyDesktop
{
    static class MyTheme
    {
        internal const string DARK = "DARK";
        internal const string LIGHT = "LIGHT";
        internal const string SYSTEM = "SYSTEM";
    }

    public sealed partial class MainWindow : Window
    {
        private AppWindow? _appWindow;

        private readonly DispatcherTimer timer;
        private readonly DispatcherTimer _inactivityTimer;
        private string currentHours = "00";
        private string currentMinutes = "00";
        private string currentSeconds = "00";

        private string _currentTheme = MyTheme.DARK;

        private bool IsChilled = false;

        private static string AudioFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics", "lofi_rain.m4a");
        private static string MusicsFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Musics");

        public MainWindow()
        {
            this.InitializeComponent();

            // Set up the clock timer
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Update every second
            };
            timer.Tick += (_, _1) =>
            {
                UpdateClockDisplay();
            };
            timer.Start();

            // Set up the inactivity timer for audio player auto-hide
            _inactivityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _inactivityTimer.Tick += InactivityTimer_Tick;

            // Subscribe to user input events
            MainContainer.PointerMoved += OnUserActivity;
            MainContainer.KeyDown += (s, e) => OnUserActivity(s, null);

            // Theme
            SetTheme(MyTheme.DARK);

            // Borderless fullscreen
            SetFullScreen();

            // Center the clock
            SetPosition();

            // Initialize the clock display
            UpdateClockDisplay();

            // Set initial font size
            SetFontSize();

            // Subscribe to the SizeChanged event to adjust font size on window resize
            this.SizeChanged += MainWindow_SizeChanged;

            // Initial audio check
            _ = CheckAudioFileExists();
        }

        private void OnUserActivity(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs? e)
        {
            // Show the audio player panel
            if (AudioPlayerPanel.Visibility == Visibility.Collapsed && File.Exists(AudioManagerDialog.GetDefaultMusicPath()))
            {
                ShowAudioPlayerPanel();
            }

            // Reset the inactivity timer
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }

        private void InactivityTimer_Tick(object? sender, object e)
        {
            // Hide the audio player panel after inactivity
            HideAudioPlayerPanel();
            _inactivityTimer.Stop();
        }

        private void ShowAudioPlayerPanel()
        {
            AudioPlayerPanel.Visibility = Visibility.Visible;

            // Create fade-in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, AudioPlayerPanel);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }

        private void HideAudioPlayerPanel()
        {
            // Create fade-out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeOut, AudioPlayerPanel);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");

            storyboard.Completed += (s, e) =>
            {
                AudioPlayerPanel.Visibility = Visibility.Collapsed;
            };

            storyboard.Children.Add(fadeOut);
            storyboard.Begin();
        }

        private void SetTheme(string selectedTheme)
        {
            ComboBoxThemes.SelectedValue = _currentTheme;
            // Apply the selected theme to the Window's content
            if (Content is FrameworkElement)
            {
                switch (selectedTheme)
                {
                    case MyTheme.LIGHT:
                        MainContainer.Background = new SolidColorBrush(Colors.White);
                        MainContainer.RequestedTheme = ElementTheme.Light;
                        break;

                    case MyTheme.DARK:
                        MainContainer.Background = new SolidColorBrush(Colors.Black);
                        MainContainer.RequestedTheme = ElementTheme.Dark;
                        break;

                    case MyTheme.SYSTEM:
                        // System theme can be set indirectly by not specifying a theme here
                        MainContainer.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
                        MainContainer.RequestedTheme = ElementTheme.Default;
                        break;
                }
            }
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            // Update when the window is resized
            SetPosition();
            SetFontSize();
        }

        private void ButtonFocusMode_Click(object _, RoutedEventArgs _1)
        {
            ButtonModeClick();
        }

        private async void ButtonChillMode_Click(object _, RoutedEventArgs _1)
        {
            if (IsChilled)
            {
                // Stop playback and remove the media source
                var mediaPlayer = BackgroundVideo.MediaPlayer;
                mediaPlayer.Pause(); // Pause the video if it's currently playing
                mediaPlayer.Source = null; // Clear the media source
                mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                IsChilled = false;
            }
            else
            {
                // Load the video from the embedded resource
                var file = await StorageFile.GetFileFromPathAsync($"{AppDomain.CurrentDomain.BaseDirectory}Assets\\background.mp4");
                var mediaPlayer = BackgroundVideo.MediaPlayer;
                mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                mediaPlayer.Play();
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

                // Load audio


                if (await CheckAudioFileExists())
                {
                    await InitializeBackgroundAudioMediaPlayer();
                }

                IsChilled = true;
            }
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            // Restart the video when it ends
            sender.Play();
        }

        private async Task<bool> CheckAudioFileExists()
        {
            var defaultMusicPath = AudioManagerDialog.GetDefaultMusicPath();

            // Check if the default music file exists
            if (!File.Exists(defaultMusicPath))
            {
                AudioPlayerPanel.Visibility = Visibility.Collapsed;
                return false;
            }

            // Don't automatically show - let user activity trigger it
            // AudioPlayerPanel will be shown on first user interaction
            return true;
        }

        private async Task<bool> InitializeBackgroundAudioMediaPlayer()
        {
            if (BackgroundAudio.MediaPlayer == null)
            {
                var defaultMusicPath = AudioManagerDialog.GetDefaultMusicPath();

                // Initialize and load the audio
                var file = await StorageFile.GetFileFromPathAsync(defaultMusicPath);
                var mediaPlayer = new Windows.Media.Playback.MediaPlayer
                {
                    Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file),
                    Volume = VolumeSlider.Value / 100.0
                };
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                BackgroundAudio.SetMediaPlayer(mediaPlayer);
                BackgroundAudio.MediaPlayer?.Play();
                PlayPauseIcon.Glyph = "\uE769"; // Pause icon
                return true;
            }

            return false;
        }

        private async void ButtonPlayPause_Click(object _, RoutedEventArgs _1)
        {
            // Reset inactivity timer on interaction
            OnUserActivity(this, null);

            if (!await CheckAudioFileExists())
            {
                return;
            }

            if (await InitializeBackgroundAudioMediaPlayer())
            {
            }
            else
            {
                // Toggle play/pause
                if (BackgroundAudio.MediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                {
                    BackgroundAudio.MediaPlayer.Pause();
                    PlayPauseIcon.Glyph = "\uE768"; // Play icon

                }
                else
                {
                    BackgroundAudio.MediaPlayer.Play();
                    PlayPauseIcon.Glyph = "\uE769"; // Pause icon
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (BackgroundAudio?.MediaPlayer != null)
            {
                BackgroundAudio.MediaPlayer.Volume = e.NewValue / 100.0;
            }
        }

        private void ButtonVolumeUp_Click(object sender, RoutedEventArgs e)
        {
            // Reset inactivity timer on interaction
            OnUserActivity(this, null);

            // Increase volume by 5%
            if (VolumeSlider.Value + 5 <= VolumeSlider.Maximum)
            {
                VolumeSlider.Value += 5;
            }
            else
            {
                VolumeSlider.Value = VolumeSlider.Maximum;
            }
        }

        private void ButtonVolumeDown_Click(object sender, RoutedEventArgs e)
        {
            // Reset inactivity timer on interaction
            OnUserActivity(this, null);

            // Decrease volume by 5%
            if (VolumeSlider.Value - 5 >= VolumeSlider.Minimum)
            {
                VolumeSlider.Value -= 5;
            }
            else
            {
                VolumeSlider.Value = VolumeSlider.Minimum;
            }
        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SetFullScreen()
        {
            // Get the AppWindow (the window associated with this instance)
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Remove the title bar and borders
            _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            // Handle Full Screen with F11 in Button_Click 
        }

        /// <summary>
        /// Return false means default, true mean succesful set to fullscreen
        /// </summary>
        /// <returns></returns>
        private bool ButtonModeClick()
        {
            // Toggle full screen on F11 press
            if (_appWindow?.Presenter != null)
            {
                if (_appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.Default);
                    return false;
                }
                else
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    return true;
                }
            }
            return false;
        }

        private void SetPosition()
        {
            DigitalClock.Margin = new Thickness(0, AppWindow.Size.Height / 4, 0, 0);
        }

        private void SetFontSize()
        {
            double width = AppWindow.Size.Width;
            double height = AppWindow.Size.Height;

            // Calculate a suitable font size based on the window size
            double fontSize = width / 5; // You can adjust the divisor to change the scaling

            // Set the font size for all TextBlocks
            HourTens.FontSize
            = NewHourTens.FontSize
            = HourUnits.FontSize
            = NewHourUnits.FontSize
            = DotSeperator1.FontSize
            = MinuteTens.FontSize
            = NewMinuteTens.FontSize
            = MinuteUnits.FontSize
            = NewMinuteUnits.FontSize
            = DotSeperator2.FontSize
            = SecondTens.FontSize
            = NewSecondTens.FontSize
            = SecondUnits.FontSize
            = NewSecondUnits.FontSize = fontSize;
        }

        private void UpdateClockDisplay()
        {
            DateTime now = DateTime.Now;
            string newHours = now.ToString("HH");
            string newMinutes = now.ToString("mm");
            string newSeconds = now.ToString("ss");

            // Update the time displays
            UpdateTimeDisplay(HourTens, HourUnits, currentHours[0].ToString(), currentHours[1].ToString(), newHours);
            UpdateTimeDisplay(MinuteTens, MinuteUnits, currentMinutes[0].ToString(), currentMinutes[1].ToString(), newMinutes);
            UpdateTimeDisplay(SecondTens, SecondUnits, currentSeconds[0].ToString(), currentSeconds[1].ToString(), newSeconds);

            // Update current values
            currentHours = newHours;
            currentMinutes = newMinutes;
            currentSeconds = newSeconds;
        }

        private static void UpdateTimeDisplay(TextBlock tensBlock, TextBlock unitsBlock, string currentTens, string currentUnits, string newTime)
        {
            bool updated = false;

            // Check tens place
            if (currentTens != newTime[0].ToString())
            {
                AnimateTextChange(tensBlock, newTime[0].ToString());
                updated = true; // Mark as updated
            }

            // Check units place
            if (currentUnits != newTime[1].ToString())
            {
                AnimateTextChange(unitsBlock, newTime[1].ToString());
                updated = true; // Mark as updated
            }

            // If both numbers are the same, keep static
            if (!updated)
            {
                // Make sure to reset the render transform
                ResetTransform(tensBlock);
                ResetTransform(unitsBlock);
            }
        }

        private static void AnimateTextChange(TextBlock textBlock, string newText)
        {
            // Create a storyboard for the fade-out animation
            Storyboard fadeOutStoryboard = new();

            // Fade out animation
            DoubleAnimation fadeOutAnimation = new()
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };

            // Move up animation
            DoubleAnimation moveUpAnimation = new()
            {
                From = 0,
                To = -30, // Move up by 20 units
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };

            // Set targets for fade-out
            Storyboard.SetTarget(fadeOutAnimation, textBlock);
            Storyboard.SetTarget(moveUpAnimation, textBlock.RenderTransform as TranslateTransform); // Ensure TranslateTransform is defined

            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
            Storyboard.SetTargetProperty(moveUpAnimation, "Y");

            // Add animations to the fade-out storyboard
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            fadeOutStoryboard.Children.Add(moveUpAnimation);

            // Start the fade-out animation first
            fadeOutStoryboard.Completed += (s, e) =>
            {
                // Update the text after fade-out
                textBlock.Text = newText;

                // Create a new storyboard for the fade-in animation
                Storyboard fadeInStoryboard = new();

                // Fade in animation
                DoubleAnimation fadeInAnimation = new()
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(250))
                };

                // Move down animation
                DoubleAnimation moveDownAnimation = new()
                {
                    From = 30, // Start from below
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(250))
                };

                // Set targets for fade-in
                Storyboard.SetTarget(fadeInAnimation, textBlock);
                Storyboard.SetTarget(moveDownAnimation, textBlock.RenderTransform as TranslateTransform);
                Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
                Storyboard.SetTargetProperty(moveDownAnimation, "Y");

                // Add animations to the fade-in storyboard
                fadeInStoryboard.Children.Add(fadeInAnimation);
                fadeInStoryboard.Children.Add(moveDownAnimation);

                // Start the fade-in animation
                fadeInStoryboard.Begin();
            };

            // Start the fade-out storyboard
            fadeOutStoryboard.Begin();
        }

        private static void ResetTransform(TextBlock textBlock)
        {
            // Reset the Y translation to ensure it stays in place
            if (textBlock.RenderTransform is TranslateTransform tt)
            {
                tt.Y = 0;
            }
        }

        private void ComboBoxThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _currentTheme = ComboBoxThemes.SelectedValue.ToString() ?? "";
                SetTheme(_currentTheme.ToUpper());
            }
            catch { }
        }

        private void ButtonCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var exePath = Environment.ProcessPath!;
            var version = FileVersionInfo.GetVersionInfo(exePath).FileVersion;

            ButtonCheckUpdateFlyoutMessage.Text = $"Version: {version}";
        }

        private async void ButtonPackageManager_Click(object sender, RoutedEventArgs e)
        {
            await ShowPackageManagerDialog();
        }

        private async Task ShowPackageManagerDialog(string? message = null)
        {
            var audioManager = new AudioManagerDialog();
            var dialog = new ContentDialog
            {
                Title = "Audio Manager",
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot,
                Content = audioManager
            };

            if (!string.IsNullOrEmpty(message))
            {
                audioManager.SetMessage(message);
            }

            dialog.Closing += async (s, args) =>
            {
                audioManager.CancelDownload();
            };

            await dialog.ShowAsync();
            await CheckAudioFileExists();
        }

        private void WelcomPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var temp = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            temp.Start();
            temp.Tick += (_, _1) =>
            {
                WelcomPanel.Visibility = Visibility.Collapsed;
                temp.Stop();
                temp = null;
            };
        }
    }
}