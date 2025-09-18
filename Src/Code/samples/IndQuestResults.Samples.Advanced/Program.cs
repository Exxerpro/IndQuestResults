using IndQuestResults;
using IndQuestResults.Extensions.Async;
using IndQuestResults.Extensions.Collections;
using IndQuestResults.Extensions.Functional;

namespace IndQuestResults.Samples.Advanced;

/// <summary>
/// Advanced samples demonstrating IndQuestResults extension capabilities.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("IndQuestResults Advanced Samples");
        Console.WriteLine("================================");
        
        // Async chain example
        var asyncResult = await Task.FromResult(Result<int>.Success(42))
            .MapAsync(async x => 
            {
                await Task.Delay(10);
                return x * 2;
            });
            
        Console.WriteLine($"Async result: {asyncResult.Value}");
        
        // Collection example
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var collectionResult = numbers.TraverseResults(x => 
            x % 2 == 0 ? Result<int>.Success(x * 2) : Result<int>.WithFailure($"Odd number: {x}"));
            
        if (collectionResult.IsSuccess)
        {
            Console.WriteLine($"Collection success: {string.Join(", ", collectionResult.Value)}");
        }
        else
        {
            Console.WriteLine($"Collection failure: {collectionResult.Error}");
        }
        
        Console.WriteLine("Advanced samples completed!");
    }
}