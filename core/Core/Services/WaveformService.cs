using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MusicMp3Downloader.App.Data;

namespace MusicMp3Downloader.App.Services;

/// <summary>Genera una forma de onda real (picos de amplitud) a partir del audio del
/// propio MP3, decodificando con el FFmpeg empaquetado. El resultado se cachea en SQLite
/// por ruta de archivo (invalidado si el tamaño del archivo cambia) para no volver a
/// decodificar cada vez que se reproduce la misma pista.</summary>
public sealed class WaveformService : IWaveformService
{
    private const int PeakCount = 80;
    private const int SampleRateHz = 8000;

    private readonly IExternalTools _tools;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public WaveformService(IExternalTools tools, IDbContextFactory<AppDbContext> dbFactory)
    {
        _tools = tools;
        _dbFactory = dbFactory;
    }

    public async Task<float[]> GetPeaksAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPeaksCoreAsync(filePath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Best-effort: si falla el caché en SQLite o FFmpeg (archivo dañado, binario
            // ausente), se muestra una forma de onda plana en vez de romper la reproducción.
            return Enumerable.Repeat(0.15f, PeakCount).ToArray();
        }
    }

    private async Task<float[]> GetPeaksCoreAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileSizeBytes = new FileInfo(filePath).Length;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCacheTableAsync(db, cancellationToken);

        var cached = await db.WaveformPeaks.FindAsync([filePath], cancellationToken);
        if (cached is not null && cached.FileSizeBytes == fileSizeBytes)
        {
            return ParsePeaks(cached.PeaksCsv);
        }

        var peaks = await DecodePeaksAsync(filePath, cancellationToken);

        var peaksCsv = string.Join(',', peaks.Select(p => p.ToString("F3", CultureInfo.InvariantCulture)));
        if (cached is null)
        {
            db.WaveformPeaks.Add(new WaveformCacheEntry
            {
                FilePath = filePath,
                FileSizeBytes = fileSizeBytes,
                PeaksCsv = peaksCsv,
                ComputedAtUtc = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            cached.FileSizeBytes = fileSizeBytes;
            cached.PeaksCsv = peaksCsv;
            cached.ComputedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return peaks;
    }

    private async Task<float[]> DecodePeaksAsync(string filePath, CancellationToken cancellationToken)
    {
        var ffmpegName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var ffmpegPath = _tools.FfmpegDirectory is { } dir ? Path.Combine(dir, ffmpegName) : ffmpegName;

        var startInfo = new ProcessStartInfo(ffmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("quiet");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(filePath);
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("s16le");
        startInfo.ArgumentList.Add("-ac");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("-ar");
        startInfo.ArgumentList.Add(SampleRateHz.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-");

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException("No se encontró FFmpeg.", ex);
        }

        process.ErrorDataReceived += static (_, _) => { };
        process.BeginErrorReadLine();

        using var pcm = new MemoryStream();
        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(pcm, cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        await copyTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"FFmpeg terminó con código {process.ExitCode}.");
        }

        return ComputePeaks(pcm.GetBuffer(), (int)pcm.Length);
    }

    private static float[] ComputePeaks(byte[] pcm, int length)
    {
        var sampleCount = length / 2;
        var peaks = new float[PeakCount];
        if (sampleCount <= 0)
        {
            return peaks;
        }

        var samplesPerBucket = Math.Max(1, sampleCount / PeakCount);
        for (var bucket = 0; bucket < PeakCount; bucket++)
        {
            var start = bucket * samplesPerBucket;
            if (start >= sampleCount)
            {
                break;
            }

            var end = Math.Min(start + samplesPerBucket, sampleCount);
            short peak = 0;
            for (var i = start; i < end; i++)
            {
                var sample = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8));
                var abs = Math.Abs((int)sample);
                if (abs > peak)
                {
                    peak = (short)abs;
                }
            }

            peaks[bucket] = peak / 32768f;
        }

        return peaks;
    }

    private static float[] ParsePeaks(string csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return Enumerable.Repeat(0.15f, PeakCount).ToArray();
        }

        return csv
            .Split(',')
            .Select(p => float.Parse(p, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static async Task EnsureCacheTableAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        // EnsureCreated no agrega tablas nuevas a una base de datos que ya existía antes
        // de que WaveformCache se declarara (solo crea el esquema completo si la base de
        // datos no existe todavía); este CREATE TABLE IF NOT EXISTS cubre ese caso.
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "WaveformPeaks" (
                "FilePath" TEXT NOT NULL CONSTRAINT "PK_WaveformPeaks" PRIMARY KEY,
                "FileSizeBytes" INTEGER NOT NULL,
                "PeaksCsv" TEXT NOT NULL,
                "ComputedAtUtc" TEXT NOT NULL
            )
            """,
            cancellationToken);
    }
}
