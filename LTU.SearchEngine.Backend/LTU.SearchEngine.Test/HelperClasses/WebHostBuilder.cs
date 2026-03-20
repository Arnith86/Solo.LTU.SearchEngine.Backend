using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace LTU.SearchEngine.Test.HelperClasses;

public class WebHostBuilder
{
public HttpClient CreateFakeInternetClient()
	{
		var host = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
		{
			webBuilder.UseTestServer();
			webBuilder.Configure(app =>
			{
				app.UseRouting();
				app.UseEndpoints(endpoint =>
				{
					endpoint.MapGet("/seed.html", () =>
					{
						var html = $"""
						<html>
							<body>
								<a href="http://localhost/page1.html">Page 1</a>
							</body>
						</html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/page1.html", () =>
					{
						var html = $"""
						<html>
							<body>
								<a href="http://localhost/page2.html">Page 2</a>
							</body>
						</html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/page2.html", () =>
					{
						var html = $"""
						<html>
							<body>
								<a href="http://localhost/final.html">Page 2</a>
							</body>
						</html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/final.html", () =>
					{
						var html = $"""
						<html>
							<body>
								<h1>Final</h1>
							</body>
						</html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					// Seed Url with external domain
					endpoint.MapGet("/SeedIncludingExternalUrl.html", () =>
					{
						var html = $"""
							<html>
								<body>
									<a href="http://localhost/page1.html">Page 1</a>
									<a href="http://external-domain.com/external.html">external-domain</a>
								</body>
							</html>
						""";
						
						return Results.Content(html, "text/html");
					
					});  
					

					// Seed Url with external domain
					endpoint.MapGet("/external.html", () =>
					{
						var html = $"""
							<html>
								<body>
									<h1>External</h1>
								</body>
							</html>
						""";

						return Results.Content(html, "text/html");

					});
				});
			});
		})
		.Build();
		
		host.Start();
		return host.GetTestClient();
	}

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
