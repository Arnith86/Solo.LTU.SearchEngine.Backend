using LTU.SearchEngine.Backend.Core.Enums;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class ExtractedQueryToken
{
	public QueryTokenType TokenType { get; }
	public string Token { get; }
	public string Language { get; }

	public ExtractedQueryToken(QueryTokenType tokenType, string token, string language = "sv")
	{
		if (string.IsNullOrWhiteSpace(token))
			throw new ArgumentException("Token cannot be empty.", nameof(token));

		if (tokenType.Equals(QueryTokenType.LogicalOperator) && token.Length > 3)
			throw new ArgumentOutOfRangeException(
				"Logical Operator cannot be longer than 3 characters.", nameof(token)
			);

		if (tokenType.Equals(QueryTokenType.GroupingOperator) && token.Length > 1)
			throw new ArgumentOutOfRangeException(
				"grouping Operator cannot be longer than a single characters.", nameof(token)
			);

		Token = token;
		TokenType = tokenType;
		Language = language;
	}
}
