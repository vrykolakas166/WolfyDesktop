using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using VirtualKey = Windows.System.VirtualKey;
using WolfyDesktop.Core.Models;
using WolfyDesktop.ViewModels;

namespace WolfyDesktop.Views;

public sealed partial class MainWindow : Window
{
    private const string PlayGlyph = "";
    private const string PauseGlyph = "";

    private static readonly TimeSpan WelcomeDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan AudioControlsIdleTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan DigitHalfDuration = TimeSpan.FromMilliseconds(250);
    private const double DigitTravel = 30;

    private readonly TextBlock[] _digits;
    private readonly DispatcherQueueTimer _audioControlsIdleTimer;
    private bool _isDialogOpen;

    public MainWindow()
    {
        ViewModel = App.Current.Services.GetRequiredService<MainWindowViewModel>();
        InitializeComponent();

        _digits = [HourTens, HourUnits, MinuteTens, MinuteUnits, SecondTens, SecondUnits];
        foreach (var digit in _digits)
        {
            digit.RenderTransform = new TranslateTransform();
        }

        _audioControlsIdleTimer = DispatcherQueue.CreateTimer();
        _audioControlsIdleTimer.Interval = AudioControlsIdleTimeout;
        _audioControlsIdleTimer.IsRepeating = false;
        _audioControlsIdleTimer.Tick += (_, _) => HideAudioControls();

        ThemeComboBox.ItemsSource = Enum.GetNames<AppTheme>();
        ThemeComboBox.SelectedItem = ViewModel.SelectedTheme.ToString();
        ApplyTheme(ViewModel.SelectedTheme);
        ShowTime(ViewModel.CurrentTime, animate: false);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "logo.ico"));
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        VisibilityChanged += (_, e) => ViewModel.OnWindowVisibilityChanged(e.Visible);
        Closed += (_, _) => ViewModel.Dispose();

        _ = ViewModel.CheckForUpdatesInBackgroundAsync();
    }

    public MainWindowViewModel ViewModel { get; }

    private string PlayPauseGlyph(bool isPlaying) => isPlaying ? PauseGlyph : PlayGlyph;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.CurrentTime):
                ShowTime(ViewModel.CurrentTime, animate: true);
                break;
            case nameof(MainWindowViewModel.SelectedTheme):
                ApplyTheme(ViewModel.SelectedTheme);
                break;
            case nameof(MainWindowViewModel.VideoPlayer):
                BackgroundVideo.SetMediaPlayer(ViewModel.VideoPlayer);
                break;
        }
    }

    private async void Accelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        // Leave keys alone while a dialog or flyout is open (arrow keys in a list or combo box, etc.).
        if (_isDialogOpen || IsInteractivePopupOpen())
        {
            return;
        }

        args.Handled = true;
        switch (sender.Key)
        {
            case VirtualKey.F2:
                FlyoutBase.ShowAttachedFlyout(ThemeFlyoutAnchor);
                break;
            case VirtualKey.F4:
                Close();
                break;
            case VirtualKey.F6:
                await ShowAudioManagerAsync();
                break;
            case VirtualKey.F10:
                FlyoutBase.ShowAttachedFlyout(AboutFlyoutAnchor);
                break;
            case VirtualKey.F11:
                ToggleFullScreen();
                break;
            case VirtualKey.F12:
                await ExecuteIfIdleAsync(ViewModel.ToggleChillModeCommand);
                break;
            case VirtualKey.Space:
                ShowAudioControls();
                await ExecuteIfIdleAsync(ViewModel.TogglePlayPauseCommand);
                break;
            case VirtualKey.Up:
                ShowAudioControls();
                ViewModel.VolumeUpCommand.Execute(null);
                break;
            case VirtualKey.Down:
                ShowAudioControls();
                ViewModel.VolumeDownCommand.Execute(null);
                break;
        }
    }

    /// <summary>Ignores repeated key presses while the previous one is still running.</summary>
    private static Task ExecuteIfIdleAsync(IAsyncRelayCommand command) =>
        command.CanExecute(null) && !command.IsRunning ? command.ExecuteAsync(null) : Task.CompletedTask;

    private bool IsInteractivePopupOpen() =>
        VisualTreeHelper.GetOpenPopupsForXamlRoot(Content.XamlRoot).Any(popup => popup.Child is not ToolTip);

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is string name && Enum.TryParse<AppTheme>(name, out var theme))
        {
            ViewModel.SelectedTheme = theme;
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        (RootGrid.RequestedTheme, RootGrid.Background) = theme switch
        {
            AppTheme.Light => (ElementTheme.Light, new SolidColorBrush(Colors.White)),
            AppTheme.Dark => (ElementTheme.Dark, new SolidColorBrush(Colors.Black)),
            _ => (ElementTheme.Default, (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]),
        };
    }

    private void ToggleFullScreen()
    {
        var isFullScreen = AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;
        AppWindow.SetPresenter(isFullScreen ? AppWindowPresenterKind.Default : AppWindowPresenterKind.FullScreen);
    }

    private async Task ShowAudioManagerAsync()
    {
        var audioManager = new AudioManagerDialog();
        var dialog = new ContentDialog
        {
            Title = "Audio Manager",
            CloseButtonText = "Close",
            Content = audioManager,
            XamlRoot = Content.XamlRoot,
            // Dialogs live outside the root grid, so pass the chosen theme on explicitly.
            RequestedTheme = RootGrid.ActualTheme,
        };
        dialog.Closing += (_, _) => audioManager.ViewModel.DownloadCancelCommand.Execute(null);

        _isDialogOpen = true;
        try
        {
            await dialog.ShowAsync();
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void WelcomePanel_Loaded(object sender, RoutedEventArgs e)
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = WelcomeDuration;
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            // Free the decoded GIF frames; the splash is never shown again.
            WelcomeImage.Source = null;
        };
        timer.Start();
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e) => RootGrid.Focus(FocusState.Programmatic);

    #region Audio controls

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e) => ShowAudioControls();

    private void ShowAudioControls()
    {
        if (!ViewModel.HasAudioTrack)
        {
            return;
        }

        if (AudioControls.Visibility == Visibility.Collapsed)
        {
            AudioControls.Visibility = Visibility.Visible;
            Fade(AudioControls, to: 1, onCompleted: null);
        }

        _audioControlsIdleTimer.Stop();
        _audioControlsIdleTimer.Start();
    }

    private void HideAudioControls() =>
        Fade(AudioControls, to: 0, onCompleted: () => AudioControls.Visibility = Visibility.Collapsed);

    private static void Fade(UIElement element, double to, Action? onCompleted)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = FadeDuration,
            EasingFunction = new CubicEase { EasingMode = to > 0 ? EasingMode.EaseOut : EasingMode.EaseIn },
        };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));

        var storyboard = new Storyboard { Children = { animation } };
        if (onCompleted is not null)
        {
            storyboard.Completed += (_, _) => onCompleted();
        }

        storyboard.Begin();
    }

    #endregion

    #region Clock

    private void ShowTime(DateTime time, bool animate)
    {
        var text = time.ToString("HHmmss", CultureInfo.InvariantCulture);
        for (var i = 0; i < _digits.Length; i++)
        {
            var digit = text[i].ToString();
            if (_digits[i].Text == digit)
            {
                continue;
            }

            if (animate)
            {
                AnimateDigit(_digits[i], digit);
            }
            else
            {
                _digits[i].Text = digit;
            }
        }
    }

    /// <summary>Slides the old digit up and out, then the new one up and in.</summary>
    private static void AnimateDigit(TextBlock block, string newDigit)
    {
        var slideOut = CreateDigitStoryboard(block, fromOpacity: 1, toOpacity: 0, fromY: 0, toY: -DigitTravel);
        slideOut.Completed += (_, _) =>
        {
            block.Text = newDigit;
            CreateDigitStoryboard(block, fromOpacity: 0, toOpacity: 1, fromY: DigitTravel, toY: 0).Begin();
        };
        slideOut.Begin();
    }

    private static Storyboard CreateDigitStoryboard(TextBlock block, double fromOpacity, double toOpacity, double fromY, double toY)
    {
        var fade = new DoubleAnimation { From = fromOpacity, To = toOpacity, Duration = DigitHalfDuration };
        Storyboard.SetTarget(fade, block);
        Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));

        var slide = new DoubleAnimation { From = fromY, To = toY, Duration = DigitHalfDuration };
        Storyboard.SetTarget(slide, block.RenderTransform);
        Storyboard.SetTargetProperty(slide, nameof(TranslateTransform.Y));

        return new Storyboard { Children = { fade, slide } };
    }

    #endregion
}
