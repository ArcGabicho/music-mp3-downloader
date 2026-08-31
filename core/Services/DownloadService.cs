using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MusicMp3Downloader.App.Data;
using MusicMp3Downloader.App.Models;

namespace MusicMp3Downloader.App.Services;

/// <summary>
/// Orquesta la descarga: yt-dlp descarga el vídeo, extrae el audio y lo convierte a MP3
/// dentro de la carpeta de música del usuario; después se guardan solo los metadatos de
/// la descarga en la base de datos SQLite (el archivo MP3 nunca se guarda en la base).
/// </summary>
public sealed partial class DownloadService : IDownloadService
{
    private readonly IMusicLibrary _musicLibrary;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DownloadService(
        IMusicLibrary musicLibrary,
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _musicLibrary = musicLibrary;
        _dbFactory = dbFactory;
    }

    public async Task<DownloadItem> DownloadAsync(
        string url,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var outputDirectory = _musicLibrary.GetMusicDirectory();
        var outputTemplate = Path.Combine(outputDirectory, "%(title)s.%(ext)s");

        var item = new DownloadItem
        {
            Url = url,
            Status = DownloadStatus.Downloading,
        };

        string[] arguments =
        [
            "--no-playlist",
            "--extract-audio",
            "--audio-format", "mp3",
            "--audio-quality", "0",
            "--embed-metadata",
            "--no-quiet",
            "--newline",
            "--progress",
            "--print", "after_move:filepath",
            "--output", outputTemplate,
            url,
        ];

        string? filePath = null;
        await foreach (var line in RunYtDlpAsync(arguments, cancellationToken))
        {
            var match = ProgressRegex().Match(line);
            if (match.Success &&
                double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var percent))
            {
                progress?.Report(Math.Clamp(percent / 100d, 0d, 1d));
                continue;
            }

            var trimmed = line.Trim();
            if (Path.IsPathRooted(trimmed) && trimmed.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                filePath = trimmed;
            }
        }

        if (filePath is null || !File.Exists(filePath))
        {
            throw new InvalidOperationException("yt-dlp no produjo ningún archivo MP3.");
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

    /// <summary>Guarda solo los metadatos de la descarga en SQLite.</summary>
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

    private static async IAsyncEnumerable<string> RunYtDlpAsync(
        IReadOnlyList<string> arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("yt-dlp")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

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
                "No se encontró 'yt-dlp'. Instálalo y asegúrate de que está en el PATH.", ex);
        }

        var errorBuffer = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errorBuffer.AppendLine(e.Data);
            }
        };
        process.BeginErrorReadLine();

        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            yield return line;
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"yt-dlp terminó con código {process.ExitCode}.{Environment.NewLine}{errorBuffer}".Trim());
        }
    }

    [GeneratedRegex(@"\[download\]\s+([0-9.]+)%")]
    private static partial Regex ProgressRegex();
}
