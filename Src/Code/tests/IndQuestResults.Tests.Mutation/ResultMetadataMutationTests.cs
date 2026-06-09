using Shouldly;
using Xunit;

namespace IndQuestResults.Tests.Mutation;

/// <summary>
/// Mutation tests for Result Metadata functionality.
/// These tests are designed to kill mutants in the metadata-related code.
/// </summary>
public class ResultMetadataMutationTests
{
    // Test metadata classes
    private sealed class ProcessingContext
    {
        public int Duration { get; set; }
        public string Stage { get; set; } = string.Empty;
    }

    private sealed class ExtractionMetadata
    {
        public double Confidence { get; set; }
        public int PatternMatches { get; set; }
    }

    private sealed class QualityMetrics
    {
        public double Score { get; set; }
    }

    #region Result (Non-Generic) Metadata Mutation Tests

    [Fact]
    public void Result_SetMetadata_CreatesDictionary_KillsNullCheckMutations()
    {
        var result = Result.Success();
        var metadata = new ProcessingContext { Duration = 100 };

        result.SetMetadata(metadata);

        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Count.ShouldBe(1);
        // Kill mutations that skip dictionary creation
        result.Metadata.ContainsKey(typeof(ProcessingContext).FullName!).ShouldBeTrue();
    }

    [Fact]
    public void Result_GetMetadata_ReturnsCorrectValue_KillsTypeCheckMutations()
    {
        var result = Result.Success();
        var expected = new ProcessingContext { Duration = 150 };
        result.SetMetadata(expected);

        var actual = result.GetMetadata<ProcessingContext>();

        actual.ShouldNotBeNull();
        // Kill mutations that change property access
        actual!.Duration.ShouldBe(150);
        actual.Stage.ShouldBe(string.Empty);
    }

    [Fact]
    public void Result_TryGetMetadata_WhenExists_ReturnsTrue_KillsBooleanMutations()
    {
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 200 });

        var success = result.TryGetMetadata<ProcessingContext>(out var metadata);

        // Kill mutations that flip boolean return values
        success.ShouldBeTrue();
        metadata.ShouldNotBeNull();
        metadata!.Duration.ShouldBe(200);
    }

    [Fact]
    public void Result_TryGetMetadata_WhenNotExists_ReturnsFalse_KillsBooleanMutations()
    {
        var result = Result.Success();

        var success = result.TryGetMetadata<ProcessingContext>(out var metadata);

        // Kill mutations that flip boolean return values
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Result_SetMetadata_MultipleTypes_StoresSeparately_KillsKeyMutations()
    {
        var result = Result.Success();
        var processing = new ProcessingContext { Duration = 100 };
        var quality = new QualityMetrics { Score = 0.95 };

        result.SetMetadata(processing);
        result.SetMetadata(quality);

        // Kill mutations that overwrite keys or skip entries
        result.Metadata!.Count.ShouldBe(2);
        result.GetMetadata<ProcessingContext>().ShouldNotBeNull();
        result.GetMetadata<QualityMetrics>().ShouldNotBeNull();
    }

    [Fact]
    public void Result_SetMetadata_SameTypeTwice_OverwritesPrevious_KillsOverwriteMutations()
    {
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100, Stage = "First" });
        result.SetMetadata(new ProcessingContext { Duration = 200, Stage = "Second" });

        var metadata = result.GetMetadata<ProcessingContext>();
        // Kill mutations that don't overwrite or skip updates
        metadata.ShouldNotBeNull();
        metadata!.Duration.ShouldBe(200);
        metadata.Stage.ShouldBe("Second");
        result.Metadata!.Count.ShouldBe(1);
    }

    [Fact]
    public void Result_GetMetadata_WrongType_ReturnsNull_KillsTypeCheckMutations()
    {
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100 });

        var metadata = result.GetMetadata<QualityMetrics>();

        // Kill mutations that return wrong type or skip type checking
        metadata.ShouldBeNull();
    }

    #endregion

    #region Result<T> (Generic) Metadata Mutation Tests

    [Fact]
    public void ResultT_SetMetadata_CreatesDictionary_KillsNullCheckMutations()
    {
        var result = Result<string>.Success("test");
        var metadata = new ExtractionMetadata { Confidence = 0.87 };

        result.SetMetadata(metadata);

        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Count.ShouldBe(1);
        result.Metadata.ContainsKey(typeof(ExtractionMetadata).FullName!).ShouldBeTrue();
    }

    [Fact]
    public void ResultT_GetMetadata_ReturnsCorrectValue_KillsTypeCheckMutations()
    {
        var result = Result<int>.Success(42);
        var expected = new ExtractionMetadata { Confidence = 0.92, PatternMatches = 30 };
        result.SetMetadata(expected);

        var actual = result.GetMetadata<int, ExtractionMetadata>();

        actual.ShouldNotBeNull();
        actual!.Confidence.ShouldBe(0.92);
        actual.PatternMatches.ShouldBe(30);
    }

    [Fact]
    public void ResultT_TryGetMetadata_WhenExists_ReturnsTrue_KillsBooleanMutations()
    {
        var result = Result<double>.Success(3.14);
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.85 });

        var success = result.TryGetMetadata<double, ExtractionMetadata>(out var metadata);

        success.ShouldBeTrue();
        metadata.ShouldNotBeNull();
        metadata!.Confidence.ShouldBe(0.85);
    }

    [Fact]
    public void ResultT_TryGetMetadata_WhenNotExists_ReturnsFalse_KillsBooleanMutations()
    {
        var result = Result<string>.Success("data");

        var success = result.TryGetMetadata<string, ExtractionMetadata>(out var metadata);

        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void ResultT_SetMetadata_MultipleTypes_StoresSeparately_KillsKeyMutations()
    {
        var result = Result<string>.Success("document");
        var extraction = new ExtractionMetadata { Confidence = 0.90 };
        var quality = new QualityMetrics { Score = 0.88 };

        result.SetMetadata(extraction);
        result.SetMetadata(quality);

        result.Metadata!.Count.ShouldBe(2);
        result.GetMetadata<string, ExtractionMetadata>().ShouldNotBeNull();
        result.GetMetadata<string, QualityMetrics>().ShouldNotBeNull();
    }

    [Fact]
    public void ResultT_GetMetadata_WrongType_ReturnsNull_KillsTypeCheckMutations()
    {
        var result = Result<string>.Success("data");
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.95 });

        var metadata = result.GetMetadata<string, QualityMetrics>();

        metadata.ShouldBeNull();
    }

    #endregion

    #region Edge Case Mutation Tests

    [Fact]
    public void Metadata_NullMetadataDictionary_GetReturnsNull_KillsNullCheckMutations()
    {
        var result = Result.Success();

        var metadata = result.GetMetadata<ProcessingContext>();

        result.Metadata.ShouldBeNull();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Metadata_NullMetadataDictionary_TryGetReturnsFalse_KillsNullCheckMutations()
    {
        var result = Result<int>.Success(42);

        var success = result.TryGetMetadata<int, ExtractionMetadata>(out var metadata);

        result.Metadata.ShouldBeNull();
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Metadata_WorksOnFailedResult_KillsStateCheckMutations()
    {
        var result = Result.WithFailure("Operation failed");
        var metadata = new ProcessingContext { Duration = 50 };

        result.SetMetadata(metadata);

        result.IsSuccess.ShouldBeFalse();
        result.Metadata.ShouldNotBeNull();
        var retrieved = result.GetMetadata<ProcessingContext>();
        retrieved.ShouldNotBeNull();
        retrieved!.Duration.ShouldBe(50);
    }

    [Fact]
    public void Metadata_WorksOnResultWithException_KillsExceptionCheckMutations()
    {
        var exception = new InvalidOperationException("Test exception");
        var result = Result.WithFailure(exception);
        var metadata = new ProcessingContext { Duration = 100 };

        result.SetMetadata(metadata);

        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Metadata.ShouldNotBeNull();
        var retrieved = result.GetMetadata<ProcessingContext>();
        retrieved.ShouldNotBeNull();
        retrieved!.Duration.ShouldBe(100);
    }

    #endregion
}

