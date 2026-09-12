using System;
using System.Threading;
using System.Threading.Tasks;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.Tests.Fakes;

/// <summary>Devuelve un arreglo de picos vacío de inmediato, sin invocar FFmpeg, para pruebas de ViewModels.</summary>
public sealed class FakeWaveformService : IWaveformService
{
    public Task<float[]> GetPeaksAsync(string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<float>());
}
