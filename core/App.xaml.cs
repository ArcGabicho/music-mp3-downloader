using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MusicMp3Downloader.App.Views;

namespace MusicMp3Downloader.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // MainPage se resuelve aquí (no por constructor) para que InitializeComponent()
        // ya haya fusionado Palette.xaml/AppStyles.xaml en Application.Current.Resources
        // antes de que MainPage.xaml intente resolver sus StaticResource.
        var mainPage = _services.GetRequiredService<MainPage>();

        return new Window(mainPage)
        {
            Title = "Music MP3 Downloader",
            Width = 1180,
            Height = 720,
            MinimumWidth = 720,
            MinimumHeight = 480,
        };
    }
}
