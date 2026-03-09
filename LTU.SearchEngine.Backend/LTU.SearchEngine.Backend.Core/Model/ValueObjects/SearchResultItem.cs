namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Represents an immutable search result item containing a title, source URL, and a text snippet.
/// This Value Object ensures that all required data is present and valid.
/// </summary>
public class SearchResultItem
{
	public string Title { get; }
	public string Url { get; }
	public string Snippet { get; }


	/// <summary>
	/// Initializes a new instance of the <see cref="SearchResultItem"/> class.
	/// </summary>
	/// <param name="title">The title of the result. Cannot be null or whitespace.</param>
	/// <param name="url">The source URL. Cannot be null or whitespace.</param>
	/// <param name="snippet">A brief text snippet. Cannot be null or whitespace.</param>
	/// <exception cref="ArgumentException">
	/// Thrown when any of the arguments are null, empty, or consist only of white-space characters.
	/// </exception>
	public SearchResultItem(string title, string url, string snippet)
	{
		ValidateAttributes(title, url, snippet);

		Title = title;
		Url = url;
		Snippet = snippet;
	}

	private void ValidateAttributes(string title, string url, string snippet)
	{
		ValidateSingleAttribute(title);
		ValidateSingleAttribute(url);
		ValidateSingleAttribute(snippet);
	}

	private void ValidateSingleAttribute(string attribute)
	{
		if (String.IsNullOrWhiteSpace(attribute))
			throw new ArgumentException("Attribute must have a value.", nameof(attribute));
	}
}
