using Xunit;
using Shouldly;

namespace IndQuestResults.Tests.Mutation;

/// <summary>
/// Unit tests for Result Metadata extension methods (v1.2.0 feature).
/// Tests type-as-key API for attaching supplementary information to Result objects.
/// </summary>
public class ResultMetadataExtensionsTests
{
    // Test metadata classes
    private class ExtractionMetadata
    {
        public double Confidence { get; set; }
        public int PatternMatches { get; set; }
    }

    private class ProcessingContext
    {
        public int Duration { get; set; }
        public string Stage { get; set; } = string.Empty;
    }

    private class QualityMetrics
    {
        public double Score { get; set; }
    }

    // ==================== Result (non-generic) Tests ====================

    [Fact]
    public void SetMetadata_OnResult_StoresMetadata()
    {
        // Arrange
        var result = Result.Success();
        var metadata = new ProcessingContext { Duration = 100, Stage = "Initial" };

        // Act
        result.SetMetadata(metadata);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Single(result.Metadata);
        Assert.True(result.Metadata.ContainsKey(typeof(ProcessingContext).FullName!));
    }

    [Fact]
    public void GetMetadata_OnResult_ReturnsCorrectMetadata()
    {
        // Arrange
        var result = Result.Success();
        var expected = new ProcessingContext { Duration = 150, Stage = "Processing" };
        result.SetMetadata(expected);

        // Act
        var actual = result.GetMetadata<ProcessingContext>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.Duration, actual.Duration);
        Assert.Equal(expected.Stage, actual.Stage);
    }

    [Fact]
    public void GetMetadata_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var metadata = result.GetMetadata<ProcessingContext>();

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void TryGetMetadata_WhenExists_ReturnsTrue()
    {
        // Arrange
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 200 });

        // Act
        var success = result.TryGetMetadata<ProcessingContext>(out var metadata);

        // Assert
        Assert.True(success);
        Assert.NotNull(metadata);
        Assert.Equal(200, metadata.Duration);
    }

    [Fact]
    public void TryGetMetadata_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var success = result.TryGetMetadata<ProcessingContext>(out var metadata);

        // Assert
        Assert.False(success);
        Assert.Null(metadata);
    }

    [Fact]
    public void SetMetadata_MultipleTypes_StoresSeparately()
    {
        // Arrange
        var result = Result.Success();
        var processing = new ProcessingContext { Duration = 100 };
        var quality = new QualityMetrics { Score = 0.95 };

        // Act
        result.SetMetadata(processing);
        result.SetMetadata(quality);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Equal(2, result.Metadata.Count);

        var retrievedProcessing = result.GetMetadata<ProcessingContext>();
        var retrievedQuality = result.GetMetadata<QualityMetrics>();

        Assert.NotNull(retrievedProcessing);
        Assert.NotNull(retrievedQuality);
        Assert.Equal(100, retrievedProcessing.Duration);
        Assert.Equal(0.95, retrievedQuality.Score);
    }

    [Fact]
    public void SetMetadata_SameTypeTwice_OverwritesPrevious()
    {
        // Arrange
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100, Stage = "First" });
        result.SetMetadata(new ProcessingContext { Duration = 200, Stage = "Second" });

        // Act
        var metadata = result.GetMetadata<ProcessingContext>();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(200, metadata.Duration);
        Assert.Equal("Second", metadata.Stage);
        result.Metadata.ShouldNotBeNull();
        Assert.Single(result.Metadata); // Only one entry
    }

    // ==================== Result<T> Tests ====================

    [Fact]
    public void SetMetadata_OnResultT_StoresMetadata()
    {
        // Arrange
        var result = Result<string>.Success("test data");
        var metadata = new ExtractionMetadata { Confidence = 0.87, PatternMatches = 25 };

        // Act
        result.SetMetadata(metadata);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Single(result.Metadata);
        Assert.True(result.Metadata.ContainsKey(typeof(ExtractionMetadata).FullName!));
    }

    [Fact]
    public void GetMetadata_OnResultT_ReturnsCorrectMetadata()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var expected = new ExtractionMetadata { Confidence = 0.92, PatternMatches = 30 };
        result.SetMetadata(expected);

        // Act
        var actual = result.GetMetadata<int, ExtractionMetadata>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expected.Confidence, actual.Confidence);
        Assert.Equal(expected.PatternMatches, actual.PatternMatches);
    }

    [Fact]
    public void GetMetadata_OnResultT_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var result = Result<string>.Success("data");

        // Act
        var metadata = result.GetMetadata<string, ExtractionMetadata>();

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void TryGetMetadata_OnResultT_WhenExists_ReturnsTrue()
    {
        // Arrange
        var result = Result<double>.Success(3.14);
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.85 });

        // Act
        var success = result.TryGetMetadata<double, ExtractionMetadata>(out var metadata);

        // Assert
        Assert.True(success);
        Assert.NotNull(metadata);
        Assert.Equal(0.85, metadata.Confidence);
    }

    [Fact]
    public void TryGetMetadata_OnResultT_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var result = Result<string>.Success("data");

        // Act
        var success = result.TryGetMetadata<string, ExtractionMetadata>(out var metadata);

        // Assert
        Assert.False(success);
        Assert.Null(metadata);
    }

    [Fact]
    public void SetMetadata_OnResultT_MultipleTypes_StoresSeparately()
    {
        // Arrange
        var result = Result<string>.Success("document");
        var extraction = new ExtractionMetadata { Confidence = 0.90, PatternMatches = 20 };
        var quality = new QualityMetrics { Score = 0.88 };

        // Act
        result.SetMetadata(extraction);
        result.SetMetadata(quality);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Equal(2, result.Metadata.Count);

        var retrievedExtraction = result.GetMetadata<string, ExtractionMetadata>();
        var retrievedQuality = result.GetMetadata<string, QualityMetrics>();

        Assert.NotNull(retrievedExtraction);
        Assert.NotNull(retrievedQuality);
        Assert.Equal(0.90, retrievedExtraction.Confidence);
        Assert.Equal(0.88, retrievedQuality.Score);
    }

    // ==================== Success + Failure Tests ====================

    [Fact]
    public void SetMetadata_OnFailedResult_StillWorks()
    {
        // Arrange
        var result = Result.WithFailure("Operation failed");
        var metadata = new ProcessingContext { Duration = 50, Stage = "Failed" };

        // Act
        result.SetMetadata(metadata);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Metadata);
        var retrieved = result.GetMetadata<ProcessingContext>();
        Assert.NotNull(retrieved);
        Assert.Equal(50, retrieved.Duration);
    }

    [Fact]
    public void SetMetadata_OnFailedResultT_StillWorks()
    {
        // Arrange
        var result = Result<string>.WithFailure("Parse error");
        var metadata = new ExtractionMetadata { Confidence = 0.42 };

        // Act
        result.SetMetadata(metadata);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Metadata);
        var retrieved = result.GetMetadata<string, ExtractionMetadata>();
        Assert.NotNull(retrieved);
        Assert.Equal(0.42, retrieved.Confidence);
    }

    // ==================== Type Safety Tests ====================

    [Fact]
    public void GetMetadata_WrongType_ReturnsNull()
    {
        // Arrange
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100 });

        // Act - Try to get as different type
        var metadata = result.GetMetadata<QualityMetrics>();

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void TryGetMetadata_WrongType_ReturnsFalse()
    {
        // Arrange
        var result = Result<string>.Success("data");
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.95 });

        // Act - Try to get as different type
        var success = result.TryGetMetadata<string, QualityMetrics>(out var metadata);

        // Assert
        Assert.False(success);
        Assert.Null(metadata);
    }

    // ==================== Null Safety Tests ====================

    [Fact]
    public void GetMetadata_NullMetadataDictionary_ReturnsNull()
    {
        // Arrange
        var result = Result.Success();
        // Metadata is null by default

        // Act
        var metadata = result.GetMetadata<ProcessingContext>();

        // Assert
        Assert.Null(result.Metadata);
        Assert.Null(metadata);
    }

    [Fact]
    public void TryGetMetadata_NullMetadataDictionary_ReturnsFalse()
    {
        // Arrange
        var result = Result<int>.Success(42);
        // Metadata is null by default

        // Act
        var success = result.TryGetMetadata<int, ExtractionMetadata>(out var metadata);

        // Assert
        Assert.Null(result.Metadata);
        Assert.False(success);
        Assert.Null(metadata);
    }

    // ==================== Integration Scenario ====================

    [Fact]
    public void FullWorkflow_ExtractorToFusion_TypeAsKey()
    {
        // Simulate extractor setting metadata
        var extractorResult = Result<string>.Success("Expediente ABC-123");
        extractorResult.SetMetadata(new ExtractionMetadata
        {
            Confidence = 0.87,
            PatternMatches = 25
        });
        extractorResult.SetMetadata(new QualityMetrics
        {
            Score = 0.92
        });

        // Simulate fusion service retrieving metadata
        if (extractorResult.TryGetMetadata<string, ExtractionMetadata>(out var extraction))
        {
            extraction.ShouldNotBeNull();
            Assert.Equal(0.87, extraction.Confidence);
            Assert.Equal(25, extraction.PatternMatches);
        }
        else
        {
            Assert.Fail("Extraction metadata should exist");
        }

        if (extractorResult.TryGetMetadata<string, QualityMetrics>(out var quality))
        {
            quality.ShouldNotBeNull();
            Assert.Equal(0.92, quality.Score);
        }
        else
        {
            Assert.Fail("Quality metadata should exist");
        }

        // Value is independent of metadata
        Assert.True(extractorResult.IsSuccess);
        Assert.Equal("Expediente ABC-123", extractorResult.Value);
    }
}
