namespace MusicMp3Downloader.App.Services;

public sealed class TagLibAudioTagger : IAudioTagger
{
    public void ApplyTags(string filePath, string? title, string? artist, string? album)
    {
        using var file = TagLib.File.Create(filePath);

        if (title is not null)
        {
            file.Tag.Title = title;
        }

        if (artist is not null)
        {
            file.Tag.Performers = new[] { artist };
        }

        if (album is not null)
        {
            file.Tag.Album = album;
        }

        file.Save();
    }
}