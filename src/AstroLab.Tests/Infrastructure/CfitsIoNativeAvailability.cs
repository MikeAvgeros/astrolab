using System.Runtime.InteropServices;

namespace AstroLab.Tests.Infrastructure;

/// <summary>
/// Every CFITSIO-backed test needs a real <c>cfitsio</c> shared library on the machine running the
/// tests. It is installed via apt in the Docker/CI image (see spec.md §5.5) but is not present on
/// every developer machine, so tests that need it check <see cref="IsAvailable"/> and dynamically
/// skip (via <see cref="Assert.Skip"/>) rather than fail when it is missing.
/// </summary>
/// <remarks>
/// Unlike an actual <c>[LibraryImport("cfitsio")]</c> P/Invoke call — which the CLR resolves with
/// OS-specific decoration, trying <c>libcfitsio.so</c> on Linux when the bare name doesn't resolve
/// directly — <see cref="NativeLibrary.TryLoad(string, out nint)"/> does not apply that decoration
/// itself, so probing only the bare name here would report unavailable on Linux even when the
/// library is installed and the real P/Invoke bindings resolve it just fine.
/// </remarks>
internal static class CfitsIoNativeAvailability
{
    public static bool IsAvailable { get; } =
        NativeLibrary.TryLoad("cfitsio", out _) || NativeLibrary.TryLoad("libcfitsio.so", out _);
}
