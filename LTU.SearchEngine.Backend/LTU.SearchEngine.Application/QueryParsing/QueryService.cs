using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

	public async Task<SearchResponseDTO> GetSearchResultsAsync(string rawQuery)
	{
		QueryNode<HashSet<int>> queryNode;

		queryNode = _queryParser.Parse(rawQuery);
		
		var resultIds = await _queryEvaluatorVisitor.ExecuteAsync(queryNode);
		
		var documentResults = await _indexRepository
			.GetDocumentsByIdAsync(resultIds.ToList());

		return new SearchResponseDTO(
			searchResults: documentResults.Select(doc => new DocumentDTO(
				Url : doc.Url,
				Title : doc.Title,
				Language : doc.Language)
			),
			currentPage: 1,
			pageSize: 1,
			totalResults: documentResults.Count(),
			message: null
		);
	}
}
