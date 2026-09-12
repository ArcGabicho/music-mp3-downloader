using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace MusicMp3Downloader.App.Views;

/// <summary>Ícono de la bandeja del sistema (solo Windows): alterna la mini-ventana emergente
/// con el clic izquierdo y ofrece "Abrir ventana principal"/"Salir" con el clic derecho.</summary>
public partial class TrayIconView : ContentView
{
    private const double MiniPlayerWidth = 340;
    private const double MiniPlayerHeight = 230;
    private const double ScreenMargin = 16;
    private const double TaskbarAllowance = 64;

    private readonly IServiceProvider _services;
    private Window? _miniPlayerWindow;

    public TrayIconView(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        BindingContext = this;
    }

    [RelayCommand]
    private void ToggleMiniPlayer()
    {
        try
        {
            if (_miniPlayerWindow is not null)
            {
                Application.Current?.CloseWindow(_miniPlayerWindow);
                return;
            }

            var page = _services.GetRequiredService<MiniPlayerPage>();
            var window = new Window(page)
            {
                Width = MiniPlayerWidth,
                Height = MiniPlayerHeight,
            };
            PositionNearTray(window);

            window.Destroying += (_, _) => _miniPlayerWindow = null;
            _miniPlayerWindow = window;
            Application.Current?.OpenWindow(window);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mp3downloader-crash.log"),
                $"[ToggleMiniPlayer] {ex}");
        }
    }

    [RelayCommand]
    private void ShowMainWindow()
    {
        var window = Application.Current?.Windows.FirstOrDefault(w => w.Page is MainPage);
        window?.Show();
    }

    [RelayCommand]
    private void Exit() => Application.Current?.Quit();

    private static void PositionNearTray(Window window)
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;
        if (display.Density <= 0)
        {
            return;
        }

        var screenWidth = display.Width / display.Density;
        var screenHeight = display.Height / display.Density;

        window.X = Math.Max(0, screenWidth - MiniPlayerWidth - ScreenMargin);
        window.Y = Math.Max(0, screenHeight - MiniPlayerHeight - TaskbarAllowance);
    }
}
