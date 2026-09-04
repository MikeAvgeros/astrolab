using System.Collections.Immutable;

namespace AstroLab.Api.Features.Images.Stack;

public sealed record ImageStackResponse
{
    private ImageStackResponse(ImmutableList<string> sourceFileIds, StackCombinationMethod method, string resultFileId)
    {
        SourceFileIds = sourceFileIds;
        Method = method;
        ResultFileId = resultFileId;
    }

    public ImmutableList<string> SourceFileIds { get; }

    public StackCombinationMethod Method { get; }

    public string ResultFileId { get; }

    public static ImageStackResponse Create(ImmutableList<string> sourceFileIds, StackCombinationMethod method, string resultFileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultFileId);

        return new ImageStackResponse(sourceFileIds, method, resultFileId);
    }
}
