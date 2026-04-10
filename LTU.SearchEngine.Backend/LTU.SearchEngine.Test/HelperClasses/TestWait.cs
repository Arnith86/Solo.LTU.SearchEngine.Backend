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
    /// Repeatedly evaluates an asynchronous condition until it returns true or the maximum wait time is reached.
    /// This method will exit silently if the timeout is reached without the condition becoming true.
    /// </summary>
    /// <param name="condition">The asynchronous delegate to evaluate (e.g., a database check).</param>
    /// <param name="maxWaitMs">The maximum total time to wait in milliseconds. Defaults to 5000ms.</param>
    /// <param name="intervalMs">The time to pause between each evaluation in milliseconds. Defaults to 100ms.</param>
    /// <returns>A task representing the asynchronous polling operation.</returns>
    public static async Task UntilTrue(Func<Task<bool>> condition, int maxWaitMs = 5000, int intervalMs = 100)
    {
        int totalWait = 0;
        
        // The loop continues ONLY IF the condition is still false AND we have time left.
        // If either becomes false, the loop exits.
        while (!await condition() && totalWait < maxWaitMs)
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