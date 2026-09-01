using System.IO;
using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.Tests;

public class MusicLibraryTests
{
    [Fact]
    public void ExpandHome_replaces_home_token()
    {
        Assert.Equal("/home/u", MusicLibrary.ExpandHome("$HOME", "/home/u"));
        Assert.Equal(Path.Combine("/home/u", "Música"), MusicLibrary.ExpandHome("$HOME/Música", "/home/u"));
        Assert.Equal("/mnt/media/music", MusicLibrary.ExpandHome("/mnt/media/music", "/home/u"));
    }

    [Fact]
    public void ParseXdgUserDirs_reads_localized_music_dir()
    {
        const string content = """
            # This file is written by xdg-user-dirs-update
            XDG_DESKTOP_DIR="$HOME/Escritorio"
            XDG_MUSIC_DIR="$HOME/Música"
            XDG_VIDEOS_DIR="$HOME/Vídeos"
            """;

        Assert.Equal(Path.Combine("/home/u", "Música"), MusicLibrary.ParseXdgUserDirs(content, "/home/u"));
    }

    [Fact]
    public void ParseXdgUserDirs_ignores_commented_entry()
    {
        Assert.Null(MusicLibrary.ParseXdgUserDirs(
            "# XDG_MUSIC_DIR=\"$HOME/x\"\nXDG_DESKTOP_DIR=\"$HOME/D\"", "/home/u"));
    }

    [Fact]
    public void ParseXdgUserDirs_returns_null_for_empty_value()
    {
        Assert.Null(MusicLibrary.ParseXdgUserDirs("XDG_MUSIC_DIR=\"\"", "/home/u"));
    }

    [Fact]
    public void ParseXdgUserDirs_keeps_absolute_path()
    {
        Assert.Equal("/data/music", MusicLibrary.ParseXdgUserDirs("XDG_MUSIC_DIR=\"/data/music\"", "/home/u"));
    }
}
