using LTU.SearchEngine.Backend.Core.Model;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
	public interface IQuerySyntaxHelper
	{
		List<string> Tokenize(string input);
		QueryMode DetectMode(List<string> tokens);
		bool IsOperatorToken(string token);
		bool IsPhraseToken(string token);
	}
}