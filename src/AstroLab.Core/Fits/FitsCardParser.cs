using System.Globalization;
using System.Text;
using AstroLab.Core.Result;

namespace AstroLab.Core.Fits;

/// <summary>
/// Pure parser for individual 80-column FITS header cards, per the FITS 4.0 standard
/// (Section 4.1). Decoding the raw header block into card-sized spans is an Infrastructure
/// concern; this class only ever operates on character data already in memory.
/// </summary>
public static class FitsCardParser
{
    public const int CardLength = 80;
    private const int KeywordLength = 8;
    private const int ValueIndicatorColumn = 8;

    /// <summary>Parses a single 80-character FITS header card.</summary>
    public static Result<FitsKeyword> Parse(ReadOnlySpan<char> card)
    {
        if (card.Length != CardLength)
        {
            return Error.Validation(
                "fits.header.invalid_card_length",
                $"FITS header card must be exactly {CardLength} characters, but was {card.Length}.");
        }

        var name = card[..KeywordLength].ToString().TrimEnd().ToUpperInvariant();

        var rest = card[KeywordLength..];

        if (name.Length == 0 || name is "COMMENT" or "HISTORY")
        {
            var text = rest.ToString().TrimEnd();

            return FitsKeywordFactory.Create(name, FitsValue.None, text.Length == 0 ? null : text);
        }

        if (name == "END")
        {
            return FitsKeywordFactory.Create(name, FitsValue.None, null);
        }

        var hasValueIndicator = card[ValueIndicatorColumn] == '=' && card[ValueIndicatorColumn + 1] == ' ';

        if (!hasValueIndicator)
        {
            var text = rest.ToString().TrimEnd();

            return FitsKeywordFactory.Create(name, FitsValue.None, text.Length == 0 ? null : text);
        }

        var valueField = card[(ValueIndicatorColumn + 2)..];

        var (value, comment) = ParseValueAndComment(valueField);

        return FitsKeywordFactory.Create(name, value, comment);
    }

    private static (FitsValue Value, string? Comment) ParseValueAndComment(ReadOnlySpan<char> field)
    {
        var index = 0;

        while (index < field.Length && field[index] == ' ')
        {
            index++;
        }

        if (index >= field.Length)
        {
            return (FitsValue.None, null);
        }

        if (field[index] == '\'')
        {
            index++;

            var builder = new StringBuilder();

            while (index < field.Length)
            {
                if (field[index] == '\'')
                {
                    if (index + 1 < field.Length && field[index + 1] == '\'')
                    {
                        builder.Append('\'');

                        index += 2;

                        continue;
                    }

                    index++;

                    break;
                }

                builder.Append(field[index]);

                index++;
            }

            var stringValue = builder.ToString().TrimEnd();

            var comment = ExtractComment(field[index..]);

            return (FitsValue.OfString(stringValue), comment);
        }
        else
        {
            var slashIndex = field[index..].IndexOf('/');

            var tokenEnd = slashIndex < 0 ? field.Length : index + slashIndex;

            var token = field[index..tokenEnd].ToString().Trim();

            var comment = ExtractComment(slashIndex < 0 ? ReadOnlySpan<char>.Empty : field[tokenEnd..]);

            return (ParseScalarToken(token), comment);
        }
    }

    private static string? ExtractComment(ReadOnlySpan<char> remainder)
    {
        var slashIndex = remainder.IndexOf('/');

        if (slashIndex < 0)
        {
            return null;
        }

        var comment = remainder[(slashIndex + 1)..].ToString().Trim();

        return comment.Length == 0 ? null : comment;
    }

    private static FitsValue ParseScalarToken(string token)
    {
        if (token.Length == 0)
        {
            return FitsValue.None;
        }

        if (token is "T")
        {
            return FitsValue.OfLogical(true);
        }

        if (token is "F")
        {
            return FitsValue.OfLogical(false);
        }

        if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return FitsValue.OfInteger(integer);
        }

        var normalized = token.Replace('D', 'E').Replace('d', 'e');

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var real))
        {
            return FitsValue.OfReal(real);
        }

        return FitsValue.OfUndefined(token);
    }
}
