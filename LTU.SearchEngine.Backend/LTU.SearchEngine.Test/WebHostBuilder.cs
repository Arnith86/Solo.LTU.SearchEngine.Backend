using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace LTU.SearchEngine.Test;

public class WebHostBuilder
{
	public HttpClient BuildHttpClient()
	{
		// Arrange: create in-memory web app
		var builder = new HostBuilder()
			.ConfigureWebHost(webHost =>
			{
				webHost.UseTestServer();

				webHost.ConfigureServices(services =>
				{
					// Register routing services
					services.AddRouting();
				});

				webHost.Configure(app =>
				{
					var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestFiles", "Documents");

					app.UseRouting();

					app.UseEndpoints(endpoints =>
					{
						// Map any file in TestData by its filename
						endpoints.MapGet("/{filename}", async context =>
						{
							var fileName = context.Request.RouteValues["filename"]?.ToString();

							if (string.IsNullOrEmpty(fileName))
							{
								context.Response.StatusCode = 400;
								await context.Response.WriteAsync("Filename missing");
								return;
							}

							var filePath = Path.Combine(testDataPath, fileName);

							if (!File.Exists(filePath))
							{
								context.Response.StatusCode = 404;
								await context.Response.WriteAsync("File not found");
								return;
							}

							// Set content type based on file extension
							var contentType = fileName.EndsWith(".html") ? "text/html" :
											  fileName.EndsWith(".pdf") ? "application/pdf" :
											  "application/octet-stream";

							context.Response.ContentType = contentType;

							await context.Response.SendFileAsync(filePath);
						});
					});
				});
			});

		var host = builder.Start();
		
		return host.GetTestClient();
	}
}
