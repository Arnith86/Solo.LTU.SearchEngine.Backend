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

    // Saves a fully processed document from the pipeline to the database.
    // Merge words from the title, headers and body content.
    public async Task AddDocumentAsync(IndexDocument document)
    {
        await using var context = await _factory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var page = await GetOrCreatePageAsync(context, document);

            // Save to get a Page.Id 
            await context.SaveChangesAsync();

            var allUniqueTerms = document.TitleTerms.Keys
                .Union(document.HeaderTerms.Keys)
                .Union(document.ContentTerms.Keys)
                .ToList();

            // Retrieve any Terms that already exist in the database.
            var termLookup = await SynchronizeTermsAsync(context, allUniqueTerms, page.Language);

            AddWordFrequencies(context, page.Id, document, termLookup);
            AddWordPositions(context, page.Id, document, termLookup);
            await AddPageLinksAsync(context, page, document.OutgoingLinks);

            // save everything at the same time        
            await context.SaveChangesAsync(); 
            await transaction.CommitAsync();
        }
        catch 
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    private async Task<Page> GetOrCreatePageAsync(SearchDbContext context, IndexDocument document)
    {
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

        return page;
    }


    private async Task<Dictionary<string, Term>> SynchronizeTermsAsync(
        SearchDbContext context, 
        List<string> words, 
        string language
        )
    {
        var existingTerms = await context.Terms
            .Where(t => words.Contains(t.Word))
            .ToDictionaryAsync(t => t.Word);

        bool hasNewTerms = false;

        foreach (var term in words)
        {
            // Creates new Terms if current Term did not exist. 
            if (!existingTerms.TryGetValue(term, out var termEntity))
            {
                termEntity = new Term { Word = term, LanguageCode = language };
                context.Terms.Add(termEntity);
                existingTerms[term] = termEntity;
                hasNewTerms = true;
            }
        }

        if (hasNewTerms) await context.SaveChangesAsync();
        
        return existingTerms;
    }


    private void AddWordFrequencies(
        SearchDbContext context, 
        int pageId, IndexDocument doc, 
        Dictionary<string, Term> terms
        )
    {
        foreach (var word in terms.Keys)
        {
            doc.TitleTerms.TryGetValue(word, out int titleFreq);
            doc.HeaderTerms.TryGetValue(word, out int headerFreq);
            doc.ContentTerms.TryGetValue(word, out int bodyFreq);

            context.PageWordFrequencies.Add(new PageWordFrequency
            {
                PageId = pageId,
                TermId = terms[word].Id,
                TitleFrequency = titleFreq,
                HeaderFrequency = headerFreq,
                BodyFrequency = bodyFreq
            });
        }
    }

    private void AddWordPositions(
        SearchDbContext context, 
        int pageId, 
        IndexDocument doc, 
        Dictionary<string, Term> terms
        )
    {
        var sources = new Dictionary<TermSource, IReadOnlyList<string>>
        {
            { TermSource.Title, doc.TitleTermPositions },
            { TermSource.Header, doc.HeaderTermPositions },
            { TermSource.Body, doc.ContentTermPositions }
        };

        foreach (var source in sources)
        {
            for (int i = 0; i < source.Value.Count; i++)
            {
                if (terms.TryGetValue(source.Value[i], out var termEntity))
                {
                    context.PageWordPositions.Add(new PageWordPosition
                    {
                        PageId = pageId,
                        TermId = termEntity.Id,
                        Position = i,
                        TermSource = source.Key
                    });
                }
            }
        }
    }

    private async Task AddPageLinksAsync(SearchDbContext context, Page fromPage, IEnumerable<string> outgoingLinks)
    {
        if (outgoingLinks == null || !outgoingLinks.Any()) return;

        var targetUrls = outgoingLinks.Distinct().ToList();
        var existingPages = await context.Pages
            .Where(p => targetUrls.Contains(p.Url))
            .ToDictionaryAsync(p => p.Url);

        foreach (var url in targetUrls)
        {
            if (!existingPages.TryGetValue(url, out var targetPage))
            {
                targetPage = new Page { Url = url, Title = "pending..", ContentHash = "", Language = "" };
                context.Pages.Add(targetPage);
                existingPages[url] = targetPage;
            }

            context.PageLinks.Add(new PageLink { FromPage = fromPage, ToPage = targetPage });
        }
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
