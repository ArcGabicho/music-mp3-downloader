using System;
using Microsoft.Maui.Controls;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Views;

public partial class MiniPlayerPage : ContentPage
{
    public MiniPlayerPage(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnSeekDragStarted(object? sender, EventArgs e)
    {
        if (BindingContext is MainWindowViewModel viewModel)
        {
            viewModel.Player.IsScrubbing = true;
        }
    }

    private void OnSeekDragCompleted(object? sender, EventArgs e)
    {
        if (sender is not Slider slider || BindingContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.Player.IsScrubbing = false;
        if (viewModel.Player.SeekCommand.CanExecute(slider.Value))
        {
            viewModel.Player.SeekCommand.Execute(slider.Value);
        }
    }
}
