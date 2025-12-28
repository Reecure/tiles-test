namespace TileServer.Services.TilesServices;

public interface ITileService
{
    Task<byte[]?> GetTileAsync(int z, int x, int y);
}