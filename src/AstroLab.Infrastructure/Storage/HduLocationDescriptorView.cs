using System.Collections;
using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Infrastructure.Storage;

/// <summary>
/// A zero-copy <see cref="IReadOnlyList{HduDescriptor}"/> view over an
/// <see cref="ImmutableArray{HduLocation}"/>, so <c>FitsDatasetReader</c> can hand its already-read
/// HDU layout straight to <see cref="FitsDatasetClassifier"/> without allocating and copying a
/// separate <see cref="ImmutableArray{HduDescriptor}"/> just to satisfy its signature.
/// </summary>
public readonly struct HduLocationDescriptorView : IReadOnlyList<HduDescriptor>
{
    private readonly ImmutableArray<HduLocation> _locations;

    public HduLocationDescriptorView(ImmutableArray<HduLocation> locations)
    {
        _locations = locations;
    }

    public int Count => _locations.Length;

    public HduDescriptor this[int index] => _locations[index].Descriptor;

    public IEnumerator<HduDescriptor> GetEnumerator()
    {
        return _locations.Select(location => location.Descriptor).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
