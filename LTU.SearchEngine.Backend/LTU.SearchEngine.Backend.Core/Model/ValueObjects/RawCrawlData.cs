using System.Net;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public record RawCrawlData(
    string Url, 
    long TimeTaken,
    HttpStatusCode HttpStatusCode,
    byte[] Content, 
    string ContentType,
    string? CharSet
);