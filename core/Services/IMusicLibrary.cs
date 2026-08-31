namespace MusicMp3Downloader.App.Services;

/// <summary>Resuelve la carpeta de música del usuario según el sistema operativo.</summary>
public interface IMusicLibrary
{
    /// <summary>
    /// Devuelve la carpeta donde se guardan los MP3 descargados, creándola si no existe.
    /// Es la carpeta «Música»/«Music» del usuario en Windows, macOS y Linux (respetando
    /// la localización configurada, p. ej. <c>~/Música</c> en un sistema en español).
    /// </summary>
    string GetMusicDirectory();
}
