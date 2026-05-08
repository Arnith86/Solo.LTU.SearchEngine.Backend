using LTU.SearchEngine.Backend.Core.Exceptions.SearchQueryExceptions;

namespace LTU.SearchEngine.Backend.Core.RequestParameters;

public class SearchQueryRequestParameters
{
    private string _query = string.Empty;

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

    public string Language { get; set; } = "sv"; 
}