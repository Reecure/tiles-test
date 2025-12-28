using Microsoft.AspNetCore.Mvc;
using TileServer.Services.TilesServices;

namespace TileServer.Controllers;

[ApiController]
[Route("tiles")]
public class TilesController : ControllerBase
{
    private readonly ITileService _tileService;
    private readonly ILogger<TilesController> _logger;

    public TilesController(ITileService tileService, ILogger<TilesController> logger)
    {
        _tileService = tileService;
        _logger = logger;
    }
    
    [HttpGet("{z:int}/{x:int}/{y:int}.pbf")]
    public async Task<IActionResult> GetTile(int z, int x, int y)
    {
        var tileData = await _tileService.GetTileAsync(z, x, y);

        if (tileData == null)
        {
            return NotFound();
        }
        
        Response.Headers.Append("Content-Encoding", "gzip");

        return File(
            tileData, 
            contentType: "application/vnd.mapbox-vector-tile",
            enableRangeProcessing: false
        );
    }
}