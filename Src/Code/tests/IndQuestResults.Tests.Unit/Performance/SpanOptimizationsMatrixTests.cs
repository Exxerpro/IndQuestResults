using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class SpanOptimizationsMatrixTests
{
    [Theory]
    // count, prefixEmpty, sepEmpty, includeNull
    [InlineData(0, true, true, false)]
    [InlineData(1, true, false, false)]
    [InlineData(1, false, true, true)]
    [InlineData(2, false, false, false)]  // small multi
    [InlineData(16, false, false, false)] // boundary small
    [InlineData(17, false, false, true)]  // boundary large
    [InlineData(32, true, false, false)]  // larger with empty prefix
    [InlineData(33, false, true, false)]  // over boundary, empty sep
    [InlineData(64, false, false, true)]  // much larger with nulls
    public void FormatCollection_Matrix(int count, bool prefixEmpty, bool sepEmpty, bool includeNull)
    {
        var items = BuildItems(count, includeNull);
        var prefix = prefixEmpty ? string.Empty : "P:";
        var sep = sepEmpty ? string.Empty : ", ";

        var s = SpanOptimizations.FormatCollection(items, prefix, sep);
        s.ShouldContain(prefix);

        if (count == 0)
        {
            s.ShouldBe(prefix);
        }
        else
        {
            if (sepEmpty)
            {
                // strings concatenated without separator
                foreach (var it in items)
                    if (it is not null)
                        s.ShouldContain(it);
            }
            else
            {
                // should contain separators between entries
                var meaningful = items.Count(i => !string.IsNullOrEmpty(i));
                if (meaningful > 1)
                    s.ShouldContain(", ");
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(17)]
    public void DeduplicatePreserveOrder_Matrix(int count)
    {
        var items = Enumerable.Range(0, count)
            .Select(i => i % 3 == 0 ? "d" : $"v{i}")
            .ToArray();

        var unique = SpanOptimizations.DeduplicatePreserveOrder(items);
        unique.Distinct().Count().ShouldBe(unique.Length);
        if (count > 0)
        {
            unique[0].ShouldBe("d");
        }
    }

    private static List<string> BuildItems(int count, bool includeNull)
    {
        var list = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            if (includeNull && i % 5 == 0)
            {
                list.Add(null!);
            }
            else
            {
                list.Add($"v{i}");
            }
        }
        return list;
    }
}
