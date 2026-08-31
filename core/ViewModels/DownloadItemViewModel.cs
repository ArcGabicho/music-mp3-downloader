using CommunityToolkit.Mvvm.ComponentModel;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.ViewModels;

public partial class DownloadItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private DownloadStatus _status;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string? _errorMessage;

    public DownloadItemViewModel(string url)
    {
        _title = url;
        _status = DownloadStatus.Queued;
    }

    public void Apply(DownloadItem item)
    {
        Title = item.Title ?? item.Url;
        Status = item.Status;
        if (item.Status == DownloadStatus.Completed)
        {
            Progress = 1d;
        }
    }

    public void Fail(string message)
    {
        Status = DownloadStatus.Failed;
        ErrorMessage = message;
    }
}