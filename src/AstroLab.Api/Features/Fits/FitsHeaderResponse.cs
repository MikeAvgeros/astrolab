using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits;

public sealed record FitsHeaderResponse(string FileId, IReadOnlyList<FitsKeywordDto> Keywords)
{
    public static FitsHeaderResponse FromHeader(string fileId, FitsHeader header)
    {
        var keywords = new List<FitsKeywordDto>(header.Count);
        
        foreach (var keyword in header)
        {
            keywords.Add(new FitsKeywordDto(keyword.Name, keyword.Value.ToString(), keyword.Comment));
        }

        return new FitsHeaderResponse(fileId, keywords);
    }
}
