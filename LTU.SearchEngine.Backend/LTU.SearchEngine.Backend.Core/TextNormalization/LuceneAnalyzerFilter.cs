using Lucene.Net.Analysis.TokenAttributes;

namespace LTU.SearchEngine.Backend.Core.TextNormalization;

public class LuceneAnalyzerFilter : ITextFilter
{
    private readonly LuceneAnalyzerStrategy _luceneAnalyzerStrategy;

    public LuceneAnalyzerFilter(LuceneAnalyzerStrategy luceneAnalyzerStrategy)
    {
        _luceneAnalyzerStrategy = luceneAnalyzerStrategy ?? 
            throw new ArgumentNullException(nameof(luceneAnalyzerStrategy));
    }

    public string? Apply(string rawTerm, string languageCode = "sv")
    {
        if (string.IsNullOrWhiteSpace(rawTerm)) return null;

        var analyzer = _luceneAnalyzerStrategy.GetAppropriateAnalyzer(languageCode);

        using var reader = new StringReader(rawTerm);
        using var tokenStream = analyzer.GetTokenStream("field", reader);

        tokenStream.Reset();

        var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();

        if (tokenStream.IncrementToken())
        {
            var result = termAttr
                .ToString()
                .ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormC);
                
            tokenStream.End();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        tokenStream.End();
        return null;
    }

}
