using Avalonia.Controls;
using Avalonia.Input;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Views;

public partial class MainWindow : Window
{
    private const double WideThreshold = 900d;

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += OnWindowSizeChanged;
        Loaded += (_, _) => UpdateLayout(Bounds.Width);
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateLayout(e.NewSize.Width);

    private void UpdateLayout(double width)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsWide = width >= WideThreshold;
        }
    }

    private void OnTrackActivated(object? sender, TappedEventArgs e)
    {
        if (TrackList.SelectedItem is TrackViewModel track && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PlayTrackCommand.Execute(track);
        }
    }

    private void OnScrimTapped(object? sender, TappedEventArgs e)
    {
        // Solo cierra al pulsar el fondo oscuro, no la tarjeta del modal.
        if (ReferenceEquals(e.Source, sender) && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsDownloadPanelOpen = false;
        }
    }
}