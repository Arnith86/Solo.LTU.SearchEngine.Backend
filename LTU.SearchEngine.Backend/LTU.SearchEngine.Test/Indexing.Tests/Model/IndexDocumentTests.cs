using LTU.SearchEngine.Test.HelperClasses;

namespace LTU.SearchEngine.Test.Indexing.Tests.Model;

public class IndexDocumentTests
{

    private IndexDocument CreateTestDocument(
        IEnumerable<string> outgoingLinks,
        Dictionary<string, int> titleTerms, 
        Dictionary<string, int> headerTerms, 
        Dictionary<string, int> contentTerms,
        List<string> titleTermsPositions, 
        List<string> headerTermsPositions, 
        List<string> contentTermPositions
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
            titleTermPositions: titleTermsPositions,
            headerTermPositions: headerTermsPositions,
            contentTermPositions: contentTermPositions,
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
        var headerTerms = new Dictionary<string, int> { { "fortuning", 1 }, { "fun", 1 }, { "fast", 1} };
        var contentTerms = new Dictionary<string, int> { { "student", 2 }, { "mark", 1 }, { "dark", 1 }};
        var titleTermPositions = new List<string> { "ltu" };
        var headerTermPositions = new List<string> {  "fortuning", "fun", "fast" };
        var contentTermPositions = new List<string> { "student", "mark", "dark", "student" };
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
            titleTermPositions: titleTermPositions,
            headerTermPositions: headerTermPositions,
            contentTermPositions: contentTermPositions, 
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
        Assert.Equal("ltu", titleTermPositions[0]);

        Assert.True(sut.HeaderTerms.ContainsKey("fortuning"));
        Assert.Equal(1, sut.HeaderTerms["fortuning"]);
        Assert.Equal("fortuning", headerTermPositions[0]);
        
        Assert.True(sut.HeaderTerms.ContainsKey("fun"));
        Assert.Equal(1, sut.HeaderTerms["fun"]);
        Assert.Equal("fun", headerTermPositions[1]);
        
        Assert.True(sut.HeaderTerms.ContainsKey("fast"));
        Assert.Equal(1, sut.HeaderTerms["fast"]);
        Assert.Equal("fast", headerTermPositions[2]);

        Assert.True(sut.ContentTerms.ContainsKey("student"));
        Assert.Equal(2, sut.ContentTerms["student"]);
        Assert.Equal("student", contentTermPositions[0]);
        Assert.Equal("student", contentTermPositions[3]);
        
        Assert.True(sut.ContentTerms.ContainsKey("mark"));
        Assert.Equal(1, sut.ContentTerms["mark"]);
        Assert.Equal("mark", contentTermPositions[1]);
        
        Assert.True(sut.ContentTerms.ContainsKey("dark"));
        Assert.Equal(1, sut.ContentTerms["dark"]);
        Assert.Equal("dark", contentTermPositions[2]);

        
    }


    [Fact]
    public void TotalWordCount_ShouldCalculateSumOfAllFrequencies()
    {
        // Arrange
        var outgoingLinks = new List<string> { "dummyLink" };
        var titleTerms = new Dictionary<string, int> { { "a", 2 }, { "b", 3 } }; // 5 words
        var headerTerms = new Dictionary<string, int> { { "c", 10 } };            // 10 words
        var contentTerms = new Dictionary<string, int> { { "d", 1 }, { "e", 4 } }; // 5 words
        var titleTermPositions = new List<string> { { "ltu" } };
        var headerTermPositions = new List<string> { { "fortuning" } };
        var contentTermPositions = new List<string> { { "student" } };
        
        var doc = CreateTestDocument(
            outgoingLinks, 
            titleTerms, 
            headerTerms, 
            contentTerms,
            titleTermPositions,
            headerTermPositions,
            contentTermPositions
        );

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
        var emptyDictionary = new Dictionary<string, int>();
        var emptyList = new List<string>();
        
        var doc = CreateTestDocument(
            outgoingLinks, 
            emptyDictionary, emptyDictionary, emptyDictionary, 
            emptyList, emptyList, emptyList
        );

        // Act
        var result = doc.TotalWordCount;

        // Assert
        Assert.Equal(0, result);
    }
}
