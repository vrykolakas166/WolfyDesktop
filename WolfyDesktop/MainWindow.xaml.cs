using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Diagnostics;
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
        private string currentHours = "00";
        private string currentMinutes = "00";
        private string currentSeconds = "00";

        private string _currentTheme = MyTheme.DARK;

        private bool IsChilled = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Set up the timer
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Update every second
            };
            timer.Tick += (_, _1) =>
            {
                UpdateClockDisplay();
            };
            timer.Start();

            // Theme
            SetTheme(MyTheme.DARK);

            // Borderless fullscreen
            SetFullScreen();

            // Center the clock
            SetPosition();

            // Initialize the clock display
            UpdateClockDisplay();

            // Set initial font size
            SetFontFamily();
            SetFontSize();

            // Subscribe to the SizeChanged event to adjust font size on window resize
            this.SizeChanged += MainWindow_SizeChanged;
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
                // Show play button
                ButtonPlayMusic.Visibility = Visibility.Collapsed;

                // Stop playback and remove the media source
                var mediaPlayer = BackgroundVideo.MediaPlayer;
                mediaPlayer.Pause(); // Pause the video if it's currently playing
                mediaPlayer.Source = null; // Clear the media source
                mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                IsChilled = false;
            }
            else
            {
                // Hide play button
                ButtonPlayMusic.Visibility = Visibility.Visible;

                // Load the video from the embedded resource
                var file = await StorageFile.GetFileFromPathAsync($"{AppDomain.CurrentDomain.BaseDirectory}Assets\\background.mp4");
                var mediaPlayer = BackgroundVideo.MediaPlayer;
                mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                mediaPlayer.Play();
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                IsChilled = true;
            }
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            // Restart the video when it ends
            sender.Play();
        }

        private void ButtonPlayMusic_Click(object _, RoutedEventArgs _1)
        {
            ButtonPlayMusicFlyoutMessage.Text = "Running spotify...";
            const string YOUR_PLAYLIST_ID = "1JLw7Y5YvlsA10XjaKHTxE";
            string spotifyUri = $"spotify:playlist:{YOUR_PLAYLIST_ID}";

            // Start the Spotify app with the specified URI
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "spotify",
                    Arguments = spotifyUri,
                    UseShellExecute = true // Important to launch the application
                };

                Process.Start(processStartInfo);

                ButtonPlayMusicFlyout.Hide();
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., if Spotify is not installed)
                Debug.WriteLine($"Error launching Spotify: {ex.Message}");
                ButtonPlayMusicFlyoutMessage.Text = ex.Message;
            }
        }

        private void ButtonPlayMusicFlyoutOk_Click(object _, RoutedEventArgs _1)
        {
            // Close the Flyout
            ButtonPlayMusicFlyout.Hide();
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

        private void SetFontFamily()
        {
            // Construct the absolute or relative path to the font file
            string fontPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "font", "SF-Pro-Display-Bold.otf");

            // Create the FontFamily using the file path
            var fontFamily = new FontFamily($"file:///{fontPath.Replace("\\", "/")}#SF Pro Display Bold");

            HourTens.FontFamily
            = NewHourTens.FontFamily
            = HourUnits.FontFamily
            = NewHourUnits.FontFamily
            = DotSeperator1.FontFamily
            = MinuteTens.FontFamily
            = NewMinuteTens.FontFamily
            = MinuteUnits.FontFamily
            = NewMinuteUnits.FontFamily
            = DotSeperator2.FontFamily
            = SecondTens.FontFamily
            = NewSecondTens.FontFamily
            = SecondUnits.FontFamily
            = NewSecondUnits.FontFamily = fontFamily;
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

        //private void Timer_Tick(object _, object _1)
        //{
        //    UpdateClockDisplay();
        //    //IsSpotifyAlive();
        //}

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

        //private static bool IsSpotifyAlive()
        //{
        //    // Check if any Spotify process is running
        //    return Process.GetProcessesByName("Spotify").Length != 0;
        //}

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

            ButtonCheckUpdateFlyoutMessage.Text = $"Version: 1.0.0";
        }
    }
}