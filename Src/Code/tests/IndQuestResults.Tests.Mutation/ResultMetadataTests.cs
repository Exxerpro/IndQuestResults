using Shouldly;
using Xunit;

namespace IndQuestResults.Tests.Mutation;

/// <summary>
/// Comprehensive tests for Metadata property on both Result and Result&lt;T&gt; classes.
/// Verifies that metadata can be attached, retrieved, and managed on both result types.
/// </summary>
public class ResultMetadataTests
{
    // Test metadata classes
    private class ProcessingContext
    {
        public int Duration { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string? SourceSystem { get; set; }
    }

    private class ExtractionMetadata
    {
        public double Confidence { get; set; }
        public int PatternMatches { get; set; }
        public bool IsValidated { get; set; }
    }

    private class QualityMetrics
    {
        public double Score { get; set; }
        public int MissingFields { get; set; }
    }

    private class AuditInfo
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    #region Result (Non-Generic) Metadata Tests

    [Fact]
    public void Result_Metadata_PropertyExists()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.Metadata.ShouldBeNull(); // Should be null by default
    }

    [Fact]
    public void Result_SetMetadata_CreatesDictionary()
    {
        // Arrange
        var result = Result.Success();
        var metadata = new ProcessingContext { Duration = 100, Stage = "Initial" };

        // Act
        result.SetMetadata(metadata);

        // Assert
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Count.ShouldBe(1);
        result.Metadata.ContainsKey(typeof(ProcessingContext).FullName!).ShouldBeTrue();
    }

    [Fact]
    public void Result_GetMetadata_ReturnsCorrectValue()
    {
        // Arrange
        var result = Result.Success();
        var expected = new ProcessingContext { Duration = 150, Stage = "Processing", SourceSystem = "Gateway" };
        result.SetMetadata(expected);

        // Act
        var actual = result.GetMetadata<ProcessingContext>();

        // Assert
        actual.ShouldNotBeNull();
        actual!.Duration.ShouldBe(150);
        actual.Stage.ShouldBe("Processing");
        actual.SourceSystem.ShouldBe("Gateway");
    }

    [Fact]
    public void Result_GetMetadata_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var metadata = result.GetMetadata<ProcessingContext>();

        // Assert
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Result_TryGetMetadata_WhenExists_ReturnsTrue()
    {
        // Arrange
        var result = Result.Success();
        var expected = new ProcessingContext { Duration = 200 };
        result.SetMetadata(expected);

        // Act
        var success = result.TryGetMetadata<ProcessingContext>(out var metadata);

        // Assert
        success.ShouldBeTrue();
        metadata.ShouldNotBeNull();
        metadata!.Duration.ShouldBe(200);
    }

    [Fact]
    public void Result_TryGetMetadata_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var success = result.TryGetMetadata<ProcessingContext>(out var metadata);

        // Assert
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Result_SetMetadata_MultipleTypes_StoresSeparately()
    {
        // Arrange
        var result = Result.Success();
        var processing = new ProcessingContext { Duration = 100, Stage = "Stage1" };
        var quality = new QualityMetrics { Score = 0.95, MissingFields = 2 };
        var audit = new AuditInfo { UserId = "user123", Timestamp = DateTime.UtcNow, CorrelationId = "corr-456" };

        // Act
        result.SetMetadata(processing);
        result.SetMetadata(quality);
        result.SetMetadata(audit);

        // Assert
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Count.ShouldBe(3);

        var retrievedProcessing = result.GetMetadata<ProcessingContext>();
        var retrievedQuality = result.GetMetadata<QualityMetrics>();
        var retrievedAudit = result.GetMetadata<AuditInfo>();

        retrievedProcessing.ShouldNotBeNull();
        retrievedQuality.ShouldNotBeNull();
        retrievedAudit.ShouldNotBeNull();

        retrievedProcessing!.Duration.ShouldBe(100);
        retrievedQuality!.Score.ShouldBe(0.95);
        retrievedAudit!.UserId.ShouldBe("user123");
    }

    [Fact]
    public void Result_SetMetadata_SameTypeTwice_OverwritesPrevious()
    {
        // Arrange
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100, Stage = "First" });

        // Act
        result.SetMetadata(new ProcessingContext { Duration = 200, Stage = "Second" });

        // Assert
        var metadata = result.GetMetadata<ProcessingContext>();
        metadata.ShouldNotBeNull();
        metadata!.Duration.ShouldBe(200);
        metadata.Stage.ShouldBe("Second");
        result.Metadata!.Count.ShouldBe(1); // Only one entry
    }

    [Fact]
    public void Result_GetMetadata_WrongType_ReturnsNull()
    {
        // Arrange
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100 });

        // Act - Try to get as different type
        var metadata = result.GetMetadata<QualityMetrics>();

        // Assert
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Result_TryGetMetadata_WrongType_ReturnsFalse()
    {
        // Arrange
        var result = Result.Success();
        result.SetMetadata(new ProcessingContext { Duration = 100 });

        // Act - Try to get as different type
        var success = result.TryGetMetadata<QualityMetrics>(out var metadata);

        // Assert
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Result_Metadata_WorksOnFailedResult()
    {
        // Arrange
        var result = Result.WithFailure("Operation failed");
        var metadata = new ProcessingContext { Duration = 50, Stage = "Failed" };

        // Act
        result.SetMetadata(metadata);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Metadata.ShouldNotBeNull();
        var retrieved = result.GetMetadata<ProcessingContext>();
        retrieved.ShouldNotBeNull();
        retrieved!.Duration.ShouldBe(50);
    }

    [Fact]
    public void Result_Metadata_WorksOnResultWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var result = Result.WithFailure(exception);
        var metadata = new AuditInfo { UserId = "admin", Timestamp = DateTime.UtcNow, CorrelationId = "ex-123" };

        // Act
        result.SetMetadata(metadata);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Metadata.ShouldNotBeNull();
        var retrieved = result.GetMetadata<AuditInfo>();
        retrieved.ShouldNotBeNull();
        retrieved!.UserId.ShouldBe("admin");
    }

    #endregion

    #region Result<T> (Generic) Metadata Tests

    [Fact]
    public void ResultT_Metadata_PropertyExists()
    {
        // Arrange & Act
        var result = Result<string>.Success("test");

        // Assert
        result.Metadata.ShouldBeNull(); // Should be null by default
    }

    [Fact]
    public void ResultT_SetMetadata_CreatesDictionary()
    {
        // Arrange
        var result = Result<string>.Success("test data");
        var metadata = new ExtractionMetadata { Confidence = 0.87, PatternMatches = 25 };

        // Act
        result.SetMetadata(metadata);

        // Assert
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Count.ShouldBe(1);
        result.Metadata.ContainsKey(typeof(ExtractionMetadata).FullName!).ShouldBeTrue();
    }

    [Fact]
    public void ResultT_GetMetadata_ReturnsCorrectValue()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var expected = new ExtractionMetadata { Confidence = 0.92, PatternMatches = 30, IsValidated = true };
        result.SetMetadata(expected);

        // Act
        var actual = result.GetMetadata<int, ExtractionMetadata>();

        // Assert
        actual.ShouldNotBeNull();
        actual!.Confidence.ShouldBe(0.92);
        actual.PatternMatches.ShouldBe(30);
        actual.IsValidated.ShouldBeTrue();
    }

    [Fact]
    public void ResultT_GetMetadata_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var result = Result<string>.Success("data");

        // Act
        var metadata = result.GetMetadata<string, ExtractionMetadata>();

        // Assert
        metadata.ShouldBeNull();
    }

    [Fact]
    public void ResultT_TryGetMetadata_WhenExists_ReturnsTrue()
    {
        // Arrange
        var result = Result<double>.Success(3.14);
        var expected = new ExtractionMetadata { Confidence = 0.85 };
        result.SetMetadata(expected);

        // Act
        var success = result.TryGetMetadata<double, ExtractionMetadata>(out var metadata);

        // Assert
        success.ShouldBeTrue();
        metadata.ShouldNotBeNull();
        metadata!.Confidence.ShouldBe(0.85);
    }

    [Fact]
    public void ResultT_TryGetMetadata_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var result = Result<string>.Success("data");

        // Act
        var success = result.TryGetMetadata<string, ExtractionMetadata>(out var metadata);

        // Assert
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void ResultT_SetMetadata_MultipleTypes_StoresSeparately()
    {
        // Arrange
        var result = Result<string>.Success("document");
        var extraction = new ExtractionMetadata { Confidence = 0.90, PatternMatches = 20 };
        var quality = new QualityMetrics { Score = 0.88, MissingFields = 1 };
        var audit = new AuditInfo { UserId = "extractor", Timestamp = DateTime.UtcNow, CorrelationId = "doc-789" };

        // Act
        result.SetMetadata(extraction);
        result.SetMetadata(quality);
        result.SetMetadata(audit);

        // Assert
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Count.ShouldBe(3);

        var retrievedExtraction = result.GetMetadata<string, ExtractionMetadata>();
        var retrievedQuality = result.GetMetadata<string, QualityMetrics>();
        var retrievedAudit = result.GetMetadata<string, AuditInfo>();

        retrievedExtraction.ShouldNotBeNull();
        retrievedQuality.ShouldNotBeNull();
        retrievedAudit.ShouldNotBeNull();

        retrievedExtraction!.Confidence.ShouldBe(0.90);
        retrievedQuality!.Score.ShouldBe(0.88);
        retrievedAudit!.CorrelationId.ShouldBe("doc-789");
    }

    [Fact]
    public void ResultT_SetMetadata_SameTypeTwice_OverwritesPrevious()
    {
        // Arrange
        var result = Result<int>.Success(100);
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.80, PatternMatches = 10 });

        // Act
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.95, PatternMatches = 25 });

        // Assert
        var metadata = result.GetMetadata<int, ExtractionMetadata>();
        metadata.ShouldNotBeNull();
        metadata!.Confidence.ShouldBe(0.95);
        metadata.PatternMatches.ShouldBe(25);
        result.Metadata!.Count.ShouldBe(1); // Only one entry
    }

    [Fact]
    public void ResultT_GetMetadata_WrongType_ReturnsNull()
    {
        // Arrange
        var result = Result<string>.Success("data");
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.95 });

        // Act - Try to get as different type
        var metadata = result.GetMetadata<string, QualityMetrics>();

        // Assert
        metadata.ShouldBeNull();
    }

    [Fact]
    public void ResultT_TryGetMetadata_WrongType_ReturnsFalse()
    {
        // Arrange
        var result = Result<int>.Success(42);
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.95 });

        // Act - Try to get as different type
        var success = result.TryGetMetadata<int, QualityMetrics>(out var metadata);

        // Assert
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void ResultT_Metadata_WorksOnFailedResult()
    {
        // Arrange
        var result = Result<string>.WithFailure("Parse error");
        var metadata = new ExtractionMetadata { Confidence = 0.42, PatternMatches = 5 };

        // Act
        result.SetMetadata(metadata);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Metadata.ShouldNotBeNull();
        var retrieved = result.GetMetadata<string, ExtractionMetadata>();
        retrieved.ShouldNotBeNull();
        retrieved!.Confidence.ShouldBe(0.42);
    }

    [Fact]
    public void ResultT_Metadata_WorksOnResultWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Processing failed");
        var result = Result<int>.WithFailure(exception, 0);
        var metadata = new AuditInfo { UserId = "processor", Timestamp = DateTime.UtcNow, CorrelationId = "err-456" };

        // Act
        result.SetMetadata(metadata);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Metadata.ShouldNotBeNull();
        var retrieved = result.GetMetadata<int, AuditInfo>();
        retrieved.ShouldNotBeNull();
        retrieved!.CorrelationId.ShouldBe("err-456");
    }

    [Fact]
    public void ResultT_Metadata_IndependentOfValue()
    {
        // Arrange
        var result = Result<string>.Success("Expediente ABC-123");
        result.SetMetadata(new ExtractionMetadata { Confidence = 0.87 });

        // Act & Assert
        result.Value.ShouldBe("Expediente ABC-123");
        result.IsSuccess.ShouldBeTrue();
        result.Metadata.ShouldNotBeNull();
        
        var metadata = result.GetMetadata<string, ExtractionMetadata>();
        metadata.ShouldNotBeNull();
        metadata!.Confidence.ShouldBe(0.87);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Metadata_NullMetadataDictionary_GetReturnsNull()
    {
        // Arrange
        var result = Result.Success();
        // Metadata is null by default

        // Act
        var metadata = result.GetMetadata<ProcessingContext>();

        // Assert
        result.Metadata.ShouldBeNull();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Metadata_NullMetadataDictionary_TryGetReturnsFalse()
    {
        // Arrange
        var result = Result<int>.Success(42);
        // Metadata is null by default

        // Act
        var success = result.TryGetMetadata<int, ExtractionMetadata>(out var metadata);

        // Assert
        result.Metadata.ShouldBeNull();
        success.ShouldBeFalse();
        metadata.ShouldBeNull();
    }

    [Fact]
    public void Metadata_FullWorkflow_ExtractorToFusion()
    {
        // Simulate extractor setting metadata
        var extractorResult = Result<string>.Success("Expediente ABC-123");
        extractorResult.SetMetadata(new ExtractionMetadata
        {
            Confidence = 0.87,
            PatternMatches = 25,
            IsValidated = true
        });
        extractorResult.SetMetadata(new QualityMetrics
        {
            Score = 0.92,
            MissingFields = 0
        });
        extractorResult.SetMetadata(new AuditInfo
        {
            UserId = "extractor-service",
            Timestamp = DateTime.UtcNow,
            CorrelationId = "extract-001"
        });

        // Simulate fusion service retrieving metadata
        extractorResult.TryGetMetadata<string, ExtractionMetadata>(out var extraction).ShouldBeTrue();
        extraction.ShouldNotBeNull();
        extraction!.Confidence.ShouldBe(0.87);
        extraction.PatternMatches.ShouldBe(25);
        extraction.IsValidated.ShouldBeTrue();

        extractorResult.TryGetMetadata<string, QualityMetrics>(out var quality).ShouldBeTrue();
        quality.ShouldNotBeNull();
        quality!.Score.ShouldBe(0.92);
        quality.MissingFields.ShouldBe(0);

        extractorResult.TryGetMetadata<string, AuditInfo>(out var audit).ShouldBeTrue();
        audit.ShouldNotBeNull();
        audit!.CorrelationId.ShouldBe("extract-001");

        // Value is independent of metadata
        extractorResult.IsSuccess.ShouldBeTrue();
        extractorResult.Value.ShouldBe("Expediente ABC-123");
    }

    [Fact]
    public void Metadata_MultipleResults_IndependentMetadata()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        
        result1.SetMetadata(new ProcessingContext { Duration = 100 });
        result2.SetMetadata(new ProcessingContext { Duration = 200 });

        // Act & Assert
        result1.GetMetadata<ProcessingContext>()!.Duration.ShouldBe(100);
        result2.GetMetadata<ProcessingContext>()!.Duration.ShouldBe(200);
        
        // Results are independent
        result1.Metadata!.Count.ShouldBe(1);
        result2.Metadata!.Count.ShouldBe(1);
    }

    [Fact]
    public void Metadata_ResultT_MultipleResults_IndependentMetadata()
    {
        // Arrange
        var result1 = Result<int>.Success(10);
        var result2 = Result<int>.Success(20);
        
        result1.SetMetadata(new ExtractionMetadata { Confidence = 0.80 });
        result2.SetMetadata(new ExtractionMetadata { Confidence = 0.90 });

        // Act & Assert
        result1.GetMetadata<int, ExtractionMetadata>()!.Confidence.ShouldBe(0.80);
        result2.GetMetadata<int, ExtractionMetadata>()!.Confidence.ShouldBe(0.90);
        
        // Results are independent
        result1.Metadata!.Count.ShouldBe(1);
        result2.Metadata!.Count.ShouldBe(1);
        result1.Value.ShouldBe(10);
        result2.Value.ShouldBe(20);
    }

    #endregion
}

