using Microsoft.EntityFrameworkCore;

namespace TileServer.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }
    
    public DbSet<MonitoredRoute.MonitoredRoute> MonitoredRoutes { get; set; }
    public DbSet<TrafficLog.TrafficLog> TrafficLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        modelBuilder.Entity<TrafficLog.TrafficLog>()
            .HasOne(tl => tl.Route)
            .WithMany()
            .HasForeignKey(tl => tl.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<TrafficLog.LogPoint>()
            .HasOne(lp => lp.TrafficLog)
            .WithMany(tl => tl.Points)
            .HasForeignKey(lp => lp.TrafficLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}