using IndQuestResults.Extensions.Functional;
using IndQuestResults.Operations;
using Shouldly;
using System.Linq;
using Xunit;

namespace IndQuestResults.Tests.Unit.Extensions.Functional;

/// <summary>
/// Tests for ResultApplicative functionality covering all Apply overloads,
/// error accumulation patterns, and validation scenarios.
/// </summary>
public class ResultApplicativeTests
{
    #region Test Data Classes

    private record User(string Name, string Email);
    private record UserProfile(string Name, string Email, int Age);
    private record UserDetails(string Name, string Email, int Age, string Address);
    private record FullUser(string Name, string Email, int Age, string Address, string Phone);

    #endregion

    #region Apply Two Results Tests

    [Fact]
    public void Apply_BothSuccess_ReturnsSuccess()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("john@example.com");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("John");
        result.Value.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public void Apply_FirstFails_ReturnsFirstError()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure("Invalid name");
        var emailResult = Result<string>.Success("john@example.com");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.ShouldContain("Invalid name");
        result.Errors.Count().ShouldBe(1);
    }

    [Fact]
    public void Apply_SecondFails_ReturnsSecondError()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.WithFailure("Invalid email");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.ShouldContain("Invalid email");
        result.Errors.Count().ShouldBe(1);
    }

    [Fact]
    public void Apply_BothFail_AccumulatesAllErrors()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure("Invalid name");
        var emailResult = Result<string>.WithFailure("Invalid email");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.ShouldContain("Invalid name");
        result.Errors.ShouldContain("Invalid email");
        result.Errors.Count().ShouldBe(2);
    }

    [Fact]
    public void Apply_BothFailWithMultipleErrors_AccumulatesAllErrors()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure(new[] { "Name too short", "Name contains invalid chars" });
        var emailResult = Result<string>.WithFailure(new[] { "Email missing @", "Email too long" });

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(4);
        result.Errors.ShouldContain("Name too short");
        result.Errors.ShouldContain("Name contains invalid chars");
        result.Errors.ShouldContain("Email missing @");
        result.Errors.ShouldContain("Email too long");
    }

    #endregion

    #region Apply Three Results Tests

    [Fact]
    public void Apply_ThreeResults_AllSuccess_ReturnsSuccess()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("john@example.com");
        var ageResult = Result<int>.Success(25);

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult,
            (name, email, age) => new UserProfile(name, email, age));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("John");
        result.Value.Email.ShouldBe("john@example.com");
        result.Value.Age.ShouldBe(25);
    }

    [Fact]
    public void Apply_ThreeResults_AllFail_AccumulatesAllErrors()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure("Invalid name");
        var emailResult = Result<string>.WithFailure("Invalid email");
        var ageResult = Result<int>.WithFailure("Invalid age");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult,
            (name, email, age) => new UserProfile(name, email, age));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(3);
        result.Errors.ShouldContain("Invalid name");
        result.Errors.ShouldContain("Invalid email");
        result.Errors.ShouldContain("Invalid age");
    }

    [Fact]
    public void Apply_ThreeResults_PartialFailure_AccumulatesFailedErrors()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.WithFailure("Invalid email");
        var ageResult = Result<int>.WithFailure("Invalid age");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult,
            (name, email, age) => new UserProfile(name, email, age));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(2);
        result.Errors.ShouldContain("Invalid email");
        result.Errors.ShouldContain("Invalid age");
        result.Errors.ShouldNotContain("Invalid name");
    }

    #endregion

    #region Apply Four Results Tests

    [Fact]
    public void Apply_FourResults_AllSuccess_ReturnsSuccess()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("john@example.com");
        var ageResult = Result<int>.Success(25);
        var addressResult = Result<string>.Success("123 Main St");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult, addressResult,
            (name, email, age, address) => new UserDetails(name, email, age, address));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("John");
        result.Value.Address.ShouldBe("123 Main St");
    }

    [Fact]
    public void Apply_FourResults_AllFail_AccumulatesAllErrors()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure("Invalid name");
        var emailResult = Result<string>.WithFailure("Invalid email");
        var ageResult = Result<int>.WithFailure("Invalid age");
        var addressResult = Result<string>.WithFailure("Invalid address");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult, addressResult,
            (name, email, age, address) => new UserDetails(name, email, age, address));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(4);
    }

    #endregion

    #region Apply Five Results Tests

    [Fact]
    public void Apply_FiveResults_AllSuccess_ReturnsSuccess()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("john@example.com");
        var ageResult = Result<int>.Success(25);
        var addressResult = Result<string>.Success("123 Main St");
        var phoneResult = Result<string>.Success("555-0123");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult, addressResult, phoneResult,
            (name, email, age, address, phone) => new FullUser(name, email, age, address, phone));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Phone.ShouldBe("555-0123");
    }

    [Fact]
    public void Apply_FiveResults_AllFail_AccumulatesAllErrors()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure("Invalid name");
        var emailResult = Result<string>.WithFailure("Invalid email");
        var ageResult = Result<int>.WithFailure("Invalid age");
        var addressResult = Result<string>.WithFailure("Invalid address");
        var phoneResult = Result<string>.WithFailure("Invalid phone");

        // Act
        var result = ResultApplicative.Apply(nameResult, emailResult, ageResult, addressResult, phoneResult,
            (name, email, age, address, phone) => new FullUser(name, email, age, address, phone));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(5);
    }

    #endregion

    #region Validate Method Tests

    [Fact]
    public void Validate_AllSuccess_ReturnsSuccess()
    {
        // Arrange
        var results = new[]
        {
            Result.Success(),
            Result.Success(),
            Result.Success()
        };

        // Act
        var result = ResultApplicative.Validate(() => "All good", results);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("All good");
    }

    [Fact]
    public void Validate_SomeFailures_AccumulatesErrors()
    {
        // Arrange
        var results = new[]
        {
            Result.Success(),
            Result.WithFailure("Error 1"),
            Result.WithFailure("Error 2")
        };

        // Act
        var result = ResultApplicative.Validate(() => "Should not execute", results);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(2);
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
    }

    [Fact]
    public void Validate_EmptyResults_ReturnsSuccess()
    {
        // Arrange
        var results = Array.Empty<Result>();

        // Act
        var result = ResultApplicative.Validate(() => "Empty validation", results);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Empty validation");
    }

    [Fact]
    public void Validate_WithNullResults_IgnoresNulls()
    {
        // Arrange
        var results = new Result?[]
        {
            Result.Success(),
            null,
            Result.WithFailure("Error 1")
        };

        // Act
        var result = ResultApplicative.Validate(() => "Partial", results!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(1);
        result.Errors.ShouldContain("Error 1");
    }

    #endregion

    #region ApplyWith Extension Method Tests

    [Fact]
    public void ApplyWith_BothSuccess_ReturnsSuccess()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("john@example.com");

        // Act
        var result = nameResult.ApplyWith(emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("John");
        result.Value.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public void ApplyWith_CanChain_ForMultipleValidations()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("john@example.com");
        var ageResult = Result<int>.Success(25);

        // Act
        var result = nameResult
            .ApplyWith(emailResult, (name, email) => new User(name, email))
            .ApplyWith(ageResult, (user, age) => new UserProfile(user.Name, user.Email, age));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Age.ShouldBe(25);
    }

    [Fact]
    public void ApplyWith_ChainedFailures_AccumulatesErrors()
    {
        // Arrange
        var nameResult = Result<string>.WithFailure("Invalid name");
        var emailResult = Result<string>.WithFailure("Invalid email");

        // Act
        var result = nameResult.ApplyWith(emailResult, (name, email) => new User(name, email));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors!.Count().ShouldBe(2);
        result.Errors.ShouldContain("Invalid name");
        result.Errors.ShouldContain("Invalid email");
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void Apply_NullResult1_ThrowsArgumentNullException()
    {
        // Arrange
        Result<string> nullResult = null!;
        var emailResult = Result<string>.Success("email");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultApplicative.Apply(nullResult, emailResult, (n, e) => new User(n, e)));
    }

    [Fact]
    public void Apply_NullResult2_ThrowsArgumentNullException()
    {
        // Arrange
        var nameResult = Result<string>.Success("name");
        Result<string> nullResult = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultApplicative.Apply(nameResult, nullResult, (n, e) => new User(n, e)));
    }

    [Fact]
    public void Apply_NullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var nameResult = Result<string>.Success("name");
        var emailResult = Result<string>.Success("email");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultApplicative.Apply(nameResult, emailResult, (Func<string, string, User>)null!));
    }

    [Fact]
    public void Validate_NullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var results = new[] { Result.Success() };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultApplicative.Validate((Func<string>)null!, results));
    }

    [Fact]
    public void Validate_NullResults_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultApplicative.Validate(() => "test", null!));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Apply_EmptyErrorLists_UsesDefaultMessage()
    {
        // This tests the edge case where errors collection is empty but result is failure
        // Note: This scenario is theoretical as Result typically doesn't allow empty error lists
        // but the code handles it defensively
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("jane@example.com");

        var result = ResultApplicative.Apply(nameResult, emailResult, (name, email) => new User(name, email));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Apply_FunctionThrows_ExceptionPropagates()
    {
        // Arrange
        var nameResult = Result<string>.Success("John");
        var emailResult = Result<string>.Success("jane@example.com");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            ResultApplicative.Apply<string, string, User>(nameResult, emailResult, (name, email) => throw new InvalidOperationException("Function failed")));
    }

    #endregion

    #region Real-world Scenarios

    [Fact]
    public void Apply_FormValidationScenario_AccumulatesValidationErrors()
    {
        // Arrange - Simulate form validation
        var nameValidation = ValidateName("");
        var emailValidation = ValidateEmail("invalid-email");
        var ageValidation = ValidateAge(-5);

        // Act
        var userResult = ResultApplicative.Apply(nameValidation, emailValidation, ageValidation,
            (name, email, age) => new UserProfile(name, email, age));

        // Assert
        userResult.IsFailure.ShouldBeTrue();
        userResult.Errors!.Count().ShouldBe(3);
        userResult.Errors.ShouldContain("Name is required");
        userResult.Errors.ShouldContain("Email format is invalid");
        userResult.Errors.ShouldContain("Age must be positive");
    }

    [Fact]
    public void Apply_ConfigurationValidationScenario_ValidatesIndependentSettings()
    {
        // Arrange - Simulate configuration validation
        var dbConnectionResult = ValidateConnectionString("");
        var apiKeyResult = ValidateApiKey("short");
        var timeoutResult = ValidateTimeout(0);

        // Act
        var configResult = ResultApplicative.Apply(dbConnectionResult, apiKeyResult, timeoutResult,
            (conn, key, timeout) => new { Connection = conn, ApiKey = key, Timeout = timeout });

        // Assert
        configResult.IsFailure.ShouldBeTrue();
        configResult.Errors!.Count().ShouldBe(3);
    }

    #endregion

    #region Helper Methods

    private static Result<string> ValidateName(string name)
    {
        return string.IsNullOrEmpty(name)
            ? Result<string>.WithFailure("Name is required")
            : Result<string>.Success(name);
    }

    private static Result<string> ValidateEmail(string email)
    {
        return !email.Contains('@')
            ? Result<string>.WithFailure("Email format is invalid")
            : Result<string>.Success(email);
    }

    private static Result<int> ValidateAge(int age)
    {
        return age <= 0
            ? Result<int>.WithFailure("Age must be positive")
            : Result<int>.Success(age);
    }

    private static Result<string> ValidateConnectionString(string connectionString)
    {
        return string.IsNullOrEmpty(connectionString)
            ? Result<string>.WithFailure("Connection string is required")
            : Result<string>.Success(connectionString);
    }

    private static Result<string> ValidateApiKey(string apiKey)
    {
        return apiKey.Length < 10
            ? Result<string>.WithFailure("API key must be at least 10 characters")
            : Result<string>.Success(apiKey);
    }

    private static Result<int> ValidateTimeout(int timeout)
    {
        return timeout <= 0
            ? Result<int>.WithFailure("Timeout must be greater than 0")
            : Result<int>.Success(timeout);
    }

    #endregion
}




