using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{

    // TODO: This implementation should be updated to implement the generic 
    // ITextNormalizer<T> interface located in the Core project.



    /// <summary>
    /// Defines the contract for normalizing textual terms before indexing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface are responsible for transforming a raw term
    /// into a normalized form suitable for indexing.
    /// </para>
    /// <para>
    /// This interface represents a normalization strategy and must not depend on
    /// indexing logic, document structure, or persistence concerns.
    /// </para>
    /// </remarks>
    public class ITextNormalizer
    {
    }
}
