using LTU.SearchEngine.Backend.Core.Model;

namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Structured representation of a parsed search query.
/// Produced by QueryParser (UC-3001) and consumed by search/index components.
/// </summary>
public sealed class ParsedQuery
{
    /// <summary>
    /// How terms should be combined (AND / OR).
    /// Default is OR (FRQ-3005).
    /// </summary>
    public QueryMode Mode { get; init; } = QueryMode.OR;

    /// <summary>
    /// Normal search terms (FRQ-3001, FRQ-3002).
    /// </summary>
    public List<string> Terms { get; } = new();

    /// <summary>
    /// Required terms prefixed with '+' (FRQ-3007).
    /// </summary>
    public List<string> RequiredTerms { get; } = new();

    /// <summary>
    /// Excluded terms prefixed with '-' or NOT (FRQ-3008).
    /// </summary>
    public List<string> ExcludedTerms { get; } = new();

    /// <summary>
    /// Phrase queries enclosed in double quotes (FRQ-3003).
    /// </summary>
    public List<string> Phrases { get; } = new();

    /// <summary>
    /// Validation or parsing errors (e.g. standalone exclusion).
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Indicates whether the query contains any errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
