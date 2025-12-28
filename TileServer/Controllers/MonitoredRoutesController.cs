using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TileServer.DTOs.MonitoredRoutesDTOs;
using TileServer.Models;
using TileServer.Models.MonitoredRoute;

namespace TileServer.Controllers;

[ApiController]
[Route("[controller]")]
public class MonitoredRoutesController : ControllerBase
{
    private readonly ILogger<MonitoredRoutesController> _logger;
    private readonly AppDbContext _context;

    public MonitoredRoutesController(ILogger<MonitoredRoutesController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonitoredRoutes()
    {
        var routes = await _context.MonitoredRoutes.ToListAsync();
        
        return Ok(routes);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMonitoredRoute(CreateMonitoredRouteDTO route)
    {
        var newRoute = new MonitoredRoute
        {
            Name = route.Name,
            OriginLatitude = route.OriginLatitude,
            OriginLongitude = route.OriginLongitude,
            DestinationLatitude = route.DestinationLatitude,
            DestinationLongitude = route.DestinationLongitude,
            IsActive = route.IsActive,
            StartTime = route.StartTime,
            EndTime = route.EndTime,
        };

        await _context.MonitoredRoutes.AddAsync(newRoute);
        
        await _context.SaveChangesAsync();
        
        return Ok(newRoute);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMonitoredRoute(Guid id)
    {
        var route = await _context.MonitoredRoutes.FindAsync(id);

        if (route == null)
        {
            return NotFound();
        }
        
        _context.MonitoredRoutes.Remove(route);
        
        await _context.SaveChangesAsync(); 

        return NoContent();
    }
}