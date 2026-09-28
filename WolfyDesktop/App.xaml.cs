using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WolfyDesktop.Core.Services;
using WolfyDesktop.Services;
using WolfyDesktop.ViewModels;
using WolfyDesktop.Views;

namespace WolfyDesktop;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        UnhandledException += (_, e) => CrashLog.Write(e.Exception, "XAML");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashLog.Write((Exception)e.ExceptionObject, "AppDomain");
        TaskScheduler.UnobservedTaskException += (_, e) => CrashLog.Write(e.Exception, "Task");

        InitializeComponent();
        Services = ConfigureServices();
    }

    public static new App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = Services.GetRequiredService<ISettingsStore>();
        await settings.LoadAsync();

        _window = new MainWindow();
        _window.Closed += (_, _) => settings.FlushAsync().GetAwaiter().GetResult();
        _window.Activate();

        await MigrateLegacyInstallAsync();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core
        services.AddSingleton(new AppPaths());
        services.AddSingleton<ISettingsStore, SettingsStore>();
        services.AddSingleton<IMusicLibrary, MusicLibrary>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IUpdateService, VelopackUpdateService>();
        services.AddSingleton<LegacyInstallMigrator>();
        services.AddSingleton(_ =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WolfyDesktop", "1.0"));
            return client;
        });

        // UI services; the app is constructed on the UI thread.
        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
        services.AddSingleton<ClockService>();
        services.AddTransient<MediaPlayerService>();
        services.AddSingleton<BackgroundAudioService>();

        // View models
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<AudioManagerViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Brings music over from the old Inno Setup install. Runs after the window is shown
    /// because copying a large library can take a moment on first launch.
    /// </summary>
    private async Task MigrateLegacyInstallAsync()
    {
        var migrator = Services.GetRequiredService<LegacyInstallMigrator>();
        var isInstalled = Services.GetRequiredService<IUpdateService>().IsSupported;

        try
        {
            await Task.Run(() => migrator.Run(allowUninstall: isInstalled));
            Services.GetRequiredService<IMusicLibrary>().NotifyChanged();
        }
        catch (Exception ex)
        {
            // Never block startup on migration; the user can still add music manually.
            Debug.WriteLine($"Legacy install migration failed: {ex}");
        }
    }
}
