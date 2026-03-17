using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

namespace LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

/// <summary>
/// Provides a concrete implementation for building an Abstract Syntax Tree (AST) 
/// from a sequence of query tokens using a Shunting-yard based approach.
/// </summary>
/// <typeparam name="TResult">The return type produced by a visitor during tree traversal.</typeparam>

public class AbstractSyntaxTreeBuilder<TResult> : ITreeBuilder<TResult, ExtractedQueryToken>
{
	private readonly IShuntingYardParser<ExtractedQueryToken> _shuntingYardParser;

	/// <summary>
	/// Initializes a new instance of the <see cref="AbstractSyntaxTreeBuilder{TResult}"/> class.
	/// </summary>
	/// <param name="shuntingYardParser">The parser used to convert infix tokens to postfix notation.</param>
	public AbstractSyntaxTreeBuilder(
		IShuntingYardParser<ExtractedQueryToken> shuntingYardParser)
	{
		_shuntingYardParser = shuntingYardParser;
	}

	/// <inheritdoc/>
	public QueryNode<TResult> BuildTree(IEnumerable<ExtractedQueryToken> tokens)
	{
		Queue<ExtractedQueryToken> postfix = 
			_shuntingYardParser.ConvertToPostfix(tokens);

		Stack<QueryNode<TResult>> Stack = new Stack<QueryNode<TResult>>();

		foreach (ExtractedQueryToken token in postfix)
		{
			switch (token.TokenType)
			{
				case QueryTokenType.Term:
					Stack.Push(new TermNode<TResult>(token.Token));
					break;
				case QueryTokenType.Phrase:
					Stack.Push(
						new PhraseNode<TResult>(ConvertPhraseToTermList(token.Token))
					);
					break;
				case QueryTokenType.LogicalOperator:
					HandleLogicalOperator(Stack, token);
					break;
				default:
					break;
			}
		}

		if (Stack.Count != 1)
			throw new QuerySyntaxException("Invalid query structure.");

		return Stack.Pop();
	}


	private void HandleLogicalOperator(
		Stack<QueryNode<TResult>> aSTStack, ExtractedQueryToken token)
	{
		// makes sure there are enough tokens to build a logical operation
		if (aSTStack.Count < 2)
			throw new QuerySyntaxException("Invalid logical operation, not enough terms or phrases, or a right side NOT operator was encountered in the query.");

		QueryNode<TResult> right = aSTStack.Pop();
		QueryNode<TResult> left = aSTStack.Pop();

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
			"AND" or "&&" => LogicalOperators.AND,
			"OR" or "||" => LogicalOperators.OR,
			"NOT" or "-" or "!" => LogicalOperators.NOT,
			// "+" => LogicalOperators.REQUIRED, When Required gets implemented, this should be un-commented.
			_ => throw new NotSupportedException()
		};
	}

	private List<ExtractedQueryToken> ConvertPhraseToTermList(string phrase)
	{
		return phrase
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(word => new ExtractedQueryToken(QueryTokenType.Term, word))
			.ToList();
	}
}
