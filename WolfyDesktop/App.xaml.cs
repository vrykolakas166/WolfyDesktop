using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WolfyDesktop
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static BitmapImage? LoadingGif { get; set; }
        public static nint MainHandle = nint.Zero;

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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            PreloadImages();
            m_window = new MainWindow();
            MainHandle = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
            m_window.Activate();
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
