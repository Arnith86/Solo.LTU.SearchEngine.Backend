using LTU.SearchEngine.Backend.Core;
using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Text;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Application.QueryParsing.Helpers;

/// <summary>
/// Implementation of <see cref="IStringTokenizer"/> that handles operator recognition, whitespace <br/>
/// separation of terms and quote-aware grouping.
/// </summary>
public class QueryStringTokenizer : IStringTokenizer<ExtractedQueryToken, IgnoredTermsDTO>
{
	private readonly IQuerySyntaxHelper _syntaxHelper;
    private readonly ITextNormalizer<string, IEnumerable<string>> _normalizer;

    public QueryStringTokenizer(IQuerySyntaxHelper syntaxHelper, ITextNormalizer<string, IEnumerable<string>> normalizer)
	{
		_syntaxHelper = syntaxHelper ?? 
			throw new ArgumentNullException(nameof(syntaxHelper));
        _normalizer = normalizer ?? 
			throw new ArgumentNullException(nameof(normalizer));
    }


	/// <inheritdoc/>
	public QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO> Tokenize(string input, string languageCode)
	{
		var session = new QueryStringTokenizationSession(input, languageCode, _syntaxHelper, _normalizer);
		return session.Execute();
	}



	private class QueryStringTokenizationSession
	{
		private readonly string _input;
		private string _globalLanguage;
		private readonly IQuerySyntaxHelper _querySyntaxHelper;
		private readonly ITextNormalizer<string, IEnumerable<string>> _textNormalizer;
		private readonly List<IgnoredTermsDTO> _ignoredTokens = new();
		private readonly List<ExtractedQueryToken> _tokens = new();
		private StringBuilder _stringBuilder = new();
		private bool _isBuildingAPhrase = false;
		private string _singleTermPhraseLanguage = null!;
		private int _index;
		private char _character; 
		private bool _isNextCharacterEscaped = false;

		public QueryStringTokenizationSession(
			string input, string languageCode, 
			IQuerySyntaxHelper syntaxHelper, 
			ITextNormalizer<string, 
			IEnumerable<string>> normalizer 
			)
		{
			_input = input;
			_globalLanguage = languageCode;
			_querySyntaxHelper = syntaxHelper;
			_textNormalizer = normalizer;
		}

		public QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO> Execute()
		{
			for (_index = 0; _index < _input.Length; _index++)
            {
                LoopAction action = LoopAction.None;
                _character = _input[_index];

            
				if (HandleEscapeCharacter().Equals(LoopAction.Continue)) continue;

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


                bool isWhitespace = char.IsWhiteSpace(_character);

                if ((isWhitespace || ShouldBreakTerm(_character, _index)) && !_isBuildingAPhrase)
                {
                    Flush(QueryTokenType.Term, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
                    _singleTermPhraseLanguage = null!;
                }

                if (isWhitespace && !_isBuildingAPhrase)
                {
                    HandleWhiteSpace();
                    continue;
                }

                if (action.Equals(LoopAction.Continue))
                {
                    _singleTermPhraseLanguage = null!;
                    continue;
                }

                // Checks for grouping operators. ( ) [ ] { } 
                if (TryHandleIsGroupingOperator().Equals(LoopAction.Continue)) continue;

                // AND, OR, NOT, are exceptions and are handled in the next method.
                if (TryHandleLogicalOperator().Equals(LoopAction.Continue)) continue;
                
				if (TryHandleIsCapitalLetterOperator().Equals(LoopAction.Continue)) continue;

                // Found start of a phrase
                if (IsEdgeOfPhrase())
                {
                    _isBuildingAPhrase = !_isBuildingAPhrase; // ToDo: Move to IsEdgeOfPhrase?
                    continue;
                }

                // Appends phrase characters and flushes phrase if is edge of phrase
                if (TryHandleIsEdgeOfPhrase().Equals(LoopAction.Continue))
                {
                    _singleTermPhraseLanguage = null!;
                    continue;
                };

                _stringBuilder.Append(_character);
            }

            // Handles the last term if there is one
            Flush(QueryTokenType.Term, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
			
			_querySyntaxHelper.ValidateGrouping(_tokens);

			return new QueryStringTokenizingResult<ExtractedQueryToken, IgnoredTermsDTO>(
				_tokens, _ignoredTokens
			); 
		}

                
		private void Flush(QueryTokenType queryTokenType, string languageCode)
		{
			if (_stringBuilder.Length == 0) return;

			var originalText = _stringBuilder.ToString().Trim();
			string finalToken;
		
			// Handles term and phrase normalization and token creation
			if (queryTokenType == QueryTokenType.Term || queryTokenType == QueryTokenType.Phrase)
			{
				var words = originalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				var normalizedWords = new List<string>();

				foreach (var word in words)
				{
					var normalizedWord = _textNormalizer.Normalize(word, languageCode);

					foreach (var token in normalizedWord)
					{
						if (!string.IsNullOrWhiteSpace(token)) 
							normalizedWords.Add(token);
						else 
							_ignoredTokens.Add(new IgnoredTermsDTO{Token = word, Language = languageCode });	
					}
				}

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


		private LoopAction HandleEscapeCharacter()
        {
            if (!_isNextCharacterEscaped && _character.Equals('\\') && _index + 1 < _input.Length)
            {
                _isNextCharacterEscaped = true;
                return LoopAction.Continue;
            }

            if (_isNextCharacterEscaped)
            {
                _stringBuilder.Append(_character);
                _isNextCharacterEscaped = false;
                return LoopAction.Continue;
            }

            return LoopAction.None;
        }


		private bool ShouldBreakTerm(char character, int index)
        {
			if (IsGroupingOperator(character)) return true;
			if ("!+-&|".Contains(character) && IsAtStartOfWordOrAfterColon(index)) return true;

			return false;
        }


		private void HandleWhiteSpace()
        {
			if (_isBuildingAPhrase || _tokens.Count == 0) return; 

            var lastToken = _tokens.Last();

			bool lastTokenWasOperand = 	
				lastToken.TokenType.Equals(QueryTokenType.Term) ||
				lastToken.TokenType.Equals(QueryTokenType.Phrase) || 
				(lastToken.TokenType.Equals(QueryTokenType.GroupingOperator) && ")}]".Contains(lastToken.Token));

			if(!lastTokenWasOperand) return;

			if (_index + 1 < _input.Length)
			{
				char next = _input[_index + 1];

				// Do not insert OR before operators
				if ("!+-&|".Contains(next) ||
					IsCapitalLetterOperator(_input, _index + 1) ||
					IsGroupingOperator(next) ||
					IsLanguagePreFix(_input, _index +1)
				) return; 

				_tokens.Add(new ExtractedQueryToken(QueryTokenType.LogicalOperator, "OR", _globalLanguage));
			}
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
				if (IsDoubleLogicalOperator(_character)) _stringBuilder.Append(_input, _index++, 2);
				else _stringBuilder.Append(_character);

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


		private LoopAction TryHandleIsEdgeOfPhrase()
		{
			if (_isBuildingAPhrase)
			{
				// Found end of phrase, builds string
				if (IsEdgeOfPhrase(checkEndPhrase: true))
				{
					_isBuildingAPhrase = !_isBuildingAPhrase;
					Flush(QueryTokenType.Phrase, ResolveLanguage(_singleTermPhraseLanguage, _globalLanguage));
					return LoopAction.Continue;
				}

				_stringBuilder.Append(_character);
				return LoopAction.Continue;
			}

			return LoopAction.None;
		}


		private bool IsLogicalOperator(char character)
		{
			bool isAtStart = _index.Equals(0);
			bool isAfterSeparator = !isAtStart && 
				(char.IsWhiteSpace(_input[_index - 1]) || " :([{".Contains(_input[_index - 1]));
			
			if (isAtStart || isAfterSeparator) 
				return "+-!&|".Contains(character);
			
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


		// ToDo: extract to own static helper class
		private bool IsNotNullIndex(int index, int length)
			=> index >= 0 && index < length;
	}
}
