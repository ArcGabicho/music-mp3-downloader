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
#if WINDOWS
using System.Linq;
using System.Threading;
using H.NotifyIcon;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
#endif

namespace MusicMp3Downloader.App;

public static class MauiProgram
{
#if WINDOWS
    private static int _mainWindowHooked;
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        Plugin.Maui.Audio.MauiAppBuilderExtensions.AddAudio(builder);

#if WINDOWS
        builder.UseNotifyIcon();
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows => windows.OnWindowCreated(window =>
            {
                // Solo la primera ventana (la principal) se va a la bandeja al cerrarla;
                // la mini-ventana emergente del ícono de bandeja debe cerrar normalmente.
                if (Interlocked.Exchange(ref _mainWindowHooked, 1) != 0)
                {
                    return;
                }

                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                if (appWindow is null)
                {
                    return;
                }

                appWindow.Closing += (_, args) =>
                {
                    args.Cancel = true;
                    Application.Current?.Windows.FirstOrDefault(w => w.Page is MainPage)?.Hide();
                };
            }));
        });
#endif

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
        services.AddTransient<MiniPlayerPage>();

#if WINDOWS
        services.AddSingleton<Views.TrayIconView>();
#endif
    }
}
