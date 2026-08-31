using System;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.Data;

public sealed class DownloadRecord
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? OutputPath { get; set; }

    public long FileSizeBytes { get; set; }

    public DownloadStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}