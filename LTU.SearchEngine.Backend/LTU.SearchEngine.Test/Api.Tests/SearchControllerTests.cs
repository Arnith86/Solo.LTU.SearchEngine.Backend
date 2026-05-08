using LTU.SearchEngine.Api;
using LTU.SearchEngine.Application;
using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LTU.SearchEngine.Test.Api.Tests;

public class SearchControllerTests
{
    private readonly Mock<IServiceManager> _mockServiceManager;
    private readonly Mock<IQueryService> _mockQueryService;
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _mockQueryService = new Mock<IQueryService>();
        _mockServiceManager = new Mock<IServiceManager>();

        // Connect ServiceManager with QueryService
        _mockServiceManager.Setup(m => m.QueryService).Returns(_mockQueryService.Object);
        _controller = new SearchController(_mockServiceManager.Object);
    }

    [Fact]
    public async Task GetSearchResponses_ShouldReturnOk_WhenQueryIsValid()
    {
        // Arrange
        string validQuery = "test search";

        var fakeItems = new List<DocumentDTO>
        {
            new DocumentDTO(1, "Test", "http://test.com", "sv"/*, "Test snippet"*/)
        };

        var expectedDto = new SearchResponseDTO(
            SearchResults: fakeItems,
            MetaData: new PaginationMetaData(1,1,1,1),
            Message: "Success"
        );

        var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(validQuery);
        var pageParam = PaginationRequestParametersBuilder.BuildPaginationParameters();

        _mockQueryService
            .Setup(s => s.GetSearchResultsAsync(searchParam, pageParam))
            .Returns(Task.FromResult(expectedDto)); 

        // Act
        var result = await _controller.GetSearchResponses(searchParam, pageParam);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<SearchResponseDTO>(okResult.Value);

        Assert.NotEmpty(responseDto.SearchResults);
        Assert.Equal(1, responseDto.MetaData.TotalItemCount);
        Assert.Equal("Success", responseDto.Message);

        // Verify that the call reached the service layer 
        _mockQueryService.Verify(s => s.GetSearchResultsAsync(searchParam, pageParam), Times.Once);
    }


    [Theory]
    [InlineData(" test search ", "test search")]
    [InlineData("test search ", "test search")]
    [InlineData(" test OR search", "test OR search")]
    [InlineData("    test search ", "test search")]
    [InlineData(" test search    ", "test search")]
    [InlineData(" test AND search ", "test AND search")]
    public async Task GetSearchResponses_ShouldTrimQuery_WhenQueryIsValidButHasLeadingOrTrailingWhiteSpaces(
        string input, string expectedTrim)
    {
        var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(query: input);
        var searchParamExpected = SearchQueryRequestParametersBuilder.BuildParameters(query: expectedTrim);
        var pageParam = PaginationRequestParametersBuilder.BuildPaginationParameters();
        
        // Arrange
        _mockQueryService
            .Setup(s => s.GetSearchResultsAsync(searchParam, pageParam));
      
        // Act
        var result = await _controller.GetSearchResponses(searchParam, pageParam);

        // Assert
        _mockQueryService.Verify(s => s.GetSearchResultsAsync(
            It.Is<SearchQueryRequestParameters>(sqrp => sqrp.Query == expectedTrim), 
            It.IsAny<PaginationRequestParameters>()), 
            Times.Once
        );
    }


    [Fact]
    public async Task GetSearchResponses_ShouldReturnOkWithEmptyList_WhenNoMatchesFound()
    {
        // Arrange
        string query = "zero-results";

        var searchParam = SearchQueryRequestParametersBuilder.BuildParameters(query);
        var pageParam = PaginationRequestParametersBuilder.BuildPaginationParameters();

        var emptyDto = new SearchResponseDTO(
            SearchResults: new List<DocumentDTO>(),
            new PaginationMetaData(0,0,0,0),
            Message: "No results found"
        );

        _mockQueryService
            .Setup(s => s.GetSearchResultsAsync(searchParam, pageParam))
            .Returns(Task.FromResult(emptyDto)); 

        // Act
        var result = await _controller.GetSearchResponses(searchParam, pageParam);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<SearchResponseDTO>(okResult.Value);

        Assert.Empty(responseDto.SearchResults);
        Assert.Equal(0, responseDto.MetaData.TotalItemCount);
        Assert.Equal("No results found", responseDto.Message);
    }

    [Theory]
    [InlineData("sv", "sv")]
    [InlineData("en", "en")]
    [InlineData("NO_INPUT", "sv")]
    public async Task GetSearchResponses_ShouldAssignCorrectLanguage(string input, string expected)
    {
        // Arrange
        string query = "zero-results";
        var searchParam = (input == "NO_INPUT") 
                ? SearchQueryRequestParametersBuilder.BuildParameters(query) 
                : SearchQueryRequestParametersBuilder.BuildParameters(query, input);

        var emptyDto = new SearchResponseDTO(
            SearchResults: new List<DocumentDTO>(),
            It.IsAny<PaginationMetaData>(),
            Message: "No results found"
        );

        var pageParam = PaginationRequestParametersBuilder.BuildPaginationParameters();

        _mockQueryService
            .Setup(s => s.GetSearchResultsAsync(searchParam, pageParam))
            .Returns(Task.FromResult(emptyDto)); 

        // Act
        await _controller.GetSearchResponses(searchParam, pageParam);
        
        // Assert
        _mockQueryService.Verify(qs => qs.GetSearchResultsAsync(
            It.Is<SearchQueryRequestParameters>(sqrp => sqrp.Language == expected),
            It.IsAny<PaginationRequestParameters>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetSearchResponses_ShouldReturnInternalServerError_WhenServiceThrowsException()
    {
        // Arrange
        string query = "error";
        var searchParam = SearchQueryRequestParametersBuilder.BuildSearchParameters();
        var pageParam = PaginationRequestParametersBuilder.BuildPaginationParameters();

        _mockQueryService
            .Setup(s => s.GetSearchResultsAsync(searchParam, pageParam))
            .ThrowsAsync(new System.Exception("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<System.Exception>(() => _controller.GetSearchResponses(searchParam, pageParam));
    }

    [Fact]
    public async Task GetSearchResponses_QueryTooLong_ThrowsException()
    {
       // Arrange & Act & Assert
        Assert.Throws<QuerySyntaxException>(() => 
            SearchQueryRequestParametersBuilder.BuildParameters(new string('a', 501)));
    }  
}
