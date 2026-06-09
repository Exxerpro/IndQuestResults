namespace IndQuestResults.Tests.Mutation.Operations;

public class CancellationAwareResultExceptionTests
{
    [Fact]
    public async Task WrapCancellationAware_Generic_OperationThrows_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapCancellationAware<int>(async _ =>
        {
            await Task.Delay(1, CancellationToken.None);
            throw new InvalidOperationException("oops");
        });
        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Operation failed:");
    }

    [Fact]
    public async Task WrapResultOperation_OperationThrows_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapResultOperation<int>(async _ =>
        {
            await Task.Delay(1, CancellationToken.None);
            throw new InvalidOperationException("boom");
        });
        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Operation failed:");
    }

    [Fact]
    public async Task WrapCancellationAware_NonGeneric_Throws_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapCancellationAware(async _ =>
        {
            await Task.Delay(1, CancellationToken.None);
            throw new InvalidOperationException("err");
        });
        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Operation failed:");
    }

    [Fact]
    public async Task WrapCancellationAware_Generic_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<int>(async ct =>
        {
            await Task.Delay(1, ct);
            throw exception;
        });

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public async Task WrapResultOperation_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new ArgumentException("Test exception");

        // Act
        var result = await CancellationAwareResult.WrapResultOperation<int>(async ct =>
        {
            await Task.Delay(1, ct);
            throw exception;
        });

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public async Task WrapCancellationAware_NonGeneric_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware(async ct =>
        {
            await Task.Delay(1, ct);
            throw exception;
        });

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public async Task WrapWithTimeout_ExceptionPreserved_InExceptionProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var result = await CancellationAwareResult.WrapWithTimeout<int>(async ct =>
        {
            await Task.Delay(10, ct);
            throw exception;
        }, timeout);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeTrue();
    }
}
