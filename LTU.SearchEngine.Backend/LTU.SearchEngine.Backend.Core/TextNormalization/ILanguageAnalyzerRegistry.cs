namespace LTU.SearchEngine.Backend.Core.TextNormalization;

using Lucene.Net.Analysis;

/// <summary>
/// Defines a registry for managing and retrieving language-specific Lucene Analyzers. <br/>
/// This allows the system to apply different normalization, stop-word filtering,  <br/>
/// and stemming rules based on the detected language of the content.
/// </summary>
public interface ILanguageAnalyzerRegistry
{
    /// <summary>
    /// Retrieves the configured <see cref="Analyzer"/> for the specified language.
    /// </summary>
    /// <param name="languageCode">The name or code of the language (e.g., "Swedish", "English").</param>
    /// <returns>A Lucene <see cref="Analyzer"/> tailored for the requested language.</returns>
    public Analyzer GetAnalyzerForLanguage(string languageCode);

    /// <summary> Checks whether an analyzer is registered for the specified language. </summary>
    /// <param name="languageCode">The name or code of the language to check.</param>
    /// <returns><c>true</c> if an analyzer exists; otherwise, <c>false</c>.</returns>
    public bool HasAnalyzerForLanguage(string languageCode);
}