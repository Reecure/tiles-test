using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TileServer.Models;
using TileServer.Models.MonitoredRoute;
using TileServer.Models.TrafficLog;

namespace TileServer.Workers;

public class TrafficWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrafficWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private const int ScanIntervalMinutes = 5;

    public TrafficWorker(
        IServiceProvider serviceProvider,
        ILogger<TrafficWorker> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrafficWorker running at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var routes = await context.MonitoredRoutes
                        .Where(r => r.IsActive)
                        .ToListAsync(stoppingToken);
                    
                    

                    if (routes.Count == 0)
                    {
                        _logger.LogInformation($"There are no active monitored routes.");
                    }
                    else
                    {
                        _logger.LogInformation($"Found {routes.Count} active routes to scan.");
                        
                        var httpClient = _httpClientFactory.CreateClient();
                        var apiKey = _configuration["TomTom:ApiKey"];
                        var baseUrl = _configuration["TomTom:BaseUrl"];

                        foreach (var route in routes)
                        {
                            await ProcessRouteAsync(route, context, httpClient, baseUrl, apiKey, stoppingToken);
                        
                            await Task.Delay(500, stoppingToken);
                        }
                    
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("All routes processed and saved.");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Critical error in TrafficWorker cycle.");
            }
            
            _logger.LogInformation("Worker sleeping for ${ScanIntervalMinutes} minutes...");
            await Task.Delay(TimeSpan.FromMinutes(ScanIntervalMinutes), stoppingToken);
        }
    }
    
    private async Task ProcessRouteAsync(
        MonitoredRoute route, 
        AppDbContext context, 
        HttpClient httpClient, 
        string baseUrl, 
        string apiKey,
        CancellationToken token)
    {
        try
        {
            var routePath = $"{route.OriginLatitude},{route.OriginLongitude}:{route.DestinationLatitude},{route.DestinationLongitude}";
            
            var url = $"{baseUrl}{routePath}/json?key={apiKey}&traffic=true&routeType=fastest&travelMode=car";

            var response = await httpClient.GetAsync(url, token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"TomTom error for route {route.Name}: {response.StatusCode}");
                return;
            }

            var json = await response.Content.ReadAsStringAsync(token);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<TomTomResponse>(json, options);

            if (data?.Routes == null || data.Routes.Length == 0) return;

            var result = data.Routes[0];
            var summary = result.Summary;
            
            var log = new TrafficLog
            {
                Id = Guid.NewGuid(),
                RouteId = route.Id,
                MeasuredAtUtc = DateTime.UtcNow,
                TravelTimeSeconds = summary.TravelTimeInSeconds,
                TrafficDelaySeconds = summary.TrafficDelayInSeconds,
                FreeFlowSeconds = summary.TravelTimeInSeconds - summary.TrafficDelayInSeconds,
                Points = new List<LogPoint>()
            };
            
            if (result.Legs?.Length > 0)
            {
                var points = result.Legs[0].Points;
                for (int i = 0; i < points.Length; i += 5) 
                {
                    log.Points.Add(new LogPoint
                    {
                        Latitude = points[i].Latitude,
                        Longitude = points[i].Longitude,
                        Order = i
                    });
                }
            }
            
            context.TrafficLogs.Add(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing route {route.Name}");
        }
    }
}