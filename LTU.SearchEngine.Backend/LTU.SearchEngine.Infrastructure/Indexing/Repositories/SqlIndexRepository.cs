using LTU.SearchEngine.Backend.Core.Entities;
using LTU.SearchEngine.Infrastructure.Data;
using LTU.SearchEngine.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Repositories
{
    public class SqlIndexRepository : IIndexRepository
    {
        private readonly IDbContextFactory<SearchDbContext> _factory;

        public SqlIndexRepository(IDbContextFactory<SearchDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<Page>> GetPagesByIdsAsync(List<int> pageIds)
        {
            await using var context = await _factory.CreateDbContextAsync();

            return await context.Pages
                .Where(p => pageIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task SaveAsync(IndexDocument document)
        {
            // 1. Skapa en fräsch koppling för denna tråd
            await using var context = await _factory.CreateDbContextAsync();

            // 2. Slå ihop Title, Header och Content till en enda stor Dictionary
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

            // 3. Skapa och spara Sidan
            var page = new Page
            {
                Url = document.Url,

                // OBS: Eftersom IndexDocument saknar originaltiteln som sträng,
                // får vi lämna den tom så länge. (Se tips nedan!)
                Title = string.Empty,

                LastCrawled = DateTime.UtcNow,

                // Räkna ut totala antalet ord genom att plussa ihop alla frekvenser
                WordCount = totalWordFrequencies.Values.Sum()
            };

            context.Pages.Add(page);
            await context.SaveChangesAsync(); // Spara för att få ett Page.Id

            // 4. Loopa igenom den sammanslagna listan av ord
            foreach (var kvp in totalWordFrequencies)
            {
                string wordText = kvp.Key;
                int frequency = kvp.Value;

                // Kolla om ordet redan finns i databasen
                var term = await context.Terms.FirstOrDefaultAsync(t => t.Word == wordText);

                if (term == null)
                {
                    // Om ordet är helt nytt, spara in det i Term-tabellen
                    term = new Term { Word = wordText };
                    context.Terms.Add(term);
                    await context.SaveChangesAsync(); // Spara för att få ett Term.Id
                }

                // 5. Koppla Sidan till Ordet med rätt frekvens
                var pageWordFrequency = new PageWordFrequency
                {
                    PageId = page.Id,
                    TermId = term.Id,
                    Frequency = frequency
                };

                context.PageWordFrequencies.Add(pageWordFrequency);
            }

            // 6. Spara alla kopplingar i ett svep!
            await context.SaveChangesAsync();
        }

        public async Task AddDocumentAsync(string url, string title, List<string> words)
        {
            await using var context = await _factory.CreateDbContextAsync();

            // 1. Skapa och spara Sidan först
            var page = new Page
            {
                Url = url,
                Title = title,
                LastCrawled = DateTime.UtcNow,
                WordCount = words.Count
            };

            context.Pages.Add(page);
            // Vi sparar här för att få ut ett Page.Id som vi behöver till orden
            await context.SaveChangesAsync();

            // 2. Räkna ut frekvensen av varje ord (hur många gånger "hej" förekommer på sidan)
            var wordFrequencies = words
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // 3. Loopa igenom varje unikt ord
            foreach (var item in wordFrequencies)
            {
                string wordText = item.Key;
                int frequency = item.Value;

                // Kolla om ordet redan finns i databasen (så vi inte skapar dubbletter)
                var term = await context.Terms.FirstOrDefaultAsync(t => t.Word == wordText);

                if (term == null)
                {
                    // Om ordet inte finns, skapa det
                    term = new Term { Word = wordText };
                    context.Terms.Add(term);
                    // Spara direkt för att få ett Term.Id
                    await context.SaveChangesAsync();
                }

                // 4. Koppla ihop Sidan och Ordet i kopplingstabellen
                var pageWordFrequency = new PageWordFrequency
                {
                    PageId = page.Id,
                    TermId = term.Id,
                    Frequency = frequency
                };

                context.PageWordFrequencies.Add(pageWordFrequency);
            }

            // Spara alla kopplingar
            await context.SaveChangesAsync();
        }

        public async Task<List<int>> GetPageIdsContainingTermAsync(string term)
        {
            await using var context = await _factory.CreateDbContextAsync();

            // Vi går via kopplingstabellen PageWordFrequency
            return await context.PageWordFrequencies
                .Where(pwf => pwf.Term.Word == term)
                .Select(pwf => pwf.PageId)
                .ToListAsync();
        }


        public void Save(IndexDocument document)
        {
            throw new NotImplementedException();
        }

    }
}
