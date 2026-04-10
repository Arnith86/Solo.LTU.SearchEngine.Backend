using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace LTU.SearchEngine.Test.HelperClasses;

public class WebHostBuilder
{
	public Dictionary<string, string> DynamicContent { get; } = new();

	public HttpClient CreateFakeInternetClient(CallTracker? callTracker = null)
	{
		var host = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
		{
			webBuilder.UseTestServer();
			webBuilder.ConfigureServices(service => service.AddSingleton(callTracker ?? new CallTracker()));
			webBuilder.ConfigureServices(service => service.AddSingleton(DynamicContent));

			webBuilder.Configure(app =>
			{
				app.UseRouting();
				app.UseEndpoints(endpoint =>
				{	
					// Dynamic generic page
					endpoint.MapGet("/{page}.html", ( string page, CallTracker tracker, Dictionary<string, string> content) =>
					{
						var url = $"http://localhost/{page}.html";
						tracker.VisitedUrls.Add(url);

						if (content.TryGetValue(url, out var customHtml))
						{
							return Results.Content(customHtml, "text/html");
						}

						return Results.Content($"<h1>Default content for {page}</h1>", "text/html");
					});

					endpoint.MapGet("/robots.txt", (CallTracker tracker) =>
					{
						tracker.VisitedUrls.Add("/robots.txt");

						var html = $"""
							User-agent: TestBot
							Disallow: /private/
							Disallow: /ignoreThisRule/
						"""; 
						
						return Results.Content(html, "text/html");
					});

					endpoint.MapGet("/robots-test-start.html", (CallTracker tracker) =>
					{
						tracker.VisitedUrls.Add("/robots-test-start.html");

						var html = $"""
						<html>
							<body>
								<a href="http://localhost/public.html">Allowed Page</a>
								<a href="http://localhost/private/secret.html">Disallowed Page</a>
								<a href="http://localhost/ignoreThisRule/ignored.html">Ignored Page</a>
							</body>
						</html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/public.html", (CallTracker tracker) => 
					{ 
						tracker.VisitedUrls.Add("/public.html");
						return Results.Content("<h1>Public</h1>", "text/html");
					});

					endpoint.MapGet("/private/secret.html", (CallTracker tracker) =>  
					{
						tracker.VisitedUrls.Add("/private/secret.html");
						return Results.Content("<h1>Private</h1>", "text/html");
					});
						
					endpoint.MapGet("/ignoreThisRule/ignored.html", (CallTracker tracker) =>  
					{
						tracker.VisitedUrls.Add("/ignoreThisRule/ignored.html");
						return Results.Content("<h1>Ignored</h1>", "text/html");
					});
					
					endpoint.MapGet("/seed.html", () =>
					{
						var html = $"""
							<html><body><a href="http://localhost/page1.html">Page 1</a></body></html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/page1.html", () =>
					{
						var html = $"""
							<html><body><a href="http://localhost/page2.html">Page 2</a></body></html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/page2.html", () =>
					{
						var html = $"""
							<html><body><a href="http://localhost/final.html">Final Page</a></body></html>
						"""; 
						
						return Results.Content(html, "text/html");

					});

					endpoint.MapGet("/final.html", () =>
					{
						var html = $"""
							<html><body><h1>Final</h1></body></html>
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
							<html><body><h1>External</h1></body></html>
						""";

						return Results.Content(html, "text/html");

					});
					
					endpoint.MapGet("/ParserTestFile.html", () =>
					{
						var html = """
							<!DOCTYPE html>
								<html lang="en">
								<head>
									<meta charset="UTF-8">
									<title>Title Text</title>
								</head>
								<body>
									<h1>Headers</h1>
									
									<!-- Text that should be indexed -->
									<p>Paragraph</p>

									<!-- Content that should be ignored -->
									<script>var secret = "ScriptText";</script>
									<style>.hidden { display: none; color: red; }</style>
									<img src="imageName.jpg" alt="AltText">
									<video controls><source src="lecture.mp4" type="video/mp4"></video>
									<div style="display:none">HiddenText</div>
								</body>
							</html>
						""";

						return Results.Content(html, "text/html");

					});

					
					endpoint.MapGet("/InvertedIndexTestFile1.html", () =>
					{
						var html = """
							<head><title>Term1</title></head>
							<body>
								<p>Term3</p>
								<a href="http://localhost/InvertedIndexTestFile2.html">InvertedIndexTestFile2</a>
							</body>
						""";

						return Results.Content(html, "text/html");
					});
					
					
					endpoint.MapGet("/InvertedIndexTestFile2.html", () =>
					{
						var html = """
							<head><title>Term1</title></head>
							<body><h1>Term2</h1></body>
						""";

						return Results.Content(html, "text/html");
					});
					
					
					endpoint.MapGet("/IndexerNormalizingTextRun.html", () =>
					{
						var html = """
						<html lang="en">
							<body>
								<h1>Running run Run runs Runs</h1>
							</body>
						</html>
						""";

						return Results.Content(html, "text/html");
					});
					
					
					endpoint.MapGet("/IndexerNormalizingTextSwim.html", () =>
					{
						var html = """
						<html lang="en">
							<body>
								<h1>Swimming swims SwiMs SwIM swim</h1>
							</body>
						</html>
						""";
						return Results.Content(html, "text/html");
					});
					
					endpoint.MapGet("/IndexerNormalizingTextCat.html", () =>
					{
						var html = """
						<html lang="en">
							<body>
								<h1>Cat Cats cat's cat cats</h1>
							</body>
						</html>
						""";
						return Results.Content(html, "text/html");
					});
					
					endpoint.MapGet("/IndexerNormalizingTextEat.html", () =>
					{
						var html = """
						<html lang="en">
							<body>
								<h1>eat Eat Eating eatINg eats</h1>
							</body>
						</html>
						""";
						return Results.Content(html, "text/html");
					});


					endpoint.MapGet("/IndexerNormalizingTextHäst.html", () =>
					{
						var html = """
						<html lang="sv">
							<body>
								<h1>Häst häst hästarna Hästarna hästen Hästen Hästar hästar</h1>
							</body>
						</html>
						""";
						return Results.Content(html, "text/html");
					});

					endpoint.MapGet("/IndexerNormalizingTextArt.html", () =>
					{
						var html = """
							<html lang="sv">
							<body>
								<h1>artig Artig artigare ARTIGARE Artigast</h1>
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
		var client = host.GetTestClient();
		client.BaseAddress = new Uri("http://localhost/");

		return client;
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
