using System.Net;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.Crawling.Tests.Model;

public class RawCrawlDataTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var url = "https://ltu.se";
        var timeTaken = 450L;
        var statusCode = HttpStatusCode.OK;
        var content = "<html></html>"u8.ToArray();
        var contentType = "text/html";
        var charSet = "utf-8";

        // Act
        var sut = new RawCrawlData(url, timeTaken, statusCode, content, contentType, charSet);

        // Assert
        Assert.Equal(url, sut.Url);
        Assert.Equal(timeTaken, sut.TimeTaken);
        Assert.Equal(statusCode, sut.HttpStatusCode);
        Assert.Equal(content, sut.Content);
        Assert.Equal(contentType, sut.ContentType);
        Assert.Equal(charSet, sut.CharSet);
    }

    [Fact]
    public void Equality_TwoInstancesWithSameData_ShouldBeEqual()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };
        var data1 = new RawCrawlData("https://ltu.se", 100, HttpStatusCode.OK, content, "text/html", "utf-8");
        var data2 = new RawCrawlData("https://ltu.se", 100, HttpStatusCode.OK, content, "text/html", "utf-8");

        // Act & Assert
        Assert.Equal(data1, data2);
        Assert.True(data1 == data2);
    }

    [Fact]
    public void Equality_InstancesWithDifferentContent_ShouldNotBeEqual()
    {
        // Arrange
        var data1 = new RawCrawlData("https://a.se", 1, HttpStatusCode.OK, new byte[] { 1 }, "text/plain", null);
        var data2 = new RawCrawlData("https://b.se", 1, HttpStatusCode.OK, new byte[] { 1 }, "text/plain", null);

        // Act & Assert
        Assert.NotEqual(data1, data2);
        Assert.False(data1 == data2);
    }

    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = new RawCrawlData("https://ltu.se", 100, HttpStatusCode.OK, new byte[] { 0 }, "text/html", "utf-8");

        // Act
        var modified = original with { HttpStatusCode = HttpStatusCode.NotFound, TimeTaken = 999 };

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, modified.HttpStatusCode);
        Assert.Equal(999, modified.TimeTaken);
        Assert.Equal(original.Url, modified.Url); // Unchanged property remains same
        Assert.NotEqual(original, modified);
    }

    [Fact]
    public void Constructor_ShouldAllowNullCharSet()
    {
        // Arrange & Act
        var sut = new RawCrawlData("https://ltu.se", 50, HttpStatusCode.OK, Array.Empty<byte>(), "application/json", null);

        // Assert
        Assert.Null(sut.CharSet);
    }
}