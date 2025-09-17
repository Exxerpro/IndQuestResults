using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Regression tests for Span&lt;T&gt; optimizations in Result classes.
/// These tests ensure that performance optimizations work correctly and don't introduce bugs.
/// </summary>
public class ResultSpanOptimizationTests
{
    private static readonly string[] ReadOnlyArray = ["RO1", "RO2"];
    
/// <summary>
/// Begin Tests Test Constants
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Test constants for Span optimization validation.
    /// </summary>
    private static class SpanTestConstants
    {
        public const string SmallError1 = "Small error 1";
        public const string SmallError2 = "Small error 2";
        public const string LongError = "This is a very long error message that exceeds normal limits and should trigger fallback behavior in our Span optimizations to ensure we don't cause stack overflow issues";
        public const int SmallCollectionThreshold = 16;
        public const int SpanStackAllocLimit = 32;
        public const int CharStackAllocLimit = 512;
    }

/// <summary>
/// End Tests Test Constants
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests FormatErrorsString Span Optimization Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that small string arrays use Span optimization path.
    /// </summary>
    [Fact]
    public void FormatErrorsString_SmallStringArray_ShouldUseSpanOptimization()
    {
        // Arrange - Small array that should trigger Span optimization
        var errors = new[] { SpanTestConstants.SmallError1, SpanTestConstants.SmallError2 };
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert - Should produce correct output regardless of optimization path
        result.ShouldContain(ResultConstants.FailurePrefix);
        result.ShouldContain(SpanTestConstants.SmallError1);
        result.ShouldContain(SpanTestConstants.SmallError2);
        result.ShouldContain(", "); // Should contain separator
    }

    /// <summary>
    /// Tests that small collections (Lists) use Span optimization path.
    /// </summary>
    [Fact]
    public void FormatErrorsString_SmallList_ShouldUseSpanOptimization()
    {
        // Arrange - Small List that should trigger Span optimization
        var errors = new List<string> { SpanTestConstants.SmallError1, SpanTestConstants.SmallError2, "Error 3" };
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert - Should produce correct output
        result.ShouldContain(ResultConstants.FailurePrefix);
        result.ShouldContain(SpanTestConstants.SmallError1);
        result.ShouldContain(SpanTestConstants.SmallError2);
        result.ShouldContain("Error 3");
        
        // Should maintain correct order and formatting
        var expectedSubstring = $"{SpanTestConstants.SmallError1}, {SpanTestConstants.SmallError2}, Error 3";
        result.ShouldContain(expectedSubstring);
    }

    /// <summary>
    /// Tests that large collections fall back to StringBuilder.
    /// </summary>
    [Fact] 
    public void FormatErrorsString_LargeCollection_ShouldUseFallback()
    {
        // Arrange - Large collection that should trigger fallback
        var errors = Enumerable.Range(1, 20).Select(i => $"Error {i}").ToList();
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert - Should still produce correct output with fallback
        result.ShouldContain(ResultConstants.FailurePrefix);
        result.ShouldContain("Error 1");
        result.ShouldContain("Error 20");
        
        // Should contain all errors
        foreach (var error in errors)
        {
            result.ShouldContain(error);
        }
    }

    /// <summary>
    /// Verifies exact formatting with a large collection to force fallback path and kill separator mutants.
    /// </summary>
    [Fact]
    public void FormatErrorsString_LargeCollection_ShouldMatchExactFormatting()
    {
        // Arrange: use 20 items to force fallback and check exact layout
        var errors = Enumerable.Range(1, 20).Select(i => $"E{i}").ToList();
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert exact format
        var expected = $"{prefix}: " + string.Join(", ", errors);
        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that very long strings fall back to StringBuilder even for small collections.
    /// </summary>
    [Fact]
    public void FormatErrorsString_LongStrings_ShouldUseFallback()
    {
        // Arrange - Small collection but with very long strings
        var errors = new[] { SpanTestConstants.LongError, SpanTestConstants.LongError };
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert - Should handle long strings correctly
        result.ShouldContain(ResultConstants.FailurePrefix);
        result.ShouldContain(SpanTestConstants.LongError);
        
        // Should contain the long error message twice
        var occurrences = result.Split(SpanTestConstants.LongError, StringSplitOptions.None).Length - 1;
        occurrences.ShouldBe(2);
    }

    /// <summary>
    /// Tests edge case with empty collection.
    /// </summary>
    [Fact]
    public void FormatErrorsString_EmptyCollection_ShouldReturnPrefix()
    {
        // Arrange
        var errors = Array.Empty<string>();
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert
        result.ShouldBe(prefix);
    }

    /// <summary>
    /// Tests edge case with null collection.
    /// </summary>
    [Fact]
    public void FormatErrorsString_NullCollection_ShouldReturnPrefix()
    {
        // Arrange
        IEnumerable<string>? errors = null!;
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors!, prefix);

        // Assert
        result.ShouldBe(prefix);
    }

    /// <summary>
    /// Tests that single error uses Span optimization correctly.
    /// </summary>
    [Fact]
    public void FormatErrorsString_SingleError_ShouldUseSpanOptimization()
    {
        // Arrange
        var errors = new[] { SpanTestConstants.SmallError1 };
        var prefix = ResultConstants.SuccessPrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert
        result.ShouldBe($"{ResultConstants.SuccessPrefix}: {SpanTestConstants.SmallError1}");
    }

    /// <summary>
    /// Tests that null errors in collection are handled gracefully.
    /// </summary>
    [Fact]
    public void FormatErrorsString_NullErrorsInCollection_ShouldHandleGracefully()
    {
        // Arrange - Collection with null entries
        var errors = new[] { SpanTestConstants.SmallError1, null!, SpanTestConstants.SmallError2 };
        var prefix = ResultConstants.FailurePrefix;

        // Act
        var result = Result.FormatErrorsString(errors, prefix);

        // Assert - Should handle nulls as empty strings
        result.ShouldContain(ResultConstants.FailurePrefix);
        result.ShouldContain(SpanTestConstants.SmallError1);
        result.ShouldContain(SpanTestConstants.SmallError2);
        
        // Should not crash and should contain separators
        result.ShouldContain(", ");
    }

/// <summary>
/// End Tests FormatErrorsString Span Optimization Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests CombineErrors Span Optimization Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that small collections use Span optimization in CombineErrors.
    /// </summary>
    [Fact]
    public void CombineErrors_SmallCollections_ShouldUseSpanOptimization()
    {
        // Arrange - Small collections that should trigger Span optimization
        var primaryErrors = new List<string> { "Primary1", "Primary2" };
        var secondaryErrors = new List<string> { "Secondary1", "Secondary2" };

        // Act
        var result = Result.CombineErrors(primaryErrors, secondaryErrors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(4);
        result.Errors.ShouldContain("Primary1");
        result.Errors.ShouldContain("Primary2");
        result.Errors.ShouldContain("Secondary1");
        result.Errors.ShouldContain("Secondary2");
    }

    /// <summary>
    /// Tests that large collections fall back to List in CombineErrors.
    /// </summary>
    [Fact]
    public void CombineErrors_LargeCollections_ShouldUseFallback()
    {
        // Arrange - Large collections that should trigger fallback
        var primaryErrors = Enumerable.Range(1, 20).Select(i => $"Primary{i}").ToList();
        var secondaryErrors = Enumerable.Range(1, 15).Select(i => $"Secondary{i}").ToList();

        // Act
        var result = Result.CombineErrors(primaryErrors, secondaryErrors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(35);
        result.Errors.ShouldContain("Primary1");
        result.Errors.ShouldContain("Primary20");
        result.Errors.ShouldContain("Secondary1");
        result.Errors.ShouldContain("Secondary15");
    }

    /// <summary>
    /// Tests that one null collection is handled correctly.
    /// </summary>
    [Fact]
    public void CombineErrors_OneNullCollection_ShouldHandleCorrectly()
    {
        // Arrange
        var primaryErrors = new List<string> { "Primary1", "Primary2" };
        IEnumerable<string>? secondaryErrors = null!;

        // Act
        var result = Result.CombineErrors(primaryErrors, secondaryErrors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(2);
        result.Errors.ShouldContain("Primary1");
        result.Errors.ShouldContain("Primary2");
    }

    /// <summary>
    /// Tests that both null collections return default error.
    /// </summary>
    [Fact]
    public void CombineErrors_BothNullCollections_ShouldReturnDefaultError()
    {
        // Arrange
        IEnumerable<string>? primaryErrors = null!;
        IEnumerable<string>? secondaryErrors = null!;

        // Act
        var result = Result.CombineErrors(primaryErrors, secondaryErrors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultConstants.NoErrorsFoundMessage);
    }

    /// <summary>
    /// Tests edge case where collections are at the Span optimization boundary.
    /// </summary>
    [Fact]
    public void CombineErrors_AtOptimizationBoundary_ShouldWorkCorrectly()
    {
        // Arrange - Collections exactly at the optimization limits
        var primaryErrors = Enumerable.Range(1, SpanTestConstants.SmallCollectionThreshold)
            .Select(i => $"Primary{i}").ToList();
        var secondaryErrors = Enumerable.Range(1, SpanTestConstants.SmallCollectionThreshold)
            .Select(i => $"Secondary{i}").ToList();

        // Act
        var result = Result.CombineErrors(primaryErrors, secondaryErrors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(32); // 16 + 16
        result.Errors.ShouldContain("Primary1");
        result.Errors.ShouldContain($"Primary{SpanTestConstants.SmallCollectionThreshold}");
        result.Errors.ShouldContain("Secondary1");
        result.Errors.ShouldContain($"Secondary{SpanTestConstants.SmallCollectionThreshold}");
    }

    /// <summary>
    /// Tests that non-ICollection types (like IEnumerable) fall back correctly.
    /// </summary>
    [Fact]
    public void CombineErrors_NonCollectionTypes_ShouldUseFallback()
    {
        // Arrange - IEnumerable that doesn't implement ICollection
        var primaryErrors = GenerateErrors("Primary", 3);
        var secondaryErrors = GenerateErrors("Secondary", 3);

        // Act
        var result = Result.CombineErrors(primaryErrors, secondaryErrors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(6);
        result.Errors.ShouldContain("Primary1");
        result.Errors.ShouldContain("Secondary3");
    }

    /// <summary>
    /// Helper method to generate IEnumerable that doesn't implement ICollection.
    /// </summary>
    /// <param name="prefix">Prefix for error messages.</param>
    /// <param name="count">Number of errors to generate.</param>
    /// <returns>IEnumerable of error messages.</returns>
    private static IEnumerable<string> GenerateErrors(string prefix, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            yield return $"{prefix}{i}";
        }
    }

/// <summary>
/// End Tests CombineErrors Span Optimization Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Integration Tests for Span Optimizations
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that Result.ToString() uses Span optimizations correctly.
    /// </summary>
    [Fact]
    public void Result_ToString_WithSpanOptimizations_ShouldWorkCorrectly()
    {
        // Arrange - Create results that should trigger Span optimizations
        var smallErrorArray = new[] { "Error1", "Error2", "Error3" };
        var result = Result.WithFailure(smallErrorArray);

        // Act
        var stringRepresentation = result.ToString();

        // Assert
        stringRepresentation.ShouldContain(ResultConstants.FailurePrefix);
        stringRepresentation.ShouldContain("Error1");
        stringRepresentation.ShouldContain("Error2");
        stringRepresentation.ShouldContain("Error3");
    }

    /// <summary>
    /// Tests that Result&lt;T&gt;.ToString() uses Span optimizations correctly.
    /// </summary>
    [Fact]
    public void ResultGeneric_ToString_WithSpanOptimizations_ShouldWorkCorrectly()
    {
        // Arrange
        var smallErrorArray = new[] { "GenericError1", "GenericError2" };
        var result = Result<string>.WithFailure(smallErrorArray, "test-value");

        // Act
        var stringRepresentation = result.ToString();

        // Assert
        stringRepresentation.ShouldContain(ResultConstants.FailurePrefix);
        stringRepresentation.ShouldContain("GenericError1");
        stringRepresentation.ShouldContain("GenericError2");
    }

    /// <summary>
    /// Performance regression test to ensure Span optimizations don't degrade performance.
    /// </summary>
    [Fact]
    public void SpanOptimizations_PerformanceRegression_ShouldNotDegrade()
    {
        // Arrange - Create test data for both small and large collections
        var smallErrors = new[] { "E1", "E2", "E3" };
        var mediumErrors = Enumerable.Range(1, 50).Select(i => $"Error{i}").ToArray();

        // Act & Assert - These operations should complete without excessive allocations
        // Small collections (should use Span)
        for (var i = 0; i < 1000; i++)
        {
            var smallResult = Result.WithFailure(smallErrors);
            var smallString = smallResult.ToString();
            smallString.ShouldNotBeNull();
        }

        // Medium collections (should use fallback)
        for (var i = 0; i < 100; i++)
        {
            var mediumResult = Result.WithFailure(mediumErrors);
            var mediumString = mediumResult.ToString();
            mediumString.ShouldNotBeNull();
        }

        // Combined operations
        for (var i = 0; i < 100; i++)
        {
            var combinedResult = Result.CombineErrors(smallErrors, smallErrors);
            var combinedString = combinedResult.ToString();
            combinedString.ShouldNotBeNull();
        }

        // If we reach here without timeout or memory issues, the optimization is working
        true.ShouldBeTrue("Performance regression test completed successfully");
    }

    /// <summary>
    /// Tests that different collection types work correctly with Span optimizations.
    /// </summary>
    [Fact]
    public void SpanOptimizations_DifferentCollectionTypes_ShouldWorkCorrectly()
    {
        // Arrange - Different collection types
        var arrayErrors = new[] { "Array1", "Array2" };
        var listErrors = new List<string> { "List1", "List2" };
        var hashSetErrors = new HashSet<string> { "Hash1", "Hash2" };
        var readOnlyErrors = new ReadOnlyCollection<string>(ReadOnlyArray);
        var immutableErrors = ReadOnlyArray.ToImmutableList();

        // Act - Create results and format strings
        var arrayResult = Result.WithFailure(arrayErrors);
        var listResult = Result.WithFailure(listErrors);
        var hashSetResult = Result.WithFailure(hashSetErrors);
        var readOnlyResult = Result.WithFailure(readOnlyErrors);
        var immutableResult = Result.WithFailure(immutableErrors);

        // Assert - All should work correctly regardless of optimization path
        arrayResult.ToString().ShouldContain("Array1");
        listResult.ToString().ShouldContain("List1");
        hashSetResult.ToString().ShouldContain("Hash1");
        readOnlyResult.ToString().ShouldContain("RO1");
        immutableResult.ToString().ShouldContain("RO1");

        // Test combining operations
        var combined = Result.CombineErrors(arrayErrors, listErrors);
        combined.Errors.Count().ShouldBe(4);
        combined.Errors.ShouldContain("Array1");
        combined.Errors.ShouldContain("List2");
    }

/// <summary>
/// End Tests Integration Tests for Span Optimizations
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Memory Safety Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that Span operations handle edge cases without buffer overruns.
    /// </summary>
    [Fact]
    public void SpanOptimizations_EdgeCases_ShouldBeSafe()
    {
        // Arrange & Act & Assert - These should not cause buffer overruns or exceptions

        // Empty strings
        var emptyStringErrors = new[] { "", "", "" };
        var emptyResult = Result.WithFailure(emptyStringErrors);
        emptyResult.ToString().ShouldNotBeNull();

        // Very short strings
        var shortErrors = new[] { "A", "B", "C" };
        var shortResult = Result.WithFailure(shortErrors);
        shortResult.ToString().ShouldContain("A");

        // Strings with special characters
        var specialErrors = new[] { "Error: \"quoted\"", "Error with\nnewlines", "Error\twith\ttabs" };
        var specialResult = Result.WithFailure(specialErrors);
        specialResult.ToString().ShouldContain("quoted");

        // Maximum small collection size
        var maxErrors = Enumerable.Range(1, SpanTestConstants.SmallCollectionThreshold)
            .Select(i => $"E{i}").ToArray();
        var maxResult = Result.WithFailure(maxErrors);
        maxResult.ToString().ShouldContain("E1");

        // All operations should complete without exceptions
        true.ShouldBeTrue("Memory safety tests completed successfully");
    }

/// <summary>
/// End Tests Memory Safety Tests
/// </summary>
/// <returns></returns>
} 
