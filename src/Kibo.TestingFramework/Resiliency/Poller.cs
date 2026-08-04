namespace Kibo.TestingFramework.Resiliency;

/// <summary>
/// Exception thrown when a polling operation times out.
/// Includes the last observed state and elapsed time for debugging.
/// </summary>
public class PollTimeoutException : Exception
{
    /// <summary>The last value observed before timeout (null if action has no return value).</summary>
    public object? LastResult { get; }

    /// <summary>Total time elapsed before timeout.</summary>
    public TimeSpan Elapsed { get; }

    public PollTimeoutException(object? lastResult, TimeSpan elapsed, string message)
        : base(message)
    {
        LastResult = lastResult;
        Elapsed = elapsed;
    }
}

/// <summary>
/// Configurable polling / wait-until utility that replaces Thread.Sleep.
/// 
/// Why polling instead of sleep:
///   - Thread.Sleep(6000) always waits 6 seconds, even if the condition is met at 5.01s
///   - Polling returns as soon as the condition is met (typically 5.1s instead of 6s)
///   - Polling is resilient to timing variations (if server takes 7s, sleep fails)
///   - Polling wastes zero CI/CD pipeline time
/// 
/// Generic design:
///   - Works with any async operation (HTTP calls, database queries, file checks)
///   - Returns the result that satisfied the condition (no need for a second call)
///   - Includes diagnostics in timeout exceptions (last observed state)
/// 
/// Usage:
///   var order = await Poller.WaitUntilAsync(
///       action: () => client.GetOrderAsync(id),
///       condition: o => o.Status == "ReadyForFulfillment",
///       interval: TimeSpan.FromMilliseconds(500),
///       timeout: TimeSpan.FromSeconds(15));
/// </summary>
public static class Poller
{
    /// <summary>
    /// Polls an async action until the condition is met or timeout is reached.
    /// Returns as soon as the condition is met (no unnecessary waiting).
    /// </summary>
    /// <typeparam name="T">The type of the result from the action.</typeparam>
    /// <param name="action">The async action to execute on each poll iteration.</param>
    /// <param name="condition">The condition to check against the result. Returns true when polling should stop.</param>
    /// <param name="interval">How often to poll. Default: 500ms. Lower = more responsive, higher = less load.</param>
    /// <param name="timeout">Maximum total time to wait. Default: 15s. Should be generous for CI/CD.</param>
    /// <returns>The result that satisfied the condition.</returns>
    /// <exception cref="PollTimeoutException">Thrown when timeout is reached. Includes LastResult for debugging.</exception>
    public static async Task<T> WaitUntilAsync<T>(
        Func<Task<T>> action,
        Func<T, bool> condition,
        TimeSpan? interval = null,
        TimeSpan? timeout = null)
    {
        var pollInterval = interval ?? TimeSpan.FromMilliseconds(500);
        var pollTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        T? lastResult = default;

        while (stopwatch.Elapsed < pollTimeout)
        {
            lastResult = await action();

            if (condition(lastResult))
            {
                return lastResult;
            }

            await Task.Delay(pollInterval);
        }

        throw new PollTimeoutException(
            lastResult,
            stopwatch.Elapsed,
            $"Polling timed out after {pollTimeout.TotalSeconds:F1}s. " +
            $"Last observed state: {lastResult}");
    }

    /// <summary>
    /// Polls an async action (no return value) until the condition is met or timeout is reached.
    /// Use this overload when the action has side effects but no meaningful return value.
    /// </summary>
    /// <param name="action">The async action to execute on each poll iteration.</param>
    /// <param name="condition">The condition to check. Returns true when polling should stop.</param>
    /// <param name="interval">How often to poll. Default: 500ms.</param>
    /// <param name="timeout">Maximum total time to wait. Default: 15s.</param>
    /// <exception cref="PollTimeoutException">Thrown when timeout is reached.</exception>
    public static async Task WaitUntilAsync(
        Func<Task> action,
        Func<bool> condition,
        TimeSpan? interval = null,
        TimeSpan? timeout = null)
    {
        var pollInterval = interval ?? TimeSpan.FromMilliseconds(500);
        var pollTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < pollTimeout)
        {
            await action();

            if (condition())
            {
                return;
            }

            await Task.Delay(pollInterval);
        }

        throw new PollTimeoutException(
            null,
            stopwatch.Elapsed,
            $"Polling timed out after {pollTimeout.TotalSeconds:F1}s. Condition was not met.");
    }
}