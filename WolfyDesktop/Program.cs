using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Velopack;

namespace WolfyDesktop;

public static class Program
{
    [STAThread]
    private static void Main()
    {
        // Must run first: handles install/update/uninstall hooks (and exits for them),
        // and applies an already-downloaded update before the app starts.
        VelopackApp.Build().Run();

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
