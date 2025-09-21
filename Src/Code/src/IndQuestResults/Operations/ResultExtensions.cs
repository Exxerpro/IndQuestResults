using IndQuestResults.Validation;
using System.Linq;
using System.Threading;

namespace IndQuestResults.Operations;

/// <summary>
/// Extension methods for Result and Result{T} providing ergonomic helpers for cancellation,
/// validation, chaining, and composition while preserving functional semantics.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Creates a result indicating that an operation was cancelled.
    /// </summary>
    /// <returns>A <see cref="Result"/> object representing a cancelled operation.</returns>
    public static Result Cancelled()
    {
        return Result.WithFailure(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Creates a generic result indicating that an operation was cancelled.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <returns>A <see cref="Result{T}"/> object representing a cancelled operation.</returns>
    public static Result<T> Cancelled<T>()
    {
        return Result<T>.WithFailure(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Determines whether the result represents a cancelled operation.
    /// </summary>
    /// <param name="result">The result to examine.</param>
    /// <returns>True if the result contains the standard cancellation error.</returns>
    public static bool IsCancelled(this Result result)
    {
        return result != null && result.Errors != null && result.Errors.Any(e => e == ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Determines whether the generic result represents a cancelled operation.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to examine.</param>
    /// <returns>True if the result contains the standard cancellation error.</returns>
    public static bool IsCancelled<T>(this Result<T> result)
    {
        return result != null && result.Errors != null && result.Errors.Any(e => e == ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Creates a failure result for a null argument with fluent syntax
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="parameterName">Name of the null parameter</param>
    /// <param name="message">Optional error message</param>
    /// <returns>Failed result with null argument error</returns>
    public static Result<T> FailForNullArgument<T>(string parameterName, string? message = null)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return Result<T>.WithFailure("Parameter name cannot be null or empty.");
        }

        var error = new NullArgumentError(parameterName, message);
        return Result<T>.WithFailure(error.ToString());
    }

    /// <summary>
    /// Creates a failure result for multiple null arguments with fluent syntax
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="parameterNames">Names of null parameters</param>
    /// <returns>Failed result with multiple null argument errors</returns>
    public static Result<T> FailForNullArguments<T>(params string[] parameterNames)
    {
        if (parameterNames == null || parameterNames.Length == 0 || parameterNames.Any(string.IsNullOrEmpty))
        {
            return Result<T>.WithFailure("Parameter names cannot be null, empty, or contain empty values.");
        }

        var error = new MultipleNullArgumentsError(parameterNames);
        return Result<T>.WithFailure(error.ToString());
    }

    /// <summary>
    /// Creates a non-generic failure result for a null argument with fluent syntax
    /// </summary>
    /// <param name="parameterName">Name of the null parameter</param>
    /// <param name="message">Optional error message</param>
    /// <returns>Failed result with null argument error</returns>
    public static Result FailForNullArgument(string parameterName, string? message = null)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return Result.WithFailure("Parameter name cannot be null or empty.");
        }

        var error = new NullArgumentError(parameterName, message);
        return Result.WithFailure(error.ToString());
    }

    /// <summary>
    /// Creates a non-generic failure result for multiple null arguments with fluent syntax
    /// </summary>
    /// <param name="parameterNames">Names of null parameters</param>
    /// <returns>Failed result with multiple null argument errors</returns>
    public static Result FailForNullArguments(params string[] parameterNames)
    {
        if (parameterNames == null || parameterNames.Length == 0 || parameterNames.Any(string.IsNullOrEmpty))
        {
            return Result.WithFailure("Parameter names cannot be null, empty, or contain empty values.");
        }

        var error = new MultipleNullArgumentsError(parameterNames);
        return Result.WithFailure(error.ToString());
    }

    /// <summary>
    /// Validates that a parameter is not null and returns appropriate result
    /// </summary>
    /// <typeparam name="T">Type of the parameter to validate</typeparam>
    /// <param name="value">Value to validate</param>
    /// <param name="parameterName">Name of the parameter</param>
    /// <returns>Success result if not null, failure result if null</returns>
    public static Result<T> EnsureNotNull<T>(T? value, string parameterName) where T : class
    {
        return string.IsNullOrEmpty(parameterName)
            ? Result<T>.WithFailure("Parameter name cannot be null or empty.")
            : value is null
            ? FailForNullArgument<T>(parameterName)
            : Result<T>.Success(value);
    }

    /// <summary>
    /// Validates that a nullable parameter is not null and returns appropriate result
    /// </summary>
    /// <typeparam name="T">Type of the parameter to validate</typeparam>
    /// <param name="value">Nullable value to validate</param>
    /// <param name="parameterName">Name of the parameter</param>
    /// <returns>Success result if has value, failure result if null</returns>
    public static Result<T> EnsureNotNull<T>(T? value, string parameterName) where T : struct
    {
        return string.IsNullOrEmpty(parameterName)
            ? Result<T>.WithFailure("Parameter name cannot be null or empty.")
            : value.HasValue
            ? Result<T>.Success(value.Value)
            : FailForNullArgument<T>(parameterName);
    }

    /// <summary>
    /// Validates multiple parameters and returns success or failure with all null parameter names
    /// </summary>
    /// <param name="validations">Array of parameter validations (value, parameterName)</param>
    /// <returns>Success result if all valid, failure result with all null parameter names</returns>
    public static Result ValidateNotNull(params (object? value, string parameterName)[] validations)
    {
        if (validations == null || validations.Length == 0)
        {
            return Result.WithFailure("Validations cannot be null or empty.");
        }

        var nullParameters = validations
            .Where(v => v.value is null)
            .Select(v => v.parameterName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();

        return nullParameters.Length switch
        {
            0 => Result.Success(),
            1 => FailForNullArgument(nullParameters[0]),
            _ => FailForNullArguments(nullParameters)
        };
    }

    /// <summary>
    /// Fluent validation chain for multiple parameters
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="factory">Factory function to create result if all validations pass</param>
    /// <param name="validations">Array of parameter validations</param>
    /// <returns>Success result with factory value or failure with validation errors</returns>
    public static Result<T> CreateIfValid<T>(Func<T> factory, params (object? value, string parameterName)[] validations)
    {
        if (factory == null)
        {
            return Result<T>.WithFailure("Factory function cannot be null.");
        }

        if (validations == null)
        {
            return Result<T>.WithFailure("Validations cannot be null.");
        }

        var nullParameters = validations
            .Where(v => v.value is null)
            .Select(v => v.parameterName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();

        return nullParameters.Length switch
        {
            0 => Result<T>.Success(factory()),
            1 => FailForNullArgument<T>(nullParameters[0]),
            _ => FailForNullArguments<T>(nullParameters)
        };
    }

    /// <summary>
    /// Chains an async operation that returns Result&lt;TOut&gt; when the previous operation succeeds.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="next">The next operation to execute.</param>
    /// <returns>A task containing the result of the chained operation.</returns>

    public static async Task<Result<TOut>> ThenAsync<TIn, TOut>(
            this Task<Result<TIn>> resultTask,
            Func<TIn, Task<Result<TOut>>> next)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? await next(result.Value).ConfigureAwait(false)
            : Result<TOut>.WithFailure(result.Errors);
    }

    // (Conventional async wrappers like BindAsync/MapAsync/TapAsync are available under IndQuestResults.Async.ResultAsync.)

    /// <summary>
    /// Maps a successful result value synchronously within an async chain.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A task containing the mapped result.</returns>
    public static async Task<Result<TOut>> ThenMap<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? Result<TOut>.Success(mapper(result.Value))
            : Result<TOut>.WithFailure(result.Errors);
    }

    /// <summary>
    /// Executes a side effect without breaking the result chain.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="action">The side effect action.</param>
    /// <returns>The original result task.</returns>
    public static async Task<Result<T>> ThenTap<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            await action(result.Value).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Executes a synchronous side effect without breaking the result chain.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="action">The side effect action.</param>
    /// <returns>The original result task.</returns>
    public static async Task<Result<T>> ThenDo<T>(
        this Task<Result<T>> resultTask,
        Action<T> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            action(result.Value);
        }
        return result;
    }

    /// <summary>
    /// Validates the result value and returns failure if validation fails.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="validator">The validation function.</param>
    /// <returns>A task containing the validated result.</returns>
    public static async Task<Result<T>> ThenValidate<T>(
        this Task<Result<T>> resultTask,
        Func<T, Result> validator)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result;
        }

        var validation = validator(result.Value!);
        return validation.IsSuccess
            ? result
            : Result<T>.WithFailure(validation.Errors, result.Value);
    }

    /// <summary>
    /// Validates the result value asynchronously.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="validator">The async validation function.</param>
    /// <returns>A task containing the validated result.</returns>
    public static async Task<Result<T>> ThenValidateAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result>> validator)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        { return result; }

        var validation = await validator(result.Value!).ConfigureAwait(false);
        return validation.IsSuccess
            ? result
            : Result<T>.WithFailure(validation.Errors, result.Value);
    }

    // (Async recovery helpers are available under IndQuestResults.Async.ResultAsync.)

    /// <summary>
    /// Ensures a condition is met, otherwise returns failure.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="predicate">The condition to check.</param>
    /// <param name="errorMessage">The error message if condition fails.</param>
    /// <returns>A task containing the result.</returns>
    public static async Task<Result<T>> ThenEnsure<T>(
        this Task<Result<T>> resultTask,
        Func<T, bool> predicate,
        string errorMessage)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure
            ? result
            : predicate(result.Value!)
            ? result
            : Result<T>.WithFailure(errorMessage, result.Value);
    }

    /// <summary>
    /// Combines two results into a tuple result.
    /// </summary>
    /// <typeparam name="T1">The first result type.</typeparam>
    /// <typeparam name="T2">The second result type.</typeparam>
    /// <param name="first">The first result task.</param>
    /// <param name="second">The second result task.</param>
    /// <returns>A task containing the combined result.</returns>
    public static async Task<Result<(T1, T2)>> CombineAsync<T1, T2>(
        this Task<Result<T1>> first,
        Task<Result<T2>> second)
    {
        var r1 = await first.ConfigureAwait(false);
        var r2 = await second.ConfigureAwait(false);

        if (r1.IsSuccess && r2.IsSuccess)
        { return Result<(T1, T2)>.Success((r1.Value!, r2.Value!)); }

        var errors = new List<string>();
        if (r1.IsFailure)
        {
            errors.AddRange(r1.Errors);
        }
        if (r2.IsFailure)
        {
            errors.AddRange(r2.Errors);
        }

        return Result<(T1, T2)>.WithFailure(errors);
    }

    /// <summary>
    /// Combines three results into a tuple result.
    /// </summary>
    /// <typeparam name="T1">The first result type.</typeparam>
    /// <typeparam name="T2">The second result type.</typeparam>
    /// <typeparam name="T3">The third result type.</typeparam>
    /// <param name="first">The first result task.</param>
    /// <param name="second">The second result task.</param>
    /// <param name="third">The third result task.</param>
    /// <returns>A task containing the combined result.</returns>
    public static async Task<Result<(T1, T2, T3)>> CombineAsync<T1, T2, T3>(
        this Task<Result<T1>> first,
        Task<Result<T2>> second,
        Task<Result<T3>> third)
    {
        var r1 = await first.ConfigureAwait(false);
        var r2 = await second.ConfigureAwait(false);
        var r3 = await third.ConfigureAwait(false);

        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess)
        { return Result<(T1, T2, T3)>.Success((r1.Value!, r2.Value!, r3.Value!)); }

        var errors = new List<string>();
        if (r1.IsFailure)
        { errors.AddRange(r1.Errors); }
        if (r2.IsFailure)
        { errors.AddRange(r2.Errors); }

        if (r3.IsFailure)
        { errors.AddRange(r3.Errors); }

        return Result<(T1, T2, T3)>.WithFailure(errors);
    }

    /// <summary>
    /// Switches between two operations based on a condition.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="onTrue">Operation to execute when condition is true.</param>
    /// <param name="onFalse">Operation to execute when condition is false.</param>
    /// <returns>A task containing the result of the selected operation.</returns>
    public static async Task<Result<T>> ThenSwitch<T>(
        this Task<Result<T>> resultTask,
        Func<T, bool> condition,
        Func<T, Task<Result<T>>> onTrue,
        Func<T, Task<Result<T>>> onFalse)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure
            ? result
            : condition(result.Value!)
            ? await onTrue(result.Value!).ConfigureAwait(false)
            : await onFalse(result.Value!).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles errors by attempting recovery.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="recover">The recovery function.</param>
    /// <returns>A task containing the original or recovered result.</returns>
    public static async Task<Result<T>> ThenRecover<T>(
        this Task<Result<T>> resultTask,
        Func<IEnumerable<string>, Task<Result<T>>> recover)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? result
            : await recover(result.Errors).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs errors without breaking the chain.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="logError">The error logging action.</param>
    /// <returns>The original result task.</returns>
    public static async Task<Result<T>> ThenLogErrors<T>(
        this Task<Result<T>> resultTask,
        Action<IEnumerable<string>> logError)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            logError(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Aggregates multiple results and executes an action if all succeed.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="results">The collection of results.</param>
    /// <param name="onSuccess">Action to execute when all succeed.</param>
    /// <returns>A result indicating overall success or aggregated failures.</returns>
    public static async Task<Result> WhenAllAsync<T>(
        IEnumerable<Task<Result<T>>> results,
        Func<IEnumerable<T>, Task> onSuccess)
    {
        var resultArray = await Task.WhenAll(results).ConfigureAwait(false);
        var failures = resultArray.Where(r => r.IsFailure).ToList();

        if (failures.Count != 0)
        {
            var errors = failures.SelectMany(f => f.Errors);
            return Result.WithFailure(errors);
        }

        var values = resultArray.Select(r => r.Value!);
        await onSuccess(values).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Converts a failed result to a different type while preserving errors.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A failed result of the target type.</returns>
    public static Result<TOut> ToFailureOf<TIn, TOut>(this Result<TIn> result)
    {
        return result.IsSuccess
            ? throw new InvalidOperationException("Cannot convert successful result to failure")
            : Result<TOut>.WithFailure(result.Errors);
    }

    /// <summary>
    /// Chains operations with cancellation support.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="next">The next operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result or cancellation failure.</returns>
    public static async Task<Result<TOut>> ThenAsyncCancellable<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, Task<Result<TOut>>> next,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        { return Cancelled<TOut>(); }

        var result = await resultTask.ConfigureAwait(false);

        return result.IsSuccess && result.Value is not null
            ? await next(result.Value, cancellationToken).ConfigureAwait(false)
            : Result<TOut>.WithFailure(result.Errors);
    }

    /// <summary>
    /// Provides a default value for failed results.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="defaultValue">The default value to use on failure.</param>
    /// <returns>A task containing the original or default value.</returns>
    public static async Task<T> DefaultIfFailure<T>(
        this Task<Result<T>> resultTask,
        T defaultValue)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? result.Value
            : defaultValue;
    }

    // (Async collection helpers like SequenceAsync/TraverseAsync/TraverseParallelAsync are available under IndQuestResults.Async.ResultAsync.)
}
