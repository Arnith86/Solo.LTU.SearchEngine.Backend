namespace LTU.SearchEngine.Test.HelperClasses;

using System.Collections.Concurrent;

/// <summary>
/// A thread-safe helper class used during integration tests to monitor and record < br />
/// outgoing HTTP requests made by the crawler to a simulated web host.
/// </summary>
public class CallTracker
{
    /// <summary>
    /// Gets a thread-safe collection of all URLs or paths that have been requested from the mock server during the test execution.
    /// </summary>
    /// <value>
    /// A <see cref="ConcurrentBag{T}"/> containing the absolute paths or full URLs recorded by the mock endpoints.
    /// </value>
    public ConcurrentBag<string> VisitedUrls { get; } = new ();
}