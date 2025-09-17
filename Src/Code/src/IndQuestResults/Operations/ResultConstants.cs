namespace IndQuestResults.Operations;

/// <summary>
/// Constants used by the Result classes to ensure consistency and avoid magic strings.
/// This class serves as the single source of truth for all Result-related messages.
/// </summary>
public static class ResultConstants
{
    /// <summary>
    /// Default error message used when no specific error is provided.
    /// </summary>
    public const string DefaultErrorMessage = "Operation failed to execute successfully";
    
    /// <summary>
    /// Message used when combining errors but no errors are found.
    /// </summary>
    public const string NoErrorsFoundMessage = "No errors were found";
    
    /// <summary>
    /// Default warning message used when warnings collection is null or empty.
    /// </summary>
    public const string DefaultWarningMessage = "Warning: unexpected condition occurred";
    
    /// <summary>
    /// Prefix used in ToString() representation for successful results.
    /// </summary>
    public const string SuccessPrefix = "Success";
    
    /// <summary>
    /// Prefix used in ToString() representation for failed results.
    /// </summary>
    public const string FailurePrefix = "WithFailure";
    
    /// <summary>
    /// Default ToString() representation for failed results with no specific errors.
    /// </summary>
    public const string DefaultFailureString = "WithFailure";
    
    /// <summary>
    /// Error message template for RecoverWith type conversion failures.
    /// Use with string.Format(template, sourceType, targetType).
    /// </summary>
    public const string RecoverWithTypeConversionError = "Cannot convert value of type '{0}' to '{1}' in RecoverWith operation";
    
    /// <summary>
    /// Cached CompositeFormat for RecoverWith type conversion error message to improve performance.
    /// </summary>
    public static readonly System.Text.CompositeFormat RecoverWithTypeConversionErrorFormat = 
        System.Text.CompositeFormat.Parse(RecoverWithTypeConversionError);

    /// <summary>
    /// Error message when a null value is encountered in a successful result where a value is expected.
    /// </summary>
    public const string NullValueInSuccessfulResult = "Operation succeeded but returned a null value";

    /// <summary>
    /// Error message when Result state is inconsistent after deserialization.
    /// </summary>
    public const string InconsistentResultState = "Result object has inconsistent internal state";

    /// <summary>
    /// Error message when a condition cannot be evaluated due to null value.
    /// </summary>
    public const string ConditionEvaluationWithNullValue = "Cannot evaluate condition with null value";
}