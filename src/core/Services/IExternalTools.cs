namespace MusicMp3Downloader.App.Services;

public interface IExternalTools
{
    string YtDlpPath { get; }

    string? FfmpegDirectory { get; }

    bool YtDlpIsBundled { get; }
}