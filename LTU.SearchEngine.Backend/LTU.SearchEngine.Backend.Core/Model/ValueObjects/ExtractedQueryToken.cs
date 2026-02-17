using LTU.SearchEngine.Backend.Core.Enums;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class ExtractedQueryToken
{
	public QueryTokenType TokenType { get; }
	public string Token { get; }

	public ExtractedQueryToken(QueryTokenType tokenType, string token)
	{
		if (string.IsNullOrWhiteSpace(token))
			throw new ArgumentException("Token cannot be empty.", nameof(token));
		
		Token = token;
		TokenType = tokenType;
	}
}
