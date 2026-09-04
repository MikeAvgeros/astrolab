using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Images.Stack;

public sealed record ImageStackRequest
{
    private const int MinimumFileCount = 2;

    [JsonConstructor]
    private ImageStackRequest(ImmutableList<string> fileIds, StackCombinationMethod method = StackCombinationMethod.Mean)
    {
        FileIds = fileIds;
        Method = method;
    }

    public ImmutableList<string> FileIds { get; }

    public StackCombinationMethod Method { get; }

    public static ImageStackRequest Create(ImmutableList<string> fileIds, StackCombinationMethod method = StackCombinationMethod.Mean)
    {
        var request = new ImageStackRequest(fileIds, method);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(FileIds);

        if (FileIds.Count < MinimumFileCount)
        {
            throw new ArgumentOutOfRangeException(nameof(FileIds), FileIds.Count, $"At least {MinimumFileCount} staged images are required to stack.");
        }
    }
}
