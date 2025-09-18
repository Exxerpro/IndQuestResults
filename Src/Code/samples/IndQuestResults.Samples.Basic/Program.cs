using IndQuestResults;

namespace IndQuestResults.Samples.Basic;

/// <summary>
/// Basic samples demonstrating IndQuestResults usage patterns.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("IndQuestResults Basic Samples");
        Console.WriteLine("=============================");
        
        // Basic success case
        var successResult = Result<string>.Success("Hello, World!");
        Console.WriteLine($"Success: {successResult.Value}");
        
        // Basic failure case
        var failureResult = Result<string>.WithFailure("Something went wrong");
        Console.WriteLine($"Failure: {failureResult.Error}");
        
        Console.WriteLine("Basic samples completed!");
    }
}