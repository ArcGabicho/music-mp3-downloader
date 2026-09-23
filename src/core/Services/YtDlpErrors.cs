using System;
using System.Linq;

namespace MusicMp3Downloader.App.Services;

/// <summary>
/// Interpreta la salida de error de yt-dlp: decide si merece la pena reintentar y
/// traduce el fallo a un mensaje legible en vez de volcar todo el stderr.
/// </summary>
internal static class YtDlpErrors
{
    // Fallos que no se arreglan reintentando ni actualizando yt-dlp.
    private static readonly (string Marker, string Message)[] PermanentErrors =
    [
        ("is not a valid URL", "El enlace no es válido."),
        ("Unsupported URL", "El enlace no es de un sitio compatible."),
        ("Private video", "El video es privado."),
        ("Video unavailable", "El video no está disponible."),
        ("This video has been removed", "El video fue eliminado."),
        ("members-only", "El video es exclusivo para miembros del canal."),
        ("Sign in to confirm your age", "El video tiene restricción de edad y requiere iniciar sesión."),
        ("not available in your country", "El video no está disponible en tu país."),
        ("blocked it in your country", "El video no está disponible en tu país."),
    ];

    public static bool IsPermanent(string stderr) =>
        PermanentErrors.Any(e => stderr.Contains(e.Marker, StringComparison.OrdinalIgnoreCase));

    public static string Describe(string stderr)
    {
        foreach (var (marker, message) in PermanentErrors)
        {
            if (stderr.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return message;
            }
        }

        var lines = stderr.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var error = lines.LastOrDefault(l => l.StartsWith("ERROR:", StringComparison.Ordinal));
        if (error is not null)
        {
            return error["ERROR:".Length..].Trim();
        }

        return lines.LastOrDefault() ?? "yt-dlp falló sin dar detalles.";
    }
}
