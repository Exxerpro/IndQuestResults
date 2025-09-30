namespace IndQuestResults.Tests.Unit.Validation;

public class NullArgumentErrorBranchTests
{
    [Fact]
    public void Ctor_NullParameterName_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new NullArgumentError(null!));
    }

    [Fact]
    public void Equals_And_GetHashCode_Work()
    {
        var a = new NullArgumentError("p","m");
        var b = new NullArgumentError("p","m");
        var c = new NullArgumentError("q","m");

        a.Equals(b).ShouldBeTrue();
        a.Equals(c).ShouldBeFalse();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}




