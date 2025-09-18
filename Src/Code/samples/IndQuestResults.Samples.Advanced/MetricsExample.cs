using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Extensions.Performance;
using Microsoft.Extensions.Logging;

namespace IndQuestResults.Samples.Advanced;

/// <summary>
/// Example of high-performance, non-blocking metrics collection for Result operations.
/// Demonstrates how to maintain 200-300ms response times while collecting comprehensive metrics.
/// </summary>
public class MetricsExample
{
    private readonly ILogger<MetricsExample> _logger;
    
    public MetricsExample(ILogger<MetricsExample> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Example of setting up metrics collection for an API application.
    /// </summary>
    public static void ConfigureMetrics(ILogger logger)
    {
        // Create a metrics processor that logs to your monitoring system
        var processor = new LoggingMetricsProcessor(logger);
        
        // Create a non-blocking collector with a large buffer
        var collector = new ChannelMetricsCollector(processor, capacity: 10000);
        
        // Set as global collector for all Result operations
        ResultMetrics.SetGlobalCollector(collector);
    }

    /// <summary>
    /// Example API endpoint maintaining 200-300ms response time with metrics.
    /// </summary>
    public async Task<Result<UserDto>> GetUserWithMetrics(int userId, CancellationToken ct)
    {
        // Create a scope for this request
        var metrics = ResultMetrics.CreateScope("api.users");
        
        // Validate input - with metrics
        var validationResult = await metrics.TimedAsync(
            () => ValidateUserIdAsync(userId),
            "validate_id",
            ct);
            
        if (!validationResult.IsSuccess)
        {
            return Result<UserDto>.WithFailure(validationResult.Errors);
        }

        // Load user - with metrics  
        var userResult = await metrics.TimedAsync(
            () => LoadUserFromDatabaseAsync(userId, ct),
            "load_user",
            ct);
            
        if (!userResult.IsSuccess)
        {
            return Result<UserDto>.WithFailure(userResult.Errors);
        }

        // Check permissions - with metrics
        var permissionsResult = await metrics.TimedAsync(
            () => CheckUserPermissionsAsync(userResult.Value!, ct),
            "check_permissions",
            ct);
            
        if (!permissionsResult.IsSuccess)
        {
            return Result<UserDto>.WithFailure("Access denied");
        }

        // Transform to DTO - with metrics
        return metrics.Timed(
            () => TransformToDto(userResult.Value!),
            "transform_dto");
    }

    /// <summary>
    /// Example showing direct usage without scopes.
    /// </summary>
    public async Task<Result<OrderSummary>> ProcessOrderWithMetrics(Order order)
    {
        // Direct metrics collection - fire and forget
        return await ResultMetrics.TimedWithMetricsAsync(
            async () =>
            {
                // Validate order
                var validationResult = await ValidateOrderAsync(order);
                if (!validationResult.IsSuccess)
                {
                    return Result<OrderSummary>.WithFailure(validationResult.Errors);
                }

                // Calculate totals
                var totalsResult = CalculateOrderTotals(order);
                if (!totalsResult.IsSuccess)
                {
                    return Result<OrderSummary>.WithFailure(totalsResult.Errors);
                }

                // Create summary
                return Result<OrderSummary>.Success(new OrderSummary
                {
                    OrderId = order.Id,
                    Total = totalsResult.Value,
                    Status = "Processed"
                });
            },
            "order.process");
    }

    /// <summary>
    /// Example of hot path optimization with selective metrics.
    /// </summary>
    public Result<decimal> CalculatePriceHotPath(Product product, int quantity)
    {
        // For ultra-hot paths, you might skip metrics entirely
        // or use sampling (e.g., only measure 1% of calls)
        
        if (ShouldSample()) // Only measure 1% of calls
        {
            return ResultMetrics.TimedWithMetrics(
                () => CalculatePrice(product, quantity),
                "pricing.calculate");
        }
        else
        {
            // Skip metrics for 99% of calls
            return CalculatePrice(product, quantity);
        }
    }

    private static bool ShouldSample()
    {
        // Simple sampling: measure 1% of operations
        return Random.Shared.Next(100) == 0;
    }

    // Simulated operations
    private async Task<Result> ValidateUserIdAsync(int userId)
    {
        await Task.Delay(10); // Simulate validation
        return userId > 0 
            ? Result.Success() 
            : Result.WithFailure("Invalid user ID");
    }

    private async Task<Result<User>> LoadUserFromDatabaseAsync(int userId, CancellationToken ct)
    {
        await Task.Delay(50, ct); // Simulate database query
        return Result<User>.Success(new User { Id = userId, Name = $"User{userId}" });
    }

    private async Task<Result> CheckUserPermissionsAsync(User user, CancellationToken ct)
    {
        await Task.Delay(20, ct); // Simulate permission check
        return Result.Success();
    }

    private Result<UserDto> TransformToDto(User user)
    {
        return Result<UserDto>.Success(new UserDto 
        { 
            Id = user.Id, 
            Name = user.Name 
        });
    }

    private async Task<Result> ValidateOrderAsync(Order order)
    {
        await Task.Delay(15); // Simulate validation
        return order.Items.Count > 0 
            ? Result.Success() 
            : Result.WithFailure("Order has no items");
    }

    private Result<decimal> CalculateOrderTotals(Order order)
    {
        decimal total = 0;
        foreach (var item in order.Items)
        {
            total += item.Price * item.Quantity;
        }
        return Result<decimal>.Success(total);
    }

    private Result<decimal> CalculatePrice(Product product, int quantity)
    {
        if (quantity <= 0)
        {
            return Result<decimal>.WithFailure("Invalid quantity", 0m);
        }
        
        var price = product.BasePrice * quantity;
        if (quantity >= 10)
        {
            price *= 0.9m; // 10% discount
        }
        
        return Result<decimal>.Success(price);
    }
}

/// <summary>
/// Example metrics processor that logs to ILogger.
/// In production, this would write to your APM tool, time-series database, etc.
/// </summary>
public class LoggingMetricsProcessor : IMetricsProcessor
{
    private readonly ILogger _logger;
    
    public LoggingMetricsProcessor(ILogger logger)
    {
        _logger = logger;
    }
    
    public Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken)
    {
        // In production, batch these and send to your metrics system
        // This example just logs them
        
        if (metric.IsException)
        {
            _logger.LogWarning(
                "Operation {OperationName} failed with {ErrorType} in {ElapsedMs:F1}ms",
                metric.OperationName,
                metric.ErrorType,
                metric.ElapsedMilliseconds);
        }
        else if (!metric.IsSuccess)
        {
            _logger.LogInformation(
                "Operation {OperationName} validation failed in {ElapsedMs:F1}ms",
                metric.OperationName,
                metric.ElapsedMilliseconds);
        }
        else if (metric.ElapsedMilliseconds > 100) // Slow operation warning
        {
            _logger.LogWarning(
                "Slow operation {OperationName} succeeded in {ElapsedMs:F1}ms",
                metric.OperationName,
                metric.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "Operation {OperationName} succeeded in {ElapsedMs:F1}ms",
                metric.OperationName,
                metric.ElapsedMilliseconds);
        }
        
        return Task.CompletedTask;
    }
}

// Example DTOs for the samples
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderSummary
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}

public class Product
{
    public decimal BasePrice { get; set; }
}