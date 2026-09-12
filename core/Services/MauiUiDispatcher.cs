using System;
using Microsoft.Maui.Dispatching;

namespace MusicMp3Downloader.App.Services;

public sealed class MauiUiDispatcher : IUiDispatcher
{
    private readonly IDispatcher _dispatcher;

    public MauiUiDispatcher(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Post(Action action) => _dispatcher.Dispatch(action);
}
