using LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;

namespace LTU.SearchEngine.Backend.Core.RequestParameters;

/// <summary>
/// Encapsulates the user's search input and associated metadata required for query processing.
/// </summary>
/// <remarks>
/// This class acts as the first line of defense for the search engine, validating that the 
/// input is within safe operational limits (e.g., length and content) before it reaches the 
/// <see cref="LTU.SearchEngine.Application.QueryParsing.IQueryParser"/>.
/// </remarks>
public class SearchQueryRequestParameters
{
    private string _query = string.Empty;

	/// <summary>
	/// Gets or sets the raw search expression provided by the user.
	/// </summary>
	/// <value>The search string (e.g., "artificial intelligence AND robots"). Input is automatically trimmed.</value>
	/// <exception cref="QuerySyntaxException">
	/// Thrown if the value is null, empty, or consists only of white space.
	/// Also thrown if the query length exceeds the 500-character safety limit.
	/// </exception>
	public string Query 
    { 
        get => _query; 
        set
        {
            if (string.IsNullOrWhiteSpace(value)) 
                throw new QuerySyntaxException("Search query cannot be empty.");

             if (value.Length > 500) 
                throw new QuerySyntaxException("Query length is limited to 500 characters!");
            
            _query = value.Trim();    
        } 
    }

	/// <summary>
	/// Gets or sets the ISO language code used to drive language-specific tokenization and normalization.
	/// </summary>
	/// <value>A two-letter ISO code. Defaults to "sv" (Swedish).</value>
	/// <remarks>
	/// This code ensures the engine uses the correct stop-word lists and stemming rules 
	/// during the processing of the <see cref="Query"/>.
	/// </remarks>
	public string Language { get; set; } = "sv"; 
}