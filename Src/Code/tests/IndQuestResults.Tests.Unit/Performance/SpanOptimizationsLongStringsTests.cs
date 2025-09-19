using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class SpanOptimizationsLongStringsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void LongStrings_ForceFallback_EvenWithSmallCounts(int count)
    {
        var longItem = new string('x', SpanOptimizations.MaxStackAllocSize + 10);
        var items = Enumerable.Repeat(longItem, count).ToArray();
        var s = SpanOptimizations.FormatCollection(items, prefix: "P:", separator: ", ");
        s.ShouldStartWith("P:");
        var occurrences = s.Split(longItem, StringSplitOptions.None).Length - 1;
        occurrences.ShouldBe(count);
    }
}

