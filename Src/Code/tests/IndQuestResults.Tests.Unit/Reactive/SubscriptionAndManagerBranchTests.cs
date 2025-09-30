namespace IndQuestResults.Tests.Unit.Reactive;

public class SubscriptionAndManagerBranchTests
{
    [Fact]
    public void Subscription_Dispose_Idempotent_RemovesObserver()
    {
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var received = new List<int>();
        var sub = subject.Subscribe(received.Add);

        subject.OnNext(Result<int>.Success(1));
        received.Count.ShouldBe(1);

        sub.Dispose();
        sub.Dispose(); // idempotent

        subject.OnNext(Result<int>.Success(2));
        received.Count.ShouldBe(1);
    }

    [Fact]
    public void SubscriptionManager_Add_Remove_Clear()
    {
        using var manager = ResultSubscriptionsCore.CreateSubscriptionManager();
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var received = new List<int>();
        var s1 = subject.Subscribe(received.Add);
        var s2 = subject.Subscribe(received.Add);

        manager.Add(s1);
        manager.Add(s2);
        manager.Count.ShouldBe(2);

        manager.Remove(s1).ShouldBeTrue();
        manager.Count.ShouldBe(2); // ConcurrentBag count unchanged; removal is dispose-only

        manager.Clear();
        subject.OnNext(Result<int>.Success(3));
        received.ShouldBeEmpty();
    }
}




