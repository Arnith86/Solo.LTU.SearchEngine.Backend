using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Repositories;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LTU.SearchEngine.Test;

public class SqlIndexRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<SearchDbContext> _factory;
    private readonly IIndexRepository _sut;

    public SqlIndexRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContextFactory<SearchDbContext>(options =>
            options.UseSqlite(_connection));
        services.AddSingleton<SemaphoreProvider>();

        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IDbContextFactory<SearchDbContext>>();
        var semaphoreProvider = provider.GetRequiredService<SemaphoreProvider>();

        using (var context = _factory.CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        _sut = new SqlIndexRepository(_factory, semaphoreProvider);
    }

    

    /// <summary>
    /// Verifies that a processed IndexDocument is saved correctly, and that 
    /// word frequencies from the title, content, and headers are summed up for the total.
    /// </summary>
    [Fact]
    public async Task AddDocumentAsync_ShouldMergeDictionaries_AndSaveCorrectly()
    {
        // Arrange
        // We add the word "student" to both title and content. 
        // Total frequency should be 1 + 3 = 4.
        var document = IndexDocumentBuilder.BuildIndexDocument(
            outgoingLinks: new List<string> { "dummyLink" },
            url: "https://ltu.se", 
            title: "LTU Start",
            language: "sv",
            titleTerms: new Dictionary<string, int>(){{"student", 1},{"ltu", 1}},
            headerTerms: new Dictionary<string, int>(){{"student", 3}},
            contentTerms: new Dictionary<string, int>(){{"utbildning", 2}},
            titleTermPositions: new List<string>(){{"student"},{"ltu"}},
            headerTermPositions: new List<string>(){{"student"}},
            contentTermPositions: new List<string>(){{"utbildning"}}
        );

        // Act
        await _sut.AddDocumentAsync(document);

        // Assert
        await using var context = await _factory.CreateDbContextAsync();

        var savedPage = await context.Pages.FirstOrDefaultAsync(p => p.Url == "https://ltu.se");
        Assert.NotNull(savedPage);
        Assert.Equal("LTU Start", savedPage.Title);

        // Total word count: 1 (student) + 1 (ltu) + 3 (student) + 2 (utbildning) = 7
        Assert.Equal(7, savedPage.WordCount);

        // Check that "student" was merged to frequency 4
        var studentTerm = await context.Terms.FirstOrDefaultAsync(t => t.Word == "student");
        Assert.NotNull(studentTerm);
        Assert.Equal("sv", studentTerm.LanguageCode);

        var frequency = await context.PageWordFrequencies
            .FirstOrDefaultAsync(pwf => pwf.PageId == savedPage.Id && pwf.TermId == studentTerm.Id);

        Assert.NotNull(frequency);
        int totalFrequency = frequency.TitleFrequency + frequency.HeaderFrequency + frequency.BodyFrequency;
        Assert.Equal(4, totalFrequency); // 1 from Title + 3 from Content
    }

    /// <summary>
    /// Verifies that the database can query and return the correct page IDs 
    /// based on a specific search term.
    /// </summary>
    [Fact]
    public async Task GetPageIdsContainingTermAsync_ShouldReturnCorrectPageIds()
    {
        // Arrange: Seed some test data directly into the database
        await using var setupContext = await _factory.CreateDbContextAsync();

        var page1 = new Page { Url = "https://page1.com", Title = "Page 1" };
        var page2 = new Page { Url = "https://page2.com", Title = "Page 2" };
        var termMatch = new Term { Word = "sökord" };
        var termNoMatch = new Term { Word = "annat" };

        setupContext.Pages.AddRange(page1, page2);
        setupContext.Terms.AddRange(termMatch, termNoMatch);
        await setupContext.SaveChangesAsync(); // Save to generate IDs

        // Link the search term to BOTH pages
        setupContext.PageWordFrequencies.Add(
            new PageWordFrequency { PageId = page1.Id, TermId = termMatch.Id, HeaderFrequency = 1 }
        );
        setupContext.PageWordFrequencies.Add(
            new PageWordFrequency { PageId = page2.Id, TermId = termMatch.Id, HeaderFrequency = 5 }
        );
        
        await setupContext.SaveChangesAsync();

        // Act
        var resultIds = await _sut.GetDocumentIdsForTermAsync("sökord");
        var noResultIds = await _sut.GetDocumentIdsForTermAsync("finns_inte");

        // Assert
        Assert.Equal(2, resultIds.Count); // Both pages contained the term
        Assert.Contains(page1.Id, resultIds);
        Assert.Contains(page2.Id, resultIds);

        Assert.Empty(noResultIds); // A non-existent word should return an empty list
    }

    /// <summary>
    /// Verifies that the correct page entities are retrieved from the database 
    /// when providing a specific list of page IDs.
    /// </summary>
    [Fact]
    public async Task GetPagesByIdsAsync_ShouldReturnRequestedPages()
    {
        // Arrange
        await using var setupContext = await _factory.CreateDbContextAsync();

        var page1 = new Page { Url = "https://test1.com", Title = "Test 1" };
        var page2 = new Page { Url = "https://test2.com", Title = "Test 2" };
        var page3 = new Page { Url = "https://test3.com", Title = "Test 3" };

        setupContext.Pages.AddRange(page1, page2, page3);
        await setupContext.SaveChangesAsync();

        var idsToFetch = new List<int> { page1.Id, page3.Id }; // We only want 1 and 3

        // Act
        var resultPages = await _sut.GetDocumentsByIdAsync(idsToFetch);

        // Assert
        Assert.Equal(2, resultPages.Count);
        Assert.Contains(resultPages, p => p.Url == "https://test1.com");
        Assert.Contains(resultPages, p => p.Url == "https://test3.com");
        Assert.DoesNotContain(resultPages, p => p.Url == "https://test2.com"); // We didn't ask for this one!
    }


    [Fact]
    public async Task GetExistingDocumentByHashAsync_ShouldReturnCorrectId_WhenHashMatches()
    {
        // Arrange
        var hash = "ABC123Hash";
  
        await using (var context = await _factory.CreateDbContextAsync())
        {
            context.Pages.Add(new Page 
                { 
                    Url = "https://hash.se", 
                    ContentHash = hash, 
                    Title = "T", 
                    Language = "sv" 
                }
            );

            await context.SaveChangesAsync();
        }

        // Act
        var resultId = await _sut.GetExistingDocumentByHashAsync(hash);
        var noMatch = await _sut.GetExistingDocumentByHashAsync("NON_EXISTENT");

        // Assert
        Assert.NotNull(resultId);
        Assert.Null(noMatch);
        Assert.Equal(resultId, 1);
    }


    [Fact]
    public async Task UpdateLastCrawledAsync_ShouldOnlyUpdateTimestamp()
    {
        // Arrange
        int pageId;
        var originalTime = DateTime.UtcNow.AddDays(-1);
        var newTime = DateTime.UtcNow;

        await using (var context = await _factory.CreateDbContextAsync())
        {
            var page = new Page {
                Url = "https://time.se", 
                LastCrawled = originalTime, 
                Title = "T",
                ContentHash = "X",
                Language = "sv" 
            };

            context.Pages.Add(page);

            await context.SaveChangesAsync();
            
            pageId = page.Id;
        }

        // Act
        await _sut.UpdateLastCrawledAsync(pageId, newTime);

        // Assert
        await using (var context = await _factory.CreateDbContextAsync())
        {
            var updatedPage = await context.Pages.FindAsync(pageId);
            Assert.Equal(newTime, updatedPage!.LastCrawled);
            Assert.Equal("T", updatedPage.Title); 
        }
    }


    [Fact]
    public async Task AddDocumentAsync_WhenPageExists_ShouldCleanupOldFrequenciesAndLinks()
    {
        // Arrange
        string oldTitle = "oldtitle";
        string oldHeader = "oldheader";
        string oldContent = "oldcontent";

        string newTitle = "newtitle";

        var url = "https://update.se";
        
        
        var doc1 = IndexDocumentBuilder.BuildIndexDocument(
            url: url, 
            titleTerms: new Dictionary<string, int> { { oldTitle, 1 } },
            headerTerms: new Dictionary<string, int> { { oldHeader, 1 } },
            contentTerms: new Dictionary<string, int> { { oldContent, 1 } },
            titleTermPositions: new List<string> { "term" },
            headerTermPositions: new List<string> { "term" },
            contentTermPositions: new List<string> { "term" },
            outgoingLinks: new List<string> { "dummyLink" }
        );

        await _sut.AddDocumentAsync(doc1);

        var doc2 = IndexDocumentBuilder.BuildIndexDocument(
            url: url, 
            titleTerms: new Dictionary<string, int> { { newTitle, 1 } },
            headerTerms: new Dictionary<string, int> { { oldHeader, 1 } },
            contentTerms: new Dictionary<string, int> { { oldContent, 1 } },
            titleTermPositions: new List<string> { "term" },
            headerTermPositions: new List<string> { "term" },
            contentTermPositions: new List<string> { "term" },
            outgoingLinks: new List<string> { "dummyLink" }
        );

        // Act
        await _sut.AddDocumentAsync(doc2);

        // Assert
        await using var context = await _factory.CreateDbContextAsync();
        
        var page = await context.Pages
            .Include(p => p.WordFrequencies)
            .ThenInclude(wf => wf.Term)
            .FirstOrDefaultAsync(p => p.Url == url);
        
        // Verify that old words are gone and new have taken their place 
        Assert.Equal(3, page!.WordFrequencies.Count);
        Assert.DoesNotContain(page.WordFrequencies, wf => wf.Term.Word.Equals("oldtitle"));
        Assert.Contains(page.WordFrequencies, wf => wf.Term.Word.Equals("newtitle"));
        Assert.Contains(page.WordFrequencies, wf => wf.Term.Word.Equals("oldheader"));
        Assert.Contains(page.WordFrequencies, wf => wf.Term.Word.Equals("oldcontent"));
    }



    [Fact]
    public async Task AddDocumentAsync_ShouldCreateStub_WhenOutgoingLinkIsToNewPage()
    {
        // Arrange
        var targetUrl = "https://stub-target.se";
        
        var doc = IndexDocumentBuilder.BuildIndexDocument(
            url: "https://source.se", 
            titleTerms: new Dictionary<string, int>(),
            headerTerms: new Dictionary<string, int>(),
            contentTerms: new Dictionary<string, int>(),
            titleTermPositions: new List<string>(),
            headerTermPositions: new List<string>(),
            contentTermPositions: new List<string>(),
            outgoingLinks: new List<string> { targetUrl }
        );

        // Act
        await _sut.AddDocumentAsync(doc);

        // Assert
        await using var context = await _factory.CreateDbContextAsync();
        
        var stubPage = await context.Pages
            .FirstOrDefaultAsync(p => p.Url == targetUrl);
        
        Assert.NotNull(stubPage);
        Assert.Equal("pending..", stubPage.Title);
        Assert.Empty(stubPage.ContentHash);
    }


    [Fact]
    public async Task AddDocumentAsync_ReIndexing_ShouldCleanupOldPositions()
    {
        // Arrange 
        var url = "http://cleanup-pos.se";
        
        var doc1 = IndexDocumentBuilder.BuildIndexDocument(
            url: url, 
            titleTerms: new Dictionary<string, int> { {"oldTerm", 1} },
            headerTerms: new Dictionary<string, int>(),
            contentTerms: new Dictionary<string, int>(),
            titleTermPositions: new List<string> { "oldTerm" },
            headerTermPositions: new List<string>(),
            contentTermPositions: new List<string>(),
            outgoingLinks: new List<string>() 
        );

        var doc2 = IndexDocumentBuilder.BuildIndexDocument(
            url: url, 
            titleTerms: new Dictionary<string, int>{ {"newTerm", 1} },
            headerTerms: new Dictionary<string, int>(),
            contentTerms: new Dictionary<string, int>(),
            titleTermPositions: new List<string> { "newTerm" },
            headerTermPositions: new List<string>(),
            contentTermPositions: new List<string>(),
            outgoingLinks: new List<string>() 
        );

        await _sut.AddDocumentAsync(doc1);

        // Act 
        await _sut.AddDocumentAsync(doc2);

        // Assert 
        await using var context = await _factory.CreateDbContextAsync();
        
        var positions = await context.PageWordPositions
            .Include(pwp => pwp.Term)
            .Where(pwp => pwp.Page.Url.Equals(url))
            .ToListAsync();

        Assert.Single(positions);
        Assert.Equal("newterm", positions[0].Term.Word);
        Assert.DoesNotContain(positions, pwp => pwp.Term.Word.Equals("oldterm"));
    }



    [Fact]
    public async Task AddDocumentAsync_SameWordAtSamePositionInDifferentSources_ShouldSaveAll()
    {
         // Arrange 
        var url = "http://cleanup-pos.se";
        
        var doc1 = IndexDocumentBuilder.BuildIndexDocument(
            url: url, 
            titleTerms: new Dictionary<string, int> { {"term", 1} },
            headerTerms: new Dictionary<string, int> { {"term", 2} },
            contentTerms: new Dictionary<string, int> { {"term", 1} },
            titleTermPositions: new List<string> { "term" },
            headerTermPositions: new List<string> { "term", "term" },
            contentTermPositions: new List<string> { "term" },
            outgoingLinks: new List<string>() 
        );

        // Act 
        await _sut.AddDocumentAsync(doc1);

        // Assert 
        await using var context = await _factory.CreateDbContextAsync();

        var positions = await context.PageWordPositions
            .Where(pwp => pwp.Page.Url.Equals(url) && pwp.Term.Word.Equals("term"))
            .ToListAsync();

        Assert.Equal(4, positions.Count);
        Assert.Contains(positions, p => p.TermSource.Equals(TermSource.Title) && p.Position.Equals(0));    
        Assert.Contains(positions, p => p.TermSource.Equals(TermSource.Header) && p.Position.Equals(1));    
        Assert.Contains(positions, p => p.TermSource.Equals(TermSource.Body) && p.Position.Equals(0));    
    }


    [Fact]
    public async Task AddDocumentAsync_WhenErrorOccurs_ShouldRollbackEverything()
    {
        // Arrange
        var invalidDoc = IndexDocumentBuilder.BuildIndexDocument(url: null!);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _sut.AddDocumentAsync(invalidDoc));

        await using var context = await _factory.CreateDbContextAsync();
        var pageCount = await context.Pages.CountAsync();
        
        Assert.Equal(0, pageCount);
    }


    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}