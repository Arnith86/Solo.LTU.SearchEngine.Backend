using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// A specialized normalizer used during the query parsing phase to standardize 
/// extracted tokens. Unlike the core TextNormalizer, this implementation focuses 
/// on preserving operator syntax while lowercasing searchable content.
/// </summary>
public class PhraseAndTermNormalizer : ITextNormalizer<ExtractedQueryToken, string>
{
    /// <summary>
    /// Normalizes an <see cref="ExtractedQueryToken"/> into a standardized string.
    /// </summary>
    /// <param name="token">The token extracted from the raw query string.</param>
    /// <param name="languageCode">The language context (unused in this simplified implementation).</param>
    /// <returns>
    /// The standardized token string. Operators are returned as-is; 
    /// terms and phrases are converted to lowercase.
    /// </returns>
    public string Normalize(ExtractedQueryToken token, string languageCode)
    {
        if (token == null || token.Token == null)
        {
            return string.Empty;
        }

        if (token.TokenType == QueryTokenType.LogicalOperator ||
            token.TokenType == QueryTokenType.GroupingOperator)
        {
            return token.Token;
        }
        return token.Token.ToLower();
    }
}
