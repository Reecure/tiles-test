using System.Globalization;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;
using CsvHelper.Configuration.Attributes;

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

app.MapGet("/get-accidents", async (DateTime start, DateTime end) =>
{
    var csvPath = Path.Combine(AppContext.BaseDirectory, "data", "ukraine_accidents.csv");
    
    using var reader = new StreamReader(csvPath);
    using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
    });
    
    var records = csv.GetRecords<AccidentRecord>()
        .Where(r => r.OccurredAt >= start && r.OccurredAt <= end)
        .ToList();

    var features = records.Select(r => new GeoJsonFeature
    {
        Geometry = new PointGeometry
        {
            Coordinates = new double[] { r.Longitude, r.Latitude }
        },
        Properties = new
        {
            r.Id,
            r.District,
            r.Region,
            r.Severity,
            r.AccidentType,
            r.Casualties,
            r.VehiclesInvolved,
            r.Weather,
            r.Source,
            OccurredAt = r.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss")
        }
    }).ToList();

    var geoJson = new
    {
        type = "FeatureCollection",
        features
    };

    return Results.Json(geoJson);
});

app.UseStaticFiles(); 

app.Run();

public class AccidentRecord
{
    [Name("id")]
    public int Id { get; set; }

    [Name("occurred_at")]
    public DateTime OccurredAt { get; set; }

    [Name("district")]
    public string District { get; set; }

    [Name("region")]
    public string Region { get; set; }

    [Name("severity")]
    public int Severity { get; set; }

    [Name("latitude")]
    public double Latitude { get; set; }

    [Name("longitude")]
    public double Longitude { get; set; }

    [Name("accident_type")]
    public string AccidentType { get; set; }

    [Name("casualties")]
    public int Casualties { get; set; }

    [Name("vehicles_involved")]
    public int VehiclesInvolved { get; set; }

    [Name("weather")]
    public string Weather { get; set; }

    [Name("source")]
    public string Source { get; set; }
}

public class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Feature";

    [JsonPropertyName("geometry")]
    public object Geometry { get; set; }

    [JsonPropertyName("properties")]
    public object Properties { get; set; }
}

public class PointGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Point";

    [JsonPropertyName("coordinates")]
    public double[] Coordinates { get; set; }
}