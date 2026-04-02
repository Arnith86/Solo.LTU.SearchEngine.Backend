using System.Diagnostics;
using System.Globalization;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Infrastructure.Repositories;

namespace LTU.SearchEngine.Application.QueryParsing;

public class QueryService : IQueryService
{
	private readonly IIndexRepository _indexRepository;
	private readonly IQueryParser _queryParser;
	private readonly IQueryVisitor<HashSet<int>> _queryEvaluatorVisitor;

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

	public async Task<SearchResponseDTO> GetSearchResultsAsync(string rawQuery, string languageCode = "sv")
	{
		QueryNode<HashSet<int>> queryNode;

		var stopWatch = Stopwatch.StartNew();

		queryNode = _queryParser.Parse(rawQuery, languageCode);
		
		var resultIds = await _queryEvaluatorVisitor.ExecuteAsync(queryNode);
		
		var documentResults = await _indexRepository
			.GetDocumentsByIdAsync(resultIds.ToList());

		stopWatch.Stop();
		var elapsedTime = stopWatch.Elapsed.TotalMilliseconds;
		string timingMessage = string.Create(
			CultureInfo.InvariantCulture, 
			$"Search completed in {elapsedTime:F2} ms!"
		);

		return new SearchResponseDTO(
			searchResults: documentResults.Select(doc => new DocumentDTO(
				Url : doc.Url,
				Title : doc.Title,
				Language : doc.Language)
			),
			currentPage: 1,
			pageSize: 1,
			totalResults: documentResults.Count(),
			message: timingMessage
		);
	}
}
