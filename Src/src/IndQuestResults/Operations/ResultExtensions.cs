using IndQuestResults.Operations;
using IndQuestResults.Validation;

namespace IndQuestResults.Operations;

/// <summary>
/// Extension methods for Result{T} following Open/Closed Principle
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
    /// <param name="result">The result to check.</param>
    /// <returns><c>true</c> if the result contains an operation cancelled error; otherwise, <c>false</c>.</returns>
    public static bool IsCancelled(this Result result)
    {
        return result != null && result.Errors != null && result.Errors.Any(e => e == ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Determines whether the generic result represents a cancelled operation.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <returns><c>true</c> if the result contains an operation cancelled error; otherwise, <c>false</c>.</returns>
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
            return Result<T>.WithFailure("Parameter name cannot be null or empty.");
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
            return Result<T>.WithFailure("Parameter names cannot be null, empty, or contain empty values.");
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
            return Result.WithFailure("Parameter name cannot be null or empty.");
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
            return Result.WithFailure("Parameter names cannot be null, empty, or contain empty values.");
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
        if (string.IsNullOrEmpty(parameterName))
            return Result<T>.WithFailure("Parameter name cannot be null or empty.");
        return value is null
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
        if (string.IsNullOrEmpty(parameterName))
            return Result<T>.WithFailure("Parameter name cannot be null or empty.");
        return value.HasValue
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
            return Result.WithFailure("Validations cannot be null or empty.");
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
            return Result<T>.WithFailure("Factory function cannot be null.");
        if (validations == null)
            return Result<T>.WithFailure("Validations cannot be null.");
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
}