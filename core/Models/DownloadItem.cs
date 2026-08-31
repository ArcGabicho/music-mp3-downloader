using System;

namespace MusicMp3Downloader.App.Models;

public sealed class DownloadItem
{
    public required string Url { get; init; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? OutputPath { get; set; }

    public long FileSizeBytes { get; set; }

    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}