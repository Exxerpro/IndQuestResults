namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Specifications for IsCancelled guards on Result and Result{T}.
/// </summary>
public class ResultIsCancelledTests
{
    [Fact]
    public void IsCancelled_Result_NullResult_ReturnsFalse()
    {
        Result? result = null;
        result!.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_Result_NullErrors_ReturnsFalse()
    {
        var result = Result.WithFailure((IEnumerable<string>?)null);
        result.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_Result_EmptyErrors_ReturnsFalse()
    {
        var result = Result.WithFailure(Array.Empty<string>());
        result.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_Result_OtherErrors_ReturnsFalse()
    {
        var result = Result.WithFailure(new[] { "Some error", "Another" });
        result.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_Result_ContainsCancelledError_ReturnsTrue()
    {
        var result = Result.WithFailure(new[] { "Some error", ResultErrors.OperationCancelled });
        result.IsCancelled().ShouldBeTrue();
    }

    [Fact]
    public void IsCancelled_GenericResult_NullResult_ReturnsFalse()
    {
        Result<int>? result = null;
        result!.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_GenericResult_NullErrors_ReturnsFalse()
    {
        var result = Result<int>.WithFailure((IEnumerable<string>?)null);
        result.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_GenericResult_EmptyErrors_ReturnsFalse()
    {
        var result = Result<int>.WithFailure(Array.Empty<string>());
        result.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_GenericResult_OtherErrors_ReturnsFalse()
    {
        var result = Result<int>.WithFailure(new[] { "Some error" });
        result.IsCancelled().ShouldBeFalse();
    }

    [Fact]
    public void IsCancelled_GenericResult_ContainsCancelledError_ReturnsTrue()
    {
        var result = Result<int>.WithFailure(new[] { ResultErrors.OperationCancelled });
        result.IsCancelled().ShouldBeTrue();
    }
}

