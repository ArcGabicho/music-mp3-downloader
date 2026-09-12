using Microsoft.Maui.Controls;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
