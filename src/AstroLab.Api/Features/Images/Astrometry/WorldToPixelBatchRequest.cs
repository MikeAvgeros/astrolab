using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.Astrometry;

public sealed record WorldToPixelBatchRequest
{
    private const int MaxPoints = 100_000;

    [JsonConstructor]
    private WorldToPixelBatchRequest(IReadOnlyList<WorldPointDto> points)
    {
        Points = points;
    }

    public IReadOnlyList<WorldPointDto> Points { get; }

    public static WorldToPixelBatchRequest Create(IReadOnlyList<WorldPointDto> points)
    {
        var request = new WorldToPixelBatchRequest(points);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Points);

        if (Points.Count == 0)
        {
            throw new ArgumentException("At least one world coordinate must be supplied.", nameof(Points));
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(Points.Count, MaxPoints, nameof(Points));

        if (Points.Any(point => point is null))
        {
            throw new ArgumentException("Every world coordinate entry must be non-null.", nameof(Points));
        }
    }
}
