using LTU.SearchEngine.Backend.Core.Entities;
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

        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IDbContextFactory<SearchDbContext>>();

        using (var context = _factory.CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        _sut = new SqlIndexRepository(_factory);
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
            url: "https://ltu.se", 
            title: "LTU Start",
            titleTerms: new Dictionary<string, int>(){{"student", 1},{"ltu", 1}},
            headerTerms: new Dictionary<string, int>(){{"student", 3}},
            contentTerms: new Dictionary<string, int>(){{"utbildning", 2}}
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

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}