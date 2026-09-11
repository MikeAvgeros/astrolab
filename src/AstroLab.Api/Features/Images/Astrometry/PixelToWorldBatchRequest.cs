using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record PixelToWorldBatchRequest
{
    [JsonConstructor]
    private PixelToWorldBatchRequest(IReadOnlyList<PixelPointDto> points)
    {
        Points = points;
    }

    public IReadOnlyList<PixelPointDto> Points { get; }

    public static PixelToWorldBatchRequest Create(IReadOnlyList<PixelPointDto> points)
    {
        var request = new PixelToWorldBatchRequest(points);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Points);

        if (Points.Count == 0)
        {
            throw new ArgumentException("At least one pixel coordinate must be supplied.", nameof(Points));
        }
    }
}
