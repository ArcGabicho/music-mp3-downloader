using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MusicMp3Downloader.App.Controls;

/// <summary>Dibuja los picos de amplitud reales de la pista (calculados por
/// <c>IWaveformService</c>) como barras verticales monocromáticas: la altura refleja la
/// amplitud real del audio, sin distinguir progreso por color. El control real de la
/// reproducción (seek) es la barra de progreso inferior, no esta forma de onda.</summary>
public sealed class WaveformView : GraphicsView, IDrawable
{
    private static readonly Color BarColor = new(1f, 1f, 1f, 0.45f);

    public static readonly BindableProperty PeaksProperty = BindableProperty.Create(
        nameof(Peaks),
        typeof(float[]),
        typeof(WaveformView),
        Array.Empty<float>(),
        propertyChanged: (bindable, _, _) => ((WaveformView)bindable).Invalidate());

    public WaveformView() => Drawable = this;

    public float[] Peaks
    {
        get => (float[])GetValue(PeaksProperty);
        set => SetValue(PeaksProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var peaks = Peaks;
        if (peaks.Length == 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
        {
            return;
        }

        const float minHeightFraction = 0.12f;
        const float gap = 2.5f;

        var barCount = peaks.Length;
        var barWidth = Math.Max(1.5f, (dirtyRect.Width - gap * (barCount - 1)) / barCount);

        canvas.FillColor = BarColor;
        for (var i = 0; i < barCount; i++)
        {
            var amplitude = Math.Clamp(peaks[i], 0f, 1f);
            var height = dirtyRect.Height * Math.Max(minHeightFraction, amplitude);
            var x = dirtyRect.X + i * (barWidth + gap);
            var y = dirtyRect.Y + (dirtyRect.Height - height) / 2f;

            canvas.FillRoundedRectangle(x, y, barWidth, height, barWidth / 2f);
        }
    }
}
