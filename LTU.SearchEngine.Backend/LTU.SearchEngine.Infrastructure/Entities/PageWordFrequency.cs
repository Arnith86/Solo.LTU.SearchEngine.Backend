using LTU.SearchEngine.Backend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Entities
{
    public class PageWordFrequency
    {
            // ER: page_id integer (Del av PK, FK)
            public int PageId { get; set; }
            public Page Page { get; set; } // Navigeringsobjekt för EF Core

            // ER: term_id integer (Del av PK, FK)
            public int TermId { get; set; }
            public Term Term { get; set; } // Navigeringsobjekt för EF Core

            // ER: frequency integer
            // Hur många gånger ordet förekommer på just denna sida.
            public int Frequency { get; set; }

            // ER: tf_weight float
            // (Term Frequency weight). Vi använder double för precision.
            // Detta värde räknas ut senare vid indexering.
            public double TfWeight { get; set; }
    }
}
