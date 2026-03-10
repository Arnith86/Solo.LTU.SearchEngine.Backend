using LTU.SearchEngine.Api;
using LTU.SearchEngine.Application;
using LTU.SearchEngine.Backend.Core.Model.DTOs; 
using LTU.SearchEngine.Backend.Core.Model.ValueObjects; 
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

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

            var expectedResults = new List<SearchResultItem>
            {
                new SearchResultItem ("Test", "http://test.com", "Test snippet" )
            };

            _mockQueryService.Setup(s => s.SearchAsync(validQuery))
                .ReturnsAsync(expectedResults);

            // Act
            var result = await _controller.GetSearchResponses(validQuery);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);

            var responseDto = Assert.IsType<SearchResponseDTO>(okResult.Value);


            Assert.NotEmpty(responseDto.searchResults);
            Assert.Equal(1, responseDto.totalResults);
            Assert.Equal("Success", responseDto.message);

            // Verify that the call reached the service layer
            _mockQueryService.Verify(s => s.SearchAsync(validQuery), Times.Once);
        }

        [Fact]
        public async Task GetSearchResponses_ShouldReturnOkWithEmptyList_WhenNoMatchesFound()
        {
            // Arrange
            string query = "zero-results";

            _mockQueryService.Setup(s => s.SearchAsync(query))
                .ReturnsAsync(new List<SearchResultItem>());

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
            _mockQueryService.Setup(s => s.SearchAsync(query))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetSearchResponses(query));
        }
    }
}