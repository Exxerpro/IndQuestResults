namespace IndQuestResults.Validation;

/// <summary>
/// Utility class for parameter null validation with Result pattern integration.
/// Provides fluent API for validating multiple parameters and returning structured error information.
/// </summary>
public static class NullArgumentValidation
{
    /// <summary>
    /// Validates that a single parameter is not null.
    /// </summary>
    /// <typeparam name="T">Type of the parameter to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static ValidationResult ValidateSingle<T>(T? value, string parameterName) where T : class
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return ValidationResult.Invalid("Parameter name cannot be null or empty.");

        return value is null
            ? ValidationResult.Invalid(new NullArgumentError(parameterName).ToString())
            : ValidationResult.Valid;
    }

    /// <summary>
    /// Validates that a nullable struct parameter has a value.
    /// </summary>
    /// <typeparam name="T">Type of the nullable struct to validate.</typeparam>
    /// <param name="value">The nullable value to validate.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static ValidationResult ValidateSingle<T>(T? value, string parameterName) where T : struct
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return ValidationResult.Invalid("Parameter name cannot be null or empty.");

        return value.HasValue
            ? ValidationResult.Valid
            : ValidationResult.Invalid(new NullArgumentError(parameterName).ToString());
    }

    /// <summary>
    /// Validates multiple parameters for null values.
    /// </summary>
    /// <param name="validations">Array of value-parameter name pairs to validate.</param>
    /// <returns>A validation result with all null parameter errors aggregated.</returns>
    public static ValidationResult ValidateMultiple(params (object? value, string parameterName)[] validations)
    {
        if (validations == null || validations.Length == 0)
            return ValidationResult.Invalid("Validations cannot be null or empty.");

        var nullParameters = validations
            .Where(v => v.value is null)
            .Select(v => v.parameterName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        return nullParameters.Length switch
        {
            0 => ValidationResult.Valid,
            1 => ValidationResult.Invalid(new NullArgumentError(nullParameters[0]).ToString()),
            _ => ValidationResult.Invalid(new MultipleNullArgumentsError(nullParameters).ToString())
        };
    }
}

/// <summary>
/// Represents the result of a parameter validation operation.
/// </summary>
public readonly struct ValidationResult
{
    private readonly bool _isValid;
    private readonly string? _errorMessage;

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        _isValid = isValid;
        _errorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid => _isValid;

    /// <summary>
    /// Gets a value indicating whether the validation failed.
    /// </summary>
    public bool IsInvalid => !_isValid;

    /// <summary>
    /// Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage => _errorMessage;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Valid => new(true);

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Invalid(string errorMessage) => new(false, errorMessage);

    /// <summary>
    /// Returns a string representation of the validation result.
    /// </summary>
    /// <returns>A string describing the validation outcome.</returns>
    public override string ToString()
    {
        return _isValid ? "Valid" : $"Invalid: {_errorMessage}";
    }
}