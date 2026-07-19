namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for the synchronous <see cref="ResultExtensions.RequireValue{T}(Result{T}, string)"/> overload,
/// including parity with the asynchronous <c>Task&lt;Result&lt;T?&gt;&gt;</c> overload.
/// </summary>
public class ResultRequireValueSyncTests
{
    [Fact]
    public void RequireValue_Failure_PropagatesErrors_DropsCarriedValue()
    {
        // Mirrors the Task overload: failures propagate errors only, even when carrying a value.
        var input = Result<string?>.WithFailure(new[] { "e1", "e2" }, "carried");

        var output = input.RequireValue("missing");

        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldBe(new[] { "e1", "e2" });
        output.Value.ShouldBeNull();
    }

    [Fact]
    public void RequireValue_SuccessNull_BecomesFailureWithExactMessage()
    {
        var input = new Result<string?>(true, (IEnumerable<string>?)null, null);

        var output = input.RequireValue("entity not found");

        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldBe(new[] { "entity not found" });
    }

    [Fact]
    public void RequireValue_SuccessNonNull_SucceedsWithSameValue()
    {
        var output = Result<string?>.Success("value").RequireValue("missing");

        output.IsSuccess.ShouldBeTrue();
        output.Value.ShouldBe("value");
    }

    [Theory]
    [InlineData("value")]
    [InlineData(null)]
    public async Task RequireValue_SyncAndTaskOverloads_AgreeOnAllInputs(string? value)
    {
        var syncInput = new Result<string?>(true, (IEnumerable<string>?)null, value);
        var taskInput = Task.FromResult(new Result<string?>(true, (IEnumerable<string>?)null, value));

        var syncOutput = syncInput.RequireValue("missing");
        var taskOutput = await taskInput.RequireValue("missing");

        taskOutput.IsSuccess.ShouldBe(syncOutput.IsSuccess);
        taskOutput.IsFailure.ShouldBe(syncOutput.IsFailure);
        taskOutput.Errors.ShouldBe(syncOutput.Errors);
        taskOutput.Value.ShouldBe(syncOutput.Value);
    }

    [Fact]
    public async Task RequireValue_SyncAndTaskOverloads_AgreeOnFailure()
    {
        var syncOutput = Result<string?>.WithFailure(new[] { "boom" }).RequireValue("missing");
        var taskOutput = await Task.FromResult(Result<string?>.WithFailure(new[] { "boom" })).RequireValue("missing");

        taskOutput.IsFailure.ShouldBe(syncOutput.IsFailure);
        taskOutput.Errors.ShouldBe(syncOutput.Errors);
    }
}
