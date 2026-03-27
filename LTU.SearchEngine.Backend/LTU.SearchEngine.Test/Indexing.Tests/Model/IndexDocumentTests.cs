using LTU.SearchEngine.Test.HelperClasses;

namespace LTU.SearchEngine.Test.Indexing.Tests.Model;

public class IndexDocumentTests
{

    private IndexDocument CreateTestDocument(
        IEnumerable<string> outgoingLinks,
        Dictionary<string, int> titleTerms, 
        Dictionary<string, int> headerTerms, 
        Dictionary<string, int> contentTerms
        )
    {
        return IndexDocumentBuilder.BuildIndexDocument(
            outgoingLinks: outgoingLinks,
            url: "https://test.com", 
            title: "Test", 
            language: "en", 
            titleTerms: titleTerms,
            headerTerms: headerTerms, 
            contentTerms: contentTerms, 
            contentHash: "hash", 
            lastCrawl: DateTime.UtcNow
        );
    }

    [Fact]
    public void Constructor_ShouldAssignPropertiesCorrectly()
    {
        // Arrange 
        var url = "https://ltu.se";
        var title = "Lulea Tennis University";
        var language = "sv";
        var links = new List<string> { "dummyLink" };
        var titleTerms = new Dictionary<string, int> { { "ltu", 1 } };
        var headerTerms = new Dictionary<string, int> { { "fortuning", 2 } };
        var contentTerms = new Dictionary<string, int> { { "student", 5 } };
        var hash = "ABC-123";
        var now = DateTime.UtcNow;

        // Act
        var sut = IndexDocumentBuilder.BuildIndexDocument(
            url: url, 
            title: title,
            language: language, 
            outgoingLinks: links,
            titleTerms: titleTerms, 
            headerTerms: headerTerms, 
            contentTerms: contentTerms, 
            contentHash: hash, 
            lastCrawl: now
        );

        // Assert 
        Assert.Equal(url, sut.Url);
        Assert.Equal(title, sut.Title);
        Assert.Equal(language, sut.Language);
        Assert.Equal(hash, sut.ContentHash);
        Assert.Equal(now, sut.LastCrawl);
        Assert.True(sut.TitleTerms.ContainsKey("ltu"));
        Assert.Equal(1, sut.TitleTerms["ltu"]);
    }

    [Fact]
    public void TotalWordCount_ShouldCalculateSumOfAllFrequencies()
    {
        // Arrange
        var outgoingLinks = new List<string> { "dummyLink" };
        var titleTerms = new Dictionary<string, int> { { "a", 2 }, { "b", 3 } }; // 5 words
        var headerTerms = new Dictionary<string, int> { { "c", 10 } };            // 10 words
        var contentTerms = new Dictionary<string, int> { { "d", 1 }, { "e", 4 } }; // 5 words
        
        var doc = CreateTestDocument(outgoingLinks, titleTerms, headerTerms, contentTerms);

        // Act
        var result = doc.TotalWordCount;

        // Assert
        // total: 2+3 + 10 + 1+4 = 20
        Assert.Equal(20, result);
    }

    [Fact]
    public void TotalWordCount_WithEmptyDictionaries_ShouldReturnZero()
    {
        // Arrange
        var outgoingLinks = new List<string> { "dummyLink" };
        var empty = new Dictionary<string, int>();
        var doc = CreateTestDocument(outgoingLinks, empty, empty, empty);

        // Act
        var result = doc.TotalWordCount;

        // Assert
        Assert.Equal(0, result);
    }
}
