namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

/// <summary>
/// Defines a contract for query nodes that can be explicitly marked as mandatory 
/// requirements within a search clause.
/// </summary>
/// <remarks>
/// This interface corresponds to the "+" prefix or "MUST" operator in query syntax, 
/// specifying that the document must contain the match defined by this node to be 
/// considered a valid result.
/// </remarks>
public interface IIsRequirable
{
    /// <summary>
    /// Indicates whether the current node is a mandatory requirement for its parent clause.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the node associated with the prefix requirement operator (e.g., '+') 
    /// indicating it must be present; <see langword="false"/> if the node is optional (should-match).
    /// </returns>
    bool IsRequirable();
}