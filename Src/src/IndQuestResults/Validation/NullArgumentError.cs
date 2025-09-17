namespace IndQuestResults.Validation;

/// <summary>
/// Represents an error for a single null argument validation failure.
/// Provides structured information about null parameter validation errors.
/// </summary>
public sealed class NullArgumentError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullArgumentError"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that is null.</param>
    /// <param name="message">Optional custom error message.</param>
    public NullArgumentError(string parameterName, string? message = null)
    {
        ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        Message = message ?? $"Parameter '{parameterName}' cannot be null.";
    }

    /// <summary>
    /// Gets the name of the parameter that is null.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    /// <returns>The error message.</returns>
    public override string ToString() => Message;

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is NullArgumentError other &&
               ParameterName == other.ParameterName &&
               Message == other.Message;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(ParameterName, Message);
    }
}