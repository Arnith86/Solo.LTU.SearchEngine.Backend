using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Entities
{
    public class Term
    {
        // ER: id integer (PK)
        public int Id { get; set; }

        // ER: normalized_word varchar UNIQUE
        // Detta är det "tvättade" ordet (t.ex. "running" -> "run")
        public string Word { get; set; } = string.Empty;

        // ER: idf_score float
        // Inverse Document Frequency - används för att veta hur unikt ordet är.
        // Vi använder double för matematikens skull i C#.
        public double IdfScore { get; set; }

        // --- RELATIONER (Dessa aktiverar vi i Subtask 3) ---
        // ER: Koppling till page_word_frequency
        // public ICollection<PageWordFrequency> PageFrequencies { get; set; }

    }
}
