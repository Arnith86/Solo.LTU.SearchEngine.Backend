using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
    public class PhraseAndTermNormalizer : ITextNormalizer<ExtractedQueryToken>
    {
        public string Normalize(ExtractedQueryToken token)
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
}
