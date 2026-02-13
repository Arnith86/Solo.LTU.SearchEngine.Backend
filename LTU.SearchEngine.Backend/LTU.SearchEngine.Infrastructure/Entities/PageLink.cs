using LTU.SearchEngine.Backend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Entities
{
    public class PageLink
    {
        public int FromPageId { get; set; }
        public Page FromPage { get; set; } 

        public int ToPageId { get; set; }
        public Page ToPage { get; set; } 
    }
}
