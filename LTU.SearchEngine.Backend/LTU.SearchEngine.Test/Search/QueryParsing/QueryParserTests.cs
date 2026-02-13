using Xunit;
using LTU.SearchEngine.Application.Search.QueryParsing;
using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Backend.Core.Model;

namespace LTU.SearchEngine.Test.Search.QueryParsing;

/// <summary>
/// Unit tests for QueryParser covering TC-FRQ-3001 through TC-FRQ-3007.
/// Tests query parsing for terms, phrases, operators, and special syntax.
/// </summary>
public class QueryParserTests
{
    private readonly IQueryParser _parser;

    public QueryParserTests()
    {
        _parser = new QueryParser();
    }

    #region TC-FRQ-3001: Query Terms and Operators

    [Fact]
    public void Parse_SimpleTermAndOperator_ReturnsCorrectMode()
    {
        // Arrange
        var query = "cats AND dogs";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Equal(QueryMode.AND, result.Mode);
        Assert.Contains("cats", result.Terms);
        Assert.Contains("dogs", result.Terms);
        Assert.Equal(2, result.Terms.Count);
    }

    [Fact]
    public void Parse_TermsWithoutOperator_DefaultsToOR()
    {
        // Arrange
        var query = "cats dogs";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Equal(QueryMode.OR, result.Mode);
        Assert.Contains("cats", result.Terms);
        Assert.Contains("dogs", result.Terms);
    }

    #endregion

    #region TC-FRQ-3002: Single Term Support

    [Fact]
    public void Parse_SingleTerm_Hello_ReturnsOneTerm()
    {
        // Arrange
        var query = "hello";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Terms);
        Assert.Contains("hello", result.Terms);
        Assert.Empty(result.Phrases);
    }

    [Fact]
    public void Parse_SingleTerm_Test_ReturnsOneTerm()
    {
        // Arrange
        var query = "test";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Terms);
        Assert.Contains("test", result.Terms);
    }

    [Fact]
    public void Parse_SingleTerm_DoesNotMatchPartialWords()
    {
        // Arrange - verifierar att "test" inte matchar "testing"
        // (Detta är mer av ett semantiskt test - parsern returnerar bara "test")
        var query = "test";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Contains("test", result.Terms);
        Assert.DoesNotContain("testing", result.Terms);
    }

    #endregion

    #region TC-FRQ-3003: Phrase Support

    [Fact]
    public void Parse_Phrase_HelloDolly_ReturnsExactPhrase()
    {
        // Arrange
        var query = "\"hello dolly\"";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Phrases);
        Assert.Contains("hello dolly", result.Phrases);
        Assert.Empty(result.Terms);
    }

    [Fact]
    public void Parse_PhraseWithOtherTerms_SeparatesPhraseFromTerms()
    {
        // Arrange
        var query = "cat \"hello dolly\" dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Single(result.Phrases);
        Assert.Contains("hello dolly", result.Phrases);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_MultipleWords_WithoutQuotes_NotTreatedAsPhrase()
    {
        // Arrange
        var query = "hello dolly";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Empty(result.Phrases);
        Assert.Contains("hello", result.Terms);
        Assert.Contains("dolly", result.Terms);
        Assert.Equal(2, result.Terms.Count);
    }

    #endregion

    #region TC-FRQ-3004: Operator Case Sensitivity

    [Fact]
    public void Parse_UppercaseAND_RecognizedAsOperator()
    {
        // Arrange
        var query = "cat AND dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.AND, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_LowercaseAnd_NotRecognizedAsOperator()
    {
        // Arrange
        var query = "cat and dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        // "and" ska behandlas som vanligt term, inte operator
        Assert.Equal(QueryMode.OR, result.Mode); // Default mode
        Assert.Contains("cat", result.Terms);
        Assert.Contains("and", result.Terms);
        Assert.Contains("dog", result.Terms);
        Assert.Equal(3, result.Terms.Count);
    }

    [Fact]
    public void Parse_UppercaseOR_RecognizedAsOperator()
    {
        // Arrange
        var query = "cat OR dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.OR, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_LowercaseOr_NotRecognizedAsOperator()
    {
        // Arrange
        var query = "cat or dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.OR, result.Mode); // Default
        Assert.Contains("cat", result.Terms);
        Assert.Contains("or", result.Terms);
        Assert.Contains("dog", result.Terms);
        Assert.Equal(3, result.Terms.Count);
    }

    [Fact]
    public void Parse_UppercaseNOT_RecognizedAsExclusionOperator()
    {
        // Arrange
        var query = "cat NOT dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.ExcludedTerms);
    }

    [Fact]
    public void Parse_LowercaseNot_NotRecognizedAsExclusionOperator()
    {
        // Arrange
        var query = "cat not dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        // "not" ska vara vanligt term
        Assert.Contains("cat", result.Terms);
        Assert.Contains("not", result.Terms);
        Assert.Contains("dog", result.Terms);
        Assert.Empty(result.ExcludedTerms);
    }

    #endregion

    #region TC-FRQ-3005: Boolean OR Logic

    [Fact]
    public void Parse_ORKeyword_SetsORMode()
    {
        // Arrange
        var query = "cat OR dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.OR, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_PipeSymbol_SetsORMode()
    {
        // Arrange
        var query = "cat || dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.OR, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ImpliesOR()
    {
        // Arrange
        var query = "cat dog fish";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.OR, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
        Assert.Contains("fish", result.Terms);
    }

    [Fact]
    public void Parse_AllORSyntaxes_ProduceEquivalentResults()
    {
        // Arrange
        var queryKeyword = "cat OR dog";
        var querySymbol = "cat || dog";
        var queryWhitespace = "cat dog";

        // Act
        var resultKeyword = _parser.Parse(queryKeyword);
        var resultSymbol = _parser.Parse(querySymbol);
        var resultWhitespace = _parser.Parse(queryWhitespace);

        // Assert
        Assert.Equal(QueryMode.OR, resultKeyword.Mode);
        Assert.Equal(QueryMode.OR, resultSymbol.Mode);
        Assert.Equal(QueryMode.OR, resultWhitespace.Mode);

        // Alla ska ha samma terms
        Assert.Equal(2, resultKeyword.Terms.Count);
        Assert.Equal(2, resultSymbol.Terms.Count);
        Assert.Equal(2, resultWhitespace.Terms.Count);
    }

    #endregion

    #region TC-FRQ-3006: Boolean AND Logic

    [Fact]
    public void Parse_ANDKeyword_SetsANDMode()
    {
        // Arrange
        var query = "cat AND dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.AND, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_DoubleAmpersand_SetsANDMode()
    {
        // Arrange
        var query = "cat && dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.AND, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
    }

    [Fact]
    public void Parse_BothANDSyntaxes_ProduceEquivalentResults()
    {
        // Arrange
        var queryKeyword = "cat AND dog";
        var querySymbol = "cat && dog";

        // Act
        var resultKeyword = _parser.Parse(queryKeyword);
        var resultSymbol = _parser.Parse(querySymbol);

        // Assert
        Assert.Equal(QueryMode.AND, resultKeyword.Mode);
        Assert.Equal(QueryMode.AND, resultSymbol.Mode);
        Assert.Equal(2, resultKeyword.Terms.Count);
        Assert.Equal(2, resultSymbol.Terms.Count);
    }

    [Fact]
    public void Parse_ANDWithMultipleTerms_AllTermsRequired()
    {
        // Arrange
        var query = "cat AND dog AND fish";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Equal(QueryMode.AND, result.Mode);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
        Assert.Contains("fish", result.Terms);
        Assert.Equal(3, result.Terms.Count);
    }

    #endregion

    #region TC-FRQ-3007: Required Term Operator (+)

    [Fact]
    public void Parse_RequiredPhraseWithOptionalTerm_CorrectClassification()
    {
        // Arrange
        var query = "+\"are cats\" dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        
        // "are cats" ska vara required phrase
        Assert.Single(result.RequiredTerms);
        Assert.Contains("are cats", result.RequiredTerms);
        
        // "dog" ska vara vanligt term (optional)
        Assert.Contains("dog", result.Terms);
        Assert.Single(result.Terms);
    }

    [Fact]
    public void Parse_RequiredTerm_AddsToRequiredList()
    {
        // Arrange
        var query = "+important optional";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Contains("important", result.RequiredTerms);
        Assert.Contains("optional", result.Terms);
    }

    [Fact]
    public void Parse_MultipleRequiredTerms_AllAdded()
    {
        // Arrange
        var query = "+term1 +term2 optional";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Contains("term1", result.RequiredTerms);
        Assert.Contains("term2", result.RequiredTerms);
        Assert.Contains("optional", result.Terms);
        Assert.Equal(2, result.RequiredTerms.Count);
    }

    [Fact]
    public void Parse_OnlyPlusSign_IgnoredAsInvalid()
    {
        // Arrange
        var query = "+ cat";

        // Act
        var result = _parser.Parse(query);

        // Assert
        // Ensam '+' ska ignoreras, "cat" ska vara vanligt term
        Assert.Contains("cat", result.Terms);
    }

    #endregion

    #region Edge Cases & Error Handling

    [Fact]
    public void Parse_EmptyQuery_ReturnsError()
    {
        // Arrange
        var query = "";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains("Query is empty.", result.Errors);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsError()
    {
        // Arrange
        var query = "   ";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains("Query is empty.", result.Errors);
    }

    [Fact]
    public void Parse_StandaloneExclusion_ReturnsError()
    {
        // Arrange
        var query = "-excluded";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains("Standalone exclusion queries are not allowed", result.Errors);
    }

    [Fact]
    public void Parse_NOTWithoutFollowingTerm_ReturnsError()
    {
        // Arrange
        var query = "cat NOT";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.True(result.HasErrors);
        Assert.Contains("NOT must be followed by a term or phrase.", result.Errors);
    }

    [Fact]
    public void Parse_ExclusionWithPositiveTerm_Valid()
    {
        // Arrange
        var query = "cat -dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.ExcludedTerms);
    }

    [Fact]
    public void Parse_NOTOperator_ExcludesTerm()
    {
        // Arrange
        var query = "cat NOT dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.ExcludedTerms);
    }

    [Fact]
    public void Parse_MinusOperator_ExcludesTerm()
    {
        // Arrange
        var query = "cat -dog";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.ExcludedTerms);
    }

    [Fact]
    public void Parse_ExcludedPhrase_WorksCorrectly()
    {
        // Arrange
        var query = "cat NOT \"hello dolly\"";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Contains("cat", result.Terms);
        Assert.Contains("hello dolly", result.ExcludedTerms);
    }

    [Fact]
    public void Parse_ComplexQuery_AllFeaturesCombined()
    {
        // Arrange
        var query = "+required \"exact phrase\" normal AND another -excluded";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.False(result.HasErrors);
        Assert.Equal(QueryMode.AND, result.Mode);
        Assert.Contains("required", result.RequiredTerms);
        Assert.Contains("exact phrase", result.Phrases);
        Assert.Contains("normal", result.Terms);
        Assert.Contains("another", result.Terms);
        Assert.Contains("excluded", result.ExcludedTerms);
    }

    [Fact]
    public void Parse_CaseInsensitiveTerms_NormalizedToLowercase()
    {
        // Arrange
        var query = "Cat DOG FiSh";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Contains("cat", result.Terms);
        Assert.Contains("dog", result.Terms);
        Assert.Contains("fish", result.Terms);
    }

    [Fact]
    public void Parse_PhraseWithMixedCase_NormalizedToLowercase()
    {
        // Arrange
        var query = "\"Hello Dolly\"";

        // Act
        var result = _parser.Parse(query);

        // Assert
        Assert.Contains("hello dolly", result.Phrases);
    }

    #endregion
}