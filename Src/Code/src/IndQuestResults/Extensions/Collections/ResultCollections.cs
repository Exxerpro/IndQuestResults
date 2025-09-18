using IndQuestResults.Operations;

namespace IndQuestResults.Extensions.Collections;

/// <summary>
/// Provides collection operations for Result types, implementing Sequence and Traverse patterns
/// from functional programming. These operations allow working with collections of Results in a
/// functional manner, similar to Haskell's Traversable or F#'s List.traverse functions.
/// </summary>
/// <remarks>
/// <para><strong>Core Operations:</strong></para>
/// <list type="bullet">
/// <item><strong>Sequence:</strong> Convert IEnumerable&lt;Result&lt;T&gt;&gt; to Result&lt;IEnumerable&lt;T&gt;&gt;</item>
/// <item><strong>Traverse:</strong> Map function over collection and sequence the results</item>
/// <item><strong>Partition:</strong> Separate successes and failures</item>
/// <item><strong>Collect:</strong> Extract successful values, ignoring failures</item>
/// </list>
///
/// <para><strong>Error Handling:</strong></para>
/// <list type="bullet">
/// <item><strong>Fail-Fast:</strong> Stop at first error (default behavior)</item>
/// <item><strong>Accumulate:</strong> Collect all errors before failing</item>
/// </list>
///
/// <para><strong>Performance:</strong> Optimized for small to medium collections with efficient enumeration.</para>
/// </remarks>
public static class ResultCollections
{
    /// <summary>
    /// Converts a collection of Results into a Result of a collection.
    /// If all Results are successful, returns a successful Result containing all values.
    /// If any Result fails, returns a failure with all accumulated errors.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="results">Collection of Results to sequence</param>
    /// <returns>Result containing all values if successful, or all errors if any failed</returns>
    /// <example>
    /// <code>
    /// var userIds = new[] { 1, 2, 3 };
    /// var userResults = userIds.Select(LoadUser);
    /// Result&lt;IEnumerable&lt;User&gt;&gt; allUsers = ResultCollections.Sequence(userResults);
    /// </code>
    /// </example>
    public static Result<IEnumerable<T>> Sequence<T>(IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = new List<T>();
        var errors = new List<string>();

        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            if (result.IsSuccess)
            {
                values.Add(result.Value!);
            }
            else if (result.Errors != null)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count > 0
            ? Result<IEnumerable<T>>.WithFailure(errors)
            : Result<IEnumerable<T>>.Success(values);
    }

    /// <summary>
    /// Converts a collection of Results into a Result of a collection, using fail-fast semantics.
    /// Stops processing at the first failure encountered, improving performance for large collections.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="results">Collection of Results to sequence</param>
    /// <returns>Result containing all values if successful, or first error encountered</returns>
    public static Result<IEnumerable<T>> SequenceFailFast<T>(IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = new List<T>();

        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            if (result.IsSuccess)
            {
                values.Add(result.Value!);
            }
            else
            {
                return Result<IEnumerable<T>>.WithFailure(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
            }
        }

        return Result<IEnumerable<T>>.Success(values);
    }

    /// <summary>
    /// Maps a function over a collection and sequences the results.
    /// This is equivalent to mapping followed by sequencing, but more efficient.
    /// </summary>
    /// <typeparam name="TInput">Type of input values</typeparam>
    /// <typeparam name="TOutput">Type of output values</typeparam>
    /// <param name="inputs">Collection of input values</param>
    /// <param name="func">Function to apply to each input</param>
    /// <returns>Result containing all transformed values if successful, or accumulated errors</returns>
    /// <example>
    /// <code>
    /// var userIds = new[] { 1, 2, 3 };
    /// Result&lt;IEnumerable&lt;User&gt;&gt; users = ResultCollections.Traverse(userIds, LoadUser);
    /// </code>
    /// </example>
    public static Result<IEnumerable<TOutput>> Traverse<TInput, TOutput>(
        IEnumerable<TInput> inputs,
        Func<TInput, Result<TOutput>> func)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(func);

        return Sequence(inputs.Select(func));
    }

    /// <summary>
    /// Maps a function over a collection and sequences the results using fail-fast semantics.
    /// </summary>
    /// <typeparam name="TInput">Type of input values</typeparam>
    /// <typeparam name="TOutput">Type of output values</typeparam>
    /// <param name="inputs">Collection of input values</param>
    /// <param name="func">Function to apply to each input</param>
    /// <returns>Result containing all transformed values if successful, or first error encountered</returns>
    public static Result<IEnumerable<TOutput>> TraverseFailFast<TInput, TOutput>(
        IEnumerable<TInput> inputs,
        Func<TInput, Result<TOutput>> func)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(func);

        return SequenceFailFast(inputs.Select(func));
    }

    /// <summary>
    /// Partitions a collection of Results into successful values and error messages.
    /// This allows you to process successes while still having access to all failures.
    /// </summary>
    /// <typeparam name="T">Type of successful values</typeparam>
    /// <param name="results">Collection of Results to partition</param>
    /// <returns>Tuple containing successful values and all error messages</returns>
    /// <example>
    /// <code>
    /// var (successes, failures) = ResultCollections.Partition(userResults);
    /// Console.WriteLine($"Loaded {successes.Count()} users, {failures.Count()} errors");
    /// </code>
    /// </example>
    public static (IEnumerable<T> Successes, IEnumerable<string> Failures) Partition<T>(
        IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var successes = new List<T>();
        var failures = new List<string>();

        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            if (result.IsSuccess)
            {
                successes.Add(result.Value!);
            }
            else if (result.Errors != null)
            {
                failures.AddRange(result.Errors);
            }
        }

        return (successes, failures);
    }

    /// <summary>
    /// Collects all successful values from a collection of Results, ignoring failures.
    /// This is useful when you want to process as many items as possible despite some failures.
    /// </summary>
    /// <typeparam name="T">Type of successful values</typeparam>
    /// <param name="results">Collection of Results to collect from</param>
    /// <returns>All successful values</returns>
    /// <example>
    /// <code>
    /// var validUsers = ResultCollections.Collect(userResults);
    /// Console.WriteLine($"Processing {validUsers.Count()} valid users");
    /// </code>
    /// </example>
    public static IEnumerable<T> Collect<T>(IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results
            .Where(result => result != null && result.IsSuccess)
            .Select(result => result.Value!);
    }

    /// <summary>
    /// Collects all error messages from a collection of Results, ignoring successes.
    /// Useful for logging or reporting all errors that occurred during batch processing.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="results">Collection of Results to collect errors from</param>
    /// <returns>All error messages from failed Results</returns>
    public static IEnumerable<string> CollectErrors<T>(IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results
            .Where(result => result != null && result.IsFailure)
            .SelectMany(result => result.Errors ?? []);
    }

    /// <summary>
    /// Filters a collection of Results to only successful ones.
    /// Returns only Results that are successful, maintaining their Result wrapper.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="results">Collection of Results to filter</param>
    /// <returns>Only successful Results</returns>
    public static IEnumerable<Result<T>> WhereSuccess<T>(IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(result => result != null && result.IsSuccess);
    }

    /// <summary>
    /// Filters a collection of Results to only failed ones.
    /// Returns only Results that have failed, maintaining their Result wrapper.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="results">Collection of Results to filter</param>
    /// <returns>Only failed Results</returns>
    public static IEnumerable<Result<T>> WhereFailure<T>(IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(result => result != null && result.IsFailure);
    }

    /// <summary>
    /// Extension method for fluent sequencing of Result collections.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="results">Collection of Results to sequence</param>
    /// <returns>Result containing all values if successful, or accumulated errors</returns>
    public static Result<IEnumerable<T>> SequenceResults<T>(this IEnumerable<Result<T>> results)
    {
        return Sequence(results);
    }

    /// <summary>
    /// Extension method for fluent traversal of collections.
    /// </summary>
    /// <typeparam name="TInput">Type of input values</typeparam>
    /// <typeparam name="TOutput">Type of output values</typeparam>
    /// <param name="inputs">Collection of input values</param>
    /// <param name="func">Function to apply to each input</param>
    /// <returns>Result containing all transformed values if successful, or accumulated errors</returns>
    public static Result<IEnumerable<TOutput>> TraverseResults<TInput, TOutput>(
        this IEnumerable<TInput> inputs,
        Func<TInput, Result<TOutput>> func)
    {
        return Traverse(inputs, func);
    }
}
