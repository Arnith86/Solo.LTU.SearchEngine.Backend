using System.Net;
using Moq;
using Moq.Protected;

namespace LTU.SearchEngine.Test.HelperClasses;

public static class HttpResponseHelper
{
    public static void SetupHttpResponse(Mock<HttpMessageHandler> handlerMock, HttpStatusCode code, string content)
    {
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = code,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "text/html")
            });
    }
}