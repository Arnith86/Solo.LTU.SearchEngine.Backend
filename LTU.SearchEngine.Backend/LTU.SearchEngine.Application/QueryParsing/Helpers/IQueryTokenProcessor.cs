using LTU.SearchEngine.Backend.Core;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
	public interface IQueryTokenProcessor
	{
		void ProcessNegativeToken(ParsedQuery parsedQuery, string token, ref bool sawPositive);
		void ProcessPhraseToken(ParsedQuery parsedQuery, string token, ref bool sawPositive);
		void ProcessRequiredToken(ParsedQuery parsedQuery, string token, ref bool sawPositive);
		void ProcessTermToken(ParsedQuery parsedQuery, string token, ref bool sawPositive);
	}
}