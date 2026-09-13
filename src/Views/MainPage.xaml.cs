using Microsoft.Maui.Controls;

namespace MusicMp3Downloader.App.Views;

public partial class MainPage : ContentPage
{
#if WINDOWS
    public MainPage(TrayIconView trayIconView)
    {
        InitializeComponent();

        if (Content is Layout root)
        {
            root.Children.Add(trayIconView);
        }
    }
#else
    public MainPage() => InitializeComponent();
#endif
}
