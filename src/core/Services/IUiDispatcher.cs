using System;

namespace MusicMp3Downloader.App.Services;

/// <summary>Marshalla una acción al hilo de UI, independiente del framework de presentación.</summary>
public interface IUiDispatcher
{
    void Post(Action action);
}
