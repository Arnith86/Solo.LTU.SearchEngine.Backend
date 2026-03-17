
using System.Runtime.CompilerServices;
using J2N;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Infrastructure.Repositories;
using Moq;

namespace LTU.SearchEngine.Application.QueryParsing;

public class QueryServiceTests
{
    private readonly Mock<IIndexRepository> _mockIndexRepository; 
    private readonly Mock<IQueryParser> _mockQueryParser; 
    private readonly Mock<IQueryVisitor<HashSet<int>>> _mockQueryEvaluatorVisitor; 
    private readonly IQueryService _sut;
    
    public QueryServiceTests()
    {
        _mockIndexRepository = new Mock<IIndexRepository>();
        _mockQueryParser = new Mock<IQueryParser>();
        _mockQueryEvaluatorVisitor = new Mock<IQueryVisitor<HashSet<int>>>();

        _sut = new QueryService(
            _mockIndexRepository.Object,
            _mockQueryParser.Object,
            _mockQueryEvaluatorVisitor.Object
        );

    }

    private Page CreateFakePage()
    {
        return new Page
        {
            Id = 1,
            Url = "https://www.example.com",
            Title = "Understanding Search Engines",
            LastCrawled = DateTime.UtcNow,
            PageRankScore = 4.5,
            ContentHash = "a1b2c3d4e5f6g7h8", 
            WordCount = 1250,
            HttpStatus = 200,
            Language = "en-US",
            
            WordFrequencies = new List<PageWordFrequency>{},
            
            OutgoingLinks = new List<PageLink>{}
        };
    }
    

    private void SetupMocks(
        string rawQuery, 
        LogicOperationNode<HashSet<int>> fakeOperatorNode, 
        HashSet<int> fakeResultIds,
         List<Page> fakeDocumentList
        )
    {
        _mockQueryParser
            .Setup(p => p.Parse(rawQuery))
            .Returns(fakeOperatorNode);

        _mockQueryEvaluatorVisitor
            .Setup(qe => qe.ExecuteAsync(fakeOperatorNode))
            .ReturnsAsync(fakeResultIds);

        _mockIndexRepository.
            Setup(ir => ir.GetDocumentsByIdAsync(fakeResultIds.ToList()))
            .ReturnsAsync(fakeDocumentList);
    }

    [Fact]
    public async Task GetSearchResultsAsync_ValidQuery_ReturnsMappedSearchResponse()
    {
        // Arrange
        string rawQuery = "Find something";
        var rightNode = new TermNode<HashSet<int>>("rightNode");
        var leftNode = new TermNode<HashSet<int>>("leftNode");
        var fakeOperatorNode = new LogicOperationNode<HashSet<int>>(rightNode, leftNode, LogicalOperators.AND);

        var fakeResultIds = new HashSet<int> { 101 };

        var fakePage = CreateFakePage();

        var fakeDocumentList = new List<Page> { fakePage };

        SetupMocks(rawQuery, fakeOperatorNode, fakeResultIds, fakeDocumentList);

        // Act
        var result = await _sut.GetSearchResultsAsync(rawQuery);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SearchResponseDTO>(result);
        Assert.Equal(fakePage.Title, result.searchResults.First().Title);

        _mockQueryParser.Verify(p => p.Parse(rawQuery), Times.Once);
        _mockQueryEvaluatorVisitor.Verify(qe => qe.ExecuteAsync(fakeOperatorNode), Times.Once);
        _mockIndexRepository.Verify(ir => ir.GetDocumentsByIdAsync(fakeResultIds.ToList()), Times.Once);
    }

     [Fact]
    public async Task GetSearchResultsAsync_ReturnsMessageWithTiming()
    {
        // Arrange
        string rawQuery = "Find something";
        var rightNode = new TermNode<HashSet<int>>("rightNode");
        var leftNode = new TermNode<HashSet<int>>("leftNode");
        var fakeOperatorNode = new LogicOperationNode<HashSet<int>>(rightNode, leftNode, LogicalOperators.AND);

        var fakeResultIds = new HashSet<int> { 101 };

        var fakePage = CreateFakePage();

        var fakeDocumentList = new List<Page> { fakePage };

        SetupMocks(rawQuery, fakeOperatorNode, fakeResultIds, fakeDocumentList);

        // Act
        var result = await _sut.GetSearchResultsAsync(rawQuery);

        // Assert
        Assert.NotNull(result.message);
        Assert.Matches(@"Search completed in \d+\.\d{2} ms", result.message);
    }

}