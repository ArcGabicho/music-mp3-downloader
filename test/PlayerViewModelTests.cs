using System;
using System.Collections.Generic;
using MusicMp3Downloader.App.Models;
using MusicMp3Downloader.App.Tests.Fakes;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Tests;

public class PlayerViewModelTests
{
    private static List<TrackViewModel> Queue(int count)
    {
        var list = new List<TrackViewModel>();
        for (var i = 1; i <= count; i++)
        {
            list.Add(new TrackViewModel(
                new Track { FilePath = $"/music/{i}.mp3", Title = $"Track {i}" }, i));
        }

        return list;
    }

    [Fact]
    public void Play_sets_current_and_marks_track_playing()
    {
        var fake = new FakeAudioPlayer();
        var vm = new PlayerViewModel(fake, new ImmediateUiDispatcher());
        var tracks = Queue(3);
        vm.SetQueue(tracks);

        vm.Play(tracks[1]);

        Assert.Same(tracks[1], vm.Current);
        Assert.True(tracks[1].IsPlaying);
        Assert.Equal("/music/2.mp3", Assert.Single(fake.Played));
    }

    [Fact]
    public void Next_advances_and_wraps_to_start()
    {
        var vm = new PlayerViewModel(new FakeAudioPlayer(), new ImmediateUiDispatcher());
        var tracks = Queue(3);
        vm.SetQueue(tracks);
        vm.Play(tracks[0]);

        vm.NextCommand.Execute(null);
        Assert.Same(tracks[1], vm.Current);

        vm.Play(tracks[2]);
        vm.NextCommand.Execute(null);
        Assert.Same(tracks[0], vm.Current);
        Assert.False(tracks[2].IsPlaying);
    }

    [Fact]
    public void Previous_restarts_track_when_past_three_seconds()
    {
        var fake = new FakeAudioPlayer();
        var vm = new PlayerViewModel(fake, new ImmediateUiDispatcher());
        var tracks = Queue(3);
        vm.SetQueue(tracks);
        vm.Play(tracks[1]);
        fake.Position = TimeSpan.FromSeconds(10);

        vm.PreviousCommand.Execute(null);

        Assert.Same(tracks[1], vm.Current);
        Assert.Equal(TimeSpan.Zero, fake.LastSeek);
    }

    [Fact]
    public void Previous_goes_to_previous_track_near_the_start()
    {
        var vm = new PlayerViewModel(new FakeAudioPlayer(), new ImmediateUiDispatcher());
        var tracks = Queue(3);
        vm.SetQueue(tracks);
        vm.Play(tracks[1]);

        vm.PreviousCommand.Execute(null);

        Assert.Same(tracks[0], vm.Current);
    }

    [Fact]
    public void PlayPause_toggles_pause_then_resume()
    {
        var fake = new FakeAudioPlayer();
        var vm = new PlayerViewModel(fake, new ImmediateUiDispatcher());
        var tracks = Queue(2);
        vm.SetQueue(tracks);
        vm.Play(tracks[0]);

        vm.PlayPauseCommand.Execute(null);
        Assert.False(vm.IsPlaying);
        Assert.Equal(1, fake.PauseCount);

        vm.PlayPauseCommand.Execute(null);
        Assert.True(vm.IsPlaying);
        Assert.Equal(1, fake.ResumeCount);
    }

    [Fact]
    public void PlayPause_with_nothing_playing_starts_first_queued_track()
    {
        var vm = new PlayerViewModel(new FakeAudioPlayer(), new ImmediateUiDispatcher());
        var tracks = Queue(2);
        vm.SetQueue(tracks);

        vm.PlayPauseCommand.Execute(null);

        Assert.Same(tracks[0], vm.Current);
    }

    [Fact]
    public void Seek_clamps_fraction_and_mirrors_progress()
    {
        var fake = new FakeAudioPlayer { Duration = TimeSpan.FromSeconds(200) };
        var vm = new PlayerViewModel(fake, new ImmediateUiDispatcher());

        vm.SeekCommand.Execute(1.5);

        Assert.Equal(1d, vm.Progress);
        Assert.Equal(TimeSpan.FromSeconds(200), fake.LastSeek);
    }

    [Fact]
    public void NowPlaying_title_and_subtitle_reflect_the_current_track()
    {
        var vm = new PlayerViewModel(new FakeAudioPlayer(), new ImmediateUiDispatcher());
        var tracks = new List<TrackViewModel>
        {
            new(new Track { FilePath = "/m/1.mp3", Title = "Hello", Artist = "Adele", Year = 2015 }, 1),
        };
        vm.SetQueue(tracks);
        vm.Play(tracks[0]);

        Assert.Equal("Hello", vm.NowPlayingTitle);
        Assert.Equal("Adele · MP3", vm.NowPlayingSubtitle);
    }
}
