using IndQuestResults;

namespace IndQuestResults.Samples.Advanced;

/// <summary>
/// Example of high-performance, non-blocking metrics collection for Result operations.
/// Demonstrates how to maintain 200-300ms response times while collecting comprehensive metrics.
/// </summary>
public class MetricsExample
{
    private readonly ILogger<MetricsExample> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsExample"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit diagnostic information during samples.</param>
    public MetricsExample(ILogger<MetricsExample> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Example of setting up metrics collection for an API application.
    /// </summary>
    /// <param name="logger">Logger used by the sample <see cref="LoggingMetricsProcessor"/>.</param>
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
    /// <param name="userId">The user identifier to load.</param>
    /// <param name="ct">Cancellation token for the asynchronous operations.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="UserDto"/> or errors.</returns>
    public async Task<Result<UserDto>> GetUserWithMetrics(int userId, CancellationToken ct)
    {
        _logger.LogInformation("Getting user {UserId}", userId);
        
        // Create a scope for this request
        var metrics = ResultMetrics.CreateScope("api.users");

        // Validate input
        var validationResult = await ValidateUserIdAsync(userId);

        if (!validationResult.IsSuccess)
        {
            return Result<UserDto>.WithFailure(validationResult.Errors);
        }

        // Load user - with metrics
        var userResult = await metrics.TimedAsync<User>(
            async () => await LoadUserFromDatabaseAsync(userId, ct),
            "load_user",
            ct);

        if (userResult.IsFailure)
        {
            return Result<UserDto>.WithFailure(userResult.Errors);
        }

        if (userResult.Value is not null)
        {
            // Check permissions  
            var permissionsResult = await CheckUserPermissionsAsync(userResult.Value!, ct);

            if (!permissionsResult.IsSuccess)
            {
                return Result<UserDto>.WithFailure("Access denied");
            }

            if (userResult.Value is not null)
            {
                // Transform to DTO - with metrics
                return metrics.Timed(
                    () => TransformToDto(userResult.Value),
                    "transform_dto");
            }
        }

        return Result<UserDto>.WithFailure("User not found");
    }

    /// <summary>
    /// Example showing direct usage without scopes.
    /// </summary>
    /// <param name="order">Order to validate and process.</param>
    /// <returns>A <see cref="Result{T}"/> with the computed <see cref="OrderSummary"/> or errors.</returns>
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
    /// <param name="product">The product to price.</param>
    /// <param name="quantity">The requested quantity.</param>
    /// <returns>A <see cref="Result{T}"/> with the calculated price or errors when invalid.</returns>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMetricsProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit metric entries.</param>
    public LoggingMetricsProcessor(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes a metric entry asynchronously.
    /// </summary>
    /// <param name="metric">The metric entry to process.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
/// <summary>
/// Represents a domain user in the sample.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string Name { get; set; } = "";
}

/// <summary>
/// Represents a data transfer object for <see cref="User"/>.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string Name { get; set; } = "";
}

/// <summary>
/// Represents an order in the sample domain.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the collection of items included in the order.
    /// </summary>
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// Represents an individual item within an order.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the unit price of the item.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the quantity of the item.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Represents a summary of an order total and status.
/// </summary>
public class OrderSummary
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the total amount for the order.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Gets or sets the processing status of the order.
    /// </summary>
    public string Status { get; set; } = "";
}

/// <summary>
/// Represents a product that can be priced.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the unit base price of the product.
    /// </summary>
    public decimal BasePrice { get; set; }
}
