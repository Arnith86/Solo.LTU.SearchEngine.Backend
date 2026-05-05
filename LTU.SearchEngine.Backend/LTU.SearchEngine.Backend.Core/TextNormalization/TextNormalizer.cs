using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Orchestrates the text normalization pipeline by coordinating noise reduction, 
/// protection of technical symbols, and linguistic analysis via Lucene.
/// </summary>
public class TextNormalizer : ITextNormalizer<string, IEnumerable<string>>
{
    private readonly INoiseFilter _noiseFilter;
    private readonly ILuceneFilter _luceneFilter;
    
    /// <summary>
    /// Matches characters that define a "Technical Term" (e.g., C++, C#, @user).
    /// These terms bypass linguistic splitting to preserve their semantic meaning.
    /// </summary>
    private static readonly Regex _protectedTechTermsRegex = 
        new Regex(@"[\@\%\#\+&|!\\(){}\[\]""]", RegexOptions.Compiled);
    
    public TextNormalizer(INoiseFilter noiseFilter, ILuceneFilter luceneFilter)
    {
        _noiseFilter = noiseFilter;
        _luceneFilter = luceneFilter;
    }

    /// <summary>
    /// Normalizes a raw string into one or more searchable tokens.
    /// </summary>
    /// <param name="rawTerm">The input string from a document or query.</param>
    /// <param name="languageCode">ISO language code (e.g., "sv", "en") for linguistic rules.</param>
    /// <returns>
    /// A collection of normalized tokens. Returns an empty list if the input is considered noise.
    /// </returns>
    public IEnumerable<string> Normalize(string rawTerm, string languageCode)
    {
        var validWord = _noiseFilter.Apply(rawTerm, languageCode);

        if (validWord == null) return new List<string>();

        // Keep technical terms as one token
        if (_protectedTechTermsRegex.IsMatch(validWord)) 
            return new List<string> { FinalCleanup(rawTerm) };
        
        return _luceneFilter.Apply(validWord, languageCode);
    }

    private string FinalCleanup(string token) => 
        token.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormC);
}

