namespace LTU.SearchEngine.Tests.Helpers;

/// <summary>
/// Provides utility methods for handling asynchronous operations in tests.
/// </summary>
public static class TestWait
{
    /// <summary>
    /// Repeatedly checks a condition until it returns true or the maximum wait time is reached.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="maxWaitMs">Maximum time to wait in milliseconds.</param>
    /// <param name="intervalMs">Time to wait between checks in milliseconds.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task UntilTrue(Func<bool> condition, int maxWaitMs = 5000, int intervalMs = 100)
    {
        int totalWait = 0;
        
        // We want to loop AS LONG AS the condition is FALSE and we haven't timed out
        while (!condition() && totalWait < maxWaitMs)
        {
            await Task.Delay(intervalMs);
            totalWait += intervalMs;
        }
    }

    /// <summary>
    /// Repeatedly check is the maximum wait time is reached.
    /// </summary>
    /// <param name="maxWaitMs">Maximum time to wait in milliseconds.</param>
    /// <param name="intervalMs">Time to wait between checks in milliseconds.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task UntilTrue(int maxWaitMs = 5000, int intervalMs = 100)
    {
        int totalWait = 0;
        
        // We want to loop AS LONG AS the condition is FALSE and we haven't timed out
        while (totalWait < maxWaitMs)
        {
            await Task.Delay(intervalMs);
            totalWait += intervalMs;
        }
    }
}