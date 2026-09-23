using MusicMp3Downloader.App.Services;

namespace MusicMp3Downloader.App.Tests;

public class YtDlpErrorsTests
{
    [Fact]
    public void IsPermanent_EsVerdaderoParaVideoPrivado()
    {
        const string stderr = "ERROR: [youtube] abc123: Private video. Sign in if you've been granted access";

        Assert.True(YtDlpErrors.IsPermanent(stderr));
        Assert.Equal("El video es privado.", YtDlpErrors.Describe(stderr));
    }

    [Fact]
    public void IsPermanent_EsFalsoParaErroresDeRed()
    {
        const string stderr = "ERROR: Unable to download webpage: <urlopen error [Errno 11001] getaddrinfo failed>";

        Assert.False(YtDlpErrors.IsPermanent(stderr));
    }

    [Fact]
    public void Describe_DevuelveLaUltimaLineaDeErrorSinElPrefijo()
    {
        const string stderr =
            "WARNING: [youtube] No supported JavaScript runtime could be found.\n" +
            "ERROR: primer error\n" +
            "ERROR: [youtube] abc123: Requested format is not available\n";

        Assert.Equal("[youtube] abc123: Requested format is not available", YtDlpErrors.Describe(stderr));
    }

    [Fact]
    public void Describe_SinLineaDeErrorDevuelveLaUltimaLinea()
    {
        Assert.Equal("algo salió mal", YtDlpErrors.Describe("WARNING: aviso\nalgo salió mal\n"));
    }

    [Fact]
    public void Describe_ConSalidaVaciaDevuelveMensajeGenerico()
    {
        Assert.Equal("yt-dlp falló sin dar detalles.", YtDlpErrors.Describe(string.Empty));
    }
}
