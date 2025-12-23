using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

var mbtilesPath = "data/ukraine.mbtiles";
var connectionString = $"Data Source={mbtilesPath};Mode=ReadOnly;";

app.MapGet("/tiles/{z}/{x}/{y}.pbf", async (int z, int x, int y) =>
{
    var tmsY = (1 << z) - 1 - y;

    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level = $z AND tile_column = $x AND tile_row = $y";
    command.Parameters.AddWithValue("$z", z);
    command.Parameters.AddWithValue("$x", x);
    command.Parameters.AddWithValue("$y", tmsY);

    var result = await command.ExecuteScalarAsync();

    if (result == null || result == DBNull.Value)
    {
        return Results.NotFound();
    }

    var tileData = (byte[])result;
    
    return Results.File(tileData, 
        contentType: "application/vnd.mapbox-vector-tile", 
        enableRangeProcessing: false,
        fileDownloadName: null,
        lastModified: null,
        entityTag: null);
})
.AddEndpointFilter(async (context, next) =>
{
    var result = await next(context);
    context.HttpContext.Response.Headers.Append("Content-Encoding", "gzip");
    return result;
});

app.UseStaticFiles(); 

app.Run();