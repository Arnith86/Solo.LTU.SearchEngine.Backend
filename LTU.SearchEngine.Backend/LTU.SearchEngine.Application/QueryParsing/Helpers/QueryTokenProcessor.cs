using LTU.SearchEngine.Backend.Core;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

public class QueryTokenProcessor : IQueryTokenProcessor
{
	private readonly IQueryNormalizer _queryNormalizer;
	private readonly ITokenizer _queryTokenizer;

	public QueryTokenProcessor(
		IQueryNormalizer queryNormalizer,
		ITokenizer queryTokenizer
		)
	{
		_queryNormalizer = queryNormalizer ??
			throw new ArgumentNullException(nameof(queryNormalizer));
		_queryTokenizer = queryTokenizer ??
			throw new ArgumentNullException(nameof(queryTokenizer));
	}
	
	public void ProcessNegativeToken(
		ParsedQuery parsedQuery,
		string token,
		ref bool sawPositive
		)
	{
		AddExcluded(parsedQuery, token, ref sawPositive);
	}


	public void ProcessRequiredToken(
		ParsedQuery parsedQuery,
		string token,
		ref bool sawPositive
		)
	{
		var value = _queryNormalizer.NormalizeTerm(token[1..]);

		if (!string.IsNullOrWhiteSpace(value))
		{
			parsedQuery.RequiredTerms.Add(value);
			sawPositive = true;
		}
	}

	public void ProcessPhraseToken(
		ParsedQuery parsedQuery,
		string token,
		ref bool sawPositive
		)
	{
		var phrase = _queryNormalizer.NormalizePhrase(token);

		if (!string.IsNullOrWhiteSpace(phrase))
		{
			parsedQuery.Phrases.Add(phrase);
			sawPositive = true;
		}
	}

	public void ProcessTermToken(
		ParsedQuery parsedQuery,
		string token,
		ref bool sawPositive
		)
	{
		// Normal term (FRQ-3001/3002)
		var term = _queryNormalizer.NormalizeTerm(token);
		if (!string.IsNullOrWhiteSpace(term))
		{
			parsedQuery.Terms.Add(term);
			sawPositive = true;
		}
	}

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
	
	public bool IsPhraseToken(string token) =>
		token.Length >= 2 &&
		token.StartsWith("\"", StringComparison.Ordinal) &&
		token.EndsWith("\"", StringComparison.Ordinal
	);

}
