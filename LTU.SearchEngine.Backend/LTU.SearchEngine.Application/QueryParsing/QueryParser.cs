using LTU.SearchEngine.Application.QueryParsing.Helpers;
using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Model;

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
	private enum LoopAction { Continue, Break, Next, None } // ToDo: extract to own enum class 
	private readonly IQueryNormalizer _queryNormalizer;
    private readonly ITokenizer _queryTokenizer;
    private readonly IQueryTokenProcessor _queryTokenProcessor;

    public QueryParser(
        IQueryNormalizer queryNormalizer,
        ITokenizer queryTokenizer,
		IQueryTokenProcessor queryTokenProcessor
        )
    {
        _queryNormalizer = queryNormalizer ?? 
            throw new ArgumentNullException(nameof(queryNormalizer));
        _queryTokenizer = queryTokenizer ??
			throw new ArgumentNullException(nameof(queryTokenizer));
		_queryTokenProcessor = queryTokenProcessor ??
			throw new ArgumentNullException(nameof(queryTokenProcessor));
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
		// ToDo: Mode detection does not take into consideration that there can be concatenated logical expressions (example: "term1 && term2 || term3 )
		var mode = DetectMode(tokens);  

        var parsedQuery = new ParsedQuery { Mode = mode };

        bool sawPositive = false;

        for (int i = 0; i < tokens.Count; i++)
        {
			LoopAction loopAction = LoopAction.None;

			var token = tokens[i];

            // Skip explicit AND/OR tokens
            if (IsOperatorToken(token)) continue;

            loopAction = 
				HandleNegativeToken(parsedQuery, tokens, token, sawPositive, index: i);

            if (loopAction.Equals(LoopAction.Continue)) 
				continue;
            if (loopAction.Equals(LoopAction.Break)) 
				break;

            loopAction = 
				HandleRequiredToken(parsedQuery, token, sawPositive);

			if (loopAction.Equals(LoopAction.Continue)) 
				continue;

			loopAction = 
				HandlePhraseToken(parsedQuery, token, sawPositive);

			if (loopAction.Equals(LoopAction.Continue)) 
				continue;

			HandleTermToken(parsedQuery, token, sawPositive);
        }

        // FRQ-3008 constraint: standalone exclusion queries should return error or zero results
        if (!sawPositive && parsedQuery.ExcludedTerms.Count > 0)
            parsedQuery.Errors.Add("Standalone exclusion queries are not allowed (must include a positive term).");

        return parsedQuery;
    }

	// --- Helpers ---
	private LoopAction HandleNegativeToken(
	    ParsedQuery parsedQuery,
	    List<string> tokens,
	    string token,
	    bool sawPositive,
	    int index
	    )
	{
		// NOT operator excludes the next term/phrase (FRQ-3008)
		if (token == "NOT")
		{
			if (index + 1 >= tokens.Count)
			{
				parsedQuery.Errors.Add("NOT must be followed by a term or phrase.");
				return LoopAction.Break;
			}

			var next = tokens[++index];
			_queryTokenProcessor.ProcessNegativeToken(parsedQuery, next, ref sawPositive);
			
			return LoopAction.Continue;
		}

		// Excluded term (-) || (!) (FRQ-3008)
		if (token.StartsWith("-", StringComparison.Ordinal) ||
			token.StartsWith("!", StringComparison.Ordinal) &&
			token.Length > 1)
		{
			_queryTokenProcessor.ProcessNegativeToken(parsedQuery, token[1..], ref sawPositive);
			return LoopAction.Continue;
		}

		return LoopAction.None;
	}

	private LoopAction HandleRequiredToken(
		ParsedQuery parsedQuery,
		string token,
		bool sawPositive
		)
	{
		// Required term (+) (FRQ-3007)
		if (token.StartsWith("+", StringComparison.Ordinal) && token.Length > 1)
		{
			_queryTokenProcessor.ProcessRequiredToken(parsedQuery, token, ref sawPositive);
			
			return LoopAction.Continue;
		}

		return LoopAction.None;
	}

	private LoopAction HandlePhraseToken(
		ParsedQuery parsedQuery,
		string token,
		bool sawPositive
		)
	{
		// Phrase (FRQ-3003)
		if (_queryTokenProcessor.IsPhraseToken(token))
		{
			_queryTokenProcessor.ProcessPhraseToken(parsedQuery, token, ref sawPositive);
			
			return LoopAction.Continue;
		}

		return LoopAction.None;
	}

	private void HandleTermToken(
		ParsedQuery parsedQuery,
		string token,
		bool sawPositive
		)
	{
		// Normal term (FRQ-3001/3002)
		_queryTokenProcessor.ProcessTermToken(parsedQuery, token, sawPositive);
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

}
