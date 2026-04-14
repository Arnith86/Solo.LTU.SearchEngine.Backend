namespace LTU.SearchEngine.Backend.Core.Model.Entities;

public class Term
{
    public int Id { get; set; }

    public string Word { get; set; } = string.Empty;

    public double IdfScore { get; set; }

    public ICollection<PageWordFrequency> PageFrequencies { get; set; } = new List<PageWordFrequency>();
    public ICollection<PageWordPosition> PagePositions { get; set; } = new List<PageWordPosition>();
}

