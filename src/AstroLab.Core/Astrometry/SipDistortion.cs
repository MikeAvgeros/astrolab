using System.Collections.Immutable;
using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Simple Imaging Polynomial (SIP) focal-plane distortion (Shupe et al. 2005), as carried by
/// <c>RA---TAN-SIP</c>/<c>DEC--TAN-SIP</c> headers from HST, Spitzer, ZTF and astrometry.net. The forward
/// polynomials <c>A_p_q</c>/<c>B_p_q</c> map pixel offsets (u, v) from CRPIX to distortion-corrected
/// offsets (u + f(u, v), v + g(u, v)) before the CD matrix is applied. The inverse starts from the
/// optional <c>AP_p_q</c>/<c>BP_p_q</c> polynomials when present (otherwise from the distorted offsets
/// themselves) and is refined by fixed-point iteration of the forward polynomials, so that
/// world-to-pixel and pixel-to-world round-trip to within <see cref="InversionTolerancePixels"/>
/// rather than only to the accuracy of the fitted inverse coefficients.
/// </summary>
public sealed class SipDistortion
{
    private const int MaxOrder = 9;
    private const int MaxInversionIterations = 50;
    private const double InversionTolerancePixels = 1e-9;

    private readonly ImmutableArray<(int P, int Q, double Coefficient)> _a;
    private readonly ImmutableArray<(int P, int Q, double Coefficient)> _b;
    private readonly ImmutableArray<(int P, int Q, double Coefficient)> _ap;
    private readonly ImmutableArray<(int P, int Q, double Coefficient)> _bp;

    private SipDistortion(
        ImmutableArray<(int P, int Q, double Coefficient)> a,
        ImmutableArray<(int P, int Q, double Coefficient)> b,
        ImmutableArray<(int P, int Q, double Coefficient)> ap,
        ImmutableArray<(int P, int Q, double Coefficient)> bp)
    {
        _a = a;
        _b = b;
        _ap = ap;
        _bp = bp;
    }

    public static Result<SipDistortion> FromHeader(FitsHeader header)
    {
        var forwardA = ReadPolynomial(header, "A");

        if (forwardA.IsFailure)
        {
            return Result<SipDistortion>.Failure(forwardA.Error);
        }

        var forwardB = ReadPolynomial(header, "B");

        if (forwardB.IsFailure)
        {
            return Result<SipDistortion>.Failure(forwardB.Error);
        }

        var inverseA = ReadOptionalPolynomial(header, "AP");

        if (inverseA.IsFailure)
        {
            return Result<SipDistortion>.Failure(inverseA.Error);
        }

        var inverseB = ReadOptionalPolynomial(header, "BP");

        if (inverseB.IsFailure)
        {
            return Result<SipDistortion>.Failure(inverseB.Error);
        }

        return new SipDistortion(forwardA.Value, forwardB.Value, inverseA.Value, inverseB.Value);
    }

    public (double U, double V) Distort(double u, double v) => (u + Evaluate(_a, u, v), v + Evaluate(_b, u, v));

    public Result<(double U, double V)> Undistort(double distortedU, double distortedV)
    {
        var u = distortedU + Evaluate(_ap, distortedU, distortedV);

        var v = distortedV + Evaluate(_bp, distortedU, distortedV);

        for (var iteration = 0; iteration < MaxInversionIterations; iteration++)
        {
            var nextU = distortedU - Evaluate(_a, u, v);

            var nextV = distortedV - Evaluate(_b, u, v);

            var change = Math.Abs(nextU - u) + Math.Abs(nextV - v);

            u = nextU;

            v = nextV;

            if (change < InversionTolerancePixels)
            {
                return (u, v);
            }
        }

        return Error.Validation(
            "astrometry.sip_inversion_failed",
            "The SIP distortion could not be inverted at this position; it lies outside the region where the distortion polynomial is valid.");
    }

    private static Result<ImmutableArray<(int P, int Q, double Coefficient)>> ReadPolynomial(FitsHeader header, string prefix)
    {
        var orderResult = header.GetInteger($"{prefix}_ORDER");

        if (orderResult.IsFailure)
        {
            return Error.Validation(
                "astrometry.invalid_sip", $"A -SIP projection requires {prefix}_ORDER, which is missing or not an integer.");
        }

        return ReadCoefficients(header, prefix, orderResult.Value);
    }

    private static Result<ImmutableArray<(int P, int Q, double Coefficient)>> ReadOptionalPolynomial(FitsHeader header, string prefix)
    {
        var orderResult = header.GetInteger($"{prefix}_ORDER");

        return orderResult.IsSuccess
            ? ReadCoefficients(header, prefix, orderResult.Value)
            : ImmutableArray<(int P, int Q, double Coefficient)>.Empty;
    }

    private static Result<ImmutableArray<(int P, int Q, double Coefficient)>> ReadCoefficients(FitsHeader header, string prefix, long order)
    {
        if (order is < 0 or > MaxOrder)
        {
            return Error.Validation("astrometry.invalid_sip", $"{prefix}_ORDER must be between 0 and {MaxOrder}, was {order}.");
        }

        var builder = ImmutableArray.CreateBuilder<(int P, int Q, double Coefficient)>();

        for (var p = 0; p <= order; p++)
        {
            for (var q = 0; p + q <= order; q++)
            {
                var coefficientResult = header.GetReal($"{prefix}_{p}_{q}");

                if (coefficientResult.IsFailure && coefficientResult.Error.Category != ErrorCategory.NotFound)
                {
                    return Error.Validation(
                        "astrometry.invalid_sip", $"SIP coefficient {prefix}_{p}_{q} is present but is not a real number.");
                }

                if (coefficientResult.IsSuccess && coefficientResult.Value != 0.0)
                {
                    builder.Add((p, q, coefficientResult.Value));
                }
            }
        }

        return builder.ToImmutable();
    }

    private static double Evaluate(ImmutableArray<(int P, int Q, double Coefficient)> terms, double u, double v)
    {
        var sum = 0.0;

        foreach (var (p, q, coefficient) in terms)
        {
            sum += coefficient * Math.Pow(u, p) * Math.Pow(v, q);
        }

        return sum;
    }
}
