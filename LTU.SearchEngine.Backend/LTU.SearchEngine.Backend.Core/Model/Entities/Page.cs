using LTU.SearchEngine.Backend.Core.Model.Entities;

namespace LTU.SearchEngine.Backend.Core.Entities;

public class Page
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime LastCrawled { get; set; }
    public double PageRankScore { get; set; } = 1.0;
    public string ContentHash { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int HttpStatus { get; set; }
    public string Language { get; set; } = string.Empty;


    public int HtmlMetaDataId { get; set; }
    public int PdfMetaDataId { get; set; }

    public HtmlMetaData HtmlMetaData { get; set; } = null!;
    public PdfMetaData PdfMetaData { get; set; } = null!;


    public ICollection<PageWordFrequency> WordFrequencies { get; set; } = new List<PageWordFrequency>();
    public ICollection<PageWordPosition> PagePositions { get; set; } = new List<PageWordPosition>();
    public ICollection<PageLink> OutgoingLinks { get; set; } = new List<PageLink>();
    public ICollection<PageLink> IncomingLinks { get; set; } = new List<PageLink>();
}
