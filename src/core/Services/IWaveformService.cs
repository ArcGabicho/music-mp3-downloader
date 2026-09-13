using System.Threading;
using System.Threading.Tasks;

namespace MusicMp3Downloader.App.Services;

public interface IWaveformService
{
    Task<float[]> GetPeaksAsync(string filePath, CancellationToken cancellationToken = default);
}
