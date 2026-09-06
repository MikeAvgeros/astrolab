using System.Text;
using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.Fits;

/// <summary>
/// Translates a cfitsio numeric <c>status</c> code into a <see cref="Result{TValue}"/>-friendly
/// <see cref="Error"/>, combining <c>ffgerr</c>'s short fixed description of the status code with
/// whatever more specific messages cfitsio pushed onto its own internal error stack (via
/// <c>ffgmsg</c>), then clearing that stack so it cannot leak into an unrelated later call.
/// </summary>
internal static class CfitsIoErrorMapper
{
    // FLEN_STATUS and FLEN_ERRMSG from cfitsio's fitsio.h, each including the terminating NUL.
    private const int StatusMessageLength = 31;
    private const int ErrorMessageLength = 81;

    // cfitsio caps its internal error stack at 25 entries (fitscore.c); this bounds the drain loop
    // rather than relying on ffgmsg's own termination to avoid ever looping indefinitely.
    private const int MaxStackedMessages = 25;

    public static Error ToError(string code, int status)
    {
        Span<byte> statusBuffer = stackalloc byte[StatusMessageLength];

        NativeMethods.GetErrorStatus(status, statusBuffer);

        var statusMessage = ToTrimmedAscii(statusBuffer);

        var detail = DrainMessageStack();

        var message = detail.Length == 0
            ? $"CFITSIO error {status}: {statusMessage}"
            : $"CFITSIO error {status}: {statusMessage} ({detail})";

        return Error.Infrastructure(code, message);
    }

    private static string DrainMessageStack()
    {
        Span<byte> buffer = stackalloc byte[ErrorMessageLength];

        var messages = new List<string>();

        for (var i = 0; i < MaxStackedMessages; i++)
        {
            buffer.Clear();

            if (NativeMethods.PopErrorMessage(buffer) == 0)
            {
                break;
            }

            var text = ToTrimmedAscii(buffer);

            if (text.Length > 0)
            {
                messages.Add(text);
            }
        }

        NativeMethods.ClearErrorMessages();

        return string.Join("; ", messages);
    }

    private static string ToTrimmedAscii(ReadOnlySpan<byte> buffer)
    {
        var nullIndex = buffer.IndexOf((byte)0);

        var slice = nullIndex >= 0 ? buffer[..nullIndex] : buffer;

        return Encoding.ASCII.GetString(slice).Trim();
    }
}
