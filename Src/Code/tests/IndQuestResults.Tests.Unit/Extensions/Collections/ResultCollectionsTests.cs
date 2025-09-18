namespace IndQuestResults.Tests.Unit.Extensions.Collections;

/// <summary>
/// Unit tests for <see cref="ResultCollections"/> operations.
/// </summary>
public class ResultCollectionsTests
{
    private static readonly string[] ErrorsE2E3 = new[] { "E2", "E3" };
    private static readonly string[] ErrorsE2aE2b = new[] { "E2a", "E2b" };
    private static readonly string[] ErrorsE3aE3b = new[] { "E3a", "E3b" };
    private static readonly int[] Expected123 = new int[] { 1, 2, 3 };
    private static readonly int[] Expected246 = new int[] { 2, 4, 6 };
    private static readonly int[] Expected10_20 = new int[] { 10, 20 };
    private static readonly int[] Expected1_2 = new int[] { 1, 2 };
    private static readonly int[] Expected4_5 = new int[] { 4, 5 };
    private static readonly string[] ExpectedAB = new string[] { "A", "B" };
    /// <summary>
    /// Ensures <see cref="ResultCollections.Sequence{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// aggregates values when all inputs are successful.
    /// </summary>
    [Fact]
    public void Sequence_WhenAllSuccessful_ShouldAggregateValues()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3)
        };

        // Act
        var result = ResultCollections.Sequence<int>(inputs);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ToArray().ShouldBe(Expected123);
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.Sequence{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// accumulates errors when any input fails.
    /// </summary>
    [Fact]
    public void Sequence_WhenAnyFailed_ShouldAccumulateErrors()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.Success(1),
            Result<int>.WithFailure("E1"),
            Result<int>.WithFailure(ErrorsE2E3)
        };

        // Act
        var result = ResultCollections.Sequence<int>(inputs);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("E1");
        result.Errors.ShouldContain("E2");
        result.Errors.ShouldContain("E3");
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.SequenceFailFast{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// stops at the first failure and returns only that failure's errors.
    /// </summary>
    [Fact]
    public void SequenceFailFast_WhenFailureEncountered_ShouldReturnFirstFailureErrors()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.WithFailure(ErrorsE2aE2b),
            Result<int>.WithFailure("E3")
        };

        // Act
        var result = ResultCollections.SequenceFailFast<int>(inputs);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("E2a");
        result.Errors.ShouldContain("E2b");
        result.Errors.ShouldNotContain("E3");
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.Traverse{TInput, TOutput}(System.Collections.Generic.IEnumerable{TInput}, System.Func{TInput, IndQuestResults.Operations.Result{TOutput}})"/>
    /// maps and sequences successfully when all mappings succeed.
    /// </summary>
    [Fact]
    public void Traverse_WhenAllSuccessful_ShouldMapAndAggregate()
    {
        // Arrange
        var inputs = new[] { 1, 2, 3 };
        static Result<int> Map(int x) => Result<int>.Success(x * 2);

        // Act
        var result = ResultCollections.Traverse<int, int>(inputs, Map);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ToArray().ShouldBe(Expected246);
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.Traverse{TInput, TOutput}(System.Collections.Generic.IEnumerable{TInput}, System.Func{TInput, IndQuestResults.Operations.Result{TOutput}})"/>
    /// accumulates errors when mappings fail.
    /// </summary>
    [Fact]
    public void Traverse_WhenSomeFail_ShouldAccumulateErrors()
    {
        // Arrange
        var inputs = new[] { 1, 2, 3 };
        static Result<int> Map(int x) => x switch
        {
            2 => Result<int>.WithFailure("E2"),
            3 => Result<int>.WithFailure(ErrorsE3aE3b),
            _ => Result<int>.Success(x * 2)
        };

        // Act
        var result = ResultCollections.Traverse<int, int>(inputs, Map);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("E2");
        result.Errors.ShouldContain("E3a");
        result.Errors.ShouldContain("E3b");
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.TraverseFailFast{TInput, TOutput}(System.Collections.Generic.IEnumerable{TInput}, System.Func{TInput, IndQuestResults.Operations.Result{TOutput}})"/>
    /// stops on the first failure and returns only that failure's errors.
    /// </summary>
    [Fact]
    public void TraverseFailFast_WhenFailureEncountered_ShouldReturnFirstFailureErrors()
    {
        // Arrange
        var inputs = new[] { 1, 2, 3 };
        static Result<int> Map(int x) => x == 2
            ? Result<int>.WithFailure(ErrorsE2aE2b)
            : Result<int>.Success(x);

        // Act
        var result = ResultCollections.TraverseFailFast<int, int>(inputs, Map);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("E2a");
        result.Errors.ShouldContain("E2b");
        result.Errors.Count().ShouldBe(2);
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.Partition{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// splits successes and failures correctly.
    /// </summary>
    [Fact]
    public void Partition_WithMixedResults_ShouldSplitSuccessesAndFailures()
    {
        // Arrange
        var inputs = new[]
        {
            Result<string>.Success("A"),
            Result<string>.WithFailure("E1"),
            Result<string>.Success("B"),
            Result<string>.WithFailure(ErrorsE2E3)
        };

        // Act
        var (successes, failures) = ResultCollections.Partition(inputs);

        // Assert
        successes.ToArray().ShouldBe(ExpectedAB);
        failures.ShouldContain("E1");
        failures.ShouldContain("E2");
        failures.ShouldContain("E3");
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.Collect{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// returns only successful values.
    /// </summary>
    [Fact]
    public void Collect_WithMixedResults_ShouldReturnOnlySuccessfulValues()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.WithFailure("E1"),
            Result<int>.Success(10),
            Result<int>.WithFailure("E2"),
            Result<int>.Success(20)
        };

        // Act
        var values = ResultCollections.Collect(inputs);

        // Assert
        values.ToArray().ShouldBe(Expected10_20);
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.CollectErrors{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// returns only errors from failed results.
    /// </summary>
    [Fact]
    public void CollectErrors_WithMixedResults_ShouldReturnOnlyErrors()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.WithFailure("E1"),
            Result<int>.Success(10),
            Result<int>.WithFailure(ErrorsE2E3)
        };

        // Act
        var errors = ResultCollections.CollectErrors(inputs);

        // Assert
        errors.ShouldContain("E1");
        errors.ShouldContain("E2");
        errors.ShouldContain("E3");
        errors.Count().ShouldBe(3);
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.WhereSuccess{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// filters only successful results.
    /// </summary>
    [Fact]
    public void WhereSuccess_WithMixedResults_ShouldFilterOnlySuccesses()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.Success(1),
            Result<int>.WithFailure("E1"),
            Result<int>.Success(2)
        };

        // Act
        var successes = ResultCollections.WhereSuccess(inputs);

        // Assert
        successes.Count().ShouldBe(2);
        successes.All(r => r.IsSuccess).ShouldBeTrue();
    }

    /// <summary>
    /// Ensures <see cref="ResultCollections.WhereFailure{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// filters only failed results.
    /// </summary>
    [Fact]
    public void WhereFailure_WithMixedResults_ShouldFilterOnlyFailures()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.Success(1),
            Result<int>.WithFailure("E1"),
            Result<int>.WithFailure("E2")
        };

        // Act
        var failures = ResultCollections.WhereFailure(inputs);

        // Assert
        failures.Count().ShouldBe(2);
        failures.All(r => r.IsFailure).ShouldBeTrue();
    }

    /// <summary>
    /// Ensures extension <see cref="ResultCollections.SequenceResults{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>
    /// behaves like <see cref="ResultCollections.Sequence{T}(System.Collections.Generic.IEnumerable{IndQuestResults.Operations.Result{T}})"/>.
    /// </summary>
    [Fact]
    public void SequenceResults_Extension_WhenAllSuccessful_ShouldAggregateValues()
    {
        // Arrange
        var inputs = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2)
        };

        // Act
        var result = inputs.SequenceResults<int>();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ToArray().ShouldBe(Expected1_2);
    }

    /// <summary>
    /// Ensures extension <see cref="ResultCollections.TraverseResults{TInput, TOutput}(System.Collections.Generic.IEnumerable{TInput}, System.Func{TInput, IndQuestResults.Operations.Result{TOutput}})"/>
    /// behaves like <see cref="ResultCollections.Traverse{TInput, TOutput}(System.Collections.Generic.IEnumerable{TInput}, System.Func{TInput, IndQuestResults.Operations.Result{TOutput}})"/>.
    /// </summary>
    [Fact]
    public void TraverseResults_Extension_WhenAllSuccessful_ShouldMapAndAggregate()
    {
        // Arrange
        var inputs = new[] { 3, 4 };
        static Result<int> Map(int x) => Result<int>.Success(x + 1);

        // Act
        var result = inputs.TraverseResults<int, int>(Map);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ToArray().ShouldBe(Expected4_5);
    }
}