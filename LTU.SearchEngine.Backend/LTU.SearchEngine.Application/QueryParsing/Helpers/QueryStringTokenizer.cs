using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Implementation of <see cref="IStringTokenizer"/> that handles operator recognition, whitespace <br/>
/// separation of terms and quote-aware grouping.
/// </summary>
public class QueryStringTokenizer : IStringTokenizer<ExtractedQueryToken, QueryTokenType>
{
	private readonly IQuerySyntaxHelper _syntaxHelper;
    private readonly ITextNormalizer<string> _normalizer;

    public QueryStringTokenizer(IQuerySyntaxHelper syntaxHelper, ITextNormalizer<string> normalizer)
	{
		_syntaxHelper = syntaxHelper ?? 
			throw new ArgumentNullException(nameof(syntaxHelper));
        _normalizer = normalizer ?? 
			throw new ArgumentNullException(nameof(normalizer));
    }


	/// <inheritdoc/>
	public List<ExtractedQueryToken> Tokenize(string input, string languageCode)
	{
		var session = new QueryStringTokenizationSession(input, languageCode, _syntaxHelper, _normalizer);
		return session.Execute();
	}



	private class QueryStringTokenizationSession
	{
		private readonly string _input;
		private string _globalLanguage;
		private readonly IQuerySyntaxHelper _querySyntaxHelper;
		private readonly ITextNormalizer<string> _textNormalizer;
		private readonly List<ExtractedQueryToken> _ignoredTokens = new();
		private readonly List<ExtractedQueryToken> _tokens = new();
		private StringBuilder _stringBuilder = new();
		private bool _isBuildingAPhrase = false;
		private string _singleTermPhraseLanguage = null!;
		private int _index;
		private char _character; 

		public QueryStringTokenizationSession(
			string input, string languageCode, IQuerySyntaxHelper syntaxHelper, ITextNormalizer<string> normalizer 
			)
		{
			_input = input;
			_globalLanguage = languageCode;
			_querySyntaxHelper = syntaxHelper;
			_textNormalizer = normalizer;
		}

		public List<ExtractedQueryToken> Execute()
		{
			for (_index = 0; _index < _input.Length; _index++)
			{
				LoopAction action = LoopAction.None;
				_character = _input[_index];

				// Checks Whole query language 
				if (_stringBuilder.Length == 0 && !_isBuildingAPhrase && IsAtStartOfWordOrAfterColon(_index))
				{
					if (IsLanguagePreFix(_input, _index, out int jump, out string detectedLanguage))
					{
						if (_index.Equals(0)) _globalLanguage = detectedLanguage;
						
						_singleTermPhraseLanguage = detectedLanguage;
						
						_index += jump;
						
						continue;
					}
				}
				
				// Checks implicit OR
				action = TryHandleImplicitOr();

				if (action.Equals(LoopAction.Continue)) 
				{
					_singleTermPhraseLanguage = null!;
					continue;
				}

				// Checks for grouping operators. ( ) [ ] { } 
				action = TryHandleIsGroupingOperator();

				if (action.Equals(LoopAction.Continue))	continue;

				// AND, OR, NOT, are exceptions and are handled in the next method.
				action = TryHandleLogicalOperator();

				if (action.Equals(LoopAction.Continue)) continue;
				

				action = TryHandleIsCapitalLetterOperator();

				if (action.Equals(LoopAction.Continue)) continue;
				
				// Found start of a phrase
				if (IsEdgeOfPhrase())
				{
					_isBuildingAPhrase = !_isBuildingAPhrase; // ToDo: Move to IsEdgeOfPhrase?
					continue;
				}

				// Appends phrase characters and flushes phrase if is edge of phrase
				action = TryHandleIsEdgeOfPhrase(_character, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));

				if (action.Equals(LoopAction.Continue)) 
				{
					_singleTermPhraseLanguage = null!;
					continue;
				};

				// If this is reached must be term
				if (IsTokenTerm(_character))
				{
					Flush(QueryTokenType.Term, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
					_singleTermPhraseLanguage = null!;
					continue;
				}
				else
				{
					_stringBuilder.Append(_character);
				}
			}

			// Handles the last term if there is one
			Flush(QueryTokenType.Term, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
			
			_querySyntaxHelper.ValidateGrouping(_tokens);

			return _tokens;
		}


		private void Flush( QueryTokenType queryTokenType, string languageCode)
		{
			if (_stringBuilder.Length == 0) return;

			var originalText = _stringBuilder.ToString().Trim();
			string finalToken;
		
			// Handles term and phrase normalization and token creation
			if (queryTokenType == QueryTokenType.Term || queryTokenType == QueryTokenType.Phrase)
			{
				var words = originalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				var normalizedWords = words
					.Select(word => _textNormalizer.Normalize(word, languageCode))
					.Where(nWord => !string.IsNullOrWhiteSpace(nWord));  

				finalToken = string.Join(' ', normalizedWords);	
			}
			else
			{
				finalToken = originalText;
			}

			if (finalToken is not null)
			{
				_tokens.Add(new ExtractedQueryToken(queryTokenType, finalToken, languageCode));
			}
			
			_stringBuilder.Clear();
		}


		private LoopAction TryHandleImplicitOr()
		{
			// Only handle whitespace outside phrases
			if (!char.IsWhiteSpace(_character) || _isBuildingAPhrase) return LoopAction.None;

			// No term being built → nothing to separate
			if (_stringBuilder.Length == 0) return LoopAction.None;

			// Prevent implicit OR when term starts with quote (unclosed phrase case)
			if (_stringBuilder[0] == '"') return LoopAction.None;

			// Look ahead to next character
			if (_index + 1 < _input.Length)
			{
				char next = _input[_index + 1];

				// Do not insert OR before phrases or operators
				if (next == '"' ||
					"!+-&|".Contains(next) ||
					IsCapitalLetterOperator(_input, _index + 1) ||
					IsLanguagePreFix(_input, _index + 1))
				{
					return LoopAction.None;
				}
			}

			int tokenCountBeforeFlush = _tokens.Count;

			Flush(QueryTokenType.Term, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));

			// Flush may not add a token if normalization removes it
			if (_tokens.Count == tokenCountBeforeFlush) return LoopAction.Continue;

			_tokens.Add(new ExtractedQueryToken(
				QueryTokenType.LogicalOperator, "OR", ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage)
			));

			return LoopAction.Continue;
		}


		private bool IsLanguagePreFix(string input, int index, out int jump, out string detectedLanguage)
		{
			jump = 0;		
			detectedLanguage = null!;

			if (index + 2 >= input.Length) return false;
			
			if (input[index + 2].Equals(':'))
			{
				detectedLanguage = input.Substring(index, 2);
				jump = 2;
				return true;
			}

			return false;
		}


		private bool IsLanguagePreFix(string input, int index) => IsLanguagePreFix(input, index, out _, out _);
		
		private string ResolveLanguage(string active, string global) => active ?? global; 



		private LoopAction TryHandleIsGroupingOperator()
		{
			if (IsGroupingOperator(_character))
			{
				_stringBuilder.Append(_character);
				Flush(QueryTokenType.GroupingOperator, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
				return LoopAction.Continue;
			}

			return LoopAction.None;
		}


		private bool IsGroupingOperator(char character) => "(){}[]".Contains(character);


		private LoopAction TryHandleLogicalOperator()
		{
			if (IsLogicalOperator(_character))
			{
				if (IsDoubleLogicalOperator(_character))
				{
					_stringBuilder.Append(_input, _index++, 2);
					Flush(QueryTokenType.LogicalOperator, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
					return LoopAction.Continue;
				}

				_stringBuilder.Append(_character);
				Flush(QueryTokenType.LogicalOperator, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
				return LoopAction.Continue;
			}

			return LoopAction.None;
		}


		private LoopAction TryHandleIsCapitalLetterOperator()
		{
			if (IsCapitalLetterOperator(_input, _index))
			{
				var span = _input.AsSpan(_index);
				int length = (span.StartsWith("AND") || span.StartsWith("NOT")) ? 3 : 2;

				_stringBuilder.Append(_input, _index, length);
				Flush(QueryTokenType.LogicalOperator, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));

				_index += (length - 1);
				return LoopAction.Continue;
			}

			return LoopAction.None;
		}


		private LoopAction TryHandleIsEdgeOfPhrase(char character, string languageCode)
		{
			if (_isBuildingAPhrase)
			{
				// Found end of phrase, builds string
				if (IsEdgeOfPhrase(checkEndPhrase: true))
				{
					_isBuildingAPhrase = !_isBuildingAPhrase;
					Flush(QueryTokenType.Phrase, languageCode);
					return LoopAction.Continue;
				}

				_stringBuilder.Append(character);
				return LoopAction.Continue;
			}

			return LoopAction.None;
		}


		private bool IsLogicalOperator(char character)
		{
			if ((_index.Equals(0) || char.IsWhiteSpace(_input[_index - 1])) &&
				Regex.IsMatch(character.ToString(), @"[\+\-\!\&\|]")
				)
			{
				return true;			
			}

			return false;
		}


		private bool IsDoubleLogicalOperator(char character)
		{
			return 
				IsNotNullIndex(_index + 1, _input.Length) && 
				_input[_index + 1].Equals(character);
		}


		private bool IsCapitalLetterOperator(string input, int index)
		{
			bool isAtStartOrAfterSpace = IsAtStartOfWordOrAfterColon(index);

			if (!isAtStartOrAfterSpace) return false;

			var remaining = input.AsSpan(index);

			
			if (remaining.StartsWith("NOT") && IsFullWord(remaining, 3)) return true;
			if (remaining.StartsWith("AND") && IsFullWord(remaining, 3)) return true;
			if (remaining.StartsWith("OR") && IsFullWord(remaining, 2)) return true;

			return false;
		}


		private bool IsAtStartOfWordOrAfterColon(int index)
		{
			if (index == 0) return true;
			
			char prev = _input[index - 1];
			
			return char.IsWhiteSpace(prev) || prev == ':';
		}


		// Makes sure that the while word is as only as long as the operator
		private bool IsFullWord(ReadOnlySpan<char> span, int length) =>	
			span.Length == length || char.IsWhiteSpace(span[length]);


		private bool IsEdgeOfPhrase(bool checkEndPhrase = false)
		{
			if (checkEndPhrase) 
			{
				char c = _input[_index];
				return c == '"' || c == '“' || c == '”';
			}

			return _input[_index].Equals('"') &&
					IsNotNullIndex(_index + 1, _input.Length) &&
					ContainsEndQuote(_input, _index + 1);
		}


		private bool ContainsEndQuote(string input, int index)
		{
			if (index < 0 || index >= input.Length) return false;

			for (int i = index; i < input.Length; i++)
				if (input[i] == '"' || input[i] == '“' || input[i] == '”') return true;

			return false;
		}


		private bool IsTokenTerm(char character) => char.IsWhiteSpace(character);


		// ToDo: extract to own static helper class
		private bool IsNotNullIndex(int index, int length)
			=> index >= 0 && index < length;
	}
}
