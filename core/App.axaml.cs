using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicMp3Downloader.App.Data;
using MusicMp3Downloader.App.Services;
using MusicMp3Downloader.App.ViewModels;
using MusicMp3Downloader.App.Views;

namespace MusicMp3Downloader.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MusicMp3Downloader");
        Directory.CreateDirectory(dataDir);

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(dataDir, "app.db")}"));

        services.AddSingleton<IAudioTagger, TagLibAudioTagger>();
        services.AddSingleton<IMusicLibrary, MusicLibrary>();
        services.AddSingleton<IDownloadService, DownloadService>();

        services.AddSingleton<MainWindowViewModel>();
    }
}