using IndQuestResults.Validation;
using IndQuestResults.Async;
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
        return await resultTask.BindAsync(next).ConfigureAwait(false);
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
        return result.Map(mapper);
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
        return await resultTask.TapAsync(action).ConfigureAwait(false);
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
        return result.Tap(action);
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
        return result.Bind(value => 
        {
            var validation = validator(value);
            return validation.IsSuccess
                ? Result<T>.Success(value)
                : Result<T>.WithFailure(validation.Errors, value);
        });
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
        return await Task.FromResult(result).BindAsync(async value =>
        {
            var validation = await validator(value).ConfigureAwait(false);
            return validation.IsSuccess
                ? Result<T>.Success(value)
                : Result<T>.WithFailure(validation.Errors, value);
        }).ConfigureAwait(false);
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
        return result.Ensure(predicate, errorMessage);
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
        return await Task.FromResult(result).BindAsync(value => 
            condition(value) 
                ? onTrue(value) 
                : onFalse(value)
        ).ConfigureAwait(false);
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
            : await recover(result.Errors ?? [ResultConstants.DefaultErrorMessage]).ConfigureAwait(false);
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
        return result.TapError(logError);
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
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<TOut>.WithFailure("Result cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsSuccess
            ? Result<TOut>.WithFailure("Cannot convert successful result to failure")
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

    /// <summary>
    /// Async alias over <see cref="ThenEnsure{T}(Task{Result{T}}, Func{T, bool}, string)"/>; restores sync/async symmetry
    /// with the synchronous <c>Result&lt;T&gt;.Ensure</c>.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="predicate">The condition to check, receiving the value.</param>
    /// <param name="errorMessage">The error message if the condition fails.</param>
    /// <returns>A task containing the validated result. Upstream failures short-circuit unchanged.</returns>
    public static Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, string errorMessage)
    {
        return resultTask.ThenEnsure(predicate, errorMessage);
    }

    /// <summary>
    /// Chains a successful result into another result, flattening the railway (preferred bind overload).
    /// Selected when the delegate returns a <see cref="Result{T}"/>; produces no nested <c>Result&lt;Result&lt;TOut&gt;&gt;</c>.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="bind">The function returning the next <see cref="Result{T}"/>.</param>
    /// <returns>The bound <see cref="Result{T}"/>, propagating failures unchanged.</returns>
    public static Result<TOut> Then<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind)
    {
        return result.Bind(bind);
    }

    /// <summary>
    /// Projects a successful result value into a new value (fallback map overload).
    /// Selected when the delegate returns a plain value rather than a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="map">The projection function.</param>
    /// <returns>The mapped <see cref="Result{T}"/>, propagating failures unchanged.</returns>
    public static Result<TOut> Then<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
    {
        return result.Map(map);
    }

    /// <summary>
    /// Chains a successful async result into an async result, flattening the railway (async bind overload).
    /// Distinct by parameter type from the value-map overload; never collides with it.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="bindAsync">The async function returning the next <see cref="Result{T}"/>.</param>
    /// <returns>A task containing the bound result, propagating failures unchanged.</returns>
    public static Task<Result<TOut>> Then<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> bindAsync)
    {
        return resultTask.ThenAsync(bindAsync);
    }

    /// <summary>
    /// Chains a successful async result into a synchronous result, flattening the railway (preferred sync-bind overload in an async chain).
    /// Selected when the delegate returns a <see cref="Result{T}"/>; produces no nested <c>Result&lt;Result&lt;TOut&gt;&gt;</c>.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="bind">The function returning the next <see cref="Result{T}"/>.</param>
    /// <returns>A task containing the bound result, propagating failures unchanged.</returns>
    public static Task<Result<TOut>> Then<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Result<TOut>> bind)
    {
        return resultTask.ThenAsync(value => Task.FromResult(bind(value)));
    }

    /// <summary>
    /// Projects a successful async result value into a new value (fallback map overload).
    /// Selected when the delegate returns a plain value rather than a <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="map">The projection function.</param>
    /// <returns>A task containing the mapped result, propagating failures unchanged.</returns>
    public static Task<Result<TOut>> Then<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> map)
    {
        return resultTask.ThenMap(map);
    }

    /// <summary>
    /// Awaits a nullable-reference task and adapts it to a <see cref="Result{T}"/>:
    /// a null value becomes a failure carrying <paramref name="errorMessage"/>; a non-null value succeeds.
    /// </summary>
    /// <typeparam name="T">The reference type produced by the task.</typeparam>
    /// <param name="task">The task producing a possibly-null value.</param>
    /// <param name="errorMessage">The failure message used when the awaited value is null.</param>
    /// <returns>A success carrying the value, or a failure carrying <paramref name="errorMessage"/> when null.</returns>
    public static async Task<Result<T>> ToResult<T>(this Task<T?> task, string errorMessage) where T : class
    {
        var value = await task.ConfigureAwait(false);
        return value is null ? Result<T>.WithFailure(errorMessage) : Result<T>.Success(value);
    }

    /// <summary>
    /// Awaits a result task and adapts it to a non-null <see cref="Result{T}"/> with documented precedence:
    /// an existing failure propagates first (its errors preserved); only a successful-but-null value becomes a
    /// failure carrying <paramref name="errorMessage"/>; a successful non-null value succeeds.
    /// </summary>
    /// <typeparam name="T">The reference type produced by the result.</typeparam>
    /// <param name="task">The task producing a <see cref="Result{T}"/> whose value may be null.</param>
    /// <param name="errorMessage">The failure message used when the awaited success value is null.</param>
    /// <returns>The propagated failure, a failure carrying <paramref name="errorMessage"/> for success-but-null, or a success.</returns>
    public static async Task<Result<T>> ToResult<T>(this Task<Result<T>> task, string errorMessage) where T : class
    {
        var result = await task.ConfigureAwait(false);
        return result.IsFailure
            ? Result<T>.WithFailure(result.Errors)
            : result.Value is null
                ? Result<T>.WithFailure(errorMessage)
                : Result<T>.Success(result.Value);
    }

    /// <summary>
    /// Awaits a result whose value is a nullable reference (<see cref="Result{T}"/> of <c>T?</c>) and adapts it to a
    /// non-null <see cref="Result{T}"/> with documented precedence: an existing failure propagates first (its errors
    /// preserved); only a successful-but-null value becomes a failure carrying <paramref name="errorMessage"/>; a
    /// successful non-null value succeeds carrying the now-non-null value.
    /// </summary>
    /// <remarks>
    /// Distinct from <c>ToResult</c> by name, not signature: a <c>ToResult&lt;T&gt;(this Task&lt;Result&lt;T?&gt;&gt;, string)</c>
    /// overload is impossible because nullable reference-type annotations are erased in CLR metadata, so it would share
    /// the same signature as the existing <c>ToResult&lt;T&gt;(this Task&lt;Result&lt;T&gt;&gt;, string)</c> and produce
    /// CS0111 (duplicate member). This verb fills the gap for repositories that return <c>Result&lt;T?&gt;</c>.
    /// </remarks>
    /// <typeparam name="T">The reference type produced by the result.</typeparam>
    /// <param name="task">The task producing a <see cref="Result{T}"/> whose value is a nullable reference.</param>
    /// <param name="errorMessage">The failure message used when the awaited success value is null.</param>
    /// <returns>The propagated failure, a failure carrying <paramref name="errorMessage"/> for success-but-null, or a success carrying the non-null value.</returns>
    public static async Task<Result<T>> RequireValue<T>(this Task<Result<T?>> task, string errorMessage) where T : class
    {
        var result = await task.ConfigureAwait(false);
        return result.RequireValue(errorMessage);
    }

    /// <summary>
    /// Adapts a result whose value is a nullable reference (<see cref="Result{T}"/> of <c>T?</c>) to a non-null
    /// <see cref="Result{T}"/> with the same precedence as the asynchronous overload: an existing failure propagates
    /// first (its errors preserved); only a successful-but-null value becomes a failure carrying
    /// <paramref name="errorMessage"/>; a successful non-null value succeeds carrying the now-non-null value.
    /// </summary>
    /// <typeparam name="T">The reference type produced by the result.</typeparam>
    /// <param name="result">The result whose value is a nullable reference.</param>
    /// <param name="errorMessage">The failure message used when the success value is null.</param>
    /// <returns>The propagated failure, a failure carrying <paramref name="errorMessage"/> for success-but-null, or a success carrying the non-null value.</returns>
    public static Result<T> RequireValue<T>(this Result<T?> result, string errorMessage) where T : class
    {
        return result.IsFailure
            ? Result<T>.WithFailure(result.Errors)
            : result.Value is null
                ? Result<T>.WithFailure(errorMessage)
                : Result<T>.Success(result.Value);
    }

    /// <summary>
    /// Validates that a component selected from the current value is not null, continuing the railway when it is
    /// present and failing with a <see cref="Validation.NullArgumentError"/>-style message when it is null.
    /// Upstream failures short-circuit unchanged.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="selector">Selects the component to check and its parameter name.</param>
    /// <returns>A task containing the original result, or a failure describing the null argument.</returns>
    public static Task<Result<T>> ValidateNotNull<T>(this Result<T> result, Func<T?, (object? value, string parameterName)> selector)
    {
        if (result.IsFailure)
        {
            return Task.FromResult(result);
        }

        var (value, parameterName) = selector(result.Value);
        return Task.FromResult(
            string.IsNullOrEmpty(parameterName)
                ? Result<T>.WithFailure("Parameter name cannot be null or empty.")
                : value is null
                    ? FailForNullArgument<T>(parameterName)
                    : result);
    }

    // (Async collection helpers like SequenceAsync/TraverseAsync/TraverseParallelAsync are available under IndQuestResults.Async.ResultAsync.)
}
