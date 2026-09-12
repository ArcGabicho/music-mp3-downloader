using System;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.Tests.Fakes;

/// <summary>Ejecuta la acción de forma síncrona, sin marshalling, para pruebas de ViewModels.</summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}
