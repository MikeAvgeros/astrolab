using System.Text.Json.Serialization;
using AstroLab.Api.Features.Fits;
using AstroLab.Api.Features.Imaging;
using AstroLab.Api.Features.Observations;
using AstroLab.Api.Features.Photometry;
using AstroLab.Api.Features.Spectroscopy;
using AstroLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAstroLabInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapFitsEndpoints();
app.MapImagingEndpoints();
app.MapPhotometryEndpoints();
app.MapSpectroscopyEndpoints();
app.MapObservationsEndpoints();

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can bootstrap the API in-process for integration tests.</summary>
public partial class Program;
