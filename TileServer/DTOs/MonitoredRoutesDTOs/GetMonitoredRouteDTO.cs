namespace TileServer.DTOs.MonitoredRoutesDTOs;

public class GetMonitoredRouteDTO
{
    public  Guid Id { get; set; }
    public string Name { get; set; }
    public double OriginLatitude { get; set; }
    public double OriginLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime NextRunAtUtc { get; set; }
}