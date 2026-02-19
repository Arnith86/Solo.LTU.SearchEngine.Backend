using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;


/// <summary>
/// A generic interface for decomposing raw strings into a collection of categorized tokens.
/// </summary>
/// <typeparam name="TToken">The type representing the finalized token (e.g., ExtractedQueryToken).</typeparam>
/// <typeparam name="TType">The enum or type representing the category (e.g., QueryTokenType).</typeparam>
public interface IStringTokenizer<TToken, TType>
{
	/// <summary>
	/// Processes the current character buffer, creates a token of type <typeparamref name="TToken"/>, 
	/// and adds it to the collection.
	/// </summary>
	/// <param name="stringBuilder">The buffer holding characters for the token currently being built.</param>
	/// <param name="tokens">The collection where the finalized token will be added.</param>
	/// <param name="tokenType">The category to assign to the token (of type <typeparamref name="TType"/>).</param>
	/// <remarks>
	/// Clears the <paramref name="stringBuilder"/> after the token is added. 
	/// If the buffer is empty, no action is taken.
	/// </remarks>
	void Flush(
		StringBuilder stringBuilder,
		List<TToken> tokens,
		TType tokenType
	);

	/// <summary>
	/// Transforms a raw input string into a sequence of categorized tokens.
	/// </summary>
	/// <param name="input">The raw string to be tokenized.</param>
	/// <returns>A list of tokens of type <typeparamref name="TToken"/>.</returns>
	List<TToken> Tokenize(string input);
}

///// <summary>
///// Handles the decomposition of raw search strings into categorized tokens <br/>
///// such as terms, quoted phrases, and logical operators.
///// </summary>
//public interface IStringTokenizer<TType, TExtractedToken>
//{
//	/// <summary>
//	/// Processes the current character buffer, creates an <see cref="ExtractedQueryToken"/>, <br/>
//	/// and adds it to the token collection.
//	/// </summary>
//	/// <param name="stringBuilder">The buffer holding characters for the token currently being built.</param>
//	/// <param name="tokens">The collection where the finalized token will be added.</param>
//	/// <param name="queryTokenType">The specific <see cref="QueryTokenType"/> (Term, Phrase, or LogicalOperator) to assign.</param>
//	/// <remarks>
//	/// Clears the <paramref name="stringBuilder"/> after the token is added. 
//	/// If the buffer is empty, no action is taken.
//	/// </remarks>
//	public void Flush(
//		StringBuilder stringBuilder,
//		List<ExtractedQueryToken> tokens,
//		QueryTokenType queryTokenType
//	);

//	/// <summary>
//	/// Transforms a raw search string into a sequence of categorized tokens.
//	/// </summary>
//	/// <param name="input">The raw search string provided by the user.</param>
//	/// <returns>
//	/// A list of <see cref="ExtractedQueryToken"/> representing individual terms, quoted phrases <br/>
//	/// (e.g., "hello world"), and logical operators (e.g., &amp;&amp;, ||, AND, OR).
//	/// </returns>
//	/// <remarks>
//	/// The tokenizer follows these primary rules:
//	/// <list type="bullet">
//	/// <item>Separates individual terms by whitespace.</item>
//	/// <item>Preserves text within double quotes as a single <c>Phrase</c> token.</item>
//	/// <item>Identifies specific logical symbols (+, -, !, &amp;&amp;, ||) as <c>LogicalOperator</c>.</item>
//	/// <item>Identifies case-sensitive keywords (AND, OR) as <c>LogicalOperator</c> when surrounded by whitespace.</item>
//	/// <item>Supports escape characters (\) to treat special symbols literally within a term.</item>
//	/// </list>
//	/// </remarks>
//	List<ExtractedQueryToken> Tokenize(string input);
//}