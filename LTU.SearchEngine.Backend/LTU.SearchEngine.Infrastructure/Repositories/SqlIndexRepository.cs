using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Repositories;

/// <summary>
/// Handles database operations for the search engine´s indexing against a SQL database.
/// Uses IDbContextFactory to ensure thread safety during parallel web crawling.
/// </summary>
public class SqlIndexRepository : IIndexRepository
{
    private readonly IDbContextFactory<SearchDbContext> _factory;
    private readonly SemaphoreProvider _semaphoreProvider;

    public SqlIndexRepository(IDbContextFactory<SearchDbContext> factory)
    public SqlIndexRepository(IDbContextFactory<SearchDbContext> factory, SemaphoreProvider semaphoreProvider)
    {
        _factory = factory;
        _semaphoreProvider = semaphoreProvider;
    }

    // Saves a fully processed document from the pipeline to the database.
    // Merge words from the title, headers and body content.
    public async Task AddDocumentAsync(IndexDocument document)
    {

        // Normalize all terms to ensure consistency
        var normalizedTitleTerms = document.TitleTerms.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant().Normalize(NormalizationForm.FormC), 
            kvp => kvp.Value);
        var normalizedHeaderTerms = document.HeaderTerms.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant().Normalize(NormalizationForm.FormC), 
            kvp => kvp.Value);
        var normalizedContentTerms = document.ContentTerms.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant().Normalize(NormalizationForm.FormC), 
            kvp => kvp.Value);
        var normalizedTitleTermPositions = document.TitleTermPositions
            .Select(w => w.ToLowerInvariant().Normalize(NormalizationForm.FormC))
            .ToList();
        var normalizedHeaderTermPositions = document.HeaderTermPositions
            .Select(w => w.ToLowerInvariant().Normalize(NormalizationForm.FormC))
            .ToList();
        var normalizedContentTermPositions = document.ContentTermPositions
            .Select(w => w.ToLowerInvariant().Normalize(NormalizationForm.FormC))
            .ToList();

        var allUniqueTerms = normalizedTitleTerms.Keys
            .Union(normalizedHeaderTerms.Keys)
            .Union(normalizedContentTerms.Keys)
            .ToList();



        await using var context = await _factory.CreateDbContextAsync();

        var termLookup = await SynchronizeTermsAsync(context, allUniqueTerms, document.Language);
        
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
            AddWordFrequencies(context, page.Id, normalizedTitleTerms, normalizedHeaderTerms, normalizedContentTerms, termLookup);
            AddWordPositions(context, page.Id, normalizedTitleTermPositions, normalizedHeaderTermPositions, normalizedContentTermPositions, termLookup);
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


    private async Task<Dictionary<string, LTU.SearchEngine.Backend.Core.Model.Entities.Term>> SynchronizeTermsAsync(
        SearchDbContext context, 
        List<string> words, 
        string language
        )
    {
        var cleanWords = words
        .Distinct()
        .ToList();

        // using var context = await _factory.CreateDbContextAsync();
        
        await _semaphoreProvider.GetTermSyncSemaphore().WaitAsync();
        try
        {
            var existingTerms = await context.Terms
            .Where(t => cleanWords.Contains(t.Word) && t.LanguageCode == language)
            .ToDictionaryAsync(t => t.Word);

            bool hasNewTerms = false;

            foreach (var term in cleanWords)
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
        finally
        {
            _semaphoreProvider.GetTermSyncSemaphore().Release();
        }
    }


    private void AddWordFrequencies(
        SearchDbContext context, 
        int pageId, 
        Dictionary<string, int> normalizedTitleTerms,
        Dictionary<string, int> normalizedHeaderTerms,
        Dictionary<string, int> normalizedContentTerms,
        Dictionary<string, LTU.SearchEngine.Backend.Core.Model.Entities.Term> terms
        )
    {
        foreach (var word in terms.Keys)
        {
            normalizedTitleTerms.TryGetValue(word, out int titleFreq);
            normalizedHeaderTerms.TryGetValue(word, out int headerFreq);
            normalizedContentTerms.TryGetValue(word, out int bodyFreq);

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
        IReadOnlyList<string> normalizedTitleTermPositions,
        IReadOnlyList<string> normalizedHeaderTermPositions,
        IReadOnlyList<string> normalizedContentTermPositions,
        Dictionary<string, LTU.SearchEngine.Backend.Core.Model.Entities.Term> terms
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
}
