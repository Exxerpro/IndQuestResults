namespace IndQuestResults.Tests.V3Minimal.Samples;

public class BasicV3Facts
{
    [Fact]
    public void Fact_passes() => true.ShouldBeTrue();

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, 1, 0)]
    public void Theory_adds(int a, int b, int sum)
    {
        (a + b).ShouldBe(sum);
    }
}
