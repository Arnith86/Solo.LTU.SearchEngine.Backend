using LTU.SearchEngine.Backend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Backend.Core.Model.Entities
{
    public class PageWordFrequency
    {
            public int PageId { get; set; }
            public Page Page { get; set; } 

            public int TermId { get; set; }
            public Term Term { get; set; } 

            public int Frequency { get; set; }

            public double TfWeight { get; set; }
    }
}
