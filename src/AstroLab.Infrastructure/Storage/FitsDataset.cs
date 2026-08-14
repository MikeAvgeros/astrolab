using AstroLab.Core.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>A fully-loaded primary-HDU image: its descriptor and physical (BZERO/BSCALE-applied) pixel values.</summary>
public readonly record struct FitsDataset(HduDescriptor Hdu, FitsImageDescriptor Image, float[] Pixels);
