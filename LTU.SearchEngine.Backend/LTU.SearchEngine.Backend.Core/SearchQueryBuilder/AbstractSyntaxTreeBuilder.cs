using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

public class AbstractSyntaxTreeBuilder<TResult>
{
	private readonly IShuntingYardParser<ExtractedQueryToken> _shuntingYardParser;

	public AbstractSyntaxTreeBuilder(IShuntingYardParser<ExtractedQueryToken> shuntingYardParser)
	{
		_shuntingYardParser = shuntingYardParser;
	}

	public QueryNode<TResult> BuildTree(IEnumerable<ExtractedQueryToken> tokens)
	{
		Queue<ExtractedQueryToken> postfix = _shuntingYardParser.ConvertToPostfix(tokens);

		Stack<QueryNode<TResult>> ASTStack = new Stack<QueryNode<TResult>>(); 

		foreach (ExtractedQueryToken token in postfix)
		{
			switch (token.TokenType)
			{
				case QueryTokenType.Term:
					ASTStack.Push(new TermNode<TResult>(token.Token));
					break;
				case QueryTokenType.Phrase:
					ASTStack.Push(
						new PhraseNode<TResult>(ConvertPhraseToTermList(token.Token))
					);
					break;
				case QueryTokenType.LogicalOperator:
					HandleLogicalOperator(ASTStack, token);
					break;
				default:
					break;
			}
		}

	}

	private void HandleLogicalOperator(
		Stack<QueryNode<TResult>> aSTStack, ExtractedQueryToken token)
	{
		if (aSTStack.Count < 2)
			throw new InvalidOperationException("Invalid logical operation.");

		QueryNode<TResult> left = AddTextNodes(aSTStack);
		QueryNode<TResult> right = AddTextNodes(aSTStack);
		  
		LogicalOperators op = ParseOperatorType(token.Token);

		aSTStack.Push(new LogicOperationNode<TResult>(
			leftNode: left,
			rightNode: right,
			logicalOperator: op
		));
	}

	private LogicalOperators ParseOperatorType(string token)
	{
		return token switch
		{
			"AND" => LogicalOperators.AND,
			"OR" => LogicalOperators.OR,
			"NOT" => LogicalOperators.NOT,
			_ => throw new NotSupportedException()
		};
	}

	private QueryNode<TResult> AddTextNodes(Stack<QueryNode<TResult>> aSTStack)
	{

		if (!aSTStack.TryPop(out var node))
			throw new InvalidOperationException("Stack is empty. Expected a Term or Phrase node.");


		if (node is not (TermNode<TResult> or PhraseNode<TResult>)) 	
			throw new InvalidOperationException(
				$"Invalid logical operation. Expected a Term or Phrase node but found {node.GetType}."
			);
		
		return node;
	}

	private List<ExtractedQueryToken> ConvertPhraseToTermList(string phrase)
	{
		return phrase
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(word => new ExtractedQueryToken(QueryTokenType.Term, word))
			.ToList();
	}
}
