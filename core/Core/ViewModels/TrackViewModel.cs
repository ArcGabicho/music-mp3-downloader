using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.ViewModels;

public partial class TrackViewModel : ViewModelBase
{
    private readonly Track _track;

    [ObservableProperty]
    private bool _isPlaying;

    public TrackViewModel(Track track, int position)
    {
        _track = track;
        Position = position;
        Seed = ComputeSeed(track.FilePath);
    }

    public string FilePath => _track.FilePath;

    public string Title => string.IsNullOrWhiteSpace(_track.Title)
        ? Path.GetFileNameWithoutExtension(_track.FilePath)
        : _track.Title;

    public string Artist => string.IsNullOrWhiteSpace(_track.Artist) ? "Desconocido" : _track.Artist;

    public string Album => _track.Album;

    public uint Year => _track.Year;

    public int Position { get; }

    public int Seed { get; }

    public string IndexLabel
    {
        get
        {
            var number = _track.TrackNumber > 0 ? _track.TrackNumber : (uint)Position;
            return _track.DiscNumber > 0 ? $"{_track.DiscNumber}-{number}" : number.ToString();
        }
    }

    public string DurationText => _track.Duration.TotalHours >= 1
        ? _track.Duration.ToString(@"h\:mm\:ss")
        : _track.Duration.ToString(@"m\:ss");

    public byte[]? CoverArtBytes => _track.CoverArt;

    private static int ComputeSeed(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = (hash * 31) + c;
            }

            return hash == 0 ? 1 : hash;
        }
    }
}