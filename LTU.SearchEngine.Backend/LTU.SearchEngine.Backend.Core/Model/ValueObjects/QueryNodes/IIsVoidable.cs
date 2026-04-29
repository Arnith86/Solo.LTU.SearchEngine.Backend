namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects.QueryNodes;

/// <summary>
/// Defines a mechanism for query nodes to indicate whether they lack semantic value 
/// and should be excluded from the final execution or tree transformation.
/// </summary>
public interface IIsVoidable
{
    /// <summary>
    /// Determines whether the current node is considered "void" or logically non-existent.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the node lacks searchable content or logical operands 
    /// and should be pruned from the query tree; otherwise, <see langword="false"/>.
    /// </returns>
    bool IsVoid();
}