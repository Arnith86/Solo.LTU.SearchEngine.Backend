namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public record QueryStringTokenizingResult<TToken, TIgnoredToken>(
    IEnumerable<TToken> Tokens,
    IEnumerable<TIgnoredToken> IgnoredTokens
);