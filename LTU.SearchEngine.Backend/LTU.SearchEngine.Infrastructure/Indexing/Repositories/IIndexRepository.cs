using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Repositories
{
    /// <summary>
    /// Defines the contract for storing and retrieving indexed documents and terms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface are responsible for persisting index data,
    /// including forward indexes and inverted indexes.
    /// </para>
    /// <para>
    /// The repository abstraction hides storage details from the indexing flow and
    /// allows different persistence strategies to be introduced without changing
    /// the indexing logic.
    /// </para>
    /// </remarks>
    public interface IIndexRepository
    {
    }
}
