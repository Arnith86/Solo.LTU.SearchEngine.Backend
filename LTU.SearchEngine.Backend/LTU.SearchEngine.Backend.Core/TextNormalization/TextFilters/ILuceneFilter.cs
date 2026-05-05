namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Defines a contract for linguistic text analysis using Lucene-based analyzers.
/// This filter is responsible for language-specific transformations such as 
/// tokenization (splitting words), stemming, and stop-word removal.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ITextFilter{T}"/> with a return type of <see cref="IEnumerable{String}"/>,
/// allowing a single input term to be "exploded" into multiple linguistic tokens 
/// (e.g., "Lars-Åke" becoming ["lars", "åke"]).
/// </remarks>
public interface ILuceneFilter : ITextFilter<IEnumerable<string>>
{
}
