using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    public class PunctuationFilter : ITextFilter
    {
        public string? Apply(string rawTerm)
        {
            if (string.IsNullOrWhiteSpace(rawTerm))
                return null;

            var builder = new StringBuilder();
            bool hasAlphaNumeric = false;

            foreach (var c in rawTerm)
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                    hasAlphaNumeric = true;
                }
                else if (c == '+' || c == '#')
                {
                    builder.Append(c);
                }
            }

            if (!hasAlphaNumeric)
                return null;

            var result = builder.ToString();

            // Remove leading + or #
            while (result.StartsWith("+") || result.StartsWith("#"))
            {
                result = result.Substring(1);
            }

            if (result.Length == 0)
                return null;

            return result;
        }
    }
}
