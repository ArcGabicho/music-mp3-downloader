using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MusicMp3Downloader.App.Data;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.Services;

public sealed partial class DownloadService : IDownloadService
{
    // Intentos totales ante fallos transitorios (red, YouTube cambiando su API, etc.).
    private const int MaxAttempts = 3;

    private readonly IMusicLibrary _musicLibrary;
    private readonly IExternalTools _tools;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DownloadService(
        IMusicLibrary musicLibrary,
        IExternalTools tools,
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _musicLibrary = musicLibrary;
        _tools = tools;
        _dbFactory = dbFactory;
    }

    public async Task<DownloadItem> DownloadAsync(
        string url,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var outputDirectory = _musicLibrary.GetMusicDirectory();
        Directory.CreateDirectory(outputDirectory);

        var item = new DownloadItem
        {
            Url = url,
            Status = DownloadStatus.Downloading,
        };

        var arguments = BuildArguments(url, outputDirectory);
        var updated = false;
        string? filePath = null;

        for (var attempt = 1; ; attempt++)
        {
            var startedAt = DateTime.UtcNow;
            var result = await RunYtDlpAsync(
                _tools.YtDlpPath,
                arguments,
                line => filePath = HandleOutputLine(line, progress) ?? filePath,
                cancellationToken);

            if (result.ExitCode == 0)
            {
                // Reserva: si la ruta impresa no coincide con un archivo real, se busca
                // el MP3 recién escrito en la carpeta de destino.
                if (filePath is null || !File.Exists(filePath))
                {
                    filePath = FindNewestMp3(outputDirectory, startedAt);
                }

                if (filePath is not null)
                {
                    break;
                }
            }

            if (YtDlpErrors.IsPermanent(result.Error) || attempt >= MaxAttempts)
            {
                throw new InvalidOperationException(
                    result.ExitCode == 0
                        ? "yt-dlp terminó sin producir ningún archivo MP3."
                        : YtDlpErrors.Describe(result.Error));
            }

            // YouTube rompe las versiones viejas de yt-dlp a menudo: tras el primer fallo
            // se actualiza el binario empaquetado antes de reintentar.
            if (!updated && _tools.YtDlpIsBundled)
            {
                updated = true;
                await TryUpdateYtDlpAsync(cancellationToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }

            progress?.Report(0d);
        }

        progress?.Report(1d);

        item.OutputPath = filePath;
        item.Title = Path.GetFileNameWithoutExtension(filePath);
        item.FileSizeBytes = new FileInfo(filePath).Length;
        item.Status = DownloadStatus.Completed;

        ReadEmbeddedTags(item, filePath);
        await PersistAsync(item, cancellationToken);

        return item;
    }

    private List<string> BuildArguments(string url, string outputDirectory)
    {
        var arguments = new List<string>
        {
            // Ignora cualquier yt-dlp.conf del usuario que pueda cambiar el comportamiento.
            "--ignore-config",
            // Sin esto, en Windows yt-dlp escribe en la página de códigos del sistema
            // (cp1252) y omite los caracteres que no caben: la ruta impresa no coincide
            // con el archivo real cuando el título tiene japonés, emojis, etc.
            "--encoding", "utf-8",
            "--no-playlist",
            "--extract-audio",
            "--audio-format", "mp3",
            "--audio-quality", "0",
            "--embed-metadata",
            "--retries", "10",
            "--fragment-retries", "10",
            "--extractor-retries", "3",
            "--socket-timeout", "30",
            "--trim-filenames", "150",
            "--no-quiet",
            "--newline",
            "--progress",
            "--print", "after_move:filepath",
            "--output", Path.Combine(outputDirectory, "%(title)s.%(ext)s"),
        };

        if (OperatingSystem.IsWindows())
        {
            arguments.Add("--windows-filenames");
        }

        // Usa el FFmpeg empaquetado si está disponible, en vez del del sistema.
        if (_tools.FfmpegDirectory is { } ffmpegDirectory)
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(ffmpegDirectory);
        }

        // Sin intérprete de JavaScript, YouTube oculta formatos o bloquea el video entero.
        if (_tools.DenoPath is { } denoPath)
        {
            arguments.Add("--js-runtimes");
            arguments.Add($"deno:{denoPath}");
        }

        // "--" evita que una URL que empiece por "-" se interprete como opción.
        arguments.Add("--");
        arguments.Add(url);
        return arguments;
    }

    private static string? HandleOutputLine(string line, IProgress<double>? progress)
    {
        var match = ProgressRegex().Match(line);
        if (match.Success &&
            double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var percent))
        {
            progress?.Report(Math.Clamp(percent / 100d, 0d, 1d));
            return null;
        }

        var trimmed = line.Trim();
        return Path.IsPathRooted(trimmed) && trimmed.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : null;
    }

    private static string? FindNewestMp3(string directory, DateTime sinceUtc)
    {
        try
        {
            return new DirectoryInfo(directory)
                .EnumerateFiles("*.mp3")
                .Where(f => f.LastWriteTimeUtc >= sinceUtc.AddSeconds(-5))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Select(f => f.FullName)
                .FirstOrDefault();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task TryUpdateYtDlpAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            await RunYtDlpAsync(_tools.YtDlpPath, ["--update"], _ => { }, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // La actualización tardó demasiado: se reintenta con la versión actual.
        }
        catch (InvalidOperationException)
        {
            // Sin permisos de escritura o sin red: se reintenta con la versión actual.
        }
    }

    private async Task PersistAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        db.Downloads.Add(new DownloadRecord
        {
            Url = item.Url,
            Title = item.Title,
            Artist = item.Artist,
            OutputPath = item.OutputPath,
            FileSizeBytes = item.FileSizeBytes,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private void ReadEmbeddedTags(DownloadItem item, string filePath)
    {
        try
        {
            using var file = TagLib.File.Create(filePath);
            if (!string.IsNullOrWhiteSpace(file.Tag.Title))
            {
                item.Title = file.Tag.Title;
            }

            item.Artist = file.Tag.FirstPerformer;
        }
        catch (Exception)
        {
            // Los metadatos son opcionales; si no se pueden leer, se conserva el título del archivo.
        }
    }

    private static async Task<YtDlpResult> RunYtDlpAsync(
        string ytDlpPath,
        IReadOnlyList<string> arguments,
        Action<string> onOutputLine,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(ytDlpPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // yt-dlp es Python: fuerza UTF-8 también en sus propios mensajes de error.
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                "No se encontró yt-dlp. El binario se descarga durante la compilación; " +
                "ejecuta 'dotnet build' o 'src/Tools/fetch-tools.ps1 -Rid win-x64'.", ex);
        }

        var errorBuffer = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (errorBuffer)
                {
                    errorBuffer.AppendLine(e.Data);
                }
            }
        };
        process.BeginErrorReadLine();

        try
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                onOutputLine(line);
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Sin esto yt-dlp (y el ffmpeg que lanza) seguirían corriendo en segundo plano.
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Ya había terminado.
            }

            throw;
        }

        lock (errorBuffer)
        {
            return new YtDlpResult(process.ExitCode, errorBuffer.ToString());
        }
    }

    [GeneratedRegex(@"\[download\]\s+([0-9.]+)%")]
    private static partial Regex ProgressRegex();
}
