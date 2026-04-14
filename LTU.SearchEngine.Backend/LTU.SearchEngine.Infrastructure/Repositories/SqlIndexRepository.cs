using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Infrastructure.Repositories;

/// <summary>
/// Handles database operations for the search engine´s indexing against a SQL database.
/// Uses IDbContextFactory to ensure thread safety during parallel web crawling.
/// </summary>
public class SqlIndexRepository : IIndexRepository
{
    private readonly IDbContextFactory<SearchDbContext> _factory;

    public SqlIndexRepository(IDbContextFactory<SearchDbContext> factory)
    {
        _factory = factory;
    }

    // ToDo: method AddDocumentAsync needs to be refactored and extract smaller portions of the code into smaller private methods
    // Saves a fully processed document from the pipeline to the database.
    // Merge words from the title, headers and body content.
    public async Task AddDocumentAsync(IndexDocument document)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Check if the Page already exists in the database
        var page = await context.Pages.FirstOrDefaultAsync(p => p.Url.Equals(document.Url));

        if (page is null)
        {
            page = new Page
            {
                Url = document.Url,
                Title = document.Title,
                LastCrawled = document.LastCrawl,
                ContentHash = document.ContentHash,
                WordCount = document.TotalWordCount,
                Language = document.Language
            };

            context.Pages.Add(page);
        }
        else
        {
            // Remove old word frequencies/positions and page links for the page before adding new ones to avoid duplicates
            var oldPageWordFrequencies = context.PageWordFrequencies.Where(pwf => pwf.PageId.Equals(page.Id));
            context.PageWordFrequencies.RemoveRange(oldPageWordFrequencies);
            
            var oldPageWordPositions = context.PageWordPositions.Where(pwf => pwf.PageId.Equals(page.Id));
            context.PageWordPositions.RemoveRange(oldPageWordPositions);
            
            var oldLinkEntries = context.PageLinks.Where(pl => pl.FromPageId.Equals(page.Id));
            context.PageLinks.RemoveRange(oldLinkEntries);

            // Update page metadata
            page.Title = document.Title;
            page.LastCrawled = document.LastCrawl;
            page.ContentHash = document.ContentHash;
            page.WordCount = document.TotalWordCount;
            page.Language = document.Language;            
        }

        // Save to get a Page.Id 
        await context.SaveChangesAsync();

        // -------------------------------- Handle WordFrequency relation --------------------------------------   
        var allUniqueTerms = document.TitleTerms.Keys
            .Union(document.HeaderTerms.Keys)
            .Union(document.ContentTerms.Keys)
            .ToList();

        // Retrieve any Terms that already exist in the database.
        var existingTerms = await context.Terms
            .Where(t => allUniqueTerms.Contains(t.Word))
            .ToDictionaryAsync(t => t.Word);

        bool hasNewTerms = false;

        foreach (var term in allUniqueTerms)
        {
            // Creates new Terms if current Term did not exist. 
            if (!existingTerms.TryGetValue(term, out var termEntity))
            {
                termEntity = new Term { Word = term };
                context.Terms.Add(termEntity);
                existingTerms[term] = termEntity;
                hasNewTerms = true;
            }
        }

        if (hasNewTerms) await context.SaveChangesAsync();


        foreach (var term in allUniqueTerms)
        {
            var termEntity = existingTerms[term];

            document.TitleTerms.TryGetValue(term, out int titleFrequency);
            document.HeaderTerms.TryGetValue(term, out int headerFrequency);
            document.ContentTerms.TryGetValue(term, out int contentFrequency);

            var pageWordFrequency = new PageWordFrequency
            {
                PageId = page.Id,
                TermId = termEntity.Id,
                TitleFrequency = titleFrequency,
                HeaderFrequency = headerFrequency,
                BodyFrequency = contentFrequency
            };

            context.PageWordFrequencies.Add(pageWordFrequency);
        }

        // -------------------------------- Handle PageWordPosition relation --------------------------------------   

        var sourceTermPosition = new Dictionary<TermSource, IReadOnlyList<string>>();
        sourceTermPosition[TermSource.Title] = document.TitleTermPositions;
        sourceTermPosition[TermSource.Header] = document.HeaderTermPositions;
        sourceTermPosition[TermSource.Body] = document.ContentTermPositions;

        foreach (var source in sourceTermPosition)
        {

            for (int i = 0; i < source.Value.Count; i++)
            {
                var currentTerm = source.Value[i];

                if (existingTerms.TryGetValue(currentTerm, out var termEntity))
                {
                    var pageWordPosition = new PageWordPosition
                    {
                        PageId = page.Id,
                        TermId = termEntity.Id,
                        Position = i,
                        TermSource = source.Key
                    };   

                    context.PageWordPositions.Add(pageWordPosition);
                }
            }
        }


        // -------------------------------- Handle PageLink relation -------------------------------------------
        if (document.OutgoingLinks is not null && document.OutgoingLinks.Any())
        {
            var targetLinks = document.OutgoingLinks.Distinct().ToList();
            var existingTargetPages = await context.Pages
                .Where(p => targetLinks.Contains(p.Url))
                .ToDictionaryAsync(p => p.Url);        

            foreach (var url in targetLinks)
            {
                // If no such page exist yet, create a stub in wait of crawl
                if (!existingTargetPages.TryGetValue(url, out var targetStubPage))
                {
                    targetStubPage = new Page
                    {
                      Url = url,
                      Title = "pending..",
                      ContentHash = string.Empty,
                      Language = string.Empty
                    };

                    context.Pages.Add(targetStubPage);
                    existingTargetPages[url] = targetStubPage;
                }

                context.PageLinks.Add( new PageLink
                {
                    FromPage = page,
                    ToPage = targetStubPage   
                });
            }
        }

        // save everything at the same time        
        await context.SaveChangesAsync(); 
    }


    /// <summary>
    /// Finds the IDs of all pages containing a specific word.
    /// </summary>
    public async Task<HashSet<int>> GetDocumentIdsForTermAsync(string term)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Go through the junction table PageWordFrequency to find matching pages
        return await context.PageWordFrequencies
            .Where(pwf => pwf.Term.Word == term)
            .Select(pwf => pwf.PageId)
            .ToHashSetAsync();
    }

	// Retrieves a list of documents based on their unique IDs
	public async Task<List<Page>> GetDocumentsByIdAsync(List<int> pageIds)
	{
		//Creates a new database context based on their unique IDs
		await using var context = await _factory.CreateDbContextAsync();

		return await context.Pages
			.Where(p => pageIds.Contains(p.Id))
			.ToListAsync();
	}

	public Task<HashSet<int>> GetDocumentIdsForPhraseAsync(PhraseNode<HashSet<int>> phrase)
	{
		throw new NotImplementedException();
	}

    public async Task<int?> GetExistingDocumentByHashAsync(string hash)
    {
        await using var context = await _factory.CreateDbContextAsync();
        
        return await context.Pages
            .Where(p => p.ContentHash.Equals(hash))
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateLastCrawledAsync(int id, DateTime newCrawl)
    {
        await using var context = await _factory.CreateDbContextAsync();
        await context.Pages
            .Where(p => p.Id.Equals(id))
            .ExecuteUpdateAsync(setter => 
                setter.SetProperty(p => p.LastCrawled, newCrawl)
            );
    }
}
