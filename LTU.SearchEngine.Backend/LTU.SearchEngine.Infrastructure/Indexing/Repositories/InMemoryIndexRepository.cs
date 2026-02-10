using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Repositories
{
    /// <summary>
    /// Provides an in-memory implementation of the index repository.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This repository stores indexed documents and inverted index structures in memory
    /// for the lifetime of the process. It is intended for demonstration and development
    /// purposes in Epic 1.
    /// </para>
    /// <para>
    /// No external persistence is performed. All indexed data is lost when the application
    /// is restarted.
    /// </para>
    /// </remarks>

    internal class InMemoryIndexRepository
    {
    }
}
