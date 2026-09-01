using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace MusicMp3Downloader.App.Controls;

public class WaveformScrubber : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<WaveformScrubber, double>(nameof(Progress));

    public static readonly StyledProperty<int> SeedProperty =
        AvaloniaProperty.Register<WaveformScrubber, int>(nameof(Seed));

    public static readonly StyledProperty<ICommand?> SeekCommandProperty =
        AvaloniaProperty.Register<WaveformScrubber, ICommand?>(nameof(SeekCommand));

    public static readonly StyledProperty<bool> IsScrubbingProperty =
        AvaloniaProperty.Register<WaveformScrubber, bool>(
            nameof(IsScrubbing), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IBrush> PlayedBrushProperty =
        AvaloniaProperty.Register<WaveformScrubber, IBrush>(nameof(PlayedBrush), Brushes.OrangeRed);

    public static readonly StyledProperty<IBrush> RemainingBrushProperty =
        AvaloniaProperty.Register<WaveformScrubber, IBrush>(nameof(RemainingBrush), Brushes.DimGray);

    private float[] _bars = Array.Empty<float>();
    private int _generatedSeed = int.MinValue;
    private int _generatedCount = -1;

    static WaveformScrubber()
    {
        AffectsRender<WaveformScrubber>(
            ProgressProperty, SeedProperty, PlayedBrushProperty, RemainingBrushProperty);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public int Seed
    {
        get => GetValue(SeedProperty);
        set => SetValue(SeedProperty, value);
    }

    public ICommand? SeekCommand
    {
        get => GetValue(SeekCommandProperty);
        set => SetValue(SeekCommandProperty, value);
    }

    public bool IsScrubbing
    {
        get => GetValue(IsScrubbingProperty);
        set => SetValue(IsScrubbingProperty, value);
    }

    public IBrush PlayedBrush
    {
        get => GetValue(PlayedBrushProperty);
        set => SetValue(PlayedBrushProperty, value);
    }

    public IBrush RemainingBrush
    {
        get => GetValue(RemainingBrushProperty);
        set => SetValue(RemainingBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        const double barWidth = 3d;
        const double gap = 2d;
        var count = Math.Max(1, (int)(width / (barWidth + gap)));
        EnsureBars(count);

        var midY = height / 2d;
        var playX = width * Math.Clamp(Progress, 0d, 1d);
        var x = 0d;

        for (var i = 0; i < count; i++)
        {
            var barHeight = 2d + (_bars[i] * (height - 4d));
            var rect = new Rect(x, midY - (barHeight / 2d), barWidth, barHeight);
            var brush = (x + barWidth) <= playX ? PlayedBrush : RemainingBrush;
            context.FillRectangle(brush, rect, 1.5f);
            x += barWidth + gap;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        e.Pointer.Capture(this);
        IsScrubbing = true;
        UpdateProgressFrom(e.GetPosition(this).X);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            UpdateProgressFrom(e.GetPosition(this).X);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!ReferenceEquals(e.Pointer.Captured, this))
        {
            return;
        }

        e.Pointer.Capture(null);
        UpdateProgressFrom(e.GetPosition(this).X);
        IsScrubbing = false;

        if (SeekCommand is { } command && command.CanExecute(Progress))
        {
            command.Execute(Progress);
        }
    }

    private void UpdateProgressFrom(double x)
    {
        if (Bounds.Width > 0)
        {
            Progress = Math.Clamp(x / Bounds.Width, 0d, 1d);
        }
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