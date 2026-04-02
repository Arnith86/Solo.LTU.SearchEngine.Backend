namespace LTU.SearchEngine.Backend.Core.TextNormalization;

public interface ITextFilter
{
    string? Apply(string rawTerm, string languageCode = "en");
}
