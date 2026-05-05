namespace LTU.SearchEngine.Backend.Core.TextNormalization;

/// <summary>
/// Defines a contract for the initial cleaning stage of the normalization pipeline.
/// The Noise Filter is responsible for removing unwanted characters, handling 
/// casing, and determining if a term is "searchable" or should be ignored.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ITextFilter{T}"/> with a return type of <see cref="string"/>. 
/// If the filter determines the input is pure noise (e.g., only punctuation or 
/// illegal symbols), it returns <c>null</c> to stop further processing in the pipeline.
/// </remarks>
public interface INoiseFilter : ITextFilter<string?>
{
}
