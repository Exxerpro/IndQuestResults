using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class SpanOptimizationsTheoryTests
{
    [Theory]
    [InlineData("", ", ")] // empty prefix
    [InlineData("P:", "")] // empty separator
    [InlineData("P:", ";")] // single-char separator
    [InlineData("PRE:", ", ")] // normal
    public void FormatCollection_Small_WithVariousPrefixAndSeparator(string prefix, string separator)
    {
        var items = new[] { "a", null!, "b" };
        var s = SpanOptimizations.FormatCollection(items, prefix, separator);
        s.ShouldContain(prefix);
        s.ShouldContain("a");
        s.ShouldContain("b");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)] // boundary
    public void FormatCollection_SmallCount_UsesSpanPath(int count)
    {
        var items = Enumerable.Range(1, count).Select(i => i.ToString()).ToArray();
        var s = SpanOptimizations.FormatCollection(items, "X:", ",");
        s.ShouldStartWith("X:");
        if (count > 0)
        {
            s.Split(',', StringSplitOptions.None).Length.ShouldBe(count);
        }
        else
        {
            s.ShouldBe("X:");
        }
    }

    [Theory]
    [InlineData(17)]
    [InlineData(40)]
    public void FormatCollection_LargeCount_UsesFallback(int count)
    {
        var items = Enumerable.Range(1, count).Select(i => i.ToString()).ToArray();
        var s = SpanOptimizations.FormatCollection(items, "L:", ", ");
        s.ShouldStartWith("L:");
        s.Split(", ").Length.ShouldBe(count);
    }
}

