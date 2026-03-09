namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public class SearchResultItem
{
	public string Title { get; }
	public string Url { get; }
	public string Snippet { get; }

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
			throw new ArgumentException(nameof(attribute), "must have a value!");
	}
}
