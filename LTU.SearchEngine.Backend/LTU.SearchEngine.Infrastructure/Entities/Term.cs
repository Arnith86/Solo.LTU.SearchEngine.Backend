using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Entities
{
    public class Term
    {
        public int Id { get; set; }

        public string Word { get; set; } = string.Empty;

        public double IdfScore { get; set; }

        public ICollection<PageWordFrequency> PageFrequencies { get; set; }

    }
}
