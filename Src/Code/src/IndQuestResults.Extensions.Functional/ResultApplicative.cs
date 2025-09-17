using IndQuestResults.Operations;

namespace IndQuestResults.Extensions.Functional;

/// <summary>
/// Provides Applicative Functor operations for Result types, enabling validation of multiple independent values
/// and accumulation of errors rather than fail-fast behavior. This implements the Apply pattern from functional
/// programming languages like Haskell, F#, and Scala.
/// </summary>
/// <remarks>
/// <para><strong>Applicative vs Monadic Operations:</strong></para>
/// <list type="bullet">
/// <item><strong>Monadic (Bind):</strong> Sequential, fail-fast - stops at first error</item>
/// <item><strong>Applicative (Apply):</strong> Independent, accumulates all errors for comprehensive validation</item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
/// <item><strong>Form Validation:</strong> Validate all fields and collect all errors</item>
/// <item><strong>Configuration Validation:</strong> Check multiple settings independently</item>
/// <item><strong>Data Import:</strong> Validate multiple rows and report all issues</item>
/// <item><strong>API Validation:</strong> Check all parameters before processing</item>
/// </list>
/// 
/// <para><strong>Performance:</strong> Optimized for small collections with Span&lt;T&gt; usage when possible.</para>
/// </remarks>
public static class ResultApplicative
{
    /// <summary>
    /// Applies a function to two Result values, accumulating errors from both if they fail.
    /// This enables independent validation where you want to collect all errors, not just the first.
    /// </summary>
    /// <typeparam name="T1">Type of the first Result value</typeparam>
    /// <typeparam name="T2">Type of the second Result value</typeparam>
    /// <typeparam name="TResult">Type of the result after applying the function</typeparam>
    /// <param name="result1">First Result to validate</param>
    /// <param name="result2">Second Result to validate</param>
    /// <param name="func">Function to apply if both Results are successful</param>
    /// <returns>Success with combined result, or failure with accumulated errors</returns>
    /// <example>
    /// <code>
    /// var nameResult = ValidateName(user.Name);
    /// var emailResult = ValidateEmail(user.Email);
    /// var userResult = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));
    /// </code>
    /// </example>
    public static Result<TResult> Apply<T1, T2, TResult>(
        Result<T1> result1,
        Result<T2> result2,
        Func<T1, T2, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(result1);
        ArgumentNullException.ThrowIfNull(result2);
        ArgumentNullException.ThrowIfNull(func);

        // Both successful - apply function
        if (result1.IsSuccess && result2.IsSuccess)
        {
            return Result<TResult>.Success(func(result1.Value!, result2.Value!));
        }

        // Accumulate errors from both failures
        var errors = new List<string>();
        if (result1.IsFailure && result1.Errors != null)
            errors.AddRange(result1.Errors);
        if (result2.IsFailure && result2.Errors != null)
            errors.AddRange(result2.Errors);

        return errors.Count > 0
            ? Result<TResult>.WithFailure(errors)
            : Result<TResult>.WithFailure(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Applies a function to three Result values, accumulating errors from all failures.
    /// </summary>
    public static Result<TResult> Apply<T1, T2, T3, TResult>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3,
        Func<T1, T2, T3, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(result1);
        ArgumentNullException.ThrowIfNull(result2);
        ArgumentNullException.ThrowIfNull(result3);
        ArgumentNullException.ThrowIfNull(func);

        // All successful - apply function
        if (result1.IsSuccess && result2.IsSuccess && result3.IsSuccess)
        {
            return Result<TResult>.Success(func(result1.Value!, result2.Value!, result3.Value!));
        }

        // Accumulate errors from all failures
        var errors = new List<string>();
        if (result1.IsFailure && result1.Errors != null)
            errors.AddRange(result1.Errors);
        if (result2.IsFailure && result2.Errors != null)
            errors.AddRange(result2.Errors);
        if (result3.IsFailure && result3.Errors != null)
            errors.AddRange(result3.Errors);

        return errors.Count > 0
            ? Result<TResult>.WithFailure(errors)
            : Result<TResult>.WithFailure(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Applies a function to four Result values, accumulating errors from all failures.
    /// </summary>
    public static Result<TResult> Apply<T1, T2, T3, T4, TResult>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3,
        Result<T4> result4,
        Func<T1, T2, T3, T4, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(result1);
        ArgumentNullException.ThrowIfNull(result2);
        ArgumentNullException.ThrowIfNull(result3);
        ArgumentNullException.ThrowIfNull(result4);
        ArgumentNullException.ThrowIfNull(func);

        // All successful - apply function
        if (result1.IsSuccess && result2.IsSuccess && result3.IsSuccess && result4.IsSuccess)
        {
            return Result<TResult>.Success(func(result1.Value!, result2.Value!, result3.Value!, result4.Value!));
        }

        // Accumulate errors from all failures
        var errors = new List<string>();
        if (result1.IsFailure && result1.Errors != null)
            errors.AddRange(result1.Errors);
        if (result2.IsFailure && result2.Errors != null)
            errors.AddRange(result2.Errors);
        if (result3.IsFailure && result3.Errors != null)
            errors.AddRange(result3.Errors);
        if (result4.IsFailure && result4.Errors != null)
            errors.AddRange(result4.Errors);

        return errors.Count > 0
            ? Result<TResult>.WithFailure(errors)
            : Result<TResult>.WithFailure(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Applies a function to five Result values, accumulating errors from all failures.
    /// </summary>
    public static Result<TResult> Apply<T1, T2, T3, T4, T5, TResult>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3,
        Result<T4> result4,
        Result<T5> result5,
        Func<T1, T2, T3, T4, T5, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(result1);
        ArgumentNullException.ThrowIfNull(result2);
        ArgumentNullException.ThrowIfNull(result3);
        ArgumentNullException.ThrowIfNull(result4);
        ArgumentNullException.ThrowIfNull(result5);
        ArgumentNullException.ThrowIfNull(func);

        // All successful - apply function
        if (result1.IsSuccess && result2.IsSuccess && result3.IsSuccess && result4.IsSuccess && result5.IsSuccess)
        {
            return Result<TResult>.Success(func(result1.Value!, result2.Value!, result3.Value!, result4.Value!, result5.Value!));
        }

        // Accumulate errors from all failures
        var errors = new List<string>();
        if (result1.IsFailure && result1.Errors != null)
            errors.AddRange(result1.Errors);
        if (result2.IsFailure && result2.Errors != null)
            errors.AddRange(result2.Errors);
        if (result3.IsFailure && result3.Errors != null)
            errors.AddRange(result3.Errors);
        if (result4.IsFailure && result4.Errors != null)
            errors.AddRange(result4.Errors);
        if (result5.IsFailure && result5.Errors != null)
            errors.AddRange(result5.Errors);

        return errors.Count > 0
            ? Result<TResult>.WithFailure(errors)
            : Result<TResult>.WithFailure(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Validates multiple Results and applies a function only if all are successful.
    /// This is a convenience method for the most common applicative pattern.
    /// </summary>
    /// <typeparam name="TResult">Type of the result after validation</typeparam>
    /// <param name="func">Function to apply if all validations pass</param>
    /// <param name="results">Results to validate</param>
    /// <returns>Success if all Results are successful, otherwise accumulated errors</returns>
    /// <example>
    /// <code>
    /// var userResult = ResultApplicative.Validate(
    ///     () => new User(name, email, age),
    ///     ValidateName(name),
    ///     ValidateEmail(email),
    ///     ValidateAge(age)
    /// );
    /// </code>
    /// </example>
    public static Result<TResult> Validate<TResult>(
        Func<TResult> func,
        params Result[] results)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(results);

        var errors = new List<string>();
        var allSuccessful = true;

        foreach (var result in results)
        {
            if (result == null) continue;
            
            if (result.IsFailure)
            {
                allSuccessful = false;
                if (result.Errors != null)
                    errors.AddRange(result.Errors);
            }
        }

        return allSuccessful
            ? Result<TResult>.Success(func())
            : Result<TResult>.WithFailure(errors.Count > 0 ? errors : new[] { ResultConstants.DefaultErrorMessage });
    }

    /// <summary>
    /// Extension method for fluent applicative validation syntax.
    /// Enables chaining multiple validations with error accumulation.
    /// </summary>
    /// <typeparam name="T1">Type of the first Result</typeparam>
    /// <typeparam name="T2">Type of the second Result</typeparam>
    /// <typeparam name="TResult">Type of the combined result</typeparam>
    /// <param name="result1">First Result to combine</param>
    /// <param name="result2">Second Result to combine</param>
    /// <param name="func">Function to apply if both are successful</param>
    /// <returns>Combined result or accumulated errors</returns>
    /// <example>
    /// <code>
    /// var userResult = ValidateName(name)
    ///     .ApplyWith(ValidateEmail(email), (n, e) => new User(n, e))
    ///     .ApplyWith(ValidateAge(age), (user, age) => user with { Age = age });
    /// </code>
    /// </example>
    public static Result<TResult> ApplyWith<T1, T2, TResult>(
        this Result<T1> result1,
        Result<T2> result2,
        Func<T1, T2, TResult> func)
    {
        return Apply(result1, result2, func);
    }
}