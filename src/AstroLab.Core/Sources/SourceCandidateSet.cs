using System.Collections.Immutable;

namespace AstroLab.Core.Sources;

/// <summary>
/// The raw region-detection output shared by <see cref="SourceDetector"/> and
/// <see cref="SourceShapeAnalyzer"/>: the flood-filled candidates (already filtered by minimum
/// area and trimmed to the requested maximum), together with the whole-image background and noise
/// (sigma) estimate used to threshold them.
/// </summary>
internal readonly record struct SourceCandidateSet(ImmutableArray<SourceCandidate> Candidates, double Background, double Sigma);
