using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using MusicMp3Downloader.App.Data;
using MusicMp3Downloader.App.Services;
using MusicMp3Downloader.App.ViewModels;
using MusicMp3Downloader.App.Views;

namespace MusicMp3Downloader.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        ConfigureServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dataDir = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(dataDir);

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(dataDir, "app.db")}"));

        services.AddSingleton(Plugin.Maui.Audio.AudioManager.Current);
        services.AddSingleton<IAudioTagger, TagLibAudioTagger>();
        services.AddSingleton<IMusicLibrary, MusicLibrary>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<IAudioPlayer, PluginMauiAudioPlayer>();
        services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();
        services.AddSingleton<IExternalTools, ExternalTools>();
        services.AddSingleton<IDownloadService, DownloadService>();

        services.AddSingleton<PlayerViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<MainPage>();
    }
}
