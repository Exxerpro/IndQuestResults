namespace IndQuestResults.Tests.Unit.Operations;

public class ResultTryExtensionsTests
{
    [Fact]
    public void Try_Catches_Exception_MapsError()
    {
        var r = ResultTryExtensions.Try<int>(() => throw new InvalidOperationException("boom"), ex => $"ERR:{ex.GetType().Name}");
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldContain("ERR:InvalidOperationException");
    }

    [Fact]
    public void Try_Success_ReturnsValue()
    {
        var r = ResultTryExtensions.Try(() => 5, _ => "err");
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(5);
    }

    [Fact]
    public async Task TryAsync_Catches_Exception_MapsError()
    {
        var r = await ResultTryExtensions.TryAsync<int>(async () => { await Task.Delay(1); throw new InvalidOperationException("boom"); }, ex => $"E:{ex.Message}");
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldContain("E:");
    }

    [Fact]
    public void MapTry_Maps_OnSuccess_ElsePropagates()
    {
        var ok = Result<string>.Success("a");
        var mapped = ok.MapTry(s => s + "b", _ => "x");
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe("ab");

        var fail = Result<string>.WithFailure("e");
        var mappedFail = fail.MapTry(s => s + "b", _ => "x");
        mappedFail.IsFailure.ShouldBeTrue();
        mappedFail.Errors.ShouldContain("e");
    }

    [Fact]
    public void MapTry_Exception_ReturnsFailure()
    {
        var ok = Result<int>.Success(1);
        var mapped = ok.MapTry<int, int>(_ => throw new InvalidOperationException("err"), ex => $"E:{ex.Message}");
        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldContain("E:err");
    }

    [Fact]
    public void BindTry_Binds_OnSuccess_ElsePropagates()
    {
        var ok = Result<int>.Success(2);
        var bound = ok.BindTry(v => Result<string>.Success(v.ToString()), _ => "x");
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("2");

        var fail = Result<int>.WithFailure("e");
        var boundFail = fail.BindTry(v => Result<string>.Success(v.ToString()), _ => "x");
        boundFail.IsFailure.ShouldBeTrue();
        boundFail.Errors.ShouldContain("e");
    }

    [Fact]
    public void BindTry_Exception_ReturnsFailure()
    {
        var ok = Result<int>.Success(3);
        var bound = ok.BindTry<int, string>(_ => throw new InvalidOperationException("err"), ex => $"E:{ex.Message}");
        bound.IsFailure.ShouldBeTrue();
        bound.Error.ShouldContain("E:err");
    }

    [Fact]
    public void Try_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = ResultTryExtensions.Try<int>(() => throw exception, ex => $"Error: {ex.Message}");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public async Task TryAsync_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = await ResultTryExtensions.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw exception;
        }, ex => $"Error: {ex.Message}");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public void MapTry_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var input = Result<int>.Success(5);

        // Act
        var result = input.MapTry<int, int>(_ => throw exception, ex => $"Error: {ex.Message}");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public void BindTry_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var input = Result<int>.Success(5);

        // Act
        var result = input.BindTry<int, int>(_ => throw exception, ex => $"Error: {ex.Message}");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }
}

