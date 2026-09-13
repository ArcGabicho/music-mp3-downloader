using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.Services;

public interface ILibraryService
{
    string LibraryPath { get; }

    Task<IReadOnlyList<Track>> ScanAsync(CancellationToken cancellationToken = default);
}