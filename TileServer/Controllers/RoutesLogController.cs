using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TileServer.Models;

namespace TileServer.Controllers;

[ApiController]
[Route("[controller]")]
public class RoutesLogController : ControllerBase
{
    private readonly ILogger<MonitoredRoutesController> _logger;
    private readonly AppDbContext _context;

    public RoutesLogController(ILogger<MonitoredRoutesController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonitoredRoutesLogs()
    {
        var logs = await _context.TrafficLogs.ToListAsync();
        return Ok(logs);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMonitoredRoutesLogs(Guid id)
    {
        var routeLog = await _context.TrafficLogs
            .Where(r => r.RouteId == id)
            .Select(log => new
            {
                log.Id,
                log.RouteId,
                log.FreeFlowSeconds,
                log.TrafficDelaySeconds,
                log.Points,
                log.MeasuredAtUtc,
                log.TravelTimeSeconds,
            })
            .ToListAsync();
        
        return Ok(routeLog);
    }
}