using LTU.SearchEngine.Backend.Core.Entities;

namespace LTU.SearchEngine.Backend.Core.Model.Entities;

public class PdfMetaDate
{
    public int PageId { get; set; } 
    public string PdfVersion { get; set; } = string.Empty;
    public string EncodingType { get; set; } = string.Empty;
    public Page Page { get; set; } = null!;
}