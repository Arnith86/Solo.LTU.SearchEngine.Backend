using LTU.SearchEngine.Backend.Core.Entities;

namespace LTU.SearchEngine.Backend.Core.Model.Entities;

public class HtmlMetaData
{
    public int PageId { get; set; }
    public string CharSet { get; set; } = string.Empty;
    public string Doctype { get; set; } = string.Empty;
    public Page Page { get; set; } = null!;  
}