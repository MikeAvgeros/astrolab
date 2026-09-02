using AstroLab.Api.Features.Fits.Inspect;
using AstroLab.Api.Features.Fits.Upload;

namespace AstroLab.Api.Features.Fits;

public static class FitsEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapFitsEndpoints()
        {
            var group = app.MapGroup("/api/fits").WithTags("Fits");

            group.MapUploadEndpoint();

            group.MapInspectEndpoint();
        }
    }
}
