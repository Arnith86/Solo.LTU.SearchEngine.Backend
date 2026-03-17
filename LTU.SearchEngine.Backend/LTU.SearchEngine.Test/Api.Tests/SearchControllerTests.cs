using LTU.SearchEngine.Api;
using LTU.SearchEngine.Application;
using LTU.SearchEngine.Application.QueryParsing;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LTU.SearchEngine.Test.Api.Tests
{
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
                new DocumentDTO("Test", "http://test.com", "sv"/*, "Test snippet"*/)
            };

            var expectedDto = new SearchResponseDTO(
                searchResults: fakeItems,
                currentPage: 1,
                pageSize: 1,
                totalResults: 1,
                message: "Success"
            );

            _mockQueryService
                .Setup(s => s.GetSearchResultsAsync(validQuery))
                .Returns(Task.FromResult(expectedDto)); 

            // Act
            var result = await _controller.GetSearchResponses(validQuery);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseDto = Assert.IsType<SearchResponseDTO>(okResult.Value);

            Assert.NotEmpty(responseDto.searchResults);
            Assert.Equal(1, responseDto.totalResults);
            Assert.Equal("Success", responseDto.message);

            // Verify that the call reached the service layer 
            _mockQueryService.Verify(s => s.GetSearchResultsAsync(validQuery), Times.Once);
        }

        [Fact]
        public async Task GetSearchResponses_ShouldReturnOkWithEmptyList_WhenNoMatchesFound()
        {
            // Arrange
            string query = "zero-results";

            var emptyDto = new SearchResponseDTO(
                searchResults: new List<DocumentDTO>(),
                currentPage: 1,
                pageSize: 0,
                totalResults: 0,
                message: "No results found"
            );

            _mockQueryService
                .Setup(s => s.GetSearchResultsAsync(query))
                .Returns(Task.FromResult(emptyDto)); 

            // Act
            var result = await _controller.GetSearchResponses(query);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseDto = Assert.IsType<SearchResponseDTO>(okResult.Value);

            Assert.Empty(responseDto.searchResults);
            Assert.Equal(0, responseDto.totalResults);
            Assert.Equal("No results found", responseDto.message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData(null)]
        public async Task GetSearchResponses_ShouldReturnBadRequest_WhenQueryIsInvalid(string invalidQuery)
        {
            // Act
            var result = await _controller.GetSearchResponses(invalidQuery);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetSearchResponses_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            // Arrange
            string query = "error";

            _mockQueryService
                .Setup(s => s.GetSearchResultsAsync(query))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetSearchResponses(query));
        }
    }
}