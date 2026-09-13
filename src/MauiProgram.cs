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

    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    private const int GwlStyle = -16;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsBorder = 0x00800000L;
    private const long WsDlgFrame = 0x00400000L;
    private const long WsSysMenu = 0x00080000L;

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // Tres capas contra el marco nativo, porque ninguna sola alcanza en una app sin
    // identidad de paquete (WindowsPackageType=None, sin MSIX):
    // 1) SetBorderAndTitleBar(false, false) (llamado por el caller) pide el modo sin
    //    marco a WinUI, pero en apps sin empaquetar no siempre limpia los bits de estilo
    //    nativos de la ventana, dejando el marco clásico (grueso, con relieve).
    // 2) Por eso se limpian también a mano los bits WS_CAPTION/WS_THICKFRAME/WS_BORDER/
    //    WS_DLGFRAME/WS_SYSMENU de GWL_STYLE.
    // 3) DWMWA_BORDER_COLOR = DWMWA_COLOR_NONE quita el borde de acento que DWM sigue
    //    dibujando en Windows 11 encima de todo lo anterior.
    // Se reaplican las tres en cada Activated (no solo al crear la ventana) porque
    // ocultar/mostrar el popup puede hacer que Windows redibuje el marco por defecto;
    // SWP_FRAMECHANGED fuerza el redibujado inmediato en vez de esperar a otro evento.
    private static void RemoveWindowChrome(IntPtr handle)
    {
        var style = GetWindowLongPtr(handle, GwlStyle).ToInt64();
        style &= ~(WsCaption | WsThickFrame | WsBorder | WsDlgFrame | WsSysMenu);
        SetWindowLongPtr(handle, GwlStyle, new IntPtr(style));

        var borderColorNone = DwmwaColorNone;
        DwmSetWindowAttribute(handle, DwmwaBorderColor, ref borderColorNone, sizeof(int));

        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
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

                RemoveWindowChrome(handle);

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

                // Reaplica en cada activación posterior: ocultar/mostrar la ventana (al
                // hacer clic en el ícono de bandeja) puede hacer que Windows vuelva a
                // dibujar el marco por defecto.
                window.Activated += (_, _) => RemoveWindowChrome(handle);
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
