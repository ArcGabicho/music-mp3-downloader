using System;

namespace MusicMp3Downloader.App.Models;

public sealed class Track
{
    public required string FilePath { get; init; }

    public required string Title { get; init; }

    public string Artist { get; init; } = "Desconocido";

    public string Album { get; init; } = string.Empty;

    public uint Year { get; init; }

    public uint TrackNumber { get; init; }

    public uint DiscNumber { get; init; }

    public TimeSpan Duration { get; init; }

    public byte[]? CoverArt { get; init; }
}