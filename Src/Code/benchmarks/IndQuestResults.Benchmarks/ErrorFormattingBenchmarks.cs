using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndQuestResults.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class ErrorFormattingBenchmarks
{
    private string[] _smallErrorSet;
    private string[] _mediumErrorSet;
    private string[] _largeErrorSet;
    private List<string> _smallErrorList;
    private List<string> _mediumErrorList;
    private List<string> _largeErrorList;
    private const string TestPrefix = "Operation Failed";

    [Params(1, 5, 10, 16, 32, 100)]
    public int ErrorCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Setup error arrays
        _smallErrorSet = Enumerable.Range(1, 5).Select(i => $"Error message {i}").ToArray();
        _mediumErrorSet = Enumerable.Range(1, 16).Select(i => $"Medium length error message number {i} with additional context").ToArray();
        _largeErrorSet = Enumerable.Range(1, 100).Select(i => $"This is a much longer error message {i} that contains detailed information about what went wrong in the operation").ToArray();
        
        // Setup error lists
        _smallErrorList = _smallErrorSet.ToList();
        _mediumErrorList = _mediumErrorSet.ToList();
        _largeErrorList = _largeErrorSet.ToList();
    }

    [Benchmark]
    public string FormatErrorsString_Array()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"Error {i}").ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    [Benchmark]
    public string FormatErrorsString_List()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"Error {i}").ToList();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    [Benchmark]
    public string FormatErrorsString_Enumerable()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"Error {i}");
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    [Benchmark]
    public string ToString_SingleError()
    {
        var result = Result.WithFailure("Single error message");
        return result.ToString();
    }

    [Benchmark]
    public string ToString_SmallErrorSet()
    {
        var result = Result.WithFailure(_smallErrorSet);
        return result.ToString();
    }

    [Benchmark]
    public string ToString_MediumErrorSet()
    {
        var result = Result.WithFailure(_mediumErrorSet);
        return result.ToString();
    }

    [Benchmark]
    public string ToString_LargeErrorSet()
    {
        var result = Result.WithFailure(_largeErrorSet);
        return result.ToString();
    }

    [Benchmark]
    public Result CombineErrors_SmallCollections()
    {
        var primary = new[] { "Primary 1", "Primary 2" };
        var secondary = new[] { "Secondary 1", "Secondary 2" };
        return Result.CombineErrors(primary, secondary);
    }

    [Benchmark]
    public Result CombineErrors_MediumCollections()
    {
        return Result.CombineErrors(_smallErrorSet, _mediumErrorSet);
    }

    [Benchmark]
    public Result CombineErrors_LargeCollections()
    {
        return Result.CombineErrors(_mediumErrorSet, _largeErrorSet);
    }

    [Benchmark]
    public Result CombineErrors_OneNull()
    {
        return Result.CombineErrors(_smallErrorSet, null);
    }

    [Benchmark]
    public Result CombineErrors_BothNull()
    {
        return Result.CombineErrors(null, null);
    }

    [Benchmark]
    public Result CombineErrors_EmptyCollections()
    {
        return Result.CombineErrors(Array.Empty<string>(), Array.Empty<string>());
    }

    [Benchmark]
    public string FormatShortMessages()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"E{i}").ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    [Benchmark]
    public string FormatLongMessages()
    {
        var errors = Enumerable.Range(1, ErrorCount)
            .Select(i => new string('X', 50) + $" Error {i} " + new string('Y', 50))
            .ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    [Benchmark]
    public string FormatWithNullErrors()
    {
        var errors = Enumerable.Range(1, ErrorCount)
            .Select(i => i % 3 == 0 ? null : $"Error {i}")
            .ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    [Benchmark]
    public Result MultipleFailureCreation()
    {
        var errors = new List<string>(ErrorCount);
        for (int i = 0; i < ErrorCount; i++)
        {
            errors.Add($"Dynamic error {i} generated during operation");
        }
        return Result.WithFailure(errors);
    }

    [Benchmark]
    public string ComplexErrorFormatting()
    {
        var result = Result.WithFailure(_smallErrorSet)
            .Combine(
                Result.WithFailure(_mediumErrorSet),
                Result.WithFailure("Additional error")
            );
        return result.ToString();
    }

    [Benchmark]
    public int ErrorEnumerationPerformance()
    {
        var result = Result.WithFailure(_largeErrorSet);
        var count = 0;
        foreach (var error in result.Errors)
        {
            count += error.Length;
        }
        return count;
    }
}