namespace MusicMp3Downloader.App.Services;

public interface IAudioTagger
{
    void ApplyTags(string filePath, string? title, string? artist, string? album);
}