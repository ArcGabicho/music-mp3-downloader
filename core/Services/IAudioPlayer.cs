using System;

namespace MusicMp3Downloader.App.Services;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,
}

public interface IAudioPlayer : IDisposable
{
    bool IsAvailable { get; }

    PlaybackState State { get; }

    TimeSpan Position { get; }

    TimeSpan Duration { get; }

    double Volume { get; set; }

    event EventHandler? PlaybackEnded;

    void Play(string filePath);

    void Pause();

    void Resume();

    void Stop();

    void Seek(TimeSpan position);
}