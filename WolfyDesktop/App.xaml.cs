using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using WolfyDesktop.Services;
using WolfyDesktop.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WolfyDesktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        public static BitmapImage? LoadingGif { get; set; }
        public static nint MainHandle = nint.Zero;

        public IServiceProvider Services => _host!.Services;
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            PreloadImages();

            // Create main window first to get DispatcherQueue
            m_window = new MainWindow();
            MainHandle = WinRT.Interop.WindowNative.GetWindowHandle(m_window);

            // Configure DI with DispatcherQueue
            ConfigureServices(m_window.DispatcherQueue);

            // Load settings
            var settingsService = Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            // Initialize main window with DI
            if (m_window is MainWindow mainWindow)
            {
                mainWindow.InitializeWithServices(Services);
            }

            m_window.Activate();
        }

        private void ConfigureServices(DispatcherQueue dispatcherQueue)
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Services (Singleton)
                    services.AddSingleton<IClockService>(sp => new ClockService(dispatcherQueue));
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<IFileService, FileService>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IDownloadService, DownloadService>();

                    // Media services (Transient - can create multiple instances)
                    services.AddTransient<IMediaService, MediaService>();

                    // ViewModels (Transient)
                    services.AddTransient<MainWindowViewModel>();
                    services.AddTransient<AudioManagerDialogViewModel>();
                })
                .Build();
        }

        private static void PreloadImages()
        {
            var bitmap = new BitmapImage
            {
                UriSource = new Uri("ms-appx:///Assets/loading.gif"),
                DecodePixelWidth = 200
            };
            LoadingGif = bitmap;
        }

        public static void ClearCachedImages()
        {
            LoadingGif = null;
        }

        private Window? m_window;
    }
}
