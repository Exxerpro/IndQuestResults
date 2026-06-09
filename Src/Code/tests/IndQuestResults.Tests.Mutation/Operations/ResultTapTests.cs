namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultTapTests
{
    [Fact]
    public void Tap_OnSuccess_InvokesAction_AndReturnsSameInstance()
    {
        var invoked = false;
        var r = Result.Success();

        var returned = r.Tap(() => invoked = true);

        invoked.ShouldBeTrue();
        ReferenceEquals(r, returned).ShouldBeTrue();
    }

    [Fact]
    public void Tap_OnFailure_DoesNotInvokeAction_AndReturnsSameInstance()
    {
        var invoked = false;
        var r = Result.WithFailure("err");

        var returned = r.Tap(() => invoked = true);

        invoked.ShouldBeFalse();
        ReferenceEquals(r, returned).ShouldBeTrue();
    }

    [Fact]
    public void Tap_Generic_OnSuccess_WithNullableValue_InvokesAction()
    {
        var received = 0;
        var r = Result<int?>.Success(5);

        var returned = r.Tap(v => received = v ?? -1);

        received.ShouldBe(5);
        returned.IsSuccess.ShouldBeTrue();
        returned.Value.ShouldBe(5);
    }

    [Fact]
    public void Tap_Generic_OnFailure_DoesNotInvokeAction()
    {
        var received = 0;
        var r = Result<int>.WithFailure("err");

        var returned = r.Tap(v => received = v);

        received.ShouldBe(0);
        returned.IsFailure.ShouldBeTrue();
    }
}


