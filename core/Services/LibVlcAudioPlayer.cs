using System;
using System.Threading;
using LibVLCSharp.Shared;

namespace MusicMp3Downloader.App.Services;

public sealed class LibVlcAudioPlayer : IAudioPlayer
{
    private readonly LibVLC? _libVlc;
    private readonly MediaPlayer? _player;
    private double _volume = 0.8;

    public LibVlcAudioPlayer()
    {
        try
        {
            Core.Initialize();

            // --no-video: solo audio.  --quiet: sin volcado de mensajes de libvlc a la consola
            // (evita el ruido de "Timestamp conversion failed" del decodificador mpg123 al buscar).
            _libVlc = new LibVLC("--no-video", "--quiet");
            _player = new MediaPlayer(_libVlc);
            _player.Volume = (int)Math.Round(_volume * 100);
            _player.EndReached += OnEndReached;
        }
        catch (Exception)
        {
            _libVlc?.Dispose();
            _libVlc = null;
            _player = null;
        }
    }

    public bool IsAvailable => _player is not null;

    public PlaybackState State => _player?.State switch
    {
        VLCState.Playing => PlaybackState.Playing,
        VLCState.Paused => PlaybackState.Paused,
        _ => PlaybackState.Stopped,
    };

    public TimeSpan Position =>
        _player is null ? TimeSpan.Zero : TimeSpan.FromMilliseconds(Math.Max(0, _player.Time));

    public TimeSpan Duration =>
        _player is null ? TimeSpan.Zero : TimeSpan.FromMilliseconds(Math.Max(0, _player.Length));

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0d, 1d);
            if (_player is not null)
            {
                _player.Volume = (int)Math.Round(_volume * 100);
            }
        }
    }

    public event EventHandler? PlaybackEnded;

    public void Play(string filePath)
    {
        if (_player is null || _libVlc is null)
        {
            return;
        }

        using var media = new Media(_libVlc, filePath, FromType.FromPath);
        _player.Play(media);
    }

    public void Pause()
    {
        if (_player?.CanPause == true)
        {
            _player.SetPause(true);
        }
    }

    public void Resume() => _player?.SetPause(false);

    public void Stop()
    {
        // Stop() no debe ejecutarse en el hilo de eventos de LibVLC; se lanza aparte.
        var player = _player;
        if (player is null)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                player.Stop();
            }
            catch (Exception)
            {
                // ignorado
            }
        });
    }

    public void Seek(TimeSpan position)
    {
        if (_player is not null)
        {
            _player.Time = (long)Math.Max(0, position.TotalMilliseconds);
        }
    }

    private void OnEndReached(object? sender, EventArgs e)
        => PlaybackEnded?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (_player is not null)
        {
            _player.EndReached -= OnEndReached;
            _player.Dispose();
        }

        _libVlc?.Dispose();
    }
}