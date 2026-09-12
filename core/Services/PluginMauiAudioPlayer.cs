using System;
using MauiAudio = Plugin.Maui.Audio;

namespace MusicMp3Downloader.App.Services;

/// <summary>Adapta <c>Plugin.Maui.Audio</c> a <see cref="IAudioPlayer"/> (reemplaza a LibVLC, sin soporte oficial en Mac Catalyst).</summary>
public sealed class PluginMauiAudioPlayer : IAudioPlayer
{
    private readonly MauiAudio.IAudioManager _audioManager;
    private MauiAudio.IAudioPlayer? _player;
    private bool _isPaused;
    private double _volume = 0.8;

    public PluginMauiAudioPlayer(MauiAudio.IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public bool IsAvailable => true;

    public PlaybackState State => _player switch
    {
        null => PlaybackState.Stopped,
        { IsPlaying: true } => PlaybackState.Playing,
        _ => _isPaused ? PlaybackState.Paused : PlaybackState.Stopped,
    };

    public TimeSpan Position => TimeSpan.FromSeconds(_player?.CurrentPosition ?? 0d);

    public TimeSpan Duration => TimeSpan.FromSeconds(_player?.Duration ?? 0d);

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0d, 1d);
            if (_player is not null)
            {
                _player.Volume = _volume;
            }
        }
    }

    public event EventHandler? PlaybackEnded;

    public void Play(string filePath)
    {
        DisposePlayer();

        _player = _audioManager.CreatePlayer(filePath);
        _player.Volume = _volume;
        _player.PlaybackEnded += OnPlaybackEnded;
        _isPaused = false;
        _player.Play();
    }

    public void Pause()
    {
        _player?.Pause();
        _isPaused = true;
    }

    public void Resume()
    {
        _player?.Play();
        _isPaused = false;
    }

    public void Stop()
    {
        _player?.Stop();
        _isPaused = false;
    }

    public void Seek(TimeSpan position)
    {
        _player?.Seek(Math.Max(0, position.TotalSeconds));
    }

    private void OnPlaybackEnded(object? sender, EventArgs e) => PlaybackEnded?.Invoke(this, EventArgs.Empty);

    private void DisposePlayer()
    {
        if (_player is null)
        {
            return;
        }

        _player.PlaybackEnded -= OnPlaybackEnded;
        _player.Dispose();
        _player = null;
    }

    public void Dispose() => DisposePlayer();
}
