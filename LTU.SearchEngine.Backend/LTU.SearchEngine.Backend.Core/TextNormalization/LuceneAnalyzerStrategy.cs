using Lucene.Net.Analysis;
using Microsoft.Extensions.Logging;

namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Provides a strategy for selecting the most appropriate Lucene <see cref="Analyzer"/>  <br/>
/// based on the detected language of a document.
/// </summary>
/// <remarks>
/// This class acts as a bridge between raw HTML language codes (e.g., "en-US", "sv")  <br/>
/// and the normalized language names used by the <see cref="ILanguageAnalyzerRegistry"/>.
/// </remarks>
public class LuceneAnalyzerStrategy
{
    private readonly ILanguageAnalyzerRegistry _languageAnalyzerRegistry; 
    private readonly IHtmlLanguageCodeConverter _languageCodeConverter;
    private readonly ILogger<LuceneAnalyzerStrategy> _logger;

    /// <summary> Initializes a new instance of the <see cref="LuceneAnalyzerStrategy"/> class. </summary>
    /// <param name="languageAnalyzerRegistry">The registry containing available language analyzers.</param>
    /// <param name="languageCodeConverter">A service to convert raw HTML language codes to standard language names.</param>
    /// <param name="logger">Logger for capturing warnings and fallback events.</param>
    /// <exception cref="ArgumentNullException">Thrown if any of the required dependencies are null.</exception>
     public LuceneAnalyzerStrategy(
        ILanguageAnalyzerRegistry languageAnalyzerRegistry, 
        IHtmlLanguageCodeConverter languageCodeConverter,
        ILogger<LuceneAnalyzerStrategy> logger
        )
    {
        _languageAnalyzerRegistry = languageAnalyzerRegistry ?? 
            throw new ArgumentNullException(nameof(languageAnalyzerRegistry));
        
        _languageCodeConverter = languageCodeConverter ?? 
            throw new ArgumentNullException(nameof(languageCodeConverter));

        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
    }

    // <summary>Selects a Lucene <see cref="Analyzer"/> based on the provided language code.</summary>
    /// <param name="languageCode">The raw language code extracted from the HTML (e.g., "sv-SE").</param>
    /// <returns>A language-specific <see cref="Analyzer"/> if found; otherwise, defaults to the "Swedish" analyzer and logs a warning.</returns>
    /// <remarks>
    /// The method first converts the raw code (like "en-GB") to a normalized 
    /// name (like "English") before checking the registry.
    /// </remarks>
    public virtual Analyzer GetAppropriateAnalyzer(string languageCode)
    {
        var language = _languageCodeConverter.Convert(languageCode);

        if (!_languageAnalyzerRegistry.HasAnalyzerForLanguage(language))
        {
            _logger.LogWarning(
                "No analyzer found for language code {LanguageCode}. Defaulting to Swedish analyzer.", 
                languageCode
            );
            return _languageAnalyzerRegistry.GetAnalyzerForLanguage("Swedish");    
        }

        return _languageAnalyzerRegistry.GetAnalyzerForLanguage(language);
    }
}