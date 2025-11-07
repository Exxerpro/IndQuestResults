using System;
using System.Linq;

namespace IndQuestResults.Operations;

/// <summary>
/// Extension methods for discoverable validation on Result types and values.
/// These methods appear in IntelliSense when typing `result.` or `value.`, making validation APIs easily discoverable.
/// </summary>
/// <remarks>
/// <para><strong>Discoverability:</strong> These extension methods are discoverable via IntelliSense when working with Result types or values.</para>
/// <para><strong>ROP-Compliant:</strong> All methods return Result types, integrating seamlessly with ROP chains.</para>
/// <para><strong>Backward Compatible:</strong> Existing validation APIs (ResultExtensions.ValidateNotNull, NullArgumentValidation) remain functional.</para>
/// </remarks>
public static class ResultValidationExtensions
{
    /// <summary>
    /// Validates that the Result value is not null.
    /// Returns the same Result if validation passes, or a failure if the value is null.
    /// </summary>
    /// <typeparam name="T">Type of the Result value</typeparam>
    /// <param name="result">The Result to validate</param>
    /// <param name="parameterName">Name of the parameter being validated</param>
    /// <returns>The same Result if value is not null, or a failure Result if value is null</returns>
    /// <example>
    /// <code>
    /// var result = GetUserAsync(id)
    ///     .ValidateNotNull(nameof(user))
    ///     .Bind(u => ProcessUser(u));
    /// </code>
    /// </example>
    public static Result<T> ValidateNotNull<T>(this Result<T> result, string parameterName) where T : class
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return Result<T>.WithFailure("Parameter name cannot be null or empty.");
        }

#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result.IsFailure) { return result; }
#pragma warning restore IDE0046

        return result.Value is null
            ? ResultExtensions.FailForNullArgument<T>(parameterName)
            : result;
    }

    /// <summary>
    /// Validates that a nullable value is not null and returns a Result.
    /// This extension method makes validation discoverable when typing `value.`.
    /// </summary>
    /// <typeparam name="T">Type of the value to validate (must be a class)</typeparam>
    /// <param name="value">The value to validate</param>
    /// <param name="parameterName">Name of the parameter being validated</param>
    /// <returns>Success Result with the value if not null, or a failure Result if null</returns>
    /// <example>
    /// <code>
    /// var result = user.EnsureNotNull(nameof(user))
    ///     .Bind(u => u.Email.EnsureNotNull(nameof(u.Email)))
    ///     .Map(u => ProcessUser(u));
    /// </code>
    /// </example>
    public static Result<T> EnsureNotNull<T>(this T? value, string parameterName) where T : class
    {
        return ResultExtensions.EnsureNotNull(value, parameterName);
    }

    /// <summary>
    /// Validates that a nullable struct value has a value and returns a Result.
    /// This extension method makes validation discoverable when typing `value.`.
    /// </summary>
    /// <typeparam name="T">Type of the nullable struct to validate</typeparam>
    /// <param name="value">The nullable value to validate</param>
    /// <param name="parameterName">Name of the parameter being validated</param>
    /// <returns>Success Result with the value if it has a value, or a failure Result if null</returns>
    /// <example>
    /// <code>
    /// var result = count.EnsureNotNull(nameof(count))
    ///     .Map(c => ProcessCount(c));
    /// </code>
    /// </example>
    public static Result<T> EnsureNotNull<T>(this T? value, string parameterName) where T : struct
    {
        return ResultExtensions.EnsureNotNull(value, parameterName);
    }

    /// <summary>
    /// Validates multiple parameters and returns success or failure with all null parameter names.
    /// This method provides a fluent API for multi-parameter validation that is discoverable.
    /// </summary>
    /// <param name="validations">Array of parameter validations (value, parameterName)</param>
    /// <returns>Success result if all valid, failure result with all null parameter names</returns>
    /// <example>
    /// <code>
    /// var result = ResultValidationExtensions.ValidateNotNull(
    ///     (user, nameof(user)),
    ///     (user.Email, nameof(user.Email)),
    ///     (user.Name, nameof(user.Name))
    /// ).Bind(() => ProcessUser(user!));
    /// </code>
    /// </example>
    public static Result ValidateNotNull(params (object? value, string parameterName)[] validations)
    {
        return ResultExtensions.ValidateNotNull(validations);
    }

    /// <summary>
    /// Creates a Result from a factory function if all validations pass.
    /// This method provides a fluent API for validation with factory pattern that is discoverable.
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="factory">Factory function to create result if all validations pass</param>
    /// <param name="validations">Array of parameter validations</param>
    /// <returns>Success result with factory value or failure with validation errors</returns>
    /// <example>
    /// <code>
    /// var result = ResultValidationExtensions.CreateIfValid(
    ///     factory: () => new User(id!),
    ///     (id, nameof(id)),
    ///     (name, nameof(name))
    /// );
    /// </code>
    /// </example>
    public static Result<T> CreateIfValid<T>(Func<T> factory, params (object? value, string parameterName)[] validations)
    {
        return ResultExtensions.CreateIfValid(factory, validations);
    }
}

