using Microsoft.EntityFrameworkCore;
using AscensoresIruna.Api.Models;

namespace AscensoresIruna.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Elevator> Elevators => Set<Elevator>();
    public DbSet<StatusReport> StatusReports => Set<StatusReport>();
    public DbSet<ReporterIp> ReporterIps => Set<ReporterIp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Elevator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Location).IsRequired();
        });

        modelBuilder.Entity<StatusReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Elevator)
                .WithMany(e => e.StatusReports)
                .HasForeignKey(e => e.ElevatorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.IpAddressHash).IsRequired();
            entity.HasIndex(e => new { e.IpAddressHash, e.ElevatorId, e.ReportedAt });
            entity.HasIndex(e => new { e.ElevatorId, e.ReportedAt });
        });

        modelBuilder.Entity<ReporterIp>(entity =>
        {
            entity.HasKey(e => e.IpAddressHash);
            entity.Property(e => e.IpAddressHash).IsRequired();
        });
    }
}