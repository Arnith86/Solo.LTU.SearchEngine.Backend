namespace LTU.SearchEngine.Backend.Core;

public interface IDomainValidator
{
	public bool IsWhitelisted(string url);  
}
