using Microsoft.EntityFrameworkCore;

namespace MusicMp3Downloader.App.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DownloadRecord> Downloads => Set<DownloadRecord>();

    public DbSet<WaveformCacheEntry> WaveformPeaks => Set<WaveformCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DownloadRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<WaveformCacheEntry>(entity =>
        {
            entity.HasKey(e => e.FilePath);
        });
    }
}