using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;

namespace LTU.SearchEngine.Test.QueryParsing.Tests;

public class SearchQueryShuntingYardParserTests
{
	private readonly SearchQueryShuntingYardParser _sut;

	public SearchQueryShuntingYardParserTests()
	{
		_sut = new SearchQueryShuntingYardParser();
	}

	// ToDo: Make sure that tests use all versions of operators includes all operators including || && ! - and +

	[Fact]
	public void ConvertToPostfix_WhenTokensIsNull_ThrowsArgumentNullException()
	{
		// Arrange
		IEnumerable<ExtractedQueryToken>? tokens = null;

		// Act
		var action = () => _sut.ConvertToPostfix(tokens!);

		// Assert
		var exception = Assert.Throws<ArgumentNullException>(action);
		Assert.Contains("must have a value.", exception.Message);
		Assert.Equal("tokens", exception.ParamName);
	}

	[Fact]
	public void ConvertToPostfix_WhenTokensIsEmpty_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var tokens = new List<ExtractedQueryToken>();

		// Act
		var action = () => _sut.ConvertToPostfix(tokens);

		// Assert
		var exception = Assert.Throws<ArgumentOutOfRangeException>(action);
		Assert.Contains("cannot be empty.", exception.Message);
		Assert.Equal("tokens", exception.ParamName);
	}

	[Fact]
	public void ConvertToPostfix_SimpleAnd_ReturnsCorrectOrder()
	{
		// Arrange
		// Infix: Java AND Luleå -> Postfix: Java Luleå AND
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("Luleå", QueryTokenType.Term)
		};

		// Act
		var result = _sut
			.ConvertToPostfix(tokens)
			.ToList();

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equal("Java", result[0].Token);
		Assert.Equal("Luleå", result[1].Token);
		Assert.Equal("AND", result[2].Token);
	}

	[Theory]
	[InlineData("AND")]
	[InlineData("&&")]
	[InlineData("OR")]
	[InlineData("||")]
	[InlineData("NOT")]
	[InlineData("!")]
	[InlineData("-")]
	public void ConvertToPostfix_HandlesAllExceptRequiredOperatorsCorrectly(string op)
	{
		// Arrange
		// Infix: Java AND Luleå -> Postfix: Java Luleå AND
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken(op, QueryTokenType.LogicalOperator),
			CreateToken("Luleå", QueryTokenType.Term)
		};

		// Act
		var result = _sut
			.ConvertToPostfix(tokens)
			.ToList();

		// Assert
		Assert.Equal(3, result.Count);
		Assert.Equal("Java", result[0].Token);
		Assert.Equal("Luleå", result[1].Token);
		Assert.Equal(op, result[2].Token);
		Assert.Equivalent(QueryTokenType.LogicalOperator, result[2].TokenType);
	}

	// ToDo: implement this test when required operators are implemented in the parser.
	//[Fact]
	//public void ConvertToPostfix_HandlesRequiredOperatorsCorrectly()
	//{
	//	// Arrange
	//	// Infix: +Java OR Luleå -> Postfix: +Java Luleå OR
	//	var tokens = new List<ExtractedQueryToken>
	//	{
	//		CreateToken("Java", QueryTokenType.Term),
	//		CreateToken(op, QueryTokenType.LogicalOperator),
	//		CreateToken("Luleå", QueryTokenType.Term)
	//	};

	//	// Act
	//	var result = _sut
	//		.ConvertToPostfix(tokens)
	//		.ToList();

	//	// Assert
	//	Assert.Equal(3, result.Count);
	//	Assert.Equal("Java", result[0].Token);
	//	Assert.Equal("Luleå", result[1].Token);
	//	Assert.Equal(op, result[2].Token);
	//	Assert.Equivalent(QueryTokenType.LogicalOperator, result[2].TokenType);
	//}

	[Fact]
	public void ConvertToPostfix_PrecedenceNotOverAnd_ReturnsNotFirst()
	{
		// Arrange
		// Infix: Ltu NOT Java AND Luleå -> Postfix: Ltu Java NOT Luleå AND
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Ltu", QueryTokenType.Term),
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("Luleå", QueryTokenType.Term)
		};

		var expected = new List<string> { "Ltu", "Java", "NOT", "Luleå", "AND" };

		// Act 
		var result = _sut
			.ConvertToPostfix(tokens)
			.Select(t => t.Token)
			.ToList();

		// Assert 	
		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConvertToPostfix_WithParentheses_RemovesParenthesesAndOrdersCorrectly()
	{
		// Arrange
		// Infix: Java AND (Luleå OR Stockholm) -> Postfix: Java Luleå Stockholm OR AND
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("(", QueryTokenType.GroupingOperator),
			CreateToken("Luleå", QueryTokenType.Term),
			CreateToken("OR", QueryTokenType.LogicalOperator),
			CreateToken("Stockholm", QueryTokenType.Term),
			CreateToken(")", QueryTokenType.GroupingOperator)
		};

		var expected = new List<string> { "Java", "Luleå", "Stockholm", "OR", "AND" };
		
		// Act
		var resultQueue = _sut.ConvertToPostfix(tokens);
		var resultList = resultQueue.ToList();

		// Assert
		// Correct order
		Assert.Equal(expected, resultList.Select(t => t.Token));
		// No parentheses in output
		Assert.All(resultList, t => Assert.NotEqual(QueryTokenType.GroupingOperator, t.TokenType));
	}

	[Fact]
	public void ConvertToPostfix_ComplexPrecedence_FollowsNotAndOrOrder()
	{
		// Arrange
		// Infix: Java OR C# AND Ltu NOT Python 
		// Precedence: NOT(3) > AND(2) > OR(1)
		// Postfix: Java C# Ltu Python NOT AND OR
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("OR", QueryTokenType.LogicalOperator),
			CreateToken("C#", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("Ltu", QueryTokenType.Term),
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Python", QueryTokenType.Term)
		};

		var expected = new List<string> { "Java", "C#", "Ltu", "Python", "NOT", "AND", "OR" };
		
		// Act
		var result = _sut
			.ConvertToPostfix(tokens)
			.Select(t => t.Token)
			.ToList();

		//Assert
		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConvertToPostfix_MissingOpeningParenthesis_ThrowsFormatException()
	{
		// Arrange 
		// Infix: Java AND Luleå )
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("Luleå", QueryTokenType.Term),
			CreateToken(")", QueryTokenType.GroupingOperator)
		};

		// Act
		var action = () => _sut.ConvertToPostfix(tokens);

		// Assert
		var exception = Assert.Throws<FormatException>(action);
		Assert.Contains("without a matching opening parenthesis", exception.Message);
	}

	[Fact]
	public void ConvertToPostfix_MissingClosingParenthesis_ThrowsFormatException()
	{
		// Arrange
		// Infix: ( Java AND Luleå
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("(", QueryTokenType.GroupingOperator),
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("Luleå", QueryTokenType.Term)
		};

		// Act
		var action = () => _sut.ConvertToPostfix(tokens);

		// Assert
		var exception = Assert.Throws<FormatException>(action);
		Assert.Contains("without a matching closing parenthesis", exception.Message);
	}

	[Fact]
	public void ConvertToPostfix_MultipleSameOperators_FollowsLeftAssociativity()
	{
		// Infix: Java AND Luleå AND Stockholm
		// Process: 
		// 1. "Java" -> Output
		// 2. "AND" (first) -> Stack
		// 3. "Luleå" -> Output
		// 4. "AND" (second) "AND" (first) on stack. 
		//    same precedence (2 >= 2), first is popped to Output.
		// 5. "Stockholm" -> Output
		// 6. last "AND" popped to Output.

		// Expect Postfix: Java Luleå AND Stockholm AND

		var tokens = new List<ExtractedQueryToken>
	{
		CreateToken("Java", QueryTokenType.Term),
		CreateToken("AND", QueryTokenType.LogicalOperator),
		CreateToken("Luleå", QueryTokenType.Term),
		CreateToken("AND", QueryTokenType.LogicalOperator),
		CreateToken("Stockholm", QueryTokenType.Term)
	};

		var result = _sut.ConvertToPostfix(tokens)
						 .Select(t => t.Token)
						 .ToList();

		var expected = new List<string> { "Java", "Luleå", "AND", "Stockholm", "AND" };

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConvertToPostfix_OperatorsInSequence_ShouldThrowFormatException()
	{
		// Arrange
		// Query: Java AND NOT Ltu
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Ltu", QueryTokenType.Term),
		};

		Assert.Throws<FormatException>(()=> _sut.ConvertToPostfix(tokens));
	}
	
	[Fact]
	public void ConvertToPostfix_OperatorsInSequenceWithParentheses_ShouldThrowFormatException()
	{
		// Arrange
		// Query: Java AND (NOT Ltu)
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("Java", QueryTokenType.Term),
			CreateToken("AND", QueryTokenType.LogicalOperator),
			CreateToken("(", QueryTokenType.GroupingOperator),
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Ltu", QueryTokenType.Term),
			CreateToken(")", QueryTokenType.GroupingOperator),
		};

		Assert.Throws<FormatException>(()=> _sut.ConvertToPostfix(tokens));
	}
	
	[Fact]
	public void ConvertToPostfix_OperatorsFirst_ShouldThrowFormatException()
	{
		// Arrange
		// Query: NOT Ltu
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Ltu", QueryTokenType.Term),
		};

		Assert.Throws<FormatException>(()=> _sut.ConvertToPostfix(tokens));
	}
	
	[Fact]
	public void ConvertToPostfix_OperatorsFirstInParentheses_ShouldThrowFormatException()
	{
		// Arrange
		// Query: (NOT Ltu)
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("(", QueryTokenType.GroupingOperator),
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Ltu", QueryTokenType.Term),
			CreateToken(")", QueryTokenType.GroupingOperator)
		};

		Assert.Throws<FormatException>(()=> _sut.ConvertToPostfix(tokens));
	}
	
	[Fact]
	public void ConvertToPostfix_OperatorsFirstInMultipleParentheses_ShouldThrowFormatException()
	{
		// Arrange
		// Query: (NOT Ltu)
		var tokens = new List<ExtractedQueryToken>
		{
			CreateToken("(", QueryTokenType.GroupingOperator),
			CreateToken("(", QueryTokenType.GroupingOperator),
			CreateToken("NOT", QueryTokenType.LogicalOperator),
			CreateToken("Ltu", QueryTokenType.Term),
			CreateToken(")", QueryTokenType.GroupingOperator),
			CreateToken(")", QueryTokenType.GroupingOperator)
		};

		Assert.Throws<FormatException>(()=> _sut.ConvertToPostfix(tokens));
	}


	private ExtractedQueryToken CreateToken(string text, QueryTokenType type)
	{
		return new ExtractedQueryToken( 
			tokenType: type,
			token : text 
		);
	}
}
