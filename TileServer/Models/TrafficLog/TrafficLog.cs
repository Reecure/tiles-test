namespace TileServer.Models.TrafficLog;

public class TrafficLog
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; } 
    
    public DateTime MeasuredAtUtc { get; set; }
    
    public int TravelTimeSeconds { get; set; }
    public int FreeFlowSeconds { get; set; }
    public int TrafficDelaySeconds { get; set; }
    
    public MonitoredRoute.MonitoredRoute Route { get; set; }
    public List<LogPoint> Points { get; set; } = new();
}

public class LogPoint
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Order { get; set; }
    
    public Guid TrafficLogId { get; set; }
    public TrafficLog TrafficLog { get; set; }
}

public class TomTomResponse
{
    public Route[] Routes { get; set; }
}

public class Route
{
    public Summary Summary { get; set; }
    public Leg[] Legs { get; set; }
}

public class Summary
{
    public int TravelTimeInSeconds { get; set; }
    public int TrafficDelayInSeconds { get; set; }
    public int LengthInMeters { get; set; }
}

public class Leg
{
    public Point[] Points { get; set; }
}

public class Point
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}