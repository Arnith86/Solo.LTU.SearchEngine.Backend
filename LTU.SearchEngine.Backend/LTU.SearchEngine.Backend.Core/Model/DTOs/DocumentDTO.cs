namespace LTU.SearchEngine.Backend.Core.Model.DTOs;

public record DocumentDTO(
	string Url, 
	string Title, 
	//double PageRankScore, 
	//double TflDfScore, 
	string Language 
	//string Snippet
);

