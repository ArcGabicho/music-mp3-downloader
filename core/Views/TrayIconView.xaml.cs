using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace MusicMp3Downloader.App.Views;

/// <summary>Ícono de la bandeja del sistema (solo Windows): la app es únicamente de
/// bandeja, sin ventana visible por defecto. El clic izquierdo alterna la visibilidad de
/// la ventana principal, posicionándola cerca de la bandeja (igual que Mega/Discord); el
/// clic derecho ofrece "Mostrar"/"Salir".</summary>
public partial class TrayIconView : ContentView
{
    private const double ScreenMargin = 16;
    private const double TaskbarAllowance = 64;

    public TrayIconView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    [RelayCommand]
    private void ToggleMainWindow()
    {
        var window = FindMainWindow();
        if (window is null)
        {
            return;
        }

        if (IsWindowVisible(window))
        {
            window.Hide();
        }
        else
        {
            PositionNearTray(window);
            window.Show();
        }
    }

    [RelayCommand]
    private void ShowMainWindow()
    {
        var window = FindMainWindow();
        if (window is null)
        {
            return;
        }

        PositionNearTray(window);
        window.Show();
    }

    [RelayCommand]
    private void Exit() => Application.Current?.Quit();

    private static Window? FindMainWindow() =>
        Application.Current?.Windows.FirstOrDefault(w => w.Page is MainPage);

    private static bool IsWindowVisible(Window window)
    {
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return false;
        }

        var handle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        return appWindow?.IsVisible ?? false;
    }

    private static void PositionNearTray(Window window)
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;
        if (display.Density <= 0)
        {
            return;
        }

        var screenWidth = display.Width / display.Density;
        var screenHeight = display.Height / display.Density;

        window.X = Math.Max(0, screenWidth - window.Width - ScreenMargin);
        window.Y = Math.Max(0, screenHeight - window.Height - TaskbarAllowance);
    }
}
