using System.Text.Json.Serialization;
using AstroLab.Api;
using AstroLab.Api.Features.Archives;
using AstroLab.Api.Features.Catalogues;
using AstroLab.Api.Features.Fits;
using AstroLab.Api.Features.Images;
using AstroLab.Api.Features.Spectroscopy;
using AstroLab.Api.Features.TimeSeries;
using AstroLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAstroLabInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapFitsEndpoints();
app.MapImagesEndpoints();
app.MapSpectroscopyEndpoints();
app.MapArchivesEndpoints();
app.MapTimeSeriesEndpoints();
app.MapCataloguesEndpoints();

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can bootstrap the API in-process for integration tests.</summary>
public partial class Program;
