using IndQuestResults.Tests.Performance.Benchmarks;

namespace IndQuestResults.Tests.Performance;

/// <summary>
/// Main program for running performance tests that validate the claims made in the documentation.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the performance test application that validates IndQuestResults performance claims.
    /// </summary>
    /// <param name="args">Command line arguments to specify which benchmarks to run.</param>
    public static void Main(string[] args)
    {
        Console.WriteLine("IndQuestResults Performance Tests");
        Console.WriteLine("=================================");
        Console.WriteLine("These tests validate the performance claims made in the documentation:");
        Console.WriteLine("- 70% reduction in LINQ allocations for error combining");
        Console.WriteLine("- 50% faster string formatting for error messages");
        Console.WriteLine("- 40% less memory pressure in high-throughput scenarios");
        Console.WriteLine("- Zero allocations for successful operations without errors");
        Console.WriteLine("- Thread-safe immutable design performance");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Select performance tests to run:");
            Console.WriteLine("1. Result Creation Benchmarks");
            Console.WriteLine("2. Error Formatting Benchmarks");
            Console.WriteLine("3. Fluent Chain Benchmarks");
            Console.WriteLine("4. Span Optimization Benchmarks");
            Console.WriteLine("5. Concurrency Benchmarks");
            Console.WriteLine("6. All Benchmarks");
            Console.WriteLine();
            Console.Write("Enter your choice (1-6): ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    RunResultCreationBenchmarks();
                    break;
                case "2":
                    RunErrorFormattingBenchmarks();
                    break;
                case "3":
                    RunFluentChainBenchmarks();
                    break;
                case "4":
                    RunSpanOptimizationBenchmarks();
                    break;
                case "5":
                    RunConcurrencyBenchmarks();
                    break;
                case "6":
                    RunAllBenchmarks();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running all benchmarks...");
                    RunAllBenchmarks();
                    break;
            }
        }
        else
        {
            // Support command line arguments for CI/CD
            var benchmarkType = args[0].ToLower();
            
            switch (benchmarkType)
            {
                case "creation":
                    RunResultCreationBenchmarks();
                    break;
                case "formatting":
                    RunErrorFormattingBenchmarks();
                    break;
                case "fluent":
                    RunFluentChainBenchmarks();
                    break;
                case "span":
                    RunSpanOptimizationBenchmarks();
                    break;
                case "concurrency":
                    RunConcurrencyBenchmarks();
                    break;
                case "all":
                    RunAllBenchmarks();
                    break;
                default:
                    Console.WriteLine($"Unknown benchmark type: {benchmarkType}");
                    Console.WriteLine("Valid options: creation, formatting, fluent, span, concurrency, all");
                    Environment.Exit(1);
                    break;
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Performance tests completed!");
        Console.WriteLine();
        Console.WriteLine("Key Findings Summary:");
        Console.WriteLine("- Results should be significantly faster than exception-based error handling");
        Console.WriteLine("- Span optimizations should show 50%+ improvement over StringBuilder");
        Console.WriteLine("- Thread-safe operations should scale well with processor count");
        Console.WriteLine("- Memory allocations should be minimal for successful operations");
        Console.WriteLine("- Async operations should maintain good performance characteristics");
    }
    
    private static void RunAllBenchmarks()
    {
        var benchmarks = new Action[]
        {
            RunResultCreationBenchmarks,
            RunErrorFormattingBenchmarks,
            RunFluentChainBenchmarks,
            RunSpanOptimizationBenchmarks,
            RunConcurrencyBenchmarks
        };
        
        foreach (var benchmark in benchmarks)
        {
            try
            {
                benchmark();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running benchmark: {ex.Message}");
                Console.WriteLine();
            }
        }
    }

    private static void RunResultCreationBenchmarks()
    {
        try
        {
            ResultCreationBenchmarks.RunAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Result Creation Benchmarks: {ex.Message}");
        }
    }

    private static void RunErrorFormattingBenchmarks()
    {
        try
        {
            ErrorFormattingBenchmarks.RunAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Error Formatting Benchmarks: {ex.Message}");
        }
    }

    private static void RunFluentChainBenchmarks()
    {
        try
        {
            FluentChainBenchmarks.RunAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Fluent Chain Benchmarks: {ex.Message}");
        }
    }

    private static void RunSpanOptimizationBenchmarks()
    {
        try
        {
            SpanOptimizationBenchmarks.RunAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Span Optimization Benchmarks: {ex.Message}");
        }
    }

    private static void RunConcurrencyBenchmarks()
    {
        try
        {
            ConcurrencyBenchmarks.RunAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Concurrency Benchmarks: {ex.Message}");
        }
    }
}