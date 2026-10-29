using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.Graphics;
using WolfyDesktop.Core.Models;
using WolfyDesktop.ViewModels;
using VirtualKey = Windows.System.VirtualKey;

namespace WolfyDesktop.Views;

public sealed partial class MainWindow : Window
{
    private const string PlayGlyph = "";
    private const string PauseGlyph = "";

    private static readonly TimeSpan WelcomeDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan AudioControlsIdleTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan DigitHalfDuration = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan PanelShowDuration = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan PanelHideDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan WindowResizeDuration = TimeSpan.FromMilliseconds(250);
    private const double DigitTravel = 30;
    private const double PanelHiddenScale = 0.95;

    /// <summary>Smallest windowed size, in pixels.</summary>
    private const int MinWindowSize = 20;

    /// <summary>How far, in pixels, the pointer must move before a press becomes a window drag.</summary>
    private const int DragThreshold = 4;

    private static readonly Shortcut[] KeyboardShortcuts =
    [
        new("F1", "Show this help"),
        new("F2", "Change theme"),
        new("F4", "Close the application"),
        new("F6", "Audio manager"),
        new("F10", "Version and updates"),
        new("F11", "Toggle full screen"),
        new("F12", "Toggle chill mode"),
        new("T", "Toggle always on top"),
        new("Space", "Play / pause music"),
        new("Up / Down", "Volume"),
        new("Esc", "Close this panel"),
    ];

    private static readonly Shortcut[] MouseShortcuts =
    [
        new("Drag", "Move the window"),
        new("Edges", "Resize the window"),
    ];

    private readonly TextBlock[] _digits;
    private readonly DispatcherQueueTimer _audioControlsIdleTimer;
    private readonly DispatcherQueueTimer _toastTimer;
    private bool _isAlwaysOnTop;
    private bool _areAudioControlsShown;
    private double _audioControlsOpacity;

    // Open panel, and whether it is still animating in or out (input is ignored meanwhile).
    private FrameworkElement? _openPanel;
    private VirtualKey _openPanelKey;
    private bool _isPanelTransitioning;

    // Window bounds before a panel grew the window, and the bounds it grew to.
    private RectInt32? _boundsBeforePanel;
    private RectInt32 _boundsForPanel;

    private EventHandler<object>? _windowAnimationFrame;

    // Window drag: where the cursor and the window were when the button went down.
    private bool _isPointerDown;
    private bool _isDragging;
    private bool _wasPressedOnBackdrop;
    private PointInt32 _dragStartCursor;
    private PointInt32 _dragStartWindow;

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

        _toastTimer = DispatcherQueue.CreateTimer();
        _toastTimer.Interval = ToastDuration;
        _toastTimer.IsRepeating = false;
        _toastTimer.Tick += (_, _) => Fade(Toast, to: 0, onCompleted: () => Toast.Visibility = Visibility.Collapsed);

        KeyboardShortcutList.ItemsSource = KeyboardShortcuts;
        MouseShortcutList.ItemsSource = MouseShortcuts;
        ThemeRadioButtons.ItemsSource = Enum.GetNames<AppTheme>();
        ThemeRadioButtons.SelectedItem = ViewModel.SelectedTheme.ToString();
        ApplyTheme(ViewModel.SelectedTheme);
        ShowTime(ViewModel.CurrentTime, animate: false);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "logo.ico"));
        RestoreWindowState();
        AppWindow.Changed += AppWindow_Changed;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        VisibilityChanged += (_, e) => ViewModel.OnWindowVisibilityChanged(e.Visible);
        Closed += (_, _) => ViewModel.Dispose();

        _ = ViewModel.CheckForUpdatesInBackgroundAsync();
    }

    public MainWindowViewModel ViewModel { get; }

    private string PlayPauseGlyph(bool isPlaying) => isPlaying ? PauseGlyph : PlayGlyph;

    private string PercentText(double value) => $"{value:F0}%";

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
            case nameof(MainWindowViewModel.ControlsOpacity):
                PreviewAudioControls();
                break;
        }
    }

    private async void Accelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_isPanelTransitioning)
        {
            args.Handled = true;
            return;
        }

        // In a panel only Esc and the key that opened it do anything (both close it); every other
        // key goes to the panel's controls (Space on a button, arrows in a list, etc.).
        if (_openPanel is not null)
        {
            if (sender.Key == VirtualKey.Escape || sender.Key == _openPanelKey)
            {
                args.Handled = true;
                ClosePanel();
            }

            return;
        }

        args.Handled = true;
        switch (sender.Key)
        {
            case VirtualKey.F1:
                OpenPanel(VirtualKey.F1, "Shortcuts", HelpPanel);
                break;
            case VirtualKey.F2:
                OpenPanel(VirtualKey.F2, "Theme", ThemeRadioButtons);
                break;
            case VirtualKey.F4:
                Close();
                break;
            case VirtualKey.F6:
                AudioManagerHost.Child = new AudioManagerView();
                OpenPanel(VirtualKey.F6, "Audio manager", AudioPanel);
                break;
            case VirtualKey.F10:
                OpenPanel(VirtualKey.F10, "About", AboutPanel);
                break;
            case VirtualKey.F11:
                ToggleFullScreen();
                break;
            case VirtualKey.F12:
                await ExecuteIfIdleAsync(ViewModel.ToggleChillModeCommand);
                break;
            case VirtualKey.T:
                ToggleAlwaysOnTop();
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

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeRadioButtons.SelectedItem is string name && Enum.TryParse<AppTheme>(name, out var theme))
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

    #region Panels

    private void OpenPanel(VirtualKey key, string title, FrameworkElement panel)
    {
        _openPanel = panel;
        _openPanelKey = key;
        _isPanelTransitioning = true;

        PanelTitle.Text = title;
        panel.Visibility = Visibility.Visible;
        PanelScroller.ChangeView(null, 0, null, disableAnimation: true);
        PanelLayer.Visibility = Visibility.Visible;

        GrowWindowToFitPanel(onCompleted: () =>
        {
            FocusPanel();
            AnimatePanel(show: true, onCompleted: () => _isPanelTransitioning = false);
        });
    }

    private void ClosePanel()
    {
        if (_openPanel is not { } panel || _isPanelTransitioning)
        {
            return;
        }

        _isPanelTransitioning = true;
        AnimatePanel(show: false, onCompleted: () =>
        {
            PanelLayer.Visibility = Visibility.Collapsed;
            panel.Visibility = Visibility.Collapsed;
            if (AudioManagerHost.Child is AudioManagerView audioManager)
            {
                audioManager.ViewModel.DownloadCancelCommand.Execute(null);
                AudioManagerHost.Child = null;
            }

            EndAudioControlsPreview();
            _openPanel = null;
            RootGrid.Focus(FocusState.Programmatic);
            RestoreWindowAfterPanel(onCompleted: () => _isPanelTransitioning = false);
        });
    }

    private void PanelCloseButton_Click(object sender, RoutedEventArgs e) => ClosePanel();

    /// <summary>
    /// Focuses the panel's first control, or its scroller so the arrow keys scroll. Pointer focus
    /// hides the focus rectangle until the user moves focus with the keyboard.
    /// </summary>
    private void FocusPanel()
    {
        var target = FocusManager.FindFirstFocusableElement(_openPanel) as Control ?? PanelScroller;
        target.Focus(FocusState.Pointer);
    }

    private void AnimatePanel(bool show, Action onCompleted)
    {
        var duration = show ? PanelShowDuration : PanelHideDuration;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var (fromScale, toScale) = show ? (PanelHiddenScale, 1.0) : (1.0, PanelHiddenScale);

        var fade = new DoubleAnimation { From = show ? 0 : 1, To = show ? 1 : 0, Duration = duration, EasingFunction = easing };
        Storyboard.SetTarget(fade, PanelLayer);
        Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));

        var storyboard = new Storyboard { Children = { fade } };
        foreach (var property in new[] { nameof(ScaleTransform.ScaleX), nameof(ScaleTransform.ScaleY) })
        {
            var scale = new DoubleAnimation { From = fromScale, To = toScale, Duration = duration, EasingFunction = easing };
            Storyboard.SetTarget(scale, PanelCardScale);
            Storyboard.SetTargetProperty(scale, property);
            storyboard.Children.Add(scale);
        }

        storyboard.Completed += (_, _) => onCompleted();
        storyboard.Begin();
    }

    /// <summary>
    /// When the window is too small for the panel (plus the card's margin), grows it around its
    /// centre just enough, within the screen, and remembers the old bounds for when the panel closes.
    /// </summary>
    private void GrowWindowToFitPanel(Action onCompleted)
    {
        if (AppWindow.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Restored })
        {
            onCompleted();
            return;
        }

        // Natural size of the card, unconstrained by the current window size.
        PanelLayer.UpdateLayout();
        PanelCard.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var scale = Content.XamlRoot.RasterizationScale;
        var growWidth = Math.Max(0, (int)Math.Ceiling(PanelCard.DesiredSize.Width * scale) - AppWindow.ClientSize.Width);
        var growHeight = Math.Max(0, (int)Math.Ceiling(PanelCard.DesiredSize.Height * scale) - AppWindow.ClientSize.Height);
        if (growWidth == 0 && growHeight == 0)
        {
            onCompleted();
            return;
        }

        var from = GetWindowBounds();
        var workArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        var width = Math.Max(from.Width, Math.Min(from.Width + growWidth, workArea.Width));
        var height = Math.Max(from.Height, Math.Min(from.Height + growHeight, workArea.Height));
        var x = KeepWithin(from.X - ((width - from.Width) / 2), width, workArea.X, workArea.Width);
        var y = KeepWithin(from.Y - ((height - from.Height) / 2), height, workArea.Y, workArea.Height);

        _boundsBeforePanel = from;
        _boundsForPanel = new RectInt32(x, y, width, height);
        AnimateWindowBounds(_boundsForPanel, onCompleted);
    }

    /// <summary>
    /// Shrinks the window back after <see cref="GrowWindowToFitPanel"/>, following any move made
    /// meanwhile; a window the user resized while the panel was open keeps its new size.
    /// </summary>
    private void RestoreWindowAfterPanel(Action onCompleted)
    {
        var before = _boundsBeforePanel;
        _boundsBeforePanel = null;

        var current = GetWindowBounds();
        if (before is not { } target
            || AppWindow.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Restored }
            || current.Width != _boundsForPanel.Width
            || current.Height != _boundsForPanel.Height)
        {
            onCompleted();
            return;
        }

        target.X = current.X + (target.X - _boundsForPanel.X);
        target.Y = current.Y + (target.Y - _boundsForPanel.Y);
        AnimateWindowBounds(target, onCompleted);
    }

    private static int KeepWithin(int start, int length, int areaStart, int areaLength) =>
        Math.Max(areaStart, Math.Min(start, areaStart + areaLength - length));

    private RectInt32 GetWindowBounds() =>
        new(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);

    /// <summary>Moves and resizes the window to <paramref name="to"/> over a few frames.</summary>
    private void AnimateWindowBounds(RectInt32 to, Action onCompleted)
    {
        var from = GetWindowBounds();
        var clock = Stopwatch.StartNew();
        _windowAnimationFrame = (_, _) =>
        {
            var progress = Math.Min(1, clock.Elapsed / WindowResizeDuration);
            var eased = 1 - Math.Pow(1 - progress, 3);
            int Lerp(int a, int b) => a + (int)Math.Round((b - a) * eased);
            AppWindow.MoveAndResize(new RectInt32(Lerp(from.X, to.X), Lerp(from.Y, to.Y), Lerp(from.Width, to.Width), Lerp(from.Height, to.Height)));

            if (progress >= 1)
            {
                CompositionTarget.Rendering -= _windowAnimationFrame;
                _windowAnimationFrame = null;
                onCompleted();
            }
        };
        CompositionTarget.Rendering += _windowAnimationFrame;
    }

    #endregion

    #region Window

    /// <summary>Reopens the window as it was left: full screen or windowed, at its last windowed bounds.</summary>
    private void RestoreWindowState()
    {
        ShowWindowed();
        if (ViewModel.SavedWindowBounds is { } saved)
        {
            // Pull the window back onto the screen if its monitor was disconnected or rearranged.
            var workArea = DisplayArea.GetFromRect(
                new RectInt32(saved.X, saved.Y, saved.Width, saved.Height), DisplayAreaFallback.Nearest).WorkArea;
            var width = Math.Min(saved.Width, workArea.Width);
            var height = Math.Min(saved.Height, workArea.Height);
            AppWindow.MoveAndResize(new RectInt32(
                KeepWithin(saved.X, width, workArea.X, workArea.Width),
                KeepWithin(saved.Y, height, workArea.Y, workArea.Height),
                width,
                height));
        }

        if (ViewModel.StartsFullScreen)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidPositionChange && !args.DidSizeChange && !args.DidPresenterChange)
        {
            return;
        }

        // Only plain windowed bounds are kept: not full screen, minimized, maximized, or grown to fit a panel.
        WindowBounds? windowedBounds = null;
        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored } && _boundsBeforePanel is null)
        {
            var bounds = GetWindowBounds();
            windowedBounds = new WindowBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        ViewModel.SaveWindowState(AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen, windowedBounds);
    }

    private void ToggleFullScreen()
    {
        if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
        {
            ShowWindowed();
        }
        else
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }

    /// <summary>
    /// Borderless window: no title bar (press and drag anywhere to move, see
    /// <see cref="RootGrid_PointerPressed"/>), but the thin system border stays so the edges resize.
    /// </summary>
    private void ShowWindowed()
    {
        // A new presenter each time, so settings are applied again after every switch.
        AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        var presenter = (OverlappedPresenter)AppWindow.Presenter;
        presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
        presenter.IsAlwaysOnTop = _isAlwaysOnTop;
        presenter.PreferredMinimumWidth = MinWindowSize;
        presenter.PreferredMinimumHeight = MinWindowSize;
    }

    /// <summary>Full screen cannot stay on top, so there the choice waits for the windowed mode.</summary>
    private void ToggleAlwaysOnTop()
    {
        _isAlwaysOnTop = !_isAlwaysOnTop;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = _isAlwaysOnTop;
            ShowToast(_isAlwaysOnTop ? "Always on top: on" : "Always on top: off");
        }
        else
        {
            ShowToast(_isAlwaysOnTop ? "Always on top: on (when windowed)" : "Always on top: off");
        }
    }

    // Buttons, sliders and lists handle their own presses, so these only fire on empty space.
    // The drag follows the real cursor every frame rather than pointer events, which stop
    // arriving once the window moves under the cursor.
    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_isPointerDown || !e.GetCurrentPoint(RootGrid).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPointerDown = true;
        _isDragging = false;
        _wasPressedOnBackdrop = ReferenceEquals(e.OriginalSource, PanelBackdrop);
        _dragStartCursor = NativeMethods.GetCursorPosition();
        _dragStartWindow = AppWindow.Position;
        RootGrid.CapturePointer(e.Pointer);
        CompositionTarget.Rendering += DragFrame;
    }

    private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // A click on the backdrop (not a drag) closes the panel.
        var closePanel = _isPointerDown && !_isDragging && _wasPressedOnBackdrop;
        EndDrag();
        RootGrid.ReleasePointerCapture(e.Pointer);
        if (closePanel)
        {
            ClosePanel();
        }
    }

    private void RootGrid_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => EndDrag();

    private void DragFrame(object? sender, object e)
    {
        // The release can be missed (another window took it, a dialog opened); never keep following the cursor then.
        if (!NativeMethods.IsPrimaryButtonDown())
        {
            EndDrag();
            return;
        }

        var cursor = NativeMethods.GetCursorPosition();
        var (dx, dy) = (cursor.X - _dragStartCursor.X, cursor.Y - _dragStartCursor.Y);
        if (!_isDragging)
        {
            var canMove = AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored }
                && _windowAnimationFrame is null;
            _isDragging = canMove && (Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold);
        }

        if (_isDragging)
        {
            AppWindow.Move(new PointInt32(_dragStartWindow.X + dx, _dragStartWindow.Y + dy));
        }
    }

    private void EndDrag()
    {
        if (_isPointerDown)
        {
            CompositionTarget.Rendering -= DragFrame;
            _isPointerDown = false;
        }
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        if (Toast.Visibility == Visibility.Collapsed)
        {
            Toast.Visibility = Visibility.Visible;
            Fade(Toast, to: 1, onCompleted: null);
        }

        _toastTimer.Stop();
        _toastTimer.Start();
    }

    #endregion

    #region Audio controls

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e) => ShowAudioControls();

    private void ShowAudioControls()
    {
        var opacity = ViewModel.ControlsOpacity / 100;
        if (!ViewModel.HasAudioTrack || opacity == 0)
        {
            return;
        }

        if (!_areAudioControlsShown || opacity != _audioControlsOpacity)
        {
            _areAudioControlsShown = true;
            _audioControlsOpacity = opacity;
            AudioControls.Visibility = Visibility.Visible;
            Fade(AudioControls, to: opacity, onCompleted: null);
        }

        _audioControlsIdleTimer.Stop();
        _audioControlsIdleTimer.Start();
    }

    private void HideAudioControls()
    {
        if (!_areAudioControlsShown)
        {
            return;
        }

        _areAudioControlsShown = false;
        _audioControlsIdleTimer.Stop();
        Fade(AudioControls, to: 0, onCompleted: () =>
        {
            // Shown again while fading out.
            if (!_areAudioControlsShown)
            {
                AudioControls.Visibility = Visibility.Collapsed;
                EndAudioControlsPreview();
            }
        });
    }

    /// <summary>
    /// Shows the controls at a newly chosen opacity. While the F6 panel is open they are lifted
    /// above its backdrop, and ignore the pointer so they never cover the panel's own controls.
    /// </summary>
    private void PreviewAudioControls()
    {
        if (ViewModel.ControlsOpacity == 0)
        {
            HideAudioControls();
            return;
        }

        if (_openPanel is not null)
        {
            Canvas.SetZIndex(AudioControls, 1);
            AudioControls.IsHitTestVisible = false;
        }

        ShowAudioControls();
    }

    private void EndAudioControlsPreview()
    {
        Canvas.SetZIndex(AudioControls, 0);
        AudioControls.IsHitTestVisible = true;
    }

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
