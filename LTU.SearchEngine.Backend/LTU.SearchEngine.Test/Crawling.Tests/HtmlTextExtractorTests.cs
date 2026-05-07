using System.Text;
using HtmlAgilityPack;

namespace LTU.SearchEngine.Test.Crawling;

public class HtmlTextExtractorTests
{
    [Theory]
    [InlineData("<li>Hem</li><li>Om oss</li>", "Hem Om oss ")]
    [InlineData("<div>Del 1</div><div>Del 2</div>", "Del 1 Del 2 ")]
    [InlineData("<p>Hello</p><br>World", "Hello World")]
    [InlineData("<h1>Rubrik</h1><p>Text</p>", "Rubrik Text ")]
    public void ExtractTextWithSpaces_BlockElements_ShouldHaveSpaces(string html, string expected)
    {
        // Arrange
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var sb = new StringBuilder();

        // Act
        HtmlTextExtractor.ExtractTextWithSpaces(doc.DocumentNode, sb);
        var result = sb.ToString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractTextWithSpaces_InlineElements_ShouldNotHaveExtraSpaces()
    {
        // Arrange
        var html = "<span>There</span><span>are</span><span>no</span><span>blocks</span>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var sb = new StringBuilder();

        // Act
        HtmlTextExtractor.ExtractTextWithSpaces(doc.DocumentNode, sb);

        // Assert
        Assert.Equal("Therearenoblocks", sb.ToString());
    }

    [Fact]
    public void ExtractTextWithSpaces_DeeplyNestedList_ShouldPreserveSeparation()
    {
        // Arrange
        var html = "<footer><ul><li>Kontakt</li><li>Support<div>FAQ</div></li></ul></footer>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var sb = new StringBuilder();

        // Act
        HtmlTextExtractor.ExtractTextWithSpaces(doc.DocumentNode, sb);
        var result = sb.ToString();

        // Assert
        // We check if key words exist with space between them
        Assert.Contains("Kontakt ", result);
        Assert.Contains("Support ", result);
        Assert.Contains("FAQ ", result);
    }


    [Fact]
    public void ExtractTextWithSpaces_EmptyNode_ShouldReturnEmpty()
    {
        // Arrange
        var doc = new HtmlDocument();
        doc.LoadHtml("");
        var sb = new StringBuilder();

        // Act
        HtmlTextExtractor.ExtractTextWithSpaces(doc.DocumentNode, sb);

        // Assert
        Assert.Empty(sb.ToString());
    }
}