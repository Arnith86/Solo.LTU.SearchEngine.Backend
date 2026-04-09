using System.Net;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class RawCrawlDataBuilder{

    public static RawCrawlData BuildRawCrawlData(
        string url = "test page", 
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        string contentType = "text/html",
        string? charSet = "utf-8"
        )
    {
        long timeTaken = 500;
        byte[] content = Array.Empty<byte>(); 

        return new RawCrawlData(
            Url: url,
            TimeTaken: timeTaken,
            Content: content,
            HttpStatusCode: httpStatusCode,
            ContentType: contentType,
            CharSet: charSet
        );
    }

    public static RawCrawlData BuildRawCrawlData(
        byte[] content, 
        long timeTaken,
        string url = "test page", 
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        string contentType = "text/html",
        string? charSet = "utf-8"
    )
    {
        return new RawCrawlData(
            Url: url,
            TimeTaken: timeTaken,
            Content: content,
            HttpStatusCode: httpStatusCode,
            ContentType: contentType,
            CharSet: charSet
        );
    }
} 