using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.Services;

public sealed class LibraryService : ILibraryService
{
    private readonly IMusicLibrary _musicLibrary;

    public LibraryService(IMusicLibrary musicLibrary)
    {
        _musicLibrary = musicLibrary;
    }

    public string LibraryPath => _musicLibrary.GetMusicDirectory();

    public Task<IReadOnlyList<Track>> ScanAsync(CancellationToken cancellationToken = default)
        => Task.Run<IReadOnlyList<Track>>(() => Scan(cancellationToken), cancellationToken);

    private IReadOnlyList<Track> Scan(CancellationToken cancellationToken)
    {
        var directory = _musicLibrary.GetMusicDirectory();
        var tracks = new List<Track>();

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, "*.mp3", SearchOption.AllDirectories);
        }
        catch (DirectoryNotFoundException)
        {
            return tracks;
        }

        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tracks.Add(ReadTrack(path));
        }

        return tracks
            .OrderBy(t => t.Artist, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Album, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.DiscNumber)
            .ThenBy(t => t.TrackNumber)
            .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Track ReadTrack(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            var tag = file.Tag;
            byte[]? cover = tag.Pictures is { Length: > 0 } pictures ? pictures[0].Data?.Data : null;

            return new Track
            {
                FilePath = path,
                Title = string.IsNullOrWhiteSpace(tag.Title)
                    ? Path.GetFileNameWithoutExtension(path)
                    : tag.Title,
                Artist = tag.FirstPerformer ?? tag.FirstAlbumArtist ?? "Desconocido",
                Album = tag.Album ?? string.Empty,
                Year = tag.Year,
                TrackNumber = tag.Track,
                DiscNumber = tag.Disc,
                Duration = file.Properties?.Duration ?? TimeSpan.Zero,
                CoverArt = cover,
            };
        }
        catch (Exception)
        {
            return new Track
            {
                FilePath = path,
                Title = Path.GetFileNameWithoutExtension(path),
            };
        }
    }
}