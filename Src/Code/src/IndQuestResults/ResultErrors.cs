namespace IndQuestResults;

/// <summary>
/// Specialized error constants for specific Result scenarios.
/// Used for domain-specific error handling in functional programming patterns.
/// </summary>
public static class ResultErrors
{
    /// <summary>
    /// Standard cancellation error message for operations cancelled by CancellationToken.
    /// Used to maintain functional purity by avoiding OperationCanceledException in control flow.
    /// </summary>
    public const string OperationCancelled = "Operation was cancelled by the user.";

    /// <summary>
    /// Error message for timeout scenarios.
    /// </summary>
    public const string OperationTimedOut = "Operation exceeded the specified timeout";

    /// <summary>
    /// Error message for operations that are no longer valid due to state changes.
    /// </summary>
    public const string OperationInvalidated = "Operation is no longer valid due to state changes";
}