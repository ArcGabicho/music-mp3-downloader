using System;
using System.IO;

namespace MusicMp3Downloader.App.Services;

public sealed class ExternalTools : IExternalTools
{
    public ExternalTools()
    {
        var toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
        var ytDlpName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";
        var ffmpegName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

        var bundledYtDlp = Path.Combine(toolsDir, ytDlpName);
        if (File.Exists(bundledYtDlp))
        {
            YtDlpPath = bundledYtDlp;
            YtDlpIsBundled = true;
            MakeExecutable(bundledYtDlp);
        }
        else
        {
            // Reserva: que lo resuelva el PATH del sistema.
            YtDlpPath = ytDlpName;
        }

        var bundledFfmpeg = Path.Combine(toolsDir, ffmpegName);
        if (File.Exists(bundledFfmpeg))
        {
            FfmpegDirectory = toolsDir;
            MakeExecutable(bundledFfmpeg);
        }
    }

    public string YtDlpPath { get; }

    public string? FfmpegDirectory { get; }

    public bool YtDlpIsBundled { get; }

    private static void MakeExecutable(string path)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(path))
        {
            return;
        }

        try
        {
            var mode = File.GetUnixFileMode(path);
            File.SetUnixFileMode(
                path,
                mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }
        catch (Exception)
        {
            // Si falla, yt-dlp lo reportará al arrancar; no es fatal aquí.
        }
    }
}