using LTU.SearchEngine.Backend.Core.Entities;

namespace LTU.SearchEngine.Backend.Core.Model.Entities;

public class PageLink
{
    public int FromPageId { get; set; }
    public Page FromPage { get; set; } = null!;

    public int ToPageId { get; set; }
    public Page ToPage { get; set; } = null!;
}

