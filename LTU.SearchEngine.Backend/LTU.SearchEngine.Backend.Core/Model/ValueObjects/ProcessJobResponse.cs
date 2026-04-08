namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public record ProcessJobResponse(
    bool ChangedContent, 
    DateTime ProcessedAt,
    CrawlResult? CrawlResult
);