using LTU.SearchEngine.Backend.Core.Entities;
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
    public async Task SaveAsync(IndexDocument document)
    {
        // 1. Creates a fresh context for this thread
        await using var context = await _factory.CreateDbContextAsync();

        // 2. Merge Title, Header and Content into a single large Dictionary
        var totalWordFrequencies = new Dictionary<string, int>();

        void MergeTerms(Dictionary<string, int> source)
        {
            foreach (var kvp in source)
            {
                if (totalWordFrequencies.ContainsKey(kvp.Key))
                    totalWordFrequencies[kvp.Key] += kvp.Value;
                else
                    totalWordFrequencies[kvp.Key] = kvp.Value;
            }
        }

        MergeTerms(document.TitleTerms);
        MergeTerms(document.HeaderTerms);
        MergeTerms(document.ContentTerms);

        // 3. Create the database entity for the Page
        var page = new Page
        {
            Url = document.Url,
            Title = document.Title,
            LastCrawled = DateTime.UtcNow,
            WordCount = totalWordFrequencies.Values.Sum()
        };

        context.Pages.Add(page);
        //Save imediatly to generate a ID
        await context.SaveChangesAsync(); 

        // 4. Iterate through all unique words and link them to the page
        foreach (var kvp in totalWordFrequencies)
        {
            string wordText = kvp.Key;
            int frequency = kvp.Value;

            // Check if the word already exists globally in the database
            var term = await context.Terms.FirstOrDefaultAsync(t => t.Word == wordText);

            if (term == null)
            {
                // If the word is new, add it to the Term table
                term = new Term { Word = wordText };
                context.Terms.Add(term);
                // Save immediately to generate a TermId
                await context.SaveChangesAsync();
            }

            // 5. Create the relationship between the Page and the Word (including frequency)
            var pageWordFrequency = new PageWordFrequency
            {
                PageId = page.Id,
                TermId = term.Id,
                Frequency = frequency
            };

            context.PageWordFrequencies.Add(pageWordFrequency);
        }

        // 6. Save all relationships to the database
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Alternative method to save a page and its words directly via parameters.
    /// </summary>
    public async Task AddDocumentAsync(string url, string title, List<string> words)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // 1. Create and save the Page first
        var page = new Page
        {
            Url = url,
            Title = title,
            LastCrawled = DateTime.UtcNow,
            WordCount = words.Count
        };

        context.Pages.Add(page);
        // Save here to get a Page.Id which we need for the words
        await context.SaveChangesAsync();

        // 2. Calculate the frequency of each word (how many times a word appears on the page)
        var wordFrequencies = words
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count());

        // 3. Loop through each unique word
        foreach (var item in wordFrequencies)
        {
            string wordText = item.Key;
            int frequency = item.Value;

            // Check if the word already exists in the database (to avoid duplicates)
            var term = await context.Terms.FirstOrDefaultAsync(t => t.Word == wordText);

            if (term == null)
            {
                // If the word does not exist, create it
                term = new Term { Word = wordText };
                context.Terms.Add(term);
                // Save immediately to get a Term.Id
                await context.SaveChangesAsync();
            }

            // 4. Link the Page and the Word in the junction table
            var pageWordFrequency = new PageWordFrequency
            {
                PageId = page.Id,
                TermId = term.Id,
                Frequency = frequency
            };

            context.PageWordFrequencies.Add(pageWordFrequency);
        }

        // Save all connections
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

		public Task<HashSet<int>> GetDocumentIdsForPhraseAsync(
        PhraseNode<HashSet<int>> phrase)
		{
			throw new NotImplementedException();
		}
	}
