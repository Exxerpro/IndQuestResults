using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace IndQuestResults.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("IndQuestResults Benchmarks");
        Console.WriteLine("=========================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Select a benchmark to run:");
            Console.WriteLine("1. Comparison Benchmarks");
            Console.WriteLine("2. Concurrency Benchmarks");
            Console.WriteLine("3. Error Formatting Benchmarks");
            Console.WriteLine("4. Fluent API Performance Benchmarks");
            Console.WriteLine("5. Result Creation Benchmarks");
            Console.WriteLine("6. Span Optimization Benchmarks");
            Console.WriteLine("7. Memory Allocation Benchmarks");
            Console.WriteLine("8. All Benchmarks");
            Console.WriteLine();
            Console.Write("Enter your choice (1-8): ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    BenchmarkRunner.Run<ComparisonBenchmarks>();
                    break;
                case "2":
                    BenchmarkRunner.Run<ConcurrencyBenchmarks>();
                    break;
                case "3":
                    BenchmarkRunner.Run<ErrorFormattingBenchmarks>();
                    break;
                case "4":
                    BenchmarkRunner.Run<FluentApiPerformanceBenchmarks>();
                    break;
                case "5":
                    BenchmarkRunner.Run<ResultCreationBenchmarks>();
                    break;
                case "6":
                    BenchmarkRunner.Run<SpanOptimizationBenchmarks>();
                    break;
                case "7":
                    BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
                    break;
                case "8":
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
                case "comparison":
                    BenchmarkRunner.Run<ComparisonBenchmarks>();
                    break;
                case "concurrency":
                    BenchmarkRunner.Run<ConcurrencyBenchmarks>();
                    break;
                case "errorformatting":
                    BenchmarkRunner.Run<ErrorFormattingBenchmarks>();
                    break;
                case "fluentapi":
                    BenchmarkRunner.Run<FluentApiPerformanceBenchmarks>();
                    break;
                case "creation":
                    BenchmarkRunner.Run<ResultCreationBenchmarks>();
                    break;
                case "span":
                    BenchmarkRunner.Run<SpanOptimizationBenchmarks>();
                    break;
                case "memory":
                    BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
                    break;
                case "all":
                    RunAllBenchmarks();
                    break;
                default:
                    Console.WriteLine($"Unknown benchmark type: {benchmarkType}");
                    Console.WriteLine("Valid options: comparison, concurrency, errorformatting, fluentapi, creation, span, memory, all");
                    Environment.Exit(1);
                    break;
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("Benchmarks completed. Check the BenchmarkDotNet.Artifacts folder for detailed results.");
    }
    
    private static void RunAllBenchmarks()
    {
        var benchmarkTypes = new[]
        {
            typeof(ComparisonBenchmarks),
            typeof(ConcurrencyBenchmarks),
            typeof(ErrorFormattingBenchmarks),
            typeof(FluentApiPerformanceBenchmarks),
            typeof(ResultCreationBenchmarks),
            typeof(SpanOptimizationBenchmarks),
            typeof(MemoryAllocationBenchmarks)
        };
        
        foreach (var benchmarkType in benchmarkTypes)
        {
            Console.WriteLine($"\nRunning {benchmarkType.Name}...");
            Console.WriteLine(new string('-', 50));
            BenchmarkRunner.Run(benchmarkType);
        }
    }
}