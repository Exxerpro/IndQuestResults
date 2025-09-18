namespace IndQuestResults.Validation;

/// <summary>
/// Represents an error for multiple null argument validation failures.
/// Provides structured information about multiple null parameter validation errors.
/// </summary>
public sealed class MultipleNullArgumentsError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultipleNullArgumentsError"/> class.
    /// </summary>
    /// <param name="parameterNames">The names of the parameters that are null.</param>
    public MultipleNullArgumentsError(params string[] parameterNames)
    {
        if (parameterNames == null || parameterNames.Length == 0)
        {
            throw new ArgumentException("At least one parameter name must be provided.", nameof(parameterNames));
        }

        ParameterNames = parameterNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        
        if (ParameterNames.Length == 0)
        {
            throw new ArgumentException("All parameter names cannot be null or whitespace.", nameof(parameterNames));
        }
    }

    /// <summary>
    /// Gets the names of the parameters that are null.
    /// </summary>
    public string[] ParameterNames { get; }

    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    /// <returns>The error message listing all null parameters.</returns>
    public override string ToString()
    {
        return ParameterNames.Length switch
        {
            1 => $"Parameter '{ParameterNames[0]}' cannot be null.",
            2 => $"Parameters '{ParameterNames[0]}' and '{ParameterNames[1]}' cannot be null.",
            _ => $"Parameters {string.Join(", ", ParameterNames.Take(ParameterNames.Length - 1).Select(p => $"'{p}'"))} and '{ParameterNames.Last()}' cannot be null."
        };
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is MultipleNullArgumentsError other &&
               ParameterNames.SequenceEqual(other.ParameterNames);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var parameterName in ParameterNames)
        {
            hash.Add(parameterName);
        }
        return hash.ToHashCode();
    }
}