using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
using WolfyDesktop.Services;
using WolfyDesktop.ViewModels;

namespace WolfyDesktop;

public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;
    private readonly DispatcherTimer _inactivityTimer;
    
    // Clock display state for animations
    private string _currentHours = "00";
    private string _currentMinutes = "00";
    private string _currentSeconds = "00";
    
    public MainWindowViewModel? ViewModel { get; private set; }

    public MainWindow()
    {
        this.InitializeComponent();

        // Set up the inactivity timer for audio player auto-hide (UI-specific)
        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _inactivityTimer.Tick += InactivityTimer_Tick;

        // Subscribe to user input events (UI-specific)
        MainContainer.PointerMoved += OnUserActivity;
        MainContainer.KeyDown += (s, e) => OnUserActivity(s, null);

        // Borderless fullscreen
        SetFullScreen();

        // Center the clock
        SetPosition();

        // Set initial font size
        SetFontSize();

        // Subscribe to the SizeChanged event
        this.SizeChanged += MainWindow_SizeChanged;
    }

    public void InitializeWithServices(IServiceProvider services)
    {
        // Create ViewModel with injected services
        var clockService = services.GetRequiredService<IClockService>();
        var videoMediaService = services.GetRequiredService<IMediaService>();
        var backgroundMediaService = services.GetRequiredService<IMediaService>();
        var themeService = services.GetRequiredService<IThemeService>();
        var settingsService = services.GetRequiredService<ISettingsService>();
        var fileService = services.GetRequiredService<IFileService>();

        ViewModel = new MainWindowViewModel(
            clockService,
            videoMediaService,
            backgroundMediaService,
            themeService,
            settingsService,
            fileService);

        // Subscribe to ViewModel events
        ViewModel.ExitRequested += (s, e) => Close();
        ViewModel.FullScreenToggleRequested += (s, e) => ButtonModeClick();
        ViewModel.PackageManagerRequested += async (s, e) => await ShowPackageManagerDialog();
        ViewModel.UserActivityDetected += (s, e) =>
        {
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        };
        ViewModel.ClockUpdated += (s, e) => UpdateClockDisplayWithAnimation();

        // Apply initial theme
        ViewModel.ApplyTheme(MainContainer);

        // Set initial values from ViewModel
        ComboBoxThemes.SelectedValue = ViewModel.SelectedTheme;
        VolumeSlider.Value = ViewModel.Volume;
    }

    #region UI Event Handlers (View-specific)

    private void OnUserActivity(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs? e)
    {
        ViewModel?.OnUserActivity();

        // Show/hide audio panel based on ViewModel state
        if (ViewModel?.IsAudioPlayerVisible == true && AudioPlayerPanel.Visibility == Visibility.Collapsed)
        {
            ShowAudioPlayerPanel();
        }
    }

    private void InactivityTimer_Tick(object? sender, object e)
    {
        HideAudioPlayerPanel();
        ViewModel?.HideAudioPlayer();
        _inactivityTimer.Stop();
    }

    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        SetPosition();
        SetFontSize();
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
            ViewModel?.HideWelcomePanel();
            temp.Stop();
            temp = null;
        };
    }

    #endregion

    #region Button Click Handlers (delegate to ViewModel commands)

    private void ButtonFocusMode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.FocusModeCommand.Execute(null);
    }

    private async void ButtonChillMode_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            // If we're about to turn off chill mode, clear the video player first
            if (ViewModel.IsChilled)
            {
                // Clear the MediaPlayerElement before resetting the service
                BackgroundVideo.SetMediaPlayer(null);
            }

            await ViewModel.ChillModeCommand.ExecuteAsync(null);

            // Update video player if we just turned on chill mode
            if (ViewModel.IsChilled && ViewModel.VideoMediaPlayer != null)
            {
                BackgroundVideo.SetMediaPlayer(ViewModel.VideoMediaPlayer);
            }

            // Update audio player if needed
            if (ViewModel.BackgroundAudioMediaPlayer != null && BackgroundAudio.MediaPlayer == null)
            {
                BackgroundAudio.SetMediaPlayer(ViewModel.BackgroundAudioMediaPlayer);
            }
        }
    }

    private async void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.PlayPauseCommand.ExecuteAsync(null);
            
            // Update audio player and icon
            if (ViewModel.BackgroundAudioMediaPlayer != null && BackgroundAudio.MediaPlayer == null)
            {
                BackgroundAudio.SetMediaPlayer(ViewModel.BackgroundAudioMediaPlayer);
            }
            PlayPauseIcon.Glyph = ViewModel.PlayPauseIcon;
        }
    }

    private void ButtonVolumeUp_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.VolumeUpCommand.Execute(null);
        if (ViewModel != null)
        {
            VolumeSlider.Value = ViewModel.Volume;
        }
    }

    private void ButtonVolumeDown_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.VolumeDownCommand.Execute(null);
        if (ViewModel != null)
        {
            VolumeSlider.Value = ViewModel.Volume;
        }
    }

    private void ButtonExit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ExitCommand.Execute(null);
    }

    private void ComboBoxThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && ComboBoxThemes.SelectedValue != null)
        {
            ViewModel.SelectedTheme = ComboBoxThemes.SelectedValue.ToString() ?? "Dark";
            ViewModel.ApplyTheme(MainContainer);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.Volume = e.NewValue;
        }
    }

    private void ButtonCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CheckUpdateCommand.Execute(null);
        if (ViewModel != null)
        {
            ButtonCheckUpdateFlyoutMessage.Text = ViewModel.VersionInfo;
        }
    }

    private async void ButtonPackageManager_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ShowPackageManagerCommand.Execute(null);
    }

    #endregion

    #region Audio Player Panel Animations (View-specific)

    private void ShowAudioPlayerPanel()
    {
        AudioPlayerPanel.Visibility = Visibility.Visible;

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

    #endregion

    #region Window Management (View-specific)

    private void SetFullScreen()
    {
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private bool ButtonModeClick()
    {
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
        double fontSize = width / 5;

        HourTens.FontSize = NewHourTens.FontSize = HourUnits.FontSize = NewHourUnits.FontSize =
        DotSeperator1.FontSize = MinuteTens.FontSize = NewMinuteTens.FontSize = MinuteUnits.FontSize =
        NewMinuteUnits.FontSize = DotSeperator2.FontSize = SecondTens.FontSize = NewSecondTens.FontSize =
        SecondUnits.FontSize = NewSecondUnits.FontSize = fontSize;
    }

    #endregion

    #region Clock Display Animations (View-specific)

    private void UpdateClockDisplayWithAnimation()
    {
        if (ViewModel == null) return;

        string newHours = ViewModel.CurrentHours;
        string newMinutes = ViewModel.CurrentMinutes;
        string newSeconds = ViewModel.CurrentSeconds;

        UpdateTimeDisplay(HourTens, HourUnits, _currentHours[0].ToString(), _currentHours[1].ToString(), newHours);
        UpdateTimeDisplay(MinuteTens, MinuteUnits, _currentMinutes[0].ToString(), _currentMinutes[1].ToString(), newMinutes);
        UpdateTimeDisplay(SecondTens, SecondUnits, _currentSeconds[0].ToString(), _currentSeconds[1].ToString(), newSeconds);

        _currentHours = newHours;
        _currentMinutes = newMinutes;
        _currentSeconds = newSeconds;
    }

    private static void UpdateTimeDisplay(TextBlock tensBlock, TextBlock unitsBlock, string currentTens, string currentUnits, string newTime)
    {
        bool updated = false;

        if (currentTens != newTime[0].ToString())
        {
            AnimateTextChange(tensBlock, newTime[0].ToString());
            updated = true;
        }

        if (currentUnits != newTime[1].ToString())
        {
            AnimateTextChange(unitsBlock, newTime[1].ToString());
            updated = true;
        }

        if (!updated)
        {
            ResetTransform(tensBlock);
            ResetTransform(unitsBlock);
        }
    }

    private static void AnimateTextChange(TextBlock textBlock, string newText)
    {
        Storyboard fadeOutStoryboard = new();

        DoubleAnimation fadeOutAnimation = new()
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(250))
        };

        DoubleAnimation moveUpAnimation = new()
        {
            From = 0,
            To = -30,
            Duration = new Duration(TimeSpan.FromMilliseconds(250))
        };

        Storyboard.SetTarget(fadeOutAnimation, textBlock);
        Storyboard.SetTarget(moveUpAnimation, textBlock.RenderTransform as TranslateTransform);

        Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
        Storyboard.SetTargetProperty(moveUpAnimation, "Y");

        fadeOutStoryboard.Children.Add(fadeOutAnimation);
        fadeOutStoryboard.Children.Add(moveUpAnimation);

        fadeOutStoryboard.Completed += (s, e) =>
        {
            textBlock.Text = newText;

            Storyboard fadeInStoryboard = new();

            DoubleAnimation fadeInAnimation = new()
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };

            DoubleAnimation moveDownAnimation = new()
            {
                From = 30,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };

            Storyboard.SetTarget(fadeInAnimation, textBlock);
            Storyboard.SetTarget(moveDownAnimation, textBlock.RenderTransform as TranslateTransform);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
            Storyboard.SetTargetProperty(moveDownAnimation, "Y");

            fadeInStoryboard.Children.Add(fadeInAnimation);
            fadeInStoryboard.Children.Add(moveDownAnimation);

            fadeInStoryboard.Begin();
        };

        fadeOutStoryboard.Begin();
    }

    private static void ResetTransform(TextBlock textBlock)
    {
        if (textBlock.RenderTransform is TranslateTransform tt)
        {
            tt.Y = 0;
        }
    }

    #endregion

    #region Package Manager Dialog

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

        dialog.Closing += (s, args) =>
        {
            audioManager.CancelDownload();
        };

        await dialog.ShowAsync();
    }

    #endregion
}
