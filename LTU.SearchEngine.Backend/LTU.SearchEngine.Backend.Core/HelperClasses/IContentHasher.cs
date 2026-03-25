namespace LTU.SearchEngine.Backend.Core.HelperClasses;

public interface IContentHasher
{
    string CalculateHash(byte[] content);
}