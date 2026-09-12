using System;

namespace MusicMp3Downloader.App.Data;

public sealed class WaveformCacheEntry
{
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string PeaksCsv { get; set; } = string.Empty;

    public DateTimeOffset ComputedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
