namespace LTU.SearchEngine.Backend.Core.Model.DTOs;

/// <summary>
/// Data Transfer Object representing a single search result item for delivery via the API.
/// </summary>
/// <param name="Title">The display title of the indexed page or document.</param>
/// <param name="Url">The absolute source URL where the content is located.</param>
/// <param name="Snippet">
/// A brief, relevant text extract from the document highlighting the search terms.
/// </param>
public record SearchResultItemDTO(
	string Title, 
	string Url, 
	string Snippet
);
