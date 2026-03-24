namespace LTU.SearchEngine.Backend.Core.Exceptions;

public class BlockedByRobotsTxtException : CrawlDomainException
{
	public BlockedByRobotsTxtException(string message) : base($"Url: {message}, is blocked by the domains robot.txt.")
	{
	}
}
