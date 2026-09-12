using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MusicMp3Downloader.App.Controls;

/// <summary>Barra de progreso con forma de onda determinista, dibujada a partir de <see cref="Seed"/>.</summary>
public sealed class WaveformScrubber : GraphicsView, IDrawable
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(WaveformScrubber), 0d,
            propertyChanged: (b, _, _) => ((WaveformScrubber)b).Invalidate());

    public static readonly BindableProperty SeedProperty =
        BindableProperty.Create(nameof(Seed), typeof(int), typeof(WaveformScrubber), 0,
            propertyChanged: (b, _, _) => ((WaveformScrubber)b).Invalidate());

    public static readonly BindableProperty SeekCommandProperty =
        BindableProperty.Create(nameof(SeekCommand), typeof(ICommand), typeof(WaveformScrubber));

    public static readonly BindableProperty IsScrubbingProperty =
        BindableProperty.Create(nameof(IsScrubbing), typeof(bool), typeof(WaveformScrubber), false, BindingMode.TwoWay);

    public static readonly BindableProperty PlayedColorProperty =
        BindableProperty.Create(nameof(PlayedColor), typeof(Color), typeof(WaveformScrubber), Colors.OrangeRed,
            propertyChanged: (b, _, _) => ((WaveformScrubber)b).Invalidate());

    public static readonly BindableProperty RemainingColorProperty =
        BindableProperty.Create(nameof(RemainingColor), typeof(Color), typeof(WaveformScrubber), Colors.DimGray,
            propertyChanged: (b, _, _) => ((WaveformScrubber)b).Invalidate());

    private float[] _bars = Array.Empty<float>();
    private int _generatedSeed = int.MinValue;
    private int _generatedCount = -1;

    public WaveformScrubber()
    {
        Drawable = this;
        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
        EndInteraction += OnEndInteraction;
        SizeChanged += (_, _) => Invalidate();
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public int Seed
    {
        get => (int)GetValue(SeedProperty);
        set => SetValue(SeedProperty, value);
    }

    public ICommand? SeekCommand
    {
        get => (ICommand?)GetValue(SeekCommandProperty);
        set => SetValue(SeekCommandProperty, value);
    }

    public bool IsScrubbing
    {
        get => (bool)GetValue(IsScrubbingProperty);
        set => SetValue(IsScrubbingProperty, value);
    }

    public Color PlayedColor
    {
        get => (Color)GetValue(PlayedColorProperty);
        set => SetValue(PlayedColorProperty, value);
    }

    public Color RemainingColor
    {
        get => (Color)GetValue(RemainingColorProperty);
        set => SetValue(RemainingColorProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var width = dirtyRect.Width;
        var height = dirtyRect.Height;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        const float barWidth = 3f;
        const float gap = 2f;
        var count = Math.Max(1, (int)(width / (barWidth + gap)));
        EnsureBars(count);

        var midY = height / 2f;
        var playX = width * (float)Math.Clamp(Progress, 0d, 1d);
        var x = 0f;

        for (var i = 0; i < count; i++)
        {
            var barHeight = 2f + (_bars[i] * (height - 4f));
            canvas.FillColor = (x + barWidth) <= playX ? PlayedColor : RemainingColor;
            canvas.FillRoundedRectangle(x, midY - (barHeight / 2f), barWidth, barHeight, 1.5f);
            x += barWidth + gap;
        }
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        IsScrubbing = true;
        UpdateProgressFrom(e.Touches);
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e) => UpdateProgressFrom(e.Touches);

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        UpdateProgressFrom(e.Touches);
        IsScrubbing = false;

        if (SeekCommand is { } command && command.CanExecute(Progress))
        {
            command.Execute(Progress);
        }
    }

    private void UpdateProgressFrom(PointF[] touches)
    {
        if (touches.Length == 0 || Width <= 0)
        {
            return;
        }

        Progress = Math.Clamp(touches[0].X / Width, 0d, 1d);
    }

    private void EnsureBars(int count)
    {
        if (_bars.Length == count && _generatedSeed == Seed && _generatedCount == count)
        {
            return;
        }

        _bars = new float[count];
        var random = new Random(Seed == 0 ? 1 : Seed);
        for (var i = 0; i < count; i++)
        {
            _bars[i] = (float)(0.15d + (0.85d * Math.Pow(random.NextDouble(), 1.5d)));
        }

        _generatedSeed = Seed;
        _generatedCount = count;
    }
}
