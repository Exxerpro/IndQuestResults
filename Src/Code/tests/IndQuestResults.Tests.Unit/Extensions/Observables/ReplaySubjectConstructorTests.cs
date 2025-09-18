using IndQuestResults.Extensions.Observables;

namespace IndQuestResults.Tests.Unit.Extensions.Observables;

public class ReplaySubjectConstructorTests
{
    [Fact]
    public void ReplayResultSubject_ZeroBuffer_Throws()
    {
        Should.Throw<ArgumentException>(() => new ReplayResultSubject<int>(0));
    }

    [Fact]
    public void ReplayResultSubject_NegativeBuffer_Throws()
    {
        Should.Throw<ArgumentException>(() => new ReplayResultSubject<int>(-5));
    }
}






