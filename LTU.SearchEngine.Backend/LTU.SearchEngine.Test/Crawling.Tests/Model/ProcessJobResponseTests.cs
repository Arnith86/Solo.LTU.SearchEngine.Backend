using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Test.HelperClasses;

namespace LTU.SearchEngine.Test.Crawling.Tests.Model;

public class ProcessJobResponseTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var changedContent = true;
        var processedAt = DateTime.UtcNow;
        var crawlResult = CrawlResultBuilder.BuildCrawlResult();

        // Act 
        var sut = new ProcessJobResponse(changedContent, processedAt, crawlResult);

        // Assert
        Assert.Equal(changedContent, sut.ChangedContent);
        Assert.Equal(processedAt, sut.ProcessedAt);
        Assert.Equal(crawlResult, sut.CrawlResult);
    }

    [Fact]
    public void Equality_TwoInstancesWithSameData_ShouldBeEqual()
    {
        // Arrange
        var changedContent = true;
        var processedAt = DateTime.UtcNow;
        var crawlResult = CrawlResultBuilder.BuildCrawlResult();

        // Act 
        var sut1 = new ProcessJobResponse(changedContent, processedAt, crawlResult);
        var sut2 = new ProcessJobResponse(changedContent, processedAt, crawlResult);

        // Assert
        Assert.Equal(sut1, sut2);
    }

    [Fact]
    public void Equality_InstancesWithDifferentContent_ShouldNotBeEqual()
    {
        // Arrange
        var processedAt = DateTime.UtcNow;
        var crawlResult = CrawlResultBuilder.BuildCrawlResult();

        // Act 
        var sut1 = new ProcessJobResponse(ChangedContent: false, processedAt, crawlResult);
        var sut2 = new ProcessJobResponse(ChangedContent: true, processedAt, crawlResult);

        // Assert
        Assert.NotEqual(sut1, sut2);
    }

    [Fact]
    public void Constructor_ShouldAllowNullCrawlResult()
    {
        // Arrange & Act 
        var sut = new ProcessJobResponse(
            ChangedContent: false, 
            ProcessedAt: DateTime.UtcNow, 
            CrawlResult: null
        );

        // Assert
        Assert.Null(sut.CrawlResult);
    }

    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        // Arrange
        var changedContent = true;
        var processedAt = DateTime.UtcNow;
        var crawlResult = CrawlResultBuilder.BuildCrawlResult();
        var original = new ProcessJobResponse(changedContent, processedAt, crawlResult);

        // Act 
        var modified = original with {ChangedContent = true, ProcessedAt = DateTime.UtcNow};

        // Assert
        Assert.NotEqual(original, modified);
        Assert.Equal(original.CrawlResult, modified.CrawlResult);
        Assert.True(original.ProcessedAt < modified.ProcessedAt);       
    }
}