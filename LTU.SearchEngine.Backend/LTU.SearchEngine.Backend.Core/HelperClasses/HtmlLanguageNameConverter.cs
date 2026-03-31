namespace LTU.SearchEngine.Backend.Core.HelperClasses;

public class HtmlLanguageNameConverter
{
    private static readonly Dictionary<string, string> LanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

    public static string GetLanguageName(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return "Unknown";

        if (LanguageMap.TryGetValue(languageCode, out var languageName)) return languageName;

        // If exact match is not found use main code as fallback
        string mainCode = languageCode.Split('-')[0];
        if (LanguageMap.TryGetValue(mainCode, out var mainLanguageName)) return mainLanguageName;

        return "Unknown";    
    }
}