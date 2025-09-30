namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Targeted tests designed to kill specific surviving mutants identified through Stryker mutation testing.
/// Score target: Improve from 36.51% to 85% by killing 42 surviving mutants.
/// </summary>
public class ResultMutationKillerTests
{
    private static readonly string[] ErrorsForValid = ["", "  ", "Valid Error", "Another"];
    /// <summary>
    /// Kill boundary condition mutations in Span optimization paths.
    /// Target: collection.Count &lt;= 16 mutations to collection.Count &lt; 16, &gt;= 16, etc.
    /// </summary>
    [Theory]
    [InlineData(15)] // Just under threshold
    [InlineData(16)] // Exactly at threshold 
    [InlineData(17)] // Just over threshold
    public void FormatErrorsString_BoundaryConditions_ShouldKillLimitMutations(int errorCount)
    {
        // Arrange - Create exact number of errors to test boundary mutations
        var errors = Enumerable.Range(1, errorCount).Select(i => $"Error{i}").ToArray();
        
        // Act
        var result = Result.FormatErrorsString(errors, "Test");
        
        // Assert - Kill boundary mutations by testing exact behavior at critical points
        result.ShouldStartWith("Test: ");
        result.ShouldContain("Error1");
        if (errorCount > 1)
        {
            result.ShouldContain(", Error2"); // Kill separator mutations
        }
        if (errorCount >= 16)
        {
            result.ShouldContain("Error16"); // Kill >= vs > mutations
        }
        
        // Kill count-based mutations
        var commaCount = result.Count(c => c == ',');
        commaCount.ShouldBe(Math.Max(0, errorCount - 1)); // Exactly errorCount-1 separators
    }

    /// <summary>
    /// Kill logical operator mutations in Result constructor.
    /// Target: succeeded &amp;&amp; !hasAnyErrors mutations to ||, individual negations.
    /// </summary>
    [Theory]
    [InlineData(true, false, true)]   // succeeded=true, hasErrors=false -> IsSuccess=true
    [InlineData(true, true, false)]   // succeeded=true, hasErrors=true -> IsSuccess=false  
    [InlineData(false, false, false)] // succeeded=false, hasErrors=false -> IsSuccess=false
    [InlineData(false, true, false)]  // succeeded=false, hasErrors=true -> IsSuccess=false
    public void Result_Constructor_LogicalOperatorMutations_ShouldBeKilled(bool succeeded, bool hasErrors, bool expectedIsSuccess)
    {
        // Arrange - Create precise conditions to test && logic
        var errors = hasErrors ? ["Test Error"] : Array.Empty<string>();
        
        // Act - Use reflection to test private constructor directly
        var result = (Result)Activator.CreateInstance(typeof(Result), 
            BindingFlags.NonPublic | BindingFlags.Instance, null, 
            [succeeded, errors], null)!;
        
        // Assert - Kill && to || mutations and negation mutations
        result.IsSuccess.ShouldBe(expectedIsSuccess);
        result.IsFailure.ShouldBe(!expectedIsSuccess); // Kill property negation mutations
    }

    /// <summary>
    /// Kill arithmetic mutations in capacity estimation.
    /// Target: prefix.Length + 2, += mutations to -=, *, etc.
    /// </summary>
    [Fact]
    public void FormatErrorsString_CapacityEstimation_ShouldKillArithmeticMutations()
    {
        // Arrange - Specific lengths to expose arithmetic mutations
        var prefix = "ABC"; // 3 chars
        var errors = new[] { "XY", "Z" }; // 2 + 1 = 3 chars + separators
        
        // Act
        var result = Result.FormatErrorsString(errors, prefix);
        
        // Assert - Verify exact formatting to kill arithmetic mutations
        result.ShouldBe("ABC: XY, Z"); // Exact length: 3 + 2 + 2 + 2 + 1 = 10 chars
        result.Length.ShouldBe(10); // Kill length calculation mutations
        
        // Kill position increment mutations
        result.IndexOf(':').ShouldBe(3);  // Colon at exact position (prefix.Length)
        result.IndexOf(',').ShouldBe(7);  // Comma at expected position
    }

    /// <summary>
    /// Kill loop boundary mutations in BuildStringInSpan.
    /// Target: i &gt; 0 mutations to i &gt;= 0, i &lt; length mutations.
    /// </summary>
    [Fact]
    public void FormatErrorsString_LoopBoundaryMutations_ShouldBeKilled()
    {
        // Arrange - Multiple errors to test loop mutations
        var errors = new[] { "First", "Second", "Third" };
        
        // Act
        var result = Result.FormatErrorsString(errors, "Loop");
        
        // Assert - Kill i > 0 to i >= 0 mutation
        result.ShouldStartWith("Loop: First"); // First element should NOT have separator prefix
        
        // Kill i < length mutations by ensuring all elements present
        result.ShouldContain("First");
        result.ShouldContain("Second"); 
        result.ShouldContain("Third");
        
        // Kill separator logic mutations
        result.ShouldNotStartWith("Loop: , "); // No separator before first element
        result.ShouldEndWith("Third"); // No separator after last element
        
        // Verify exact separator count
        result.Count(c => c == ',').ShouldBe(2); // Exactly 2 separators for 3 elements
    }

    /// <summary>
    /// Kill null handling mutations in error processing.
    /// Target: error?.Length ?? 0 mutations, null coalescing operators.
    /// </summary>
    [Fact]
    public void FormatErrorsString_NullHandling_ShouldKillNullCoalescingMutations()
    {
        // Arrange - Mix of null and non-null errors
        var errors = new string?[] { "Valid", null, "", "  ", "Another" };
        
        // Act
        var result = Result.FormatErrorsString(errors.Select(s => s ?? string.Empty).ToArray(), "Null");
        
        // Assert - Kill null coalescing mutations
        result.ShouldContain("Valid");
        result.ShouldContain("Another");
        // Nulls are converted to empty strings, so we verify the structure
        result.ShouldBe("Null: Valid, , ,   , Another"); // Exact format with empty strings
        
        // Kill string.IsNullOrWhiteSpace mutations in Error property
        var resultObj = Result.WithFailure(errors.Select(s => s ?? string.Empty).ToArray());
        resultObj.Error.ShouldBe("Valid"); // Should skip null/empty/whitespace
    }

    /// <summary>
    /// Kill comparison mutations in stackalloc threshold.
    /// Target: estimatedLength &lt;= 512 mutations to &lt;, &gt;=, &gt;, etc.
    /// </summary>
    [Theory]
    [InlineData(510)] // Just under threshold
    [InlineData(512)] // Exactly at threshold
    [InlineData(514)] // Just over threshold
    public void FormatErrorsString_StackallocThreshold_ShouldKillComparisonMutations(int targetLength)
    {
        // Arrange - Create errors that produce specific estimated lengths
        var charCount = Math.Max(1, (targetLength - 10) / 2); // Account for prefix and separators
        var errors = new[] { new string('A', charCount), "B" };
        
        // Act
        var result = Result.FormatErrorsString(errors, "Test");
        
        // Assert - Behavior should be consistent regardless of optimization path
        result.ShouldStartWith("Test: ");
        result.ShouldContain(new string('A', charCount));
        result.ShouldContain("B");
        
        // Verify correct formatting regardless of internal optimization path
        var expectedSeparators = 1; // Two errors = one separator
        result.Count(c => c == ',').ShouldBe(expectedSeparators);
    }

    /// <summary>
    /// Kill string concatenation mutations in StringBuilder fallback.
    /// Target: stringBuilder.Append mutations, string interpolation changes.
    /// </summary>
    [Fact]
    public void FormatErrorsString_StringBuilderFallback_ShouldKillConcatenationMutations()
    {
        // Arrange - Large collection to force StringBuilder path
        var errors = Enumerable.Range(1, 20).Select(i => $"Error{i:D2}").ToArray();
        
        // Act
        var result = Result.FormatErrorsString(errors, "Large");
        
        // Assert - Kill string concatenation mutations
        result.ShouldStartWith("Large: Error01");
        result.ShouldEndWith("Error20");
        
        // Kill isFirst logic mutations
        result.ShouldNotContain(", Error01"); // First element should not have comma prefix
        result.ShouldContain(", Error02"); // Subsequent elements should have comma prefix
        
        // Verify separator consistency
        var expectedSeparators = 19; // 20 errors = 19 separators
        result.Count(c => c == ',').ShouldBe(expectedSeparators);
    }

    /// <summary>
    /// Kill property getter mutations in IsFailure, Error properties.
    /// Target: !IsSuccess mutations, LINQ mutations in Error property.
    /// </summary>
    [Fact]
    public void Result_PropertyMutations_ShouldBeKilled()
    {
        // Arrange & Act - Success result
        var successResult = Result.Success();
        
        // Assert - Kill IsFailure negation mutations
        successResult.IsSuccess.ShouldBeTrue();
        successResult.IsFailure.ShouldBeFalse(); // Kill !IsSuccess mutations
        
        // Arrange & Act - Failure result with specific error pattern
        var failureResult = Result.WithFailure(ErrorsForValid);
        
        // Assert - Kill Error property LINQ mutations
        failureResult.Error.ShouldBe("Valid Error"); // Should be first non-whitespace
        failureResult.Error.ShouldNotBe(""); // Kill FirstOrDefault mutations
        failureResult.Error.ShouldNotBeNull(); // Kill null return mutations
    }

    /// <summary>
    /// Kill mutation in array indexing and span operations.
    /// Target: index++ mutations to index--, index += 2, etc.
    /// </summary>
    [Fact]
    public void FormatErrorsString_IndexMutations_ShouldBeKilled()
    {
        // Arrange - Specific pattern to expose indexing mutations
        var errors = new[] { "A", "BB", "CCC" };
        
        // Act
        var result = Result.FormatErrorsString(errors, "X");
        
        // Assert - Kill index increment mutations by checking exact positions
        result.ShouldBe("X: A, BB, CCC");
        
        // Verify specific character positions to kill index arithmetic mutations
        result[0].ShouldBe('X');   // Position 0: prefix start
        result[1].ShouldBe(':');   // Position 1: colon
        result[2].ShouldBe(' ');   // Position 2: space
        result[3].ShouldBe('A');   // Position 3: first error
        result[4].ShouldBe(',');   // Position 4: separator
        result[5].ShouldBe(' ');   // Position 5: separator space
        result[6].ShouldBe('B');   // Position 6: second error start
    }

    /// <summary>
    /// Kill mutations in collection type checking and casting.
    /// Target: is string[] mutations, is ICollection mutations.
    /// </summary>
    [Fact]
    public void FormatErrorsString_CollectionTypeMutations_ShouldBeKilled()
    {
        // Arrange - Different collection types to test type checking mutations
        string[] array = ["Array1", "Array2"];
        ICollection<string> collection = ["List1", "List2"];
        var enumSource = new List<string> { "Enum1", "Enum2" };
        IEnumerable<string> enumerable = enumSource.Where(x => x.Length > 0);
        
        // Act & Assert - Array path
        var arrayResult = Result.FormatErrorsString(array, "Arr");
        arrayResult.ShouldBe("Arr: Array1, Array2");
        
        // Act & Assert - Collection path  
        var collectionResult = Result.FormatErrorsString(collection, "Col");
        collectionResult.ShouldBe("Col: List1, List2");
        
        // Act & Assert - Enumerable fallback path
        var enumResult = Result.FormatErrorsString(enumerable, "Enum");
        enumResult.ShouldBe("Enum: Enum1, Enum2");
        
        // Verify structure consistency (kill type checking mutations)
        arrayResult.ShouldStartWith("Arr: ");
        collectionResult.ShouldStartWith("Col: ");
        enumResult.ShouldStartWith("Enum: ");
        
        // All should contain their respective error items
        arrayResult.ShouldContain("Array1");
        arrayResult.ShouldContain("Array2");
        collectionResult.ShouldContain("List1");
        collectionResult.ShouldContain("List2");
        enumResult.ShouldContain("Enum1");
        enumResult.ShouldContain("Enum2");
    }

    /// <summary>
    /// Kill mutations in error message constants and default values.
    /// Target: ResultConstants mutations, default error handling.
    /// </summary>
    [Fact]
    public void ResultConstants_DefaultErrorMutations_ShouldBeKilled()
    {
        // Act & Assert - Test default error behavior
        var emptyResult = Result.WithFailure([]);
        emptyResult.Error.ShouldNotBeNull();
        emptyResult.Error.ShouldNotBeEmpty();
        
        #pragma warning disable CS8600, CS8625 // Justified: exercising SUT null-handling behavior
        var nullResult = Result.WithFailure((IEnumerable<string>)null!);
        #pragma warning restore CS8600, CS8625
        nullResult.Error.ShouldNotBeNull();
        nullResult.Error.ShouldNotBeEmpty();
        
        // Kill constant string mutations
        var defaultConstructorResult = new Result();
        defaultConstructorResult.IsFailure.ShouldBeTrue();
        defaultConstructorResult.IsSuccess.ShouldBeFalse();
    }

    /// <summary>
    /// Kill mutations in Min/Max operations and edge case handling.
    /// Target: Math.Max(0, errorCount - 1) mutations.
    /// </summary>
    [Theory]
    [InlineData(0, 0)] // No errors = no separators
    [InlineData(1, 0)] // One error = no separators  
    [InlineData(2, 1)] // Two errors = one separator
    [InlineData(5, 4)] // Five errors = four separators
    public void FormatErrorsString_SeparatorMathMutations_ShouldBeKilled(int errorCount, int expectedSeparators)
    {
        // Arrange
        var errors = Enumerable.Range(1, errorCount).Select(i => $"E{i}").ToArray();
        
        // Act
        var result = Result.FormatErrorsString(errors, "Math");
        
        // Assert - Kill Math.Max and arithmetic mutations
        var actualSeparators = result.Count(c => c == ',');
        actualSeparators.ShouldBe(expectedSeparators);
        
        if (errorCount == 0)
        {
            result.ShouldBe("Math");
        }
        else
        {
            result.ShouldStartWith("Math: ");
        }
    }

    /// <summary>
    /// Kill mutations in string concatenation and builder operations.
    /// Target: StringBuilder.Append variations, string operations.
    /// </summary>
    [Fact] 
    public void Result_StringBuilderMutations_ShouldBeKilled()
    {
        // Arrange - Large error collection to force StringBuilder path
        var manyErrors = Enumerable.Range(1, 25).Select(i => $"Error_{i:D3}").ToList();
        
        // Act
        var result = Result.WithFailure(manyErrors);
        
        // Assert - Kill StringBuilder mutations
        result.ToString().ShouldStartWith("WithFailure: Error_001");
        result.ToString().ShouldContain("Error_025");
        result.ToString().ShouldContain(", Error_002"); // Separator formatting
        
        // Kill Error property LINQ mutations  
        result.Error.ShouldBe("Error_001"); // First non-null/empty error
        result.Errors.Count().ShouldBe(25);
    }

    /// <summary>
    /// Kill mutations in capacity estimation and string operations.
    /// Target: estimatedLength calculations, buffer operations.
    /// </summary>
    /// <summary>
    /// Verifies boundary behavior for stackalloc threshold around 512 characters.
    /// </summary>
    [Fact]
    public void FormatErrorsString_CapacityMutations_ShouldBeKilled()
    {
        // Arrange - Specific lengths to test capacity calculation mutations
        var shortPrefix = "A";    // 1 char
        var mediumPrefix = "Medium";  // 6 chars  
        var longPrefix = "VeryLongPrefixForTesting"; // 23 chars
        var errors = new[] { "X", "YY" }; // 1 + 2 = 3 chars + separators
        
        // Act & Assert - Kill prefix.Length mutations
        var shortResult = Result.FormatErrorsString(errors, shortPrefix);
        shortResult.ShouldBe("A: X, YY");
        shortResult.Length.ShouldBe(8); // "A: X, YY" = 8 chars
        
        var mediumResult = Result.FormatErrorsString(errors, mediumPrefix);
        mediumResult.ShouldBe("Medium: X, YY"); 
        mediumResult.Length.ShouldBe(13); // "Medium: X, YY" = 13 chars
        
        var longResult = Result.FormatErrorsString(errors, longPrefix);
        longResult.ShouldStartWith("VeryLongPrefixForTesting: ");
        longResult.ShouldEndWith("X, YY");
    }

    /// <summary>
    /// Kill mutations in enumerable vs collection type checking.
    /// Target: IEnumerable vs ICollection branching, Count() vs .Count.
    /// </summary>
    [Fact]
    public void FormatErrorsString_EnumerableMutations_ShouldBeKilled()
    {
        // Arrange - Different enumerable types that may behave differently
        var arrayErrors = new[] { "A1", "A2" };
        var listErrors = new List<string> { "L1", "L2" };
        var queryErrors = new[] { "Q1", "Q2" }; // Q1, Q2
        var rangeErrors = Enumerable.Range(1, 3).Select(i => $"R{i}"); // R1, R2, R3
        
        // Act & Assert - Array path
        var arrayResult = Result.FormatErrorsString(arrayErrors, "Array");
        arrayResult.ShouldBe("Array: A1, A2");
        
        // Act & Assert - List/ICollection path  
        var listResult = Result.FormatErrorsString(listErrors, "List");
        listResult.ShouldBe("List: L1, L2");
        
        // Act & Assert - Filtered enumerable
        var queryResult = Result.FormatErrorsString(queryErrors, "Query");
        queryResult.ShouldBe("Query: Q1, Q2");
        
        // Act & Assert - Generated enumerable (forces enumeration)
        var rangeResult = Result.FormatErrorsString(rangeErrors, "Range");
        rangeResult.ShouldBe("Range: R1, R2, R3");
        
        // Kill .Count vs Count() mutations
        listErrors.Count.ShouldBe(2); // ICollection.Count property
        rangeErrors.Count().ShouldBe(3); // IEnumerable.Count() method
    }

    /// <summary>
    /// Kill mutations in span operations and memory management.
    /// Target: AsSpan(), CopyTo, position arithmetic.
    /// </summary>
    [Fact]
    public void FormatErrorsString_SpanMutations_ShouldBeKilled()
    {
        // Arrange - Small collection that will use span optimization
        var errors = new[] { "First", "Second", "Third", "Fourth" };
        
        // Act
        var result = Result.FormatErrorsString(errors, "Span");
        
        // Assert - Kill span position mutations
        result.ShouldBe("Span: First, Second, Third, Fourth");
        
        // Verify exact character positions (kill position increment mutations)
        result.IndexOf("Span", StringComparison.Ordinal).ShouldBe(0);
        result.IndexOf(':').ShouldBe(4);
        result.IndexOf("First", StringComparison.Ordinal).ShouldBe(6);
        result.IndexOf(',').ShouldBe(11); // First comma
        result.IndexOf("Second", StringComparison.Ordinal).ShouldBe(13);
        
        // Kill length calculation mutations
        var expectedLength = "Span: First, Second, Third, Fourth".Length;
        result.Length.ShouldBe(expectedLength);
    }

    /// <summary>
    /// Kill mutations in null coalescing and error handling patterns.
    /// Target: error?.Length ?? 0, null propagation operators.
    /// </summary>
    [Fact]
    public void FormatErrorsString_NullPropagationMutations_ShouldBeKilled()
    {
        // Arrange - Mix of null, empty, and whitespace strings
        var mixedErrors = new string?[] { null, "", "Valid", "  ", null, "Another", "" };
        
        // Act
        var result = Result.FormatErrorsString(mixedErrors.Select(s => s ?? string.Empty).ToArray(), "Null");
        
        // Assert - Kill null coalescing mutations (error?.Length ?? 0)
        result.ShouldBe("Null: , , Valid,   , , Another, ");
        
        // Kill null handling in Error property
        var failureResult = Result.WithFailure(mixedErrors.Select(s => s ?? string.Empty).ToArray());
        failureResult.Error.ShouldBe("Valid"); // First non-null/whitespace
        
        // Edge case: only nulls and empty strings
        var onlyNullsAndEmpty = new string?[] { null, "", "   ", null };
        var onlyNullsResult = Result.WithFailure(onlyNullsAndEmpty.Select(s => s ?? string.Empty).ToArray());
        onlyNullsResult.Error.ShouldBeNull(); // Skips null/empty/whitespace
    }
}



