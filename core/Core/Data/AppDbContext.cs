using Microsoft.EntityFrameworkCore;

namespace MusicMp3Downloader.App.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DownloadRecord> Downloads => Set<DownloadRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DownloadRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}