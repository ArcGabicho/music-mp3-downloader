using System;
using System.IO;

namespace MusicMp3Downloader.App.Services;

/// <inheritdoc />
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

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(home, "Music");
        }

        // Linux: respetar las carpetas XDG (localizadas: Música, Musique, Musik…).
        var fromEnv = Environment.GetEnvironmentVariable("XDG_MUSIC_DIR");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return Expand(fromEnv, home);
        }

        var fromConfig = ReadXdgUserDirs(home);
        if (fromConfig is not null)
        {
            return fromConfig;
        }

        // Sin configuración XDG: preferir una carpeta «Música»/«Music» ya existente.
        foreach (var name in new[] { "Música", "Music" })
        {
            var candidate = Path.Combine(home, name);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(home, "Music");
    }

    /// <summary>Lee <c>XDG_MUSIC_DIR</c> de <c>~/.config/user-dirs.dirs</c>.</summary>
    private static string? ReadXdgUserDirs(string home)
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
        {
            configHome = Path.Combine(home, ".config");
        }

        var file = Path.Combine(configHome, "user-dirs.dirs");
        if (!File.Exists(file))
        {
            return null;
        }

        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw.Trim();
            if (line.StartsWith('#') || !line.StartsWith("XDG_MUSIC_DIR=", StringComparison.Ordinal))
            {
                continue;
            }

            var value = line["XDG_MUSIC_DIR=".Length..].Trim().Trim('"');
            return value.Length == 0 ? null : Expand(value, home);
        }

        return null;
    }

    private static string Expand(string value, string home)
    {
        if (value == "$HOME")
        {
            return home;
        }

        if (value.StartsWith("$HOME/", StringComparison.Ordinal))
        {
            return Path.Combine(home, value["$HOME/".Length..]);
        }

        return value;
    }
}
