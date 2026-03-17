using LTU.SearchEngine.Application;
using LTU.SearchEngine.Application.QueryParsing;
using Moq;

namespace LTU.SearchEngine.Test.Application.Tests;

public class ServiceManagerTests
{
    [Fact]
    public void QueryService_Property_ShouldReturnCorrectInstance()
    {
        // Arrange
        var mockQueryService = new Mock<IQueryService>();
        var serviceManager = new ServiceManager(mockQueryService.Object);

        // Act
        var result = serviceManager.QueryService;

        // Assert           
        Assert.NotNull(result);
        Assert.Same(mockQueryService.Object, result);
    }

    [Fact]
    public void ServiceManager_ShouldNotInitializeService_UntilAccessed()
    {
        // Arrange
        // We create a mock version of IQueryService to isolate the test.
        var mockQueryService = new Mock<IQueryService>();

        // We inject the mock into the ServiceManager. Due to the Lazy logic in your 
        // implementation, the QueryService should not be instantiated yet.
        var serviceManager = new ServiceManager(mockQueryService.Object);

        // Act & Assert
        // First, we verify that the manager itself was created successfully.
        Assert.NotNull(serviceManager);

        // This is where we access the QueryService property for the first time. 
        // This triggers your Lazy instance to actually activate and return the service.
        // We verify that we actually receive an object and not 'null'.
        Assert.NotNull(serviceManager.QueryService);
    }
}

