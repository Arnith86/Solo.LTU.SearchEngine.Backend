using Lucene.Net.Analysis.TokenAttributes;

namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Implements linguistic analysis using Lucene.Net analyzers. 
/// This filter handles the "explosion" of strings into multiple tokens and applies 
/// language-specific rules like stemming and word-splitting.
/// </summary>
public class LuceneAnalyzerFilter : ILuceneFilter
{
    private readonly LuceneAnalyzerStrategy _luceneAnalyzerStrategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneAnalyzerFilter"/> class.
    /// </summary>
    /// <param name="luceneAnalyzerStrategy">The strategy used to resolve the correct Lucene analyzer for a given language.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided strategy is null.</exception>    
    public LuceneAnalyzerFilter(LuceneAnalyzerStrategy luceneAnalyzerStrategy)
    {
        _luceneAnalyzerStrategy = luceneAnalyzerStrategy ?? 
            throw new ArgumentNullException(nameof(luceneAnalyzerStrategy));
    }

    /// <summary>
    /// Processes a string through a Lucene TokenStream to produce a collection of normalized tokens.
    /// </summary>
    /// <param name="rawTerm">The text to be analyzed (e.g., "Lars-Åke").</param>
    /// <param name="languageCode">The ISO language code used to select the appropriate analyzer (defaults to "sv").</param>
    /// <returns>
    /// A collection of tokens produced by the analyzer. For example, hyphenated names 
    /// may be split into multiple distinct strings.
    /// </returns>
    /// <remarks>
    /// This method manages the full lifecycle of the Lucene <c>TokenStream</c>, including 
    /// resetting the stream, iterating through attributes, and ensuring proper disposal.
    /// </remarks>
    public IEnumerable<string> Apply(string rawTerm, string languageCode = "sv")
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(rawTerm)) return results;

        var analyzer = _luceneAnalyzerStrategy.GetAppropriateAnalyzer(languageCode);
        using var reader = new StringReader(rawTerm);
        using var tokenStream = analyzer.GetTokenStream("field", reader);

        var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();
        tokenStream.Reset();
        
        while(tokenStream.IncrementToken())
        {
            var token = termAttr
                .ToString()
                .ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormC);

            if (!string.IsNullOrWhiteSpace(token)) results.Add(token);
        }
       
        tokenStream.End();

        return results;
    }
}
