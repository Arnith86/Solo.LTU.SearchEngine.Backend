namespace LTU.SearchEngine.Backend.Core.Exceptions;

public abstract class CrawlDomainException : Exception
{
	public CrawlDomainException(string message) : base(message) { }
}
