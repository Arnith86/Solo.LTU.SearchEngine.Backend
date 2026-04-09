using System.Net;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class HttpResponseHelper
{
    public static void SetupHttpResponse(
        Mock<HttpMessageHandler> httpMessageHandlerMock, 
        HttpStatusCode httpStatusCode, 
        string content,
        string contentType = "text/html",
        string charSet = "utf-8")
    {
        var response = new HttpResponseMessage
        {
            StatusCode = httpStatusCode,
            Content = new StringContent(content, System.Text.Encoding.GetEncoding(charSet), contentType)
        };

        response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType){ CharSet = charSet };

        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);
    }
}