using Microsoft.Data.Sqlite;

namespace TileServer.Services.TilesServices;

public class TileService : ITileService
{
    private readonly string _connectionString;
    
    public TileService(IConfiguration configuration)
    {
        var basePath = AppContext.BaseDirectory;
        var dbPath = Path.Combine(basePath, "data", "ukraine.mbtiles");
        
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? $"Data Source={dbPath};Mode=ReadOnly";
    }

    public async Task<byte[]?> GetTileAsync(int z, int x, int y)
    {
        var tmsY = (1 << z) - 1 - y;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level = $z AND tile_column = $x AND tile_row = $y";
        
        command.Parameters.AddWithValue("$z", z);
        command.Parameters.AddWithValue("$x", x);
        command.Parameters.AddWithValue("$y", tmsY);

        var result = await command.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return (byte[])result;
    }
}