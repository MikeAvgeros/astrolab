using System.Collections;
using System.Text;
using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

/// <summary>
/// An immutable, ordered collection of parsed FITS header cards for a single HDU.
/// </summary>
public sealed class FitsHeader : IReadOnlyList<FitsKeyword>
{
    private readonly FitsKeyword[] _keywords;
    private readonly Dictionary<string, int> _index;

    private FitsHeader(FitsKeyword[] keywords)
    {
        _keywords = keywords;
        _index = new Dictionary<string, int>(keywords.Length, StringComparer.Ordinal);

        for (var i = 0; i < keywords.Length; i++)
        {
            _index.TryAdd(keywords[i].Name, i);
        }
    }

    public int Count => _keywords.Length;

    public FitsKeyword this[int index] => _keywords[index];

    public IEnumerator<FitsKeyword> GetEnumerator() => ((IEnumerable<FitsKeyword>)_keywords).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetValue(string keyword, out FitsValue value)
    {
        if (_index.TryGetValue(keyword, out var i))
        {
            value = _keywords[i].Value;

            return true;
        }

        value = FitsValue.None;

        return false;
    }

    public Result<FitsValue> Get(string keyword) => TryGetValue(keyword, out var value)
        ? value
        : Error.NotFound("fits.header.keyword_missing", $"Header keyword '{keyword}' was not found.");

    public Result<long> GetInteger(string keyword) => Get(keyword).Bind(v => v.Kind == FitsValueKind.Integer
        ? Result<long>.Success(v.AsInteger)
        : Error.Validation("fits.header.keyword_wrong_type", $"Header keyword '{keyword}' is not an integer."));

    public Result<double> GetReal(string keyword) => Get(keyword).Bind(v => v.Kind is FitsValueKind.Real or FitsValueKind.Integer
        ? Result<double>.Success(v.AsReal)
        : Error.Validation("fits.header.keyword_wrong_type", $"Header keyword '{keyword}' is not numeric."));

    public Result<string> GetString(string keyword) => Get(keyword).Bind(v => v.Kind == FitsValueKind.String
        ? Result<string>.Success(v.AsString)
        : Error.Validation("fits.header.keyword_wrong_type", $"Header keyword '{keyword}' is not a string."));

    public Result<bool> GetLogical(string keyword) => Get(keyword).Bind(v => v.Kind == FitsValueKind.Logical
        ? Result<bool>.Success(v.AsLogical)
        : Error.Validation("fits.header.keyword_wrong_type", $"Header keyword '{keyword}' is not logical."));

    public static Result<FitsHeader> Parse(ReadOnlySpan<byte> headerBlock)
    {
        if (headerBlock.Length % FitsCardParser.CardLength != 0)
        {
            return Error.Validation(
                "fits.header.misaligned_block",
                $"Header block length ({headerBlock.Length}) is not a multiple of {FitsCardParser.CardLength}.");

        }

        var cardCount = headerBlock.Length / FitsCardParser.CardLength;

        var keywords = new List<FitsKeyword>(cardCount);

        Span<char> cardChars = stackalloc char[FitsCardParser.CardLength];

        for (var i = 0; i < cardCount; i++)
        {
            var cardBytes = headerBlock.Slice(i * FitsCardParser.CardLength, FitsCardParser.CardLength);

            // Encoding.ASCII.GetChars silently replaces bytes outside 7-bit ASCII with '?' instead
            // of failing, which would otherwise mask a corrupted/non-ASCII header as a valid card.
            if (cardBytes.IndexOfAnyInRange((byte)0x80, (byte)0xFF) >= 0)
            {
                return Error.Validation(
                    "fits.header.invalid_ascii",
                    $"Header card {i} contains a byte outside the 7-bit ASCII range required by the FITS standard.");
            }

            Encoding.ASCII.GetChars(cardBytes, cardChars);

            var parsed = FitsCardParser.Parse(cardChars);

            if (parsed.IsFailure)
            {
                return Result<FitsHeader>.Failure(parsed.Error);

            }

            var keyword = parsed.Value;

            if (keyword.Name == "CONTINUE" && keywords.Count > 0 && TryMergeContinuation(keywords[^1], keyword, out var merged))
            {
                keywords[^1] = merged;
            }
            else
            {
                keywords.Add(keyword);
            }

            if (keyword.Name == "END")
            {
                return new FitsHeader([.. keywords]);
            }
        }

        return Error.Validation("fits.header.missing_end", "Header block did not contain a terminating END card.");
    }
    
    private static bool TryMergeContinuation(FitsKeyword previous, FitsKeyword continuation, out FitsKeyword merged)
    {
        merged = previous;

        if (previous.Value.Kind != FitsValueKind.String || continuation.Value.Kind != FitsValueKind.String)
        {
            return false;
        }

        var previousString = previous.Value.AsString;

        if (!previousString.EndsWith('&'))
        {
            return false;
        }

        var combined = previousString[..^1] + continuation.Value.AsString;

        merged = FitsKeyword.Create(previous.Name, FitsValue.OfString(combined), continuation.Comment ?? previous.Comment);

        return true;
    }
}
