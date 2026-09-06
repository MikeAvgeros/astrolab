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

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<HduDescriptor> IEnumerable<HduDescriptor>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<HduDescriptor>
    {
        private readonly HduLocationDescriptorView _view;
        private int _index;

        internal Enumerator(HduLocationDescriptorView view)
        {
            _view = view;
            _index = -1;
        }

        public HduDescriptor Current => _view[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _view.Count;

        public void Reset() => _index = -1;

        public void Dispose()
        {
        }
    }
}
