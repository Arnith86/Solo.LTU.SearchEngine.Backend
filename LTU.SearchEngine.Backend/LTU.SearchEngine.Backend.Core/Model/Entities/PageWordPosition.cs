using LTU.SearchEngine.Backend.Core.Entities;

namespace LTU.SearchEngine.Backend.Core.Model.Entities;

public class PageWordPosition
{
        public int PageId { get; set; }
        public Page Page { get; set; } = null!;
        public int TermId { get; set; }
        public Term Term { get; set; } = null!;
        public int Position { get; set; }
}

