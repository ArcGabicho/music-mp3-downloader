using System;
using System.Collections.Generic;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.ViewModels;

public partial class PlayerViewModel : ViewModelBase
{
    private readonly IAudioPlayer _player;
    private readonly IUiDispatcher _dispatcher;
    private readonly Timer _timer;
    private IReadOnlyList<TrackViewModel> _queue = Array.Empty<TrackViewModel>();

    [ObservableProperty]
    private TrackViewModel? _current;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _positionText = "0:00";

    [ObservableProperty]
    private string _durationText = "0:00";

    [ObservableProperty]
    private double _volume = 0.8;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private int _seed;

    [ObservableProperty]
    private bool _isScrubbing;

    [ObservableProperty]
    private string? _statusMessage;

    public PlayerViewModel(IAudioPlayer player, IUiDispatcher dispatcher)
    {
        _player = player;
        _dispatcher = dispatcher;
        _player.Volume = _volume;
        _player.PlaybackEnded += (_, _) => _dispatcher.Post(Next);

        var interval = TimeSpan.FromMilliseconds(250);
        _timer = new Timer(_ => _dispatcher.Post(Tick), null, interval, interval);

        if (!_player.IsAvailable)
        {
            StatusMessage = "Reproducción no disponible en este equipo.";
        }
    }

    public bool IsAvailable => _player.IsAvailable;

    public string HeaderArtist => Current?.Artist.ToUpperInvariant() ?? "BIBLIOTECA LOCAL";

    public string HeaderYear => Current is { Year: > 0 } ? Current.Year.ToString() : string.Empty;

    public string BigTitle => Current is null
        ? "BIBLIOTECA"
        : $"{IndexOf(Current) + 1:00}–{Current.Title.ToUpperInvariant()}";

    public string? NowPlayingLabel => Current is null
        ? null
        : $"{Current.IndexLabel}   {Current.Title.ToUpperInvariant()}";

    public void SetQueue(IReadOnlyList<TrackViewModel> queue) => _queue = queue;

    public void Play(TrackViewModel track)
    {
        if (!_player.IsAvailable)
        {
            return;
        }

        if (Current is { } previous)
        {
            previous.IsPlaying = false;
        }

        Current = track;
        track.IsPlaying = true;
        Seed = track.Seed;
        _player.Play(track.FilePath);
        IsPlaying = true;
        RaiseNowPlaying();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (Current is null)
        {
            if (_queue.Count > 0)
            {
                Play(_queue[0]);
            }

            return;
        }

        if (IsPlaying)
        {
            _player.Pause();
            IsPlaying = false;
        }
        else
        {
            _player.Resume();
            IsPlaying = true;
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (_queue.Count == 0)
        {
            return;
        }

        var index = Current is null ? -1 : IndexOf(Current);
        Play(_queue[(index + 1) % _queue.Count]);
    }

    [RelayCommand]
    private void Previous()
    {
        if (_queue.Count == 0)
        {
            return;
        }

        if (_player.Position > TimeSpan.FromSeconds(3))
        {
            _player.Seek(TimeSpan.Zero);
            return;
        }

        var index = Current is null ? 0 : IndexOf(Current);
        Play(_queue[(index - 1 + _queue.Count) % _queue.Count]);
    }

    [RelayCommand]
    private void Seek(double fraction)
    {
        fraction = Math.Clamp(fraction, 0d, 1d);
        var duration = _player.Duration;
        if (duration > TimeSpan.Zero)
        {
            _player.Seek(TimeSpan.FromSeconds(duration.TotalSeconds * fraction));
        }

        // Refleja de inmediato la posición pedida para que la onda no dé un salto
        // hasta que el reproductor reporte la nueva posición.
        Progress = fraction;
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _player.Volume = IsMuted ? 0d : Volume;
    }

    partial void OnVolumeChanged(double value)
    {
        if (!IsMuted)
        {
            _player.Volume = value;
        }
    }

    partial void OnCurrentChanged(TrackViewModel? value) => RaiseNowPlaying();

    private void RaiseNowPlaying()
    {
        OnPropertyChanged(nameof(HeaderArtist));
        OnPropertyChanged(nameof(HeaderYear));
        OnPropertyChanged(nameof(BigTitle));
        OnPropertyChanged(nameof(NowPlayingLabel));
    }

    private void Tick()
    {
        if (!_player.IsAvailable || IsScrubbing)
        {
            // Mientras se arrastra la barra, no se pisa la posición que marca el usuario.
            return;
        }

        var duration = _player.Duration;
        var position = _player.Position;

        Progress = duration > TimeSpan.Zero
            ? Math.Clamp(position.TotalSeconds / duration.TotalSeconds, 0d, 1d)
            : 0d;
        PositionText = Format(position);
        DurationText = Format(duration);
        IsPlaying = _player.State == PlaybackState.Playing;
    }

    private int IndexOf(TrackViewModel track)
    {
        for (var i = 0; i < _queue.Count; i++)
        {
            if (ReferenceEquals(_queue[i], track))
            {
                return i;
            }
        }

        return -1;
    }

    private static string Format(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{value.Minutes}:{value.Seconds:00}";
}