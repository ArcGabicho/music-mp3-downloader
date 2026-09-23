namespace MusicMp3Downloader.App.Services;

public interface IExternalTools
{
    string YtDlpPath { get; }

    string? FfmpegDirectory { get; }

    bool YtDlpIsBundled { get; }

    /// <summary>
    /// Deno empaquetado, que yt-dlp usa como intérprete de JavaScript para resolver los
    /// desafíos de YouTube. <c>null</c> si no está: yt-dlp lo buscará en el PATH.
    /// </summary>
    string? DenoPath { get; }
}