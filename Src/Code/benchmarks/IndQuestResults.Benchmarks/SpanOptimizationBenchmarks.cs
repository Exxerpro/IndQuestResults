using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndQuestResults.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class SpanOptimizationBenchmarks
{
    private string[] _smallErrorArray;
    private string[] _mediumErrorArray;
    private string[] _largeErrorArray;
    private List<string> _smallErrorList;
    private List<string> _mediumErrorList;
    private List<string> _largeErrorList;

    [Params(4, 8, 16, 32, 64, 128)]
    public int ErrorSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Small errors (suitable for Span optimization)
        _smallErrorArray = Enumerable.Range(1, 8).Select(i => $"Err{i}").ToArray();
        _smallErrorList = _smallErrorArray.ToList();

        // Medium errors (edge case for Span vs StringBuilder)
        _mediumErrorArray = Enumerable.Range(1, 16).Select(i => $"Medium error message {i}").ToArray();
        _mediumErrorList = _mediumErrorArray.ToList();

        // Large errors (should use StringBuilder)
        _largeErrorArray = Enumerable.Range(1, 100).Select(i => $"This is a large error message number {i} with additional context").ToArray();
        _largeErrorList = _largeErrorArray.ToList();
    }

    [Benchmark]
    public string FormatWithSpanOptimization()
    {
        var errors = GenerateErrors(ErrorSize);
        return Result.FormatErrorsString(errors, "Operation Failed");
    }

    [Benchmark(Baseline = true)]
    public string FormatWithStringBuilder()
    {
        var errors = GenerateErrors(ErrorSize);
        return FormatUsingStringBuilder(errors, "Operation Failed");
    }

    [Benchmark]
    public string FormatSmallArrayWithSpan()
    {
        return Result.FormatErrorsString(_smallErrorArray, "Failed");
    }

    [Benchmark]
    public string FormatSmallListWithSpan()
    {
        return Result.FormatErrorsString(_smallErrorList, "Failed");
    }

    [Benchmark]
    public string FormatMediumArrayWithSpan()
    {
        return Result.FormatErrorsString(_mediumErrorArray, "Failed");
    }

    [Benchmark]
    public string FormatLargeArrayFallback()
    {
        return Result.FormatErrorsString(_largeErrorArray, "Failed");
    }

    [Benchmark]
    public Result CombineErrorsSmallSpan()
    {
        var primary = new[] { "P1", "P2", "P3", "P4" };
        var secondary = new[] { "S1", "S2", "S3", "S4" };
        return Result.CombineErrors(primary, secondary);
    }

    [Benchmark]
    public Result CombineErrorsMediumSpan()
    {
        return Result.CombineErrors(_smallErrorArray, _mediumErrorArray);
    }

    [Benchmark]
    public Result CombineErrorsLargeFallback()
    {
        return Result.CombineErrors(_mediumErrorArray, _largeErrorArray);
    }

    [Benchmark]
    public string FormatVariableLengthErrors()
    {
        var errors = Enumerable.Range(1, ErrorSize)
            .Select(i => new string('X', i))
            .ToArray();
        return Result.FormatErrorsString(errors, "Variable");
    }

    [Benchmark]
    public Result MultipleSmallCombines()
    {
        Result result = Result.Success();
        
        for (int i = 0; i < 10; i++)
        {
            var errors = new[] { $"E{i}1", $"E{i}2" };
            result = result.Combine(Result.WithFailure(errors));
        }
        
        return result;
    }

    [Benchmark]
    public string FormatEmptyErrors()
    {
        return Result.FormatErrorsString(Array.Empty<string>(), "Empty");
    }

    [Benchmark]
    public string FormatSingleError()
    {
        return Result.FormatErrorsString(new[] { "Single error" }, "Failed");
    }

    [Benchmark]
    public string FormatWithNullsInArray()
    {
        var errors = new string[] { "Error1", null, "Error3", null, "Error5" };
        return Result.FormatErrorsString(errors, "WithNulls");
    }

    [Benchmark]
    public int SpanVsStringBuilderThreshold()
    {
        var count = 0;
        
        // Test different sizes to find optimal threshold
        for (int size = 1; size <= 32; size++)
        {
            var errors = Enumerable.Range(1, size).Select(i => $"E{i}").ToArray();
            var result = Result.FormatErrorsString(errors, "Test");
            count += result.Length;
        }
        
        return count;
    }

    [Benchmark]
    public Result CombineWithVariousSizes()
    {
        var results = new[]
        {
            Result.WithFailure(new[] { "A" }),
            Result.WithFailure(new[] { "B1", "B2" }),
            Result.WithFailure(new[] { "C1", "C2", "C3", "C4" }),
            Result.WithFailure(_smallErrorArray),
            Result.Success()
        };
        
        return results[0].Combine(results.Skip(1).ToArray());
    }

    private string[] GenerateErrors(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"Error message {i}")
            .ToArray();
    }

    private static string FormatUsingStringBuilder(IEnumerable<string> errors, string prefix)
    {
        var sb = new StringBuilder($"{prefix}: ");
        var isFirst = true;
        
        foreach (var error in errors)
        {
            if (!isFirst)
                sb.Append(", ");
            sb.Append(error);
            isFirst = false;
        }
        
        return sb.ToString();
    }
}