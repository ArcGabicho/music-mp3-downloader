namespace MusicMp3Downloader.App.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Converting,
    Tagging,
    Completed,
    Failed,
}