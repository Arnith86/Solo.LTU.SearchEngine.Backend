namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public interface IScoringRankContext
{
	List<List<string>> Phrases { get; }
	HashSet<string> RegularTerms { get; }

	IEnumerable<string> GetAllUniqueTerms();
}