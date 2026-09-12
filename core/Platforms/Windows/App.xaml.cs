using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace MusicMp3Downloader.App.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception, "AppDomain");
        this.UnhandledException += (_, e) =>
        {
            LogCrash(e.Exception, "WinUI");
            e.Handled = true;
        };

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            LogCrash(ex, "InitializeComponent");
            throw;
        }
    }

    protected override MauiApp CreateMauiApp()
    {
        try
        {
            return MauiProgram.CreateMauiApp();
        }
        catch (Exception ex)
        {
            LogCrash(ex, "CreateMauiApp");
            throw;
        }
    }

    private static void LogCrash(Exception? ex, string source)
    {
        try
        {
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "mp3downloader-crash.log"),
                $"[{source}] {ex}");
        }
        catch
        {
            // ignorado: diagnóstico best-effort.
        }
    }
}
