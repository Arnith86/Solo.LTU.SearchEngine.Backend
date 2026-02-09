namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Represents a term extracted from a document together with its source.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexedTerm"/> is a value object used during indexing to describe <br />
/// a single searchable term and the context in which it was found, such as <br />
/// page content, title, or headings.
/// </para>
/// </remarks>
/// <param name="Term">The textual representation of the indexed term.</param>
/// <param name="Source">
/// Indicates where in the document the term was extracted from, which may be used <br />
/// to influence term weighting during indexing and ranking.
/// </param>
public sealed record IndexedTerm(
	string Term,
	TermSource Source
);