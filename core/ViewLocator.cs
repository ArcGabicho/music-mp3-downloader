using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        var name = param?.GetType().FullName?
            .Replace("ViewModels", "Views", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var type = name is null ? null : Type.GetType(name);

        return type is not null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = $"View no encontrada: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}