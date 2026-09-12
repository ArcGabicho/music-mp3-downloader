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

    private const int DwmwaBorderColor = 34;
    private const int DwmwaColorNone = unchecked((int)0xFFFFFFFE);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
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

                // Ventana emergente sin marco (como el flyout de Mega/Discord): sin barra
                // de título nativa, sin borde y de tamaño fijo.
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(false, false);
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                }

                // SetBorderAndTitleBar(false, false) quita el marco de WinUI, pero en
                // Windows 11 el propio DWM sigue dibujando su borde de sistema (blanco/
                // acento) alrededor de la ventana; hay que desactivarlo aparte.
                var borderColorNone = DwmwaColorNone;
                DwmSetWindowAttribute(handle, DwmwaBorderColor, ref borderColorNone, sizeof(int));

                // La app es únicamente de bandeja: arranca oculta, solo aparece al hacer
                // clic en el ícono (igual que Mega/Discord). Ocultarla ya aquí no sirve:
                // MAUI la activa/muestra justo después de OnWindowCreated, pisando el
                // Hide(); por eso se oculta en cuanto se activa por primera vez.
                void HideOnFirstActivate(object? _, Microsoft.UI.Xaml.WindowActivatedEventArgs __)
                {
                    window.Activated -= HideOnFirstActivate;
                    Application.Current?.Windows.FirstOrDefault(w => w.Page is MainPage)?.Hide();
                }

                window.Activated += HideOnFirstActivate;
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
        services.AddSingleton<IWaveformService, WaveformService>();

        services.AddSingleton<PlayerViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<MainPage>();

#if WINDOWS
        services.AddSingleton<Views.TrayIconView>();
#endif
    }
}
