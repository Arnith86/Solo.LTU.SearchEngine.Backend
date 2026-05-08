using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.Entities;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;


namespace LTU.SearchEngine.Infrastructure.Repositories;

/// <summary>
/// Handles database operations for the search engine´s indexing against a SQL database.
/// Uses IDbContextFactory to ensure thread safety during parallel web crawling.
/// </summary>
public class SqlIndexRepository : IIndexRepository
{
    private readonly IDbContextFactory<SearchDbContext> _factory;
    private readonly SemaphoreProvider _semaphoreProvider;

    public SqlIndexRepository(IDbContextFactory<SearchDbContext> factory, SemaphoreProvider semaphoreProvider)
    {
        _factory = factory;
        _semaphoreProvider = semaphoreProvider;
    }

    // Saves a fully processed document from the pipeline to the database.
    // Merge words from the title, headers and body content.
    public async Task AddDocumentAsync(IndexDocument document)
    {
        var allUniqueTerms = document.TitleTerms.Keys
            .Union(document.HeaderTerms.Keys)
            .Union(document.ContentTerms.Keys)
            .ToList();

        await using var context = await _factory.CreateDbContextAsync();

        var termLookup = await SynchronizeTermsAsync(context, allUniqueTerms, document.Language);
        
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var page = await GetOrCreatePageAsync(context, document);

            // Save to get a Page.Id 
            await context.SaveChangesAsync();

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


    // Retrieves a list of documents based on their unique IDs
    // public async Task<List<Page>> GetDocumentsByIdAsync(List<int> pageIds)
    public async Task<PaginatedResult<Page>> GetDocumentsByIdAsync(
        List<int> pageIds, 
        PaginationRequestParameters paginationParameters
        )
	{
		//Creates a new database context based on their unique IDs
		await using var context = await _factory.CreateDbContextAsync();

		return await context.Pages
			.Where(p => pageIds.Contains(p.Id))
            .ToPaginatedResultAsync(paginationParameters);
			// .ToListAsync();
	}

	public async Task<HashSet<int>> GetDocumentIdsForPhraseAsync(PhraseNode<HashSet<int>> phraseNode)
	{
        await using var context = await _factory.CreateDbContextAsync();

        var tokenStrings = phraseNode.Phrase
            .Select(t => t.Token).ToList();


        var firstTerm = tokenStrings[0];

        // First part of a deferred execution of the phrase search.
        var query = context.PageWordPositions
            .Where(pwp => pwp.Term.Word.Equals(firstTerm))
            .Select(pwp => new { pwp.PageId, firstWordPosition = pwp.Position});

        // Build the rest of the query by chaining the subsequent words using Inner Joins
        for (int i = 0; i < tokenStrings.Count; i++)
        {
            var currentTerm = tokenStrings[i];
            int offset = i;

            query = 
                from q in query
                join next in context.PageWordPositions on q.PageId equals next.PageId
                where next.Term.Word.Equals(currentTerm)  && next.Position == q.firstWordPosition + offset
                select new { q.PageId, q.firstWordPosition };
        }    
        
        var result = await query.Select(q => q.PageId).ToHashSetAsync();

		return result;
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


    /// <summary>
    /// Finds the IDs of all pages containing a specific word.
    /// </summary>
    public async Task<HashSet<int>> GetDocumentIdsForTermAsync(string term)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.PageWordFrequencies
            .Where(pwf => pwf.Term.Word == term)
            .Select(pwf => pwf.PageId)
            .ToHashSetAsync();
    }


    private async Task<Page> GetOrCreatePageAsync(SearchDbContext context, IndexDocument document)
    {
        // Check if the Page already exists in the database
        var page = await context.Pages
            .Include(p => p.HtmlMetaData)
            .Include(p => p.PdfMetaData)
            .FirstOrDefaultAsync(p => p.Url.Equals(document.Url));

        if (page is null)
        {
            page = new Page{ Url = document.Url };
            context.Pages.Add(page);
        }
        else
        {
            // Remove old word frequencies/positions and page links for the page before adding new ones to avoid duplicates
            ClearExistingPageData(context, page.Id);
        }

        // Update page metadata
        page.Title = document.Title;
        page.LastCrawled = document.LastCrawl;
        page.ContentHash = document.ContentHash;
        page.WordCount = document.TotalWordCount;
        page.Language = document.Language;
        SetDocumentMetaData(context, document, page);

        return page;
    }


    private static void SetDocumentMetaData(SearchDbContext context, IndexDocument document, Page page)
    {
        if (document.MetaData is HtmlDocumentMetaData html)
        {
            if (page.PdfMetaData is not null) context.PdfMetaEntries.Remove(page.PdfMetaData);
            if (page.HtmlMetaData is not null)
            {
                page.HtmlMetaData.CharSet = html.CharSet;
                page.HtmlMetaData.Doctype = html.DocType;
            }
            else
            {
                page.HtmlMetaData = new HtmlMetaData { CharSet = html.CharSet, Doctype = html.DocType };
            }
        }
        else if (document.MetaData is PdfDocumentMetaData pdf)
        {
            if (page.HtmlMetaData is not null) context.HtmlMetaEntries.Remove(page.HtmlMetaData);
            if (page.PdfMetaData is not null)
            {
                page.PdfMetaData.PdfVersion = pdf.PdfVersion;
                page.PdfMetaData.EncodingType = pdf.EncodingType;
            }
            else
            {
                page.PdfMetaData = new PdfMetaData { PdfVersion = pdf.PdfVersion, EncodingType = pdf.EncodingType };
            }
        }
    }


    private static void ClearExistingPageData(SearchDbContext context, int pageId)
    {
        context.PageWordFrequencies.RemoveRange(
            context.PageWordFrequencies.Where(pwf => pwf.PageId.Equals(pageId))
        );
        context.PageWordPositions.RemoveRange(
            context.PageWordPositions.Where(pwf => pwf.PageId.Equals(pageId))
        );
        context.PageLinks.RemoveRange(
            context.PageLinks.Where(pl => pl.FromPageId.Equals(pageId))
        );
    }


    private async Task<Dictionary<string, Term>> SynchronizeTermsAsync(
        SearchDbContext context, 
        List<string> words, 
        string language
        )
    {
        var cleanWords = words.Distinct().ToList();
        
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

        await _semaphoreProvider.GetPageSyncSemaphore().WaitAsync();
        try
        {
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
        finally
        {
            _semaphoreProvider.GetPageSyncSemaphore().Release();
        }
    }
}
