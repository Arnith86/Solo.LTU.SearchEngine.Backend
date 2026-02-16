using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Backend.Core.Model.Entities
{
    public class Term
    {
        public int Id { get; set; }

        public string Word { get; set; } = string.Empty;

        public double IdfScore { get; set; }

        public ICollection<PageWordFrequency> PageFrequencies { get; set; }

    }
}
