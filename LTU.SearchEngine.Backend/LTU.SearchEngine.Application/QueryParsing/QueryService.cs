using System.Diagnostics;
using System.Globalization;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.RequestParameters;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Infrastructure.Repositories;

namespace LTU.SearchEngine.Application.QueryParsing;

/// <summary>
/// Provides a concrete implementation of <see cref="IQueryService"/> that orchestrates the 
/// end-to-end search process, from query parsing to database retrieval.
/// </summary>
/// <remarks>
/// The service follows a strict execution pipeline:
/// <list type="number">
///     <item><description><b>Parsing:</b> Converts the raw request into a logical query tree.</description></item>
///     <item><description><b>Evaluation:</b> Uses a Visitor pattern to resolve the tree into a set of document IDs.</description></item>
///     <item><description><b>Retrieval:</b> Fetches the actual document content and metadata using paginated repository calls.</description></item>
/// </list>
/// Performance is tracked via <see cref="Stopwatch"/> and included in the final response.
/// </remarks>
public class QueryService : IQueryService
{
	private readonly IIndexRepository _indexRepository;
	private readonly IQueryParser _queryParser;
	private readonly IQueryVisitor<HashSet<int>> _queryEvaluatorVisitor;

	/// <summary>
	/// Initializes a new instance of the <see cref="QueryService"/> class with required infrastructure and domain services.
	/// </summary>
	/// <param name="indexRepository">The repository used for fetching physical document data.</param>
	/// <param name="queryParser">The parser used to transform raw input into a logical tree.</param>
	/// <param name="queryEvaluatorVisitor">The visitor implementation used to evaluate boolean logic across the index.</param>
	public QueryService(
		IIndexRepository indexRepository,
		IQueryParser queryParser,
		IQueryVisitor<HashSet<int>> queryEvaluatorVisitor
		)
	{
		_indexRepository = indexRepository;
		_queryParser = queryParser;
		_queryEvaluatorVisitor = queryEvaluatorVisitor;
	}

	
	/// <inheritdoc/>
	public async Task<SearchResponseDTO> GetSearchResultsAsync(
		SearchQueryRequestParameters searchParameters,
        PaginationRequestParameters paginationParameters
		)
	{
		var stopWatch = Stopwatch.StartNew();

		var (queryNode, ignoredTokens) = _queryParser.Parse(searchParameters);
		
		HashSet<int> resultIds;
		
		if (queryNode is IIsVoidable voidableNode && voidableNode.IsVoid()) 
			resultIds = new HashSet<int>();
		else
			resultIds = await _queryEvaluatorVisitor.ExecuteAsync(queryNode);
		
		
		var documentResults = await _indexRepository
			.GetDocumentsByIdAsync(resultIds.ToList(), paginationParameters);

		stopWatch.Stop();
		var elapsedTime = stopWatch.Elapsed.TotalMilliseconds;
		string timingMessage = string.Create(
			CultureInfo.InvariantCulture, 
			$"Search completed in {elapsedTime:F2} ms!"
		);


		return new SearchResponseDTO(
			SearchResults: documentResults.Items.Select(doc => new DocumentDTO(
				Id : doc.Id,
				Url : doc.Url,
				Title : doc.Title,
				Language : doc.Language)
			),
			MetaData: documentResults.MetaData,
			Message: timingMessage,
			IgnoredTokens: ignoredTokens
		);
	}
}
