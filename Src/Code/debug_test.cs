using IndQuestResults;
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public void TestMethod()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var results = numbers.Select(n => Result<int>.Success(n)).ToList(); // Should trigger IQR104
        ProcessResults(results);
    }

    private void ProcessResults(List<Result<int>> results) { }
}