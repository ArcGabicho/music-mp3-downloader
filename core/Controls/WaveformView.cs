using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MusicMp3Downloader.App.Controls;

/// <summary>Dibuja los picos de amplitud reales de la pista (calculados por
/// <c>IWaveformService</c>) como barras verticales, resaltando la porción ya reproducida
/// según <see cref="Progress"/>. Es puramente visual: el control real de la reproducción
/// (seek) sigue siendo la barra de progreso inferior.</summary>
public sealed class WaveformView : GraphicsView, IDrawable
{
    public static readonly BindableProperty PeaksProperty = BindableProperty.Create(
        nameof(Peaks),
        typeof(float[]),
        typeof(WaveformView),
        Array.Empty<float>(),
        propertyChanged: (bindable, _, _) => ((WaveformView)bindable).Invalidate());

    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress),
        typeof(double),
        typeof(WaveformView),
        0d,
        propertyChanged: (bindable, _, _) => ((WaveformView)bindable).Invalidate());

    public WaveformView() => Drawable = this;

    public float[] Peaks
    {
        get => (float[])GetValue(PeaksProperty);
        set => SetValue(PeaksProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
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
        var playedBars = (int)(barCount * Math.Clamp(Progress, 0d, 1d));

        for (var i = 0; i < barCount; i++)
        {
            var amplitude = Math.Clamp(peaks[i], 0f, 1f);
            var height = dirtyRect.Height * Math.Max(minHeightFraction, amplitude);
            var x = dirtyRect.X + i * (barWidth + gap);
            var y = dirtyRect.Y + (dirtyRect.Height - height) / 2f;

            canvas.FillColor = i < playedBars ? Colors.White : new Color(1f, 1f, 1f, 0.25f);
            canvas.FillRoundedRectangle(x, y, barWidth, height, barWidth / 2f);
        }
    }
}
