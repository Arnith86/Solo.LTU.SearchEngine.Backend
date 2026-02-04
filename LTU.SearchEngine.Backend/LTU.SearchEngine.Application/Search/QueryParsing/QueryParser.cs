using System.Text;

namespace LTU.SearchEngine.Application.Search.QueryParsing;

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
    public ParsedQuery Parse(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            var empty = new ParsedQuery();
            empty.Errors.Add("Query is empty.");
            return empty;
        }

        var tokens = Tokenize(rawQuery);
        var mode = DetectMode(tokens);

        var pq = new ParsedQuery { Mode = mode };

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
                    pq.Errors.Add("NOT must be followed by a term or phrase.");
                    break;
                }

                var next = tokens[++i];
                AddExcluded(pq, next, ref sawPositive);
                continue;
            }

            // Required term (+) (FRQ-3007)
            if (token.StartsWith("+", StringComparison.Ordinal) && token.Length > 1)
            {
                var value = NormalizeTerm(token[1..]);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    pq.RequiredTerms.Add(value);
                    sawPositive = true;
                }
                continue;
            }

            // Excluded term (-) (FRQ-3008)
            if (token.StartsWith("-", StringComparison.Ordinal) && token.Length > 1)
            {
                AddExcluded(pq, token[1..], ref sawPositive);
                continue;
            }

            // Phrase (FRQ-3003)
            if (IsPhraseToken(token))
            {
                var phrase = NormalizePhrase(token);
                if (!string.IsNullOrWhiteSpace(phrase))
                {
                    pq.Phrases.Add(phrase);
                    sawPositive = true;
                }
                continue;
            }

            // Normal term (FRQ-3001/3002)
            var term = NormalizeTerm(token);
            if (!string.IsNullOrWhiteSpace(term))
            {
                pq.Terms.Add(term);
                sawPositive = true;
            }
        }

        // FRQ-3008 constraint: standalone exclusion queries should return error or zero results
        if (!sawPositive && pq.ExcludedTerms.Count > 0)
            pq.Errors.Add("Standalone exclusion queries are not allowed (must include a positive term).");

        return pq;
    }

    // --- Helpers ---

    private static void AddExcluded(ParsedQuery pq, string rawToken, ref bool sawPositive)
    {
        // FRQ-3008: Exclusion operator must be preceded by a positive term
        if (!sawPositive)
        {
            pq.Errors.Add("Exclusion operator must be preceded by a positive term.");
            return;
        }

        // rawToken can be a phrase token ("...") or a term
        if (IsPhraseToken(rawToken))
        {
            var phrase = NormalizePhrase(rawToken);
            if (!string.IsNullOrWhiteSpace(phrase))
                pq.ExcludedTerms.Add(phrase);
            return;
        }

        var value = NormalizeTerm(rawToken);
        if (!string.IsNullOrWhiteSpace(value))
            pq.ExcludedTerms.Add(value);
    }

    private static QueryMode DetectMode(List<string> tokens)
    {
        // FRQ-3004: only uppercase operators count as operators
        // FRQ-3005: whitespace implies OR by default
        if (tokens.Any(t => t is "AND" or "&&")) return QueryMode.AND;
        if (tokens.Any(t => t is "OR" or "||")) return QueryMode.OR;
        return QueryMode.OR;
    }

    private static bool IsOperatorToken(string t) => t is "AND" or "&&" or "OR" or "||";

    private static bool IsPhraseToken(string t) =>
        t.Length >= 2 && t.StartsWith("\"", StringComparison.Ordinal) && t.EndsWith("\"", StringComparison.Ordinal);

    /// <summary>
    /// Tokenizes input by whitespace while keeping quoted phrases together (including quotes).
    /// Example: cat "hello dolly" dog -> [cat, "hello dolly", dog]
    /// </summary>
    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                Flush(sb, tokens);
                continue;
            }

            sb.Append(c);
        }

        Flush(sb, tokens);
        return tokens;
    }

    private static void Flush(StringBuilder sb, List<string> tokens)
    {
        if (sb.Length == 0) return;

        var t = sb.ToString().Trim();
        if (t.Length > 0)
            tokens.Add(t);

        sb.Clear();
    }

    // For UC-3001 we normalize to lowercase to match common indexing/search behavior.
    private static string NormalizeTerm(string s) => s.Trim().ToLowerInvariant();

    private static string NormalizePhrase(string quoted)
    {
        var inner = quoted.Trim();

        // Remove surrounding quotes
        if (inner.StartsWith("\"", StringComparison.Ordinal) &&
            inner.EndsWith("\"", StringComparison.Ordinal) &&
            inner.Length >= 2)
        {
            inner = inner[1..^1];
        }

        return inner.Trim().ToLowerInvariant();
    }
}
