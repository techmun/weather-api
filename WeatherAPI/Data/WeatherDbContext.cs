using Microsoft.EntityFrameworkCore;
using WeatherAPI.Models;

namespace WeatherAPI.Data;

public sealed class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : DbContext(options)
{
    public DbSet<ApiUser> ApiUsers => Set<ApiUser>();

    public DbSet<WeatherStation> WeatherStations => Set<WeatherStation>();

    public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherStation>(entity =>
        {
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<WeatherReading>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.StationId, x.Metric, x.TimestampUtc }).IsUnique();
            entity.HasOne(x => x.Station)
                .WithMany(x => x.Readings)
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(200);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
        });
    }
}
