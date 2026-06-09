namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultGenericTapTests
{
    [Fact]
    public void Tap_Generic_OnSuccess_NonNullable_InvokesAction()
    {
        var received = 0;
        var r = Result<int>.Success(10);

        var returned = r.Tap(v => received = v + 1);

        received.ShouldBe(11);
        returned.IsSuccess.ShouldBeTrue();
        returned.Value.ShouldBe(10);
    }

    [Fact]
    public void Tap_Generic_OnSuccess_NullableNull_StillInvokes()
    {
        var invoked = false;
        var r = Result<string?>.Success(null);

        var returned = r.Tap(v => invoked = true);

        invoked.ShouldBeTrue();
        // Library allows OnSuccess/Tap invocation when T is nullable, regardless of null value
        (returned.IsSuccess || returned.IsSuccessMayBeNull).ShouldBeTrue();
    }
}


