using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MusicMp3Downloader.App.Converters;

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null and not (string { Length: 0 });

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
