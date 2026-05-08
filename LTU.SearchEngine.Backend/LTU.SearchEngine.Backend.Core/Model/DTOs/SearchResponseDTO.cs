using LTU.SearchEngine.Backend.Core.RequestParameters;

namespace LTU.SearchEngine.Backend.Core.Model.DTOs;

/// <summary>
/// Represents the final paginated search response delivered to the client.
/// </summary>
/// <remarks>
/// This DTO aggregates the actual matching documents with critical execution metadata, 
/// such as pagination state (total pages, current offset), performance messages, 
/// and information regarding query preprocessing (e.g., stop-words removed).
/// </remarks>
/// <param name="SearchResults">A collection of <see cref="DocumentDTO"/> containing the hydrated data of matching web pages.</param>
/// <param name="MetaData">The pagination state, including total item counts and page boundaries.</param>
/// <param name="Message">A status or telemetry message, typically containing the search execution time.</param>
/// <param name="IgnoredTokens">
/// An optional collection of <see cref="IgnoredTermsDTO"/> detailing terms that were 
/// stripped from the query during parsing (e.g., stop-words or invalid characters).
/// </param>
public record SearchResponseDTO(
	IEnumerable<DocumentDTO> SearchResults,
	PaginationMetaData MetaData,
	string? Message,
	IEnumerable<IgnoredTermsDTO>? IgnoredTokens = null
);
