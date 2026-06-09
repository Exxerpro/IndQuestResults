using IndQuestResults;
using Shouldly;
using Xunit;

namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Simple tests for Succeeded and Failed property aliases in Result and Result&lt;T&gt; classes.
/// </summary>
public class ResultSucceededFailedTests
{
    #region Result.Succeeded and Failed Tests

    [Fact]
    public void Succeeded_ShouldMatchIsSuccess_WhenSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.Succeeded.ShouldBe(result.IsSuccess);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Succeeded_ShouldMatchIsSuccess_WhenFailure()
    {
        // Act
        var result = Result.WithFailure("Error message");

        // Assert
        result.Succeeded.ShouldBe(result.IsSuccess);
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void Failed_ShouldMatchIsFailure_WhenSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.Failed.ShouldBe(result.IsFailure);
        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Failed_ShouldMatchIsFailure_WhenFailure()
    {
        // Act
        var result = Result.WithFailure("Error message");

        // Assert
        result.Failed.ShouldBe(result.IsFailure);
        result.Failed.ShouldBeTrue();
    }

    #endregion

    #region Result<T>.Succeeded and Failed Tests

    [Fact]
    public void ResultT_Succeeded_ShouldMatchIsRecoverable_WhenSuccess()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        result.Succeeded.ShouldBe(result.IsRecoverable);
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ResultT_Succeeded_ShouldMatchIsRecoverable_WhenFailure()
    {
        // Act
        var result = Result<string>.WithFailure("Error message");

        // Assert
        result.Succeeded.ShouldBe(result.IsRecoverable);
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void ResultT_Failed_ShouldMatchIsFailure_WhenSuccess()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        result.Failed.ShouldBe(result.IsFailure);
        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void ResultT_Failed_ShouldMatchIsFailure_WhenFailure()
    {
        // Act
        var result = Result<string>.WithFailure("Error message");

        // Assert
        result.Failed.ShouldBe(result.IsFailure);
        result.Failed.ShouldBeTrue();
    }

    #endregion
}

