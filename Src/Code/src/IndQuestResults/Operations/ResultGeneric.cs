using System.Text.Json.Serialization;

namespace IndQuestResults.Operations;

/// <summary>
/// Represents the result of an operation that returns a value, including success status, value, and error messages.
/// This generic version extends the Result pattern to include strongly-typed return values with comprehensive error handling.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
/// <remarks>
/// <para><strong>Result&lt;T&gt; Specific Features:</strong></para>
/// <list type="bullet">
/// <item><strong>Strongly-Typed Values:</strong> Type-safe access to operation results</item>
/// <item><strong>Null Safety:</strong> Explicit handling of nullable return types</item>
/// <item><strong>Warning Support:</strong> Successful operations can include diagnostic warnings</item>
/// <item><strong>Functional Composition:</strong> Map, Bind, and Match operations for chaining</item>
/// <item><strong>State Semantics:</strong> Clear distinction between success, warnings, and failures</item>
/// <item><strong>Implicit Conversions:</strong> Seamless conversion from values to results</item>
/// </list>
///
/// <para><strong>Value Semantics:</strong></para>
/// <list type="bullet">
/// <item><strong>IsSuccess:</strong> Operation succeeded and value is not null</item>
/// <item><strong>IsSuccessMayBeNull:</strong> Operation succeeded (value may be null for nullable types)</item>
/// <item><strong>HasWarnings:</strong> Successful operation with diagnostic messages</item>
/// <item><strong>IsFailure:</strong> Operation failed</item>
/// <item><strong>IsRecoverable:</strong> Operation can be recovered from (alias for IsSuccess)</item>
/// </list>
/// </remarks>
public sealed class Result<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class for deserialization.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="errors">A collection of error messages.</param>
    /// <param name="value">The value returned by the operation.</param>
    [JsonConstructor]
    public Result(bool isSuccess, IEnumerable<string>? errors, T? value = default)
    {
        IsRecoverable = isSuccess;
        var errorArray = errors?.ToArray() ?? [];
        HasErrors = errorArray.Length > 0;
        Errors = errorArray;
        Value = value;

        // Validate state consistency after deserialization
        ValidateInternalState();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with a list of errors.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="errors">A list of error messages.</param>
    /// <param name="value">The value returned by the operation.</param>
    public Result(bool isSuccess, List<string>? errors, T? value = default)
    {
        IsRecoverable = isSuccess;
        var errorArray = errors?.ToArray() ?? [];
        HasErrors = errorArray.Length > 0;
        Errors = errorArray;
        Value = value;
    }

    /// <summary>
    /// Validates the internal state consistency of the Result object.
    /// </summary>
    private void ValidateInternalState()
    {
        // Check for inconsistent states that could indicate deserialization issues
        var actualHasErrors = Errors?.Any() == true;

        if (HasErrors != actualHasErrors)
        {
            // Log warning but don't throw - fix the inconsistency
            HasErrors = actualHasErrors;
        }
    }

    /// <summary>
    /// Gets the value associated with the result, or null if the operation failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// A result is considered successful if it was explicitly marked as successful
    /// (warnings do not affect success status - they are just diagnostic information).
    /// A Success result can still have a null Value, which is valid in the Result{T} pattern.
    /// Null defense: Result{T} allows null values as valid success results when T is nullable.
    /// </summary>
    public bool IsSuccessMayBeNull => IsRecoverable;

    /// <summary>
    /// Gets a value indicating whether the result is a success and the value is not null.
    /// </summary>
    public bool IsSuccess => IsRecoverable && (Value is not null);

    /// <summary>
    /// Gets a value indicating whether the result is a success and the value is not null.
    /// </summary>
    public bool IsSuccessNotNull => IsRecoverable && (Value is not null);

    /// <summary>
    /// Gets a value indicating whether the result is a success but the value is null.
    /// </summary>
    public bool IsSuccessValueNull => IsRecoverable && (Value is null);

    /// <summary>
    /// Gets a value indicating whether the result has warnings or error messages.
    /// This includes both diagnostic warnings (for successful operations) and error messages (for failures).
    /// </summary>
    public bool HasErrors { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the result has warnings (i.e., is successful but contains diagnostic messages).
    /// </summary>
    public bool HasWarnings => IsRecoverable && HasErrors;

    /// <summary>
    /// Gets a value indicating whether the result is recoverable (successful operations, even with warnings).
    /// </summary>
    public bool IsRecoverable { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsRecoverable;

    /// <summary>
    /// Gets the collection of error messages associated with the result.
    /// </summary>
    public IEnumerable<string> Errors { get; init; }

    /// <summary>
    /// Gets the first non-empty error message, or null if none exist.
    /// </summary>
    public string? Error => Errors?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

    /// <summary>
    /// Creates a successful result with the specified value.
    /// Follows industry standard Result&lt;T&gt; pattern: null values are valid success results when T is nullable.
    /// </summary>
    /// <param name="data">The value associated with the result.</param>
    /// <returns>A successful <see cref="Result{T}"/> instance.</returns>
    public static Result<T> Success(T data)
    {
        return new Result<T>(true, Array.Empty<string>(), data);
    }

    /// <summary>
    /// Creates a successful result with the specified value (alias for Success).
    /// Follows industry standard Result&lt;T&gt; pattern: null values are valid success results when T is nullable.
    /// </summary>
    /// <param name="data">The value associated with the result.</param>
    /// <returns>A successful <see cref="Result{T}"/> instance.</returns>
    public static Result<T> WithSuccess(T data)
    {
        return new Result<T>(true, Array.Empty<string>(), data);
    }

    /// <summary>
    /// Returns a string representation of the result, showing either the value or the list of errors.
    /// </summary>
    public override string ToString()
    {
        return IsRecoverable
            ? $"{ResultConstants.SuccessPrefix}: {Value?.ToString()}"
            : Result.FormatErrorsString(Errors, ResultConstants.FailurePrefix);
    }

    /// <summary>
    /// Creates a failed result with the specified errors and optional value.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    /// <param name="value">The value to associate with the result (optional).</param>
    /// <returns>A failed <see cref="Result{T}"/> instance.</returns>
    public static Result<T> WithFailure(IEnumerable<string>? errors, T? value = default)
    {
        // Use the provided errors or fall back to the default error message
        var errorArray = errors?.ToArray();
        if (errorArray is null || errorArray.Length == 0)
        {
            errorArray = [ResultConstants.DefaultErrorMessage];
        }
        return new Result<T>(false, errorArray, value);
    }

    /// <summary>
    /// Creates a failed result with the specified errors and optional value.
    /// </summary>
    /// <param name="value">The value to associate with the result (optional).</param>
    /// <param name="errors">The collection of error messages.</param>
    /// <returns>A failed <see cref="Result{T}"/> instance.</returns>
    public static Result<T> WithFailure(T? value = default, IEnumerable<string>? errors = default)
    {
        // Use the provided errors or fall back to the default error message
        var errorArray = errors?.ToArray();
        if (errorArray is null || errorArray.Length == 0)
        {
            errorArray = [ResultConstants.DefaultErrorMessage];
        }
        return new Result<T>(false, errorArray, value);
    }

    /// <summary>
    /// Creates a successful result with warnings (non-fatal diagnostics).
    /// </summary>
    /// <param name="warnings">The collection of warning messages.</param>
    /// <param name="value">The value to associate with the result.</param>
    /// <returns>A successful <see cref="Result{T}"/> instance with warnings.</returns>
    public static Result<T> WithWarnings(IEnumerable<string> warnings, T value)
    {
        var warningArray = warnings?.ToArray();
        if (warningArray is null || warningArray.Length == 0)
        {
            warningArray = [ResultConstants.DefaultWarningMessage];
        }
        return new Result<T>(true, warningArray, value);
    }

    /// <summary>
    /// Creates a failed result with the specified errors and optional value (overload for string array).
    /// </summary>
    /// <param name="errors">The array of error messages.</param>
    /// <param name="value">The value to associate with the result (optional).</param>
    /// <returns>A failed <see cref="Result{T}"/> instance.</returns>
    public static Result<T> WithFailure(string[] errors, T? value = default)
    {
        // Check for empty array and provide default error message (consistent with IEnumerable overload)
        if (errors is null || errors.Length == 0)
        {
            errors = [ResultConstants.DefaultErrorMessage];
        }
        return new Result<T>(false, errors, value);
    }

    /// <summary>
    /// Creates a failed result with a single error message and optional value.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="value">The value to associate with the result (optional).</param>
    /// <returns>A failed <see cref="Result{T}"/> instance.</returns>
    public static Result<T> WithFailure(string error, T? value = default)
    {
        return new Result<T>(false, [error], value);
    }

    /// <summary>
    /// Implicitly converts a value of type T to a successful <see cref="Result{T}"/>.
    /// Follows industry standard Result&lt;T&gt; pattern: null values are valid success results when T is nullable.
    /// </summary>
    /// <param name="data">The value to convert.</param>
    public static implicit operator Result<T>(T data)
    {
        return Success(data);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Result{T}"/> to a non-generic <see cref="Result"/>.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static implicit operator Result(Result<T> result)
    {
        return result.IsRecoverable ? Result.Success() : Result.WithFailure(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
    }

    /// <summary>
    /// Deconstructs the result into its success state, value, and errors.
    /// </summary>
    /// <param name="succeeded">Indicates whether the operation succeeded.</param>
    /// <param name="data">The value returned by the operation.</param>
    /// <param name="errors">The collection of error messages.</param>
    public void Deconstruct(out bool succeeded, out T? data, out IEnumerable<string> errors)
    {
        succeeded = IsRecoverable;
        data = Value;
        errors = Errors ?? [];
    }

    /// <summary>
    /// Executes the specified action if the result is successful.
    /// Follows industry standard Result&lt;T&gt; pattern: executes for all successful results when T is nullable.
    /// For non-nullable types, only executes when value is not null to respect C# developer expectations.
    /// </summary>
    /// <param name="action">The action to execute on success, receiving the value.</param>
    /// <returns>The current <see cref="Result{T}"/> instance.</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (!IsRecoverable)
        {
            return this;
        }

        // Check if T is nullable
        bool isNullableType = typeof(T).IsClass ||
                           Nullable.GetUnderlyingType(typeof(T)) != null ||
                           !typeof(T).IsValueType;

        // Execute action for successful results:
        // - Always execute if value is not null
        // - Execute if value is null but T is explicitly nullable
        if (Value is not null || isNullableType)
        {
            action(Value!);
        }

        return this;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute on failure, receiving the error messages.</param>
    /// <returns>The current <see cref="Result{T}"/> instance.</returns>
    public Result<T> OnFailure(Action<IEnumerable<string>> action)
    {
        if (IsFailure)
        {
            if (Errors is not null && HasErrors)
            {
                action(Errors);
            }
            else
            {
                action([ResultConstants.DefaultErrorMessage]);
            }
        }
        return this;
    }

    /// <summary>
    /// Maps a successful result to a <see cref="Result{TOut}"/> using the provided function, or propagates errors.
    /// Follows industry standard Result&lt;T&gt; pattern: maps successful results when T is nullable or value is not null.
    /// </summary>
    /// <typeparam name="TOut">The type of the value to return on success.</typeparam>
    /// <param name="func">The function to execute on success.</param>
    /// <returns>A <see cref="Result{TOut}"/> representing the outcome.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> func)
    {
        if (!IsRecoverable)
        {
            return Result<TOut>.WithFailure(Errors);
        }

        // Check if T is nullable
        bool isNullableType = typeof(T).IsClass ||
                           Nullable.GetUnderlyingType(typeof(T)) != null ||
                           !typeof(T).IsValueType;

        // Execute function for successful results:
        // - Always execute if value is not null
        // - Execute if value is null but T is explicitly nullable
        if (Value is not null || isNullableType)
        {
            return Result<TOut>.Success(func(Value!));
        }

        // For non-nullable types with null values, propagate as failure
        return Result<TOut>.WithFailure("Cannot map null value for non-nullable type");
    }

    /// <summary>
    /// Binds a successful result to another <see cref="Result{TOut}"/> using the provided function, or propagates errors.
    /// Follows industry standard Result&lt;T&gt; pattern: binds successful results when T is nullable or value is not null.
    /// </summary>
    /// <typeparam name="TOut">The type of the value to return on success.</typeparam>
    /// <param name="func">The function to execute on success.</param>
    /// <returns>A <see cref="Result{TOut}"/> representing the outcome.</returns>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func)
    {
        if (!IsRecoverable)
        {
            return Result<TOut>.WithFailure(Errors);
        }

        // Check if T is nullable
        bool isNullableType = typeof(T).IsClass ||
                           Nullable.GetUnderlyingType(typeof(T)) != null ||
                           !typeof(T).IsValueType;

        // Execute function for successful results:
        // - Always execute if value is not null
        // - Execute if value is null but T is explicitly nullable
        if (Value is not null || isNullableType)
        {
            return func(Value!);
        }

        // For non-nullable types with null values, propagate as failure
        return Result<TOut>.WithFailure("Cannot bind null value for non-nullable type");
    }

    /// <summary>
    /// Ensures a condition is met for a successful result, otherwise returns a failure with the specified error message.
    /// </summary>
    /// <param name="condition">The condition to check, receiving the value.</param>
    /// <param name="errorMessage">The error message if the condition fails.</param>
    /// <returns>A <see cref="Result{T}"/> representing the outcome.</returns>
    public Result<T> Ensure(Func<T, bool> condition, string errorMessage)
    {
        return !IsRecoverable
            ? this
            : Value is null
            ? WithFailure(ResultConstants.ConditionEvaluationWithNullValue)
            : !condition(Value) ? WithFailure(errorMessage) : this;
    }

    /// <summary>
    /// Executes the specified action if the result is successful, returning the current result.
    /// Follows industry standard Result&lt;T&gt; pattern: executes for all successful results when T is nullable.
    /// For non-nullable types, only executes when value is not null to respect C# developer expectations.
    /// </summary>
    /// <param name="action">The action to execute on success, receiving the value.</param>
    /// <returns>The current <see cref="Result{T}"/> instance.</returns>
    public Result<T> Tap(Action<T> action)
    {
        if (!IsRecoverable)
        {
            return this;
        }

        // Check if T is nullable
        bool isNullableType = typeof(T).IsClass ||
                           Nullable.GetUnderlyingType(typeof(T)) != null ||
                           !typeof(T).IsValueType;

        // Execute action for successful results:
        // - Always execute if value is not null
        // - Execute if value is null but T is explicitly nullable
        if (Value is not null || isNullableType)
        {
            action(Value!);
        }

        return this;
    }

    /// <summary>
    /// Combines multiple non-generic results, aggregating all errors. Returns success if all are successful.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A <see cref="Result{T}"/> representing the combined outcome, with the current value if successful.</returns>
    public Result<T> Combine(params Result[] results)
    {
        if (results is null || results.Length == 0)
        {
            return this;
        }

        List<string> errorList = [];

        // Add current result's errors if it's a failure
        if (IsFailure && Errors is not null)
        {
            errorList.AddRange(Errors);
        }

        // Add errors from all failed results
        foreach (var result in results)
        {
            if (result.IsFailure && result.Errors is not null)
            {
                errorList.AddRange(result.Errors);
            }
        }

        if (errorList.Count > 0)
        {
            return Result<T>.WithFailure(errorList);
        }

        // All operations succeeded - return the current successful result (null values are valid)
        return IsRecoverable && Value is not null
            ? Result<T>.Success(Value)
            : this;
    }

    /// <summary>
    /// Matches the result to either a success or failure function.
    /// Follows industry standard Result&lt;T&gt; pattern: calls success function for all successful operations.
    /// </summary>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="onSuccess">Function to execute on success, receiving the value.</param>
    /// <param name="onFailure">Function to execute on failure, receiving the errors.</param>
    /// <returns>The result of the executed function.</returns>
    public Result<TOut> Match<TOut>(Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure)
    {
        if (!IsRecoverable)
        {
            IEnumerable<string> errs = (Errors?.Any() == true) ? Errors : [ResultConstants.DefaultErrorMessage];
            return Result<TOut>.Success(onFailure(errs));
        }

        // Industry standard: successful operations should call success function regardless of null values
        return Result<TOut>.Success(onSuccess(Value!));
    }

    /// <summary>
    /// Recovers from a failure by executing the provided recovery function.
    /// </summary>
    /// <param name="recoverFunc">The function to execute on failure.</param>
    /// <returns>The recovered or original <see cref="Result{T}"/>.</returns>
    public Result<T> Recover(Func<Result<T>> recoverFunc)
    {
        return IsFailure ? recoverFunc() : this;
    }

    /// <summary>
    /// Recovers from a failure by executing the provided recovery function, returning a result of a different type.
    /// </summary>
    /// <typeparam name="TOut">The type of the value to return on recovery.</typeparam>
    /// <param name="recoverFunc">The function to execute on failure.</param>
    /// <returns>The recovered <see cref="Result{TOut}"/> or a successful result with the current value.</returns>
    public Result<TOut> RecoverWith<TOut>(Func<Result<TOut>> recoverFunc)
    {
        if (IsFailure)
        {
            return recoverFunc();
        }

        // Safe type conversion: only proceed if Value can be safely converted to TOut
        if (Value is TOut convertedValue)
        {
            return Result<TOut>.Success(convertedValue);
        }

        // If types are incompatible, return a failure instead of throwing
        return Result<TOut>.WithFailure(string.Format(System.Globalization.CultureInfo.InvariantCulture, ResultConstants.RecoverWithTypeConversionErrorFormat, typeof(T).Name, typeof(TOut).Name));
    }

    /// <summary>
    /// Combines two sets of errors into a single failed result of type Result&lt;TOut&gt;, or a default failure if both are empty.
    /// </summary>
    /// <typeparam name="TOut">The type of the value to associate with the result.</typeparam>
    /// <param name="primaryErrors">The primary error messages.</param>
    /// <param name="secondaryErrors">The secondary error messages.</param>
    /// <param name="value">The value to associate with the result (optional).</param>
    /// <returns>A failed result with all errors.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "This is a utility method that provides type-safe error combining for Result<T> types")]
    public static Result<TOut> CombineErrors<TOut>(IEnumerable<string>? primaryErrors, IEnumerable<string>? secondaryErrors, TOut? value = default)
    {
        List<string> errorList = [];

        if (primaryErrors is not null)
        {
            errorList.AddRange(primaryErrors);
        }

        if (secondaryErrors is not null)
        {
            errorList.AddRange(secondaryErrors);
        }

        return errorList.Count > 0
            ? Result<TOut>.WithFailure(errorList, value)
            : Result<TOut>.WithFailure(ResultConstants.NoErrorsFoundMessage, value);
    }
}
