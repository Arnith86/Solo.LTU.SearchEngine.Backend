using LTU.SearchEngine.Api.ExtensionsUseExceptionHandler.CustomExceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LTU.SearchEngine.Api.ExtensionsUseExceptionHandler;

/// <summary>
/// Provides an extension method for configuring centralized exception handling using ProblemDetails.
/// </summary>
public static class ProblemDetailsExceptionHandler
{
	/// <summary>
	/// Configures a global exception handler middleware that returns ProblemDetails responses
	/// for specific domain exceptions (e.g., MovieNotFoundException, MovieGenreNotFoundException, ActorNotFoundException)
	/// and general unhandled exceptions.
	/// </summary>
	/// <param name="app">The WebApplication to configure.</param>
	/// <remarks>
	/// The middleware intercepts unhandled exceptions, maps them to appropriate HTTP status codes,
	/// and returns a standardized ProblemDetails response for API consumers.
	/// </remarks>
	public static void ConfigureExceptionHandler(this WebApplication app)
	{
		app.UseExceptionHandler(builder =>
		{
			builder.Run(async context =>
			{
				var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

				if (contextFeature != null)
				{
					var problemDetailsFactory = app.Services.GetRequiredService<ProblemDetailsFactory>();

					ProblemDetails problemDetails;
					int statusCode;

					switch (contextFeature.Error)
					{
						case QuerySyntaxException querySyntaxException: // Query Syntax Violations
							statusCode = StatusCodes.Status400BadRequest;
							problemDetails = problemDetailsFactory.CreateProblemDetails(
								context,
								statusCode,
								title: querySyntaxException.Title,
								detail: querySyntaxException.Message,
								instance: context.Request.Path
							);
							break;
						//case PaginationArgumentOutOfRangeException argumentOutOfRangeException: // Paging parameters out of range
						//	statusCode = StatusCodes.Status400BadRequest;
						//	problemDetails = problemDetailsFactory.CreateProblemDetails(
						//		context,
						//		statusCode,
						//		title: argumentOutOfRangeException.Title,
						//		detail: argumentOutOfRangeException.Message,
						//		instance: context.Request.Path
						//	);
						//	break;
						default:
							statusCode = StatusCodes.Status500InternalServerError;  // General server error
							problemDetails = problemDetailsFactory.CreateProblemDetails(
									context,
									statusCode,
									title: "Internal Server Error",
									detail: contextFeature.Error.Message,
									instance: context.Request.Path);
							break;
					}

					context.Response.StatusCode = statusCode;
					await context.Response.WriteAsJsonAsync(problemDetails);
				}
			});
		});
	}
}
