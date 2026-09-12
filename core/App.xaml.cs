using Microsoft.Maui.Controls;
using MusicMp3Downloader.App.Views;

namespace MusicMp3Downloader.App;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_mainPage)
        {
            Title = "Music MP3 Downloader",
            Width = 1180,
            Height = 720,
            MinimumWidth = 720,
            MinimumHeight = 480,
        };
    }
}
