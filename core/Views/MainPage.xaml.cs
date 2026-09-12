using Microsoft.Maui.Controls;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Views;

public partial class MainPage : ContentPage
{
    private const double WideThreshold = 900d;

    public MainPage(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (BindingContext is MainWindowViewModel viewModel)
        {
            viewModel.IsWide = width >= WideThreshold;
        }
    }
}
