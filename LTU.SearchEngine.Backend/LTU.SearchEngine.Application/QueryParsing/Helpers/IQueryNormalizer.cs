namespace LTU.SearchEngine.Application.QueryParsing.Helpers
{
	public interface IQueryNormalizer
	{
		string NormalizePhrase(string quoted);
		string NormalizeTerm(string s);
	}
}