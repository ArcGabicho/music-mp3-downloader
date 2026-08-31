using System;
using System.Threading;
using System.Threading.Tasks;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.Services;

public interface IDownloadService
{
    Task<DownloadItem> DownloadAsync(
        string url,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}