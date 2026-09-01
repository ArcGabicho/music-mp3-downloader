using System;
using System.Collections.Generic;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.Tests.Fakes;

/// <summary>Reproductor de mentira para probar <c>PlayerViewModel</c> sin LibVLC.</summary>
public sealed class FakeAudioPlayer : IAudioPlayer
{
    public List<string> Played { get; } = new();

    public bool IsAvailable { get; set; } = true;

    public PlaybackState State { get; set; } = PlaybackState.Stopped;

    public TimeSpan Position { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(3);

    public double Volume { get; set; }

    public TimeSpan? LastSeek { get; private set; }

    public int PauseCount { get; private set; }

    public int ResumeCount { get; private set; }

    public event EventHandler? PlaybackEnded;

    public void Play(string filePath)
    {
        Played.Add(filePath);
        State = PlaybackState.Playing;
        Position = TimeSpan.Zero;
    }

    public void Pause()
    {
        PauseCount++;
        State = PlaybackState.Paused;
    }

    public void Resume()
    {
        ResumeCount++;
        State = PlaybackState.Playing;
    }

    public void Stop() => State = PlaybackState.Stopped;

    public void Seek(TimeSpan position)
    {
        LastSeek = position;
        Position = position;
    }

    public void RaisePlaybackEnded() => PlaybackEnded?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
    }
}
