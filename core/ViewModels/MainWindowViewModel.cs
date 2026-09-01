using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDownloadService _downloads;
    private readonly ILibraryService _library;

    [ObservableProperty]
    private bool _isWide = true;

    [ObservableProperty]
    private bool _isDownloadPanelOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _libraryStatus = string.Empty;

    [ObservableProperty]
    private TrackViewModel? _selectedTrack;

    [ObservableProperty]
    private Bitmap? _coverArt;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _downloadUrl = string.Empty;

    [ObservableProperty]
    private string _downloadStatus = string.Empty;

    public MainWindowViewModel(IDownloadService downloads, ILibraryService library, PlayerViewModel player)
    {
        _downloads = downloads;
        _library = library;
        Player = player;
        Player.PropertyChanged += OnPlayerPropertyChanged;

        _ = LoadLibraryAsync();
    }

    public PlayerViewModel Player { get; }

    public ObservableCollection<TrackViewModel> Tracks { get; } = new();

    public ObservableCollection<DownloadItemViewModel> DownloadQueue { get; } = new();

    public string LibraryPath => _library.LibraryPath;

    public string LibraryCountLabel => $"#{Tracks.Count}";

    [RelayCommand]
    private async Task LoadLibraryAsync()
    {
        IsLoading = true;
        LibraryStatus = "Escaneando biblioteca…";
        try
        {
            var tracks = await _library.ScanAsync();

            Tracks.Clear();
            var position = 1;
            foreach (var track in tracks)
            {
                Tracks.Add(new TrackViewModel(track, position++));
            }

            Player.SetQueue(Tracks);
            OnPropertyChanged(nameof(LibraryCountLabel));

            LibraryStatus = Tracks.Count == 0
                ? $"No hay MP3 en {_library.LibraryPath}"
                : $"{Tracks.Count} pistas · {_library.LibraryPath}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void PlayTrack(TrackViewModel? track)
    {
        if (track is null)
        {
            return;
        }

        SelectedTrack = track;
        Player.Play(track);
    }

    [RelayCommand]
    private void PlayAll()
    {
        if (Tracks.Count > 0)
        {
            Player.Play(Tracks[0]);
        }
    }

    [RelayCommand]
    private void ToggleDownloadPanel() => IsDownloadPanelOpen = !IsDownloadPanelOpen;

    private bool CanDownload() => !string.IsNullOrWhiteSpace(DownloadUrl);

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        var url = DownloadUrl.Trim();
        DownloadUrl = string.Empty;

        var item = new DownloadItemViewModel(url);
        DownloadQueue.Insert(0, item);
        DownloadStatus = $"Descargando: {url}";

        var progress = new Progress<double>(value => item.Progress = value);
        try
        {
            var result = await _downloads.DownloadAsync(url, progress);
            item.Apply(result);
            DownloadStatus = "Descarga completada";
            await LoadLibraryAsync();
        }
        catch (Exception ex)
        {
            item.Fail(ex.Message);
            DownloadStatus = "Error en la descarga";
        }
    }

    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.Current))
        {
            CoverArt = Player.Current?.Cover;
        }
    }
}