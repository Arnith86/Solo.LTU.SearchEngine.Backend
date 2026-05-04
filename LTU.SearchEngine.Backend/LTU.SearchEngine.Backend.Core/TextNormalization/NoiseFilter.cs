namespace LTU.SearchEngine.Backend.Core.TextNormalization;

public class NoiseFilter : ITextFilter
{
    public string? Apply(string rawTerm, string languageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(rawTerm))
            return null;

        bool isSearchable = false;

        foreach (var character in rawTerm)
        {
            if (char.IsLetterOrDigit(character))
            {
                isSearchable = true;
                break;
            }
        }

        return isSearchable ? rawTerm : null;

        // var builder = new StringBuilder();
        // bool hasAlphaNumeric = false;

        // foreach (var c in rawTerm)
        // {
        //     if (char.IsLetterOrDigit(c))
        //     {
        //         builder.Append(c);
        //         hasAlphaNumeric = true;
        //     }
        //     else if (c == '+' || c == '#')
        //     {
        //         builder.Append(c);
        //     }
        // }

        // if (!hasAlphaNumeric)
        //     return null;

        // var result = builder.ToString();

        // // Remove leading + or #
        // while (result.StartsWith("+") || result.StartsWith("#"))
        // {
        //     result = result.Substring(1);
        // }

        // if (result.Length == 0)
        //     return null;

        // return result;
    }
}
