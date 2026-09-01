using MusicMp3Downloader.App.Models;
using MusicMp3Downloader.App.ViewModels;

namespace MusicMp3Downloader.App.Tests;

public class DownloadItemViewModelTests
{
    [Fact]
    public void New_item_starts_queued_with_url_as_title()
    {
        var item = new DownloadItemViewModel("https://youtu.be/abc");

        Assert.Equal("https://youtu.be/abc", item.Title);
        Assert.Equal(DownloadStatus.Queued, item.Status);
        Assert.Equal(0d, item.Progress);
        Assert.Null(item.ErrorMessage);
    }

    [Fact]
    public void Apply_copies_result_and_completes_progress()
    {
        var item = new DownloadItemViewModel("https://youtu.be/abc");

        item.Apply(new DownloadItem
        {
            Url = "https://youtu.be/abc",
            Title = "Great Song",
            Status = DownloadStatus.Completed,
        });

        Assert.Equal("Great Song", item.Title);
        Assert.Equal(DownloadStatus.Completed, item.Status);
        Assert.Equal(1d, item.Progress);
    }

    [Fact]
    public void Fail_sets_failed_status_and_message()
    {
        var item = new DownloadItemViewModel("https://youtu.be/abc");

        item.Fail("network down");

        Assert.Equal(DownloadStatus.Failed, item.Status);
        Assert.Equal("network down", item.ErrorMessage);
    }
}
