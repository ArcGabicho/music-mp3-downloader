using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace MusicMp3Downloader.App.Converters;

public sealed class ByteArrayToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is byte[] { Length: > 0 } bytes
            ? ImageSource.FromStream(() => new MemoryStream(bytes))
            : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
