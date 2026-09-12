using System;
using MusicMp3Downloader.App.Models;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Tests;

public class TrackViewModelTests
{
    private static Track Sample(
        string path = "/music/song.mp3",
        string title = "Song",
        uint track = 0,
        uint disc = 0,
        double seconds = 0) => new()
        {
            FilePath = path,
            Title = title,
            TrackNumber = track,
            DiscNumber = disc,
            Duration = TimeSpan.FromSeconds(seconds),
        };

    [Fact]
    public void IndexLabel_uses_position_when_no_track_number()
    {
        var vm = new TrackViewModel(Sample(), position: 7);
        Assert.Equal("07", vm.IndexLabel);
    }

    [Fact]
    public void IndexLabel_uses_track_number_when_present()
    {
        var vm = new TrackViewModel(Sample(track: 4), position: 7);
        Assert.Equal("04", vm.IndexLabel);
    }

    [Fact]
    public void IndexLabel_includes_disc_when_present()
    {
        var vm = new TrackViewModel(Sample(track: 3, disc: 2), position: 7);
        Assert.Equal("2-03", vm.IndexLabel);
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(5, "0:05")]
    [InlineData(241, "4:01")]
    [InlineData(3723, "1:02:03")]
    public void DurationText_is_formatted(double seconds, string expected)
    {
        var vm = new TrackViewModel(Sample(seconds: seconds), position: 1);
        Assert.Equal(expected, vm.DurationText);
    }

    [Fact]
    public void Title_falls_back_to_file_name_when_blank()
    {
        var vm = new TrackViewModel(Sample(path: "/music/My Track.mp3", title: "   "), position: 1);
        Assert.Equal("My Track", vm.Title);
    }

    [Fact]
    public void Seed_is_deterministic_and_non_zero()
    {
        var a = new TrackViewModel(Sample(path: "/music/a.mp3"), position: 1);
        var b = new TrackViewModel(Sample(path: "/music/a.mp3"), position: 9);
        var c = new TrackViewModel(Sample(path: "/music/b.mp3"), position: 1);

        Assert.NotEqual(0, a.Seed);
        Assert.Equal(a.Seed, b.Seed);
        Assert.NotEqual(a.Seed, c.Seed);
    }
}
