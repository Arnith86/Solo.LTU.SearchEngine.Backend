using LTU.SearchEngine.Backend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Entities
{
    public class PageLink
    {
        // ER: from_page_id integer
        // Sidan som länken finns PÅ
        public int FromPageId { get; set; }
        public Page FromPage { get; set; } // Navigering (EF Core)

        // ER: to_page_id integer
        // Sidan som länken pekar TILL
        public int ToPageId { get; set; }
        public Page ToPage { get; set; } // Navigering (EF Core)
    }
}
