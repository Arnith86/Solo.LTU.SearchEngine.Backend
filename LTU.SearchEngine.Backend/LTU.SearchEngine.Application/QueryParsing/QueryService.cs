using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;
using LTU.SearchEngine.Backend.Core.SearchQueryBuilder;
using LTU.SearchEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LTU.SearchEngine.Application.QueryParsing;

public class QueryService
{
	private readonly IDbContextFactory<SearchDbContext> _context;
	private readonly IQueryParser _queryParser;
	private readonly IQueryVisitor<HashSet<int>> _queryEvaluatorVisitor;

	public QueryService(
		IDbContextFactory<SearchDbContext> context,
		IQueryParser queryParser,
		IQueryVisitor<HashSet<int>> queryEvaluatorVisitor
		)
	{
		_context = context;
		_queryParser = queryParser;
		_queryEvaluatorVisitor = queryEvaluatorVisitor;
	}

	public Task<SearchResponseDTO> GetSearchResultsAsync(string rawQuery, int page)
	{
		var queryNode = _queryParser.Parse(rawQuery);


		var resultIds = _queryEvaluatorVisitor.ExecuteAsync(queryNode);

		return 
	}
}
