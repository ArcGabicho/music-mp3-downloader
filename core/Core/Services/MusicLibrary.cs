using System;
using System.IO;

namespace MusicMp3Downloader.App.Services;

public sealed class MusicLibrary : IMusicLibrary
{
    public string GetMusicDirectory()
    {
        var directory = Resolve();
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Resolve()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
        {
            // Devuelve la carpeta «Música»/«Music» localizada por Windows.
            var known = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            return string.IsNullOrEmpty(known) ? Path.Combine(home, "Music") : known;
        }

        // macOS y Mac Catalyst comparten la misma carpeta de música del usuario.
        return Path.Combine(home, "Music");
    }
}
