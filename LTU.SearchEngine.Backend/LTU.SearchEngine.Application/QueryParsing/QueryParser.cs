using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;
using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Parses raw user search query strings into a structured <see cref="ParsedQuery"/>.
/// Covers FRQ-3001..FRQ-3008:
/// - Terms, phrases ("...")
/// - AND/OR operators (case-sensitive; uppercase only)
/// - Required terms (+)
/// - Exclusion (- / NOT) with constraint: must be preceded by a positive term
/// </summary>
public sealed class QueryParser : IQueryParser
{
    private readonly IQueryNormalizer _queryNormalizer;
    private readonly ITokenizer _queryTokenizer;

    public QueryParser(
        IQueryNormalizer queryNormalizer,
        ITokenizer queryTokenizer
        )
    {
        _queryNormalizer = queryNormalizer ?? 
            throw new ArgumentNullException(nameof(queryNormalizer));
        _queryTokenizer = queryTokenizer ??
			throw new ArgumentNullException(nameof(queryTokenizer));
	}

    public ParsedQuery Parse(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            var empty = new ParsedQuery();
            empty.Errors.Add("Query is empty.");  // ToDo: Ask why this step is necessary. 
            return empty;
        }

        var tokens = _queryTokenizer.Tokenize(rawQuery);
        var mode = DetectMode(tokens);

        var parsedQuery = new ParsedQuery { Mode = mode };

        bool sawPositive = false;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // Skip explicit AND/OR tokens
            if (IsOperatorToken(token))
                continue;

            // NOT operator excludes the next term/phrase (FRQ-3008)
            if (token == "NOT")
            {
                if (i + 1 >= tokens.Count)
                {
                    parsedQuery.Errors.Add("NOT must be followed by a term or phrase.");
                    break;
                }

                var next = tokens[++i];
                AddExcluded(parsedQuery, next, ref sawPositive);
                continue;
            }

            // Required term (+) (FRQ-3007)
            if (token.StartsWith("+", StringComparison.Ordinal) && token.Length > 1)
            {
                var value = _queryNormalizer.NormalizeTerm(token[1..]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    parsedQuery.RequiredTerms.Add(value);
                    sawPositive = true;
                }
                continue;
            }

            // Excluded term (-) (FRQ-3008)
            if (token.StartsWith("-", StringComparison.Ordinal) && token.Length > 1)
            {
                AddExcluded(parsedQuery, token[1..], ref sawPositive);
                continue;
            }

            // Phrase (FRQ-3003)
            if (IsPhraseToken(token))
            {
                var phrase = _queryNormalizer.NormalizePhrase(token);
                if (!string.IsNullOrWhiteSpace(phrase))
                {
                    parsedQuery.Phrases.Add(phrase);
                    sawPositive = true;
                }
                continue;
            }

            // Normal term (FRQ-3001/3002)
            var term = _queryNormalizer.NormalizeTerm(token);
            if (!string.IsNullOrWhiteSpace(term))
            {
                parsedQuery.Terms.Add(term);
                sawPositive = true;
            }
        }

        // FRQ-3008 constraint: standalone exclusion queries should return error or zero results
        if (!sawPositive && parsedQuery.ExcludedTerms.Count > 0)
            parsedQuery.Errors.Add("Standalone exclusion queries are not allowed (must include a positive term).");

        return parsedQuery;
    }

    // --- Helpers ---

    private void AddExcluded(ParsedQuery parsedQuery, string rawToken, ref bool sawPositive)
    {
        // FRQ-3008: Exclusion operator must be preceded by a positive term
        if (!sawPositive)
        {
            parsedQuery.Errors.Add("Exclusion operator must be preceded by a positive term.");
            return;
        }

        // rawToken can be a phrase token ("...") or a term
        if (IsPhraseToken(rawToken))
        {
            var phrase = _queryNormalizer.NormalizePhrase(rawToken);
            if (!string.IsNullOrWhiteSpace(phrase))
                parsedQuery.ExcludedTerms.Add(phrase);
            return;
        }

        var value = _queryNormalizer.NormalizeTerm(rawToken);
        if (!string.IsNullOrWhiteSpace(value))
            parsedQuery.ExcludedTerms.Add(value);
    }

    private static QueryMode DetectMode(List<string> tokens)
    {
        // FRQ-3004: only uppercase operators count as operators
        // FRQ-3005: whitespace implies OR by default
        if (tokens.Any(t => t is "AND" or "&&")) return QueryMode.AND;
        if (tokens.Any(t => t is "OR" or "||")) return QueryMode.OR;
        return QueryMode.OR;
    }

    private static bool IsOperatorToken(string t) => 
        t is "AND" or "&&" or "OR" or "||";

    private static bool IsPhraseToken(string t) =>
        t.Length >= 2 && 
        t.StartsWith("\"", StringComparison.Ordinal) && 
        t.EndsWith("\"", StringComparison.Ordinal
    );

 

   
}
