namespace AstroLab.Infrastructure.CFITSIO;

/// <summary>The <c>iomode</c> argument accepted by <c>ffopen</c> (<c>fits_open_file</c>).</summary>
public enum CfitsIoMode
{
    ReadOnly = 0,
    ReadWrite = 1,
}
