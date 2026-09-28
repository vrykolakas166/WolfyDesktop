using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WolfyDesktop.Core.Models;
using WolfyDesktop.ViewModels;

namespace WolfyDesktop.Views;

public sealed partial class AudioManagerDialog : UserControl
{
    public AudioManagerDialog()
    {
        ViewModel = App.Current.Services.GetRequiredService<AudioManagerViewModel>();
        InitializeComponent();
        ViewModel.Refresh();
    }

    public AudioManagerViewModel ViewModel { get; }

    private async void AddMusicButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.MusicLibrary };
        foreach (var extension in ViewModel.SupportedExtensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        // Unpackaged apps must tell the picker which window owns it.
        var hwnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            await ViewModel.ImportAsync(files);
        }
    }

    private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ViewModel.SelectedTrack = TrackList.SelectedItem as MusicTrack;
}
