public interface IHtmlLanguageCodeConverter
{
    string Convert(string htmlLanguageCode);
}


public class HtmlLanguageCodeConverter : IHtmlLanguageCodeConverter
{
    // StringComparer.OrdinalIgnoreCase is used as to allow both "en-US" and "en-us" 
    private static readonly Dictionary<string, string> LanguageMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "en", "English" },
        { "en-US", "English" },
        { "en-GB", "English" },
        { "en-AU", "English" },
        { "en-CA", "English" },
        { "en-NZ", "English" },
        { "en-IE", "English" },
        { "en-ZA", "English" },
        { "en-IN", "English" },

        { "sv", "Swedish" },
        { "sv-SE", "Swedish" },
        { "sv-FI", "Swedish" },
        { "da", "Danish" },
        { "no", "Norwegian" },
        { "fi", "Finnish" },
        { "is", "Icelandic" },

        { "de", "German" },
        { "fr", "French" },
        { "es", "Spanish" },
        { "it", "Italian" },
        { "nl", "Dutch" },
        { "pt", "Portuguese" },
        { "pl", "Polish" },
        { "ru", "Russian" },

        { "zh", "Chinese" },
        { "zh-CN", "Chinese (Simplified)" },
        { "zh-TW", "Chinese (Traditional)" },
        { "ja", "Japanese" },
        { "ko", "Korean" }
    };

    public string Convert(string htmlLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(htmlLanguageCode)) return "Unknown";

        if (LanguageMap.TryGetValue(htmlLanguageCode, out var language)) return language;

        return "Unknown";
    }
}