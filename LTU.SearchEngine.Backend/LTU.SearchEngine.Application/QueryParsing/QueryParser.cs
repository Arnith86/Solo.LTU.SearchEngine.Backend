//using LTU.SearchEngine.Application.QueryParsing.Helpers;
//using LTU.SearchEngine.Backend.Core;

//namespace LTU.SearchEngine.Application.QueryParsing;

///// <summary>
///// Parses raw user search query strings into a structured <see cref="ParsedQuery"/>.
///// Covers FRQ-3001..FRQ-3008:
///// - Terms, phrases ("...")
///// - AND/OR operators (case-sensitive; uppercase only)
///// - Required terms (+)
///// - Exclusion (- / NOT) with constraint: must be preceded by a positive term
///// </summary>
//public sealed class QueryParser : IQueryParser
//{
//	private enum LoopAction { Continue, Break, Next, None } // ToDo: extract to own enum class 
//    private readonly IQueryTokenProcessor _queryTokenProcessor;
//	//private readonly IQuerySyntaxHelper _querySyntaxHelper;
//	private readonly ITokenizer _tokenizer;

//	public QueryParser(
//  		IQueryTokenProcessor queryTokenProcessor,
//		//IQuerySyntaxHelper querySyntaxHelper
//		ITokenizer tokenizer
//		)
//    {
//		_queryTokenProcessor = queryTokenProcessor ??
//			throw new ArgumentNullException(nameof(queryTokenProcessor));
//		//_querySyntaxHelper = querySyntaxHelper ??
//		//	throw new ArgumentNullException(nameof(querySyntaxHelper));
//		_tokenizer = tokenizer ??
//			throw new ArgumentNullException(nameof(tokenizer));

//	}

//	public ParsedQuery Parse(string rawQuery)
//    {
//        if (string.IsNullOrWhiteSpace(rawQuery))
//        {
//            var empty = new ParsedQuery();
//            empty.Errors.Add("Query is empty.");   
//            return empty;
//        }

//        //var tokens = _querySyntaxHelper.Tokenize(rawQuery);
//        var tokens = _tokenizer.Tokenize(rawQuery);
//		// ToDo: Mode detection does not take into consideration that there can be concatenated logical expressions (example: "term1 && term2 || term3 )
//		var mode = _querySyntaxHelper.DetectMode(tokens);  

//        var parsedQuery = new ParsedQuery { Mode = mode };

//        bool sawPositive = false;

//        for (int i = 0; i < tokens.Count; i++)
//        {
//			LoopAction loopAction = LoopAction.None;

//			var token = tokens[i];

//            // Skip explicit AND/OR tokens
//            if (_querySyntaxHelper.IsOperatorToken(token)) continue;

//            loopAction = 
//				HandleNegativeToken(parsedQuery, tokens, token, sawPositive, index: i);

//            if (loopAction.Equals(LoopAction.Continue)) 
//				continue;
//            if (loopAction.Equals(LoopAction.Break)) 
//				break;

//            loopAction = 
//				HandleRequiredToken(parsedQuery, token, sawPositive);

//			if (loopAction.Equals(LoopAction.Continue)) 
//				continue;

//			loopAction = 
//				HandlePhraseToken(parsedQuery, token, sawPositive);

//			if (loopAction.Equals(LoopAction.Continue)) 
//				continue;

//			HandleTermToken(parsedQuery, token, sawPositive);
//        }

//        // FRQ-3008 constraint: standalone exclusion queries should return error or zero results
//        if (!sawPositive && parsedQuery.ExcludedTerms.Count > 0)
//            parsedQuery.Errors.Add("Standalone exclusion queries are not allowed (must include a positive term).");

//        return parsedQuery;
//    }


//	private LoopAction HandleNegativeToken(
//	    ParsedQuery parsedQuery,
//	    List<string> tokens,
//	    string token,
//	    bool sawPositive,
//	    int index
//	    )
//	{
//		// NOT operator excludes the next term/phrase (FRQ-3008)
//		if (token == "NOT")
//		{
//			if (index + 1 >= tokens.Count)
//			{
//				parsedQuery.Errors.Add("NOT must be followed by a term or phrase.");
//				return LoopAction.Break;
//			}

//			var next = tokens[++index];
//			_queryTokenProcessor.ProcessNegativeToken(parsedQuery, next, ref sawPositive);
			
//			return LoopAction.Continue;
//		}

//		// Excluded term (-) || (!) (FRQ-3008)
//		if (token.StartsWith("-", StringComparison.Ordinal) ||
//			token.StartsWith("!", StringComparison.Ordinal) &&
//			token.Length > 1)
//		{
//			_queryTokenProcessor.ProcessNegativeToken(parsedQuery, token[1..], ref sawPositive);
//			return LoopAction.Continue;
//		}

//		return LoopAction.None;
//	}

//	private LoopAction HandleRequiredToken(
//		ParsedQuery parsedQuery,
//		string token,
//		bool sawPositive
//		)
//	{
//		// Required term (+) (FRQ-3007)
//		if (token.StartsWith("+", StringComparison.Ordinal) && token.Length > 1)
//		{
//			_queryTokenProcessor.ProcessRequiredToken(parsedQuery, token, ref sawPositive);
//			return LoopAction.Continue;
//		}

//		return LoopAction.None;
//	}

//	private LoopAction HandlePhraseToken(
//		ParsedQuery parsedQuery,
//		string token,
//		bool sawPositive
//		)
//	{
//		// Phrase (FRQ-3003)
//		if (_querySyntaxHelper.IsPhraseToken(token))
//		{
//			_queryTokenProcessor.ProcessPhraseToken(parsedQuery, token, ref sawPositive);
//			return LoopAction.Continue;
//		}

//		return LoopAction.None;
//	}

//	// Normal term (FRQ-3001/3002)
//	private void HandleTermToken(
//		ParsedQuery parsedQuery,
//		string token,
//		bool sawPositive
//		) => _queryTokenProcessor.ProcessTermToken(parsedQuery, token, ref sawPositive);
//}
