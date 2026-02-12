using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Backend.Core.Entities
{
    public class Page
    {
        // ER: id integer (PK)
        public int Id { get; set; }

        // ER: url varchar UNIQUE
        // Detta är sidans unika adress.
        public string Url { get; set; } = string.Empty;

        // ER: title varchar
        // Sidans rubrik (<title>).
        public string Title { get; set; } = string.Empty;

        // ER: last_crawled datetime
        // När vi senast besökte sidan.
        public DateTime LastCrawled { get; set; }

        // ER: page_rank_score float DEFAULT 1.0
        // Används för ranking (Milestone 3). Vi sätter default till 1.0.
        public double PageRankScore { get; set; } = 1.0;

        // ER: content_hash varchar
        // En hash (t.ex. MD5) av innehållet för att upptäcka ändringar.
        public string ContentHash { get; set; } = string.Empty;

        // ER: word_count integer
        // Antal ord på sidan.
        public int WordCount { get; set; }

        // ER: http_status integer
        // Sparar t.ex. 200 (OK), 404 (Not Found) eller 301 (Redirect).
        // Diagrammet noterar: "used to clarify if a page is no longer reachable"
        public int HttpStatus { get; set; }

        // ER: language varchar(10)
        // T.ex. "en", "sv".
        public string Language { get; set; } = string.Empty;

        // --- RELATIONER (Dessa lägger vi till i nästa steg/Subtask 3) ---
        // public ICollection<PageWordFrequency> WordFrequencies { get; set; }
        // public ICollection<PageLink> OutgoingLinks { get; set; }
    }
}
