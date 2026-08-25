using AstroLab.Api.Features.Fits.Inspect;
using AstroLab.Api.Features.Fits.Upload;

namespace AstroLab.Api.Features.Fits;

/// <summary>
/// "What is this file?" — staging and understanding user-supplied FITS files: uploading raw
/// bytes to local storage (Upload) and inspecting HDU/header metadata (Inspect).
/// </summary>
public static class FitsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapFitsEndpoints()
        {
            var group = app.MapGroup("/api/fits").WithTags("Fits");

            group.MapUploadEndpoint();

            group.MapInspectEndpoint();

            return group;
        }
    }
}
