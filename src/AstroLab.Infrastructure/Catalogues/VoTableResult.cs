namespace AstroLab.Infrastructure.Catalogues;

/// <summary>The columns and row cells of a single VOTable <c>TABLE</c> element, as plain strings (no type coercion).</summary>
internal sealed record VoTableResult(IReadOnlyList<string> FieldNames, IReadOnlyList<IReadOnlyList<string?>> Rows);
