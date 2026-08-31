using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDownloadService _downloads;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _status = "Listo";

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();

    public MainWindowViewModel(IDownloadService downloads)
    {
        _downloads = downloads;
    }

    private bool CanDownload() => !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        var url = Url.Trim();
        Url = string.Empty;

        var item = new DownloadItemViewModel(url);
        Downloads.Insert(0, item);
        Status = $"Descargando: {url}";

        var progress = new Progress<double>(value => item.Progress = value);
        try
        {
            var result = await _downloads.DownloadAsync(url, progress);
            item.Apply(result);
            Status = "Listo";
        }
        catch (Exception ex)
        {
            item.Fail(ex.Message);
            Status = "Error en la descarga";
        }
    }
}