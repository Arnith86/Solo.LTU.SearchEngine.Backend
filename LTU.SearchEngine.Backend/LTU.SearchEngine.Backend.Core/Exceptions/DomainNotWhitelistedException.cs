namespace LTU.SearchEngine.Backend.Core.Exceptions;

public class DomainNotWhitelistedException : CrawlDomainException
{
	public DomainNotWhitelistedException(string message) : base($"Url: {message}, is not in the white list.")
	{
	}
}
