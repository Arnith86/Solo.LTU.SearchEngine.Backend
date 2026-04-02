namespace LTU.SearchEngine.Backend.Core.TextNormalization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Sv;
using Lucene.Net.Util;


/// <summary>
/// Implementation of <see cref="ILanguageAnalyzerRegistry"/> that provides 
/// customized Lucene analysis pipelines for supported languages.
/// </summary>
/// <remarks>
/// Each analyzer utilizes a <see cref="StandardTokenizer"/> followed by 
/// <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>, and a language-specific 
/// <see cref="SnowballFilter"/> for stemming.
/// </remarks>
public class LanguageAnalyzerRegistry : ILanguageAnalyzerRegistry
{
    private readonly Dictionary<string, Analyzer> _languageAnalyzers;


    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageAnalyzerRegistry"/> class <br/>
    /// and registers default analyzers for Swedish and English.
    /// </summary>
    public LanguageAnalyzerRegistry()
    {
        _languageAnalyzers = new Dictionary<string, Analyzer>
        {
            {"Swedish", CreateSwedishAnalyzer()},
            {"English", CreateEnglishAnalyzer()}
        };
    }

    /// <inheritdoc />
    public Analyzer GetAnalyzerForLanguage(string language) =>  _languageAnalyzers[language];

    /// <inheritdoc />
    public bool HasAnalyzerForLanguage(string language) => _languageAnalyzers.ContainsKey(language);


    private Analyzer CreateSwedishAnalyzer()
    {
        return Analyzer.NewAnonymous((fieldName, reader) =>
        {
            var tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
            TokenStream stream = new LowerCaseFilter(LuceneVersion.LUCENE_48, tokenizer);
            stream = new StopFilter(LuceneVersion.LUCENE_48, stream, SwedishAnalyzer.DefaultStopSet); 
            stream = new SnowballFilter(stream, "Swedish");

            return new TokenStreamComponents(tokenizer, stream);
        });
    }

    private Analyzer CreateEnglishAnalyzer()
    {
        return Analyzer.NewAnonymous((fieldName, reader) =>
        {
            var tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
            TokenStream stream = new LowerCaseFilter(LuceneVersion.LUCENE_48, tokenizer);
            stream = new StopFilter(LuceneVersion.LUCENE_48, stream, EnglishAnalyzer.DefaultStopSet); 
            stream = new SnowballFilter(stream, "English");

            return new TokenStreamComponents(tokenizer, stream);
        });
    }
}