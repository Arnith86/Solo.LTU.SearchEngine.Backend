namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public sealed record IndexedTerm(
	string Term,
	TermSource Source
);