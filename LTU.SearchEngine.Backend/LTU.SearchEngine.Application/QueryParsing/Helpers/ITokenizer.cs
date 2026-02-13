using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
	public interface ITokenizer
	{
		void Flush(StringBuilder stringBuilder, List<string> tokens);
		List<string> Tokenize(string input);
	}
}