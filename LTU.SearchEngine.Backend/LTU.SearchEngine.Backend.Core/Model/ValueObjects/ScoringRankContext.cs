using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class ScoringRankContext : IScoringRankContext
{
	public HashSet<string> RegularTerms { get; } = new();
	public List<List<string>> Phrases { get; } = new();

	public IEnumerable<string> GetAllUniqueTerms()
	{
		var allTerms = new HashSet<string>(RegularTerms);

		foreach (var phrase in Phrases)
			foreach (var term in phrase) allTerms.Add(term);

		return allTerms;
	}

	public ScoringRankContext(QueryNode<HashSet<int>> node)
	{
		ExtractScoringContext(node);
	}


	private void ExtractScoringContext(QueryNode<HashSet<int>> node)
	{
		if (node is TermNode<HashSet<int>> termNode)
		{
			RegularTerms.Add(termNode.Term);
		}
		else if (node is PhraseNode<HashSet<int>> phraseNode)
		{
			var phraseWords = phraseNode.Phrase.Select(p => p.Token).ToList();
			Phrases.Add(phraseWords);
		}
		else if (node is RequiredNode<HashSet<int>> requiredNode)
		{
			ExtractScoringContext(requiredNode.Node);
		}
		else if (node is LogicOperationNode<HashSet<int>> logicOperationNode)
		{
			ExtractScoringContext(logicOperationNode.LeftNode);
			ExtractScoringContext(logicOperationNode.RightNode);
		}
	}
}
