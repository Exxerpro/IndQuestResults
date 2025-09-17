namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Comprehensive test suite for functional cancellation handling in IndQuestResults operations.
/// <para>
/// Tests the <see cref="CancellationAwareResult"/> utility class and <see cref="ResultExtensions"/>
/// cancellation detection methods, which provide functional programming patterns for handling
/// cancellation without exceptions.
/// </para>
/// <para>
/// <b>KEY BEHAVIORS TESTED:</b>
/// <list type="bullet">
/// <item><description><strong>Functional Error Handling:</strong> All wrapper methods return <c>Result&lt;T&gt;</c>
/// instead of throwing exceptions, enabling pure functional composition</description></item>
/// <item><description><strong>Null Operation Handling:</strong> Null operations return descriptive failure results
/// with detailed error messages rather than throwing <c>ArgumentNullException</c></description></item>
/// <item><description><strong>Graceful Cancellation:</strong> <c>OperationCanceledException</c> is caught and
/// converted to cancelled <c>Result&lt;T&gt;</c> instances with proper error classification</description></item>
/// <item><description><strong>Exception Wrapping:</strong> All other exceptions are caught and wrapped in
/// failure results with "Operation failed: {message}" format</description></item>
/// <item><description><strong>Early Cancellation Detection:</strong> Pre-cancelled tokens are detected before
/// operation execution, returning immediate cancellation results</description></item>
/// <item><description><strong>Timeout Integration:</strong> Timeout operations distinguish between timeout
/// failures and external cancellation requests</description></item>
/// </list>
/// </para>
/// <para>
/// <b>WRAPPER METHODS COVERED:</b>
/// <list type="number">
/// <item><description><c>WrapCancellationAware&lt;T&gt;(Func&lt;CancellationToken, Task&lt;T&gt;&gt;)</c> -
/// Wraps generic async operations with cancellation handling</description></item>
/// <item><description><c>WrapCancellationAware(Func&lt;CancellationToken, Task&gt;)</c> -
/// Wraps non-generic async operations with cancellation handling</description></item>
/// <item><description><c>WrapResultOperation&lt;T&gt;(Func&lt;CancellationToken, Task&lt;Result&lt;T&gt;&gt;&gt;)</c> -
/// Wraps operations that already return Result&lt;T&gt; types</description></item>
/// <item><description><c>WrapWithTimeout&lt;T&gt;(operation, timeout)</c> -
/// Combines cancellation handling with timeout management</description></item>
/// </list>
/// </para>
/// <para>
/// <b>CANCELLATION DETECTION PATTERNS:</b>
/// <list type="bullet">
/// <item><description><c>IsCancelled()</c> extension methods detect cancellation by checking for
/// <c>ResultErrors.OperationCancelled</c> in the error collection</description></item>
/// <item><description>Cancellation results are created using <c>ResultExtensions.Cancelled()</c> and
/// <c>ResultExtensions.Cancelled&lt;T&gt;()</c> factory methods</description></item>
/// <item><description>Mixed error scenarios are supported - results can contain both cancellation
/// and other error types</description></item>
/// </list>
/// </para>
/// -------------------------------------------------------------------------------------------------
/// <para>
/// <b>PRAGMA WARNING SUPPRESSION CONTEXT:</b>
/// </para>
/// <para>
/// This test class uses <c>CancellationTokenSource.Cancel()</c> to simulate pre-cancelled tokens.
/// Although <c>AsyncFixer02</c> and <c>xUnit1051</c> typically flag "long-running or blocking operations
/// inside async methods", this synchronous and deterministic call is explicitly used in unit tests
/// to verify cancellation behavior in a controlled and predictable way.
/// </para>
/// <para>
/// <b>PRAGMA WARNING SUPPRESSION JUSTIFICATION:</b>
/// <list type="number">
/// <item><description><c>cts.Cancel()</c> is neither long-running nor blocking; it completes in microseconds.</description></item>
/// <item><description>Unit tests require deterministic and immediate token cancellation to validate behavior under pre-cancelled conditions.</description></item>
/// <item><description>Delaying cancellation via asynchronous means would reduce test clarity and isolation.</description></item>
/// <item><description>This pattern is an industry-accepted technique for simulating cancellation in xUnit-based tests.</description></item>
/// </list>
/// Suppression scope is kept narrow to prevent unintentional masking of genuine issues elsewhere.
/// </para>
/// -------------------------------------------------------------------------------------------------
/// <code>
/// #pragma warning disable AsyncFixer02 // Long-running or blocking operations inside async methods
/// #pragma warning disable xUnit1051
/// cts.Cancel(); // Synchronous, intentional, test-driven immediate cancellation
/// #pragma warning restore xUnit1051
/// #pragma warning restore AsyncFixer02
/// </code>
/// -------------------------------------------------------------------------------------------------
/// </summary>
public class CancellationHandlingTests
{
/// <summary>
/// Begin Tests Test Constants
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Test constants for cancellation scenarios.
    /// </summary>
    private static class CancellationTestConstants
    {
        public const int ShortDelayMs = 50;
        public const int MediumDelayMs = 200;
        public const int LongDelayMs = 1000;
        public const string TestValue = "Cancellation test value";
        public const string TestError = "Simulated operation error";
    }

/// <summary>
/// End Tests Test Constants
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests ResultExtensions Cancellation Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests the basic cancellation factory methods and predicates.
    /// </summary>
    [Fact]
    public void Cancelled_ShouldCreateFailedResult_WithCancellationError()
    {
        // Act - Test non-generic cancellation
        var result = ResultExtensions.Cancelled();
        var genericResult = ResultExtensions.Cancelled<string>();

        // Assert - Both should be failures with cancellation error
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationCancelled);

        genericResult.IsFailure.ShouldBeTrue();
        genericResult.IsCancelled().ShouldBeTrue();
        genericResult.Errors.ShouldContain(ResultErrors.OperationCancelled);
        genericResult.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests the IsCancelled predicate methods.
    /// </summary>
    [Fact]
    public void IsCancelled_ShouldDetectCancellationCorrectly()
    {
        // Arrange
        var cancelledResult = ResultExtensions.Cancelled();
        var cancelledGenericResult = ResultExtensions.Cancelled<int>();
        var successResult = Result.Success();
        var failureResult = Result.WithFailure("Regular error");
        var successGenericResult = Result<int>.Success(42);
        var failureGenericResult = Result<int>.WithFailure("Regular error");

        // Assert - Only cancelled results should be detected
        cancelledResult.IsCancelled().ShouldBeTrue();
        cancelledGenericResult.IsCancelled().ShouldBeTrue();

        successResult.IsCancelled().ShouldBeFalse();
        failureResult.IsCancelled().ShouldBeFalse();
        successGenericResult.IsCancelled().ShouldBeFalse();
        failureGenericResult.IsCancelled().ShouldBeFalse();
    }

    /// <summary>
    /// Tests cancellation detection with mixed error messages.
    /// </summary>
    [Fact]
    public void IsCancelled_WithMixedErrors_ShouldDetectCancellationCorrectly()
    {
        // Arrange - Result with both cancellation and other errors
        var mixedErrors = new[]
        {
            "Some other error",
            ResultErrors.OperationCancelled,
            "Another error"
        };
        var mixedResult = Result.WithFailure(mixedErrors);
        var mixedGenericResult = Result<string>.WithFailure(mixedErrors, "test");

        // Assert - Should detect cancellation even with other errors
        mixedResult.IsCancelled().ShouldBeTrue();
        mixedGenericResult.IsCancelled().ShouldBeTrue();
    }

/// <summary>
/// End Tests ResultExtensions Cancellation Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests CancellationAwareResult Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that successful async operations are properly wrapped and return success results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapCancellationAware&lt;T&gt;</c> executes the provided operation
    /// and returns <c>Result&lt;T&gt;.Success</c> containing the operation's return value when no
    /// exceptions or cancellation occur.
    /// </para>
    /// <para>
    /// <b>FUNCTIONAL PATTERN:</b> This demonstrates the happy path where async operations are
    /// transparently wrapped without changing their behavior, only adding cancellation awareness.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_WithSuccessfulOperation_ShouldReturnSuccess()
    {
        // Arrange
        var expectedValue = CancellationTestConstants.TestValue;

        // Act
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        var result = await CancellationAwareResult.WrapCancellationAware<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.ShortDelayMs, ct);
                return expectedValue;
            });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Value.ShouldBe(expectedValue);
    }

    /// <summary>
    /// Tests that operations throwing regular exceptions are converted to failure results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapCancellationAware&lt;T&gt;</c> catches non-cancellation exceptions
    /// and converts them to <c>Result&lt;T&gt;.WithFailure</c> with "Operation failed: {exception.Message}" format.
    /// The resulting failure is not classified as cancellation.
    /// </para>
    /// <para>
    /// <b>FUNCTIONAL EXCEPTION HANDLING:</b> This demonstrates how the wrapper maintains functional purity
    /// by preventing exception propagation and converting them to explicit failure states that can be
    /// safely composed with other operations.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_WithException_ShouldReturnFailure()
    {
        // Act
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        var result = await CancellationAwareResult.WrapCancellationAware<string>(
            ct => throw new InvalidOperationException(CancellationTestConstants.TestError));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Errors.ShouldContain($"Operation failed: {CancellationTestConstants.TestError}");
    }

    /// <summary>
    /// Tests that operations cancelled during execution are properly handled and return cancellation results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapCancellationAware&lt;T&gt;</c> catches <c>OperationCanceledException</c> 
    /// thrown by operations and converts them to <c>ResultExtensions.Cancelled&lt;T&gt;()</c> results 
    /// containing the standard cancellation error message.
    /// </para>
    /// <para>
    /// <b>RUNTIME CANCELLATION HANDLING:</b> This demonstrates how the wrapper handles cancellation 
    /// that occurs during operation execution, as opposed to pre-cancelled tokens, ensuring consistent 
    /// cancellation result representation regardless of when cancellation occurs.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_WithCancellationDuringExecution_ShouldReturnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - Start operation and cancel after short delay
        var operationTask = CancellationAwareResult.WrapCancellationAware<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.LongDelayMs, ct);
                return CancellationTestConstants.TestValue;
            },
            cts.Token);

        // Cancel after a short delay
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of

        _ = Task.Delay(CancellationTestConstants.ShortDelayMs).ContinueWith(_ => cts.Cancel());

        var result = await operationTask;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Tests that non-generic async operations are properly wrapped and return success results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> Non-generic <c>WrapCancellationAware</c> executes operations that return 
    /// <c>Task</c> (instead of <c>Task&lt;T&gt;</c>) and returns <c>Result.Success()</c> when no 
    /// exceptions or cancellation occur.
    /// </para>
    /// <para>
    /// <b>NON-GENERIC PATTERN:</b> This demonstrates how the wrapper handles operations that perform 
    /// side effects or state changes without returning values, maintaining the same functional 
    /// cancellation patterns as generic operations.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_NonGeneric_WithSuccessfulOperation_ShouldReturnSuccess()
    {
        // Arrange
        var operationExecuted = false;

        // Act
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        var result = await CancellationAwareResult.WrapCancellationAware(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.ShortDelayMs, ct);
                operationExecuted = true;
            });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        operationExecuted.ShouldBeTrue();
    }

    /// <summary>
    /// Tests wrapping operations that already return <c>Result&lt;T&gt;</c> types.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapResultOperation&lt;T&gt;</c> wraps operations that already return 
    /// <c>Result&lt;T&gt;</c> and preserves their success/failure state while adding cancellation handling.
    /// The original <c>Result&lt;T&gt;</c> is returned unchanged when successful.
    /// </para>
    /// <para>
    /// <b>RESULT PRESERVATION:</b> This demonstrates how the wrapper can handle operations that already 
    /// use the Result pattern, adding cancellation detection without modifying the business logic results.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapResultOperation_WithSuccessfulResult_ShouldReturnOriginalResult()
    {
        // Arrange
        var expectedValue = CancellationTestConstants.TestValue;

        // Act
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        var result = await CancellationAwareResult.WrapResultOperation<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.ShortDelayMs, ct);
                return Result<string>.Success(expectedValue);
            });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Value.ShouldBe(expectedValue);
    }

    /// <summary>
    /// Tests that <c>WrapResultOperation</c> properly handles cancellation for operations returning <c>Result&lt;T&gt;</c>.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapResultOperation&lt;T&gt;</c> catches <c>OperationCanceledException</c> 
    /// thrown by <c>Result&lt;T&gt;</c>-returning operations and converts them to cancellation results, 
    /// overriding any business logic results.
    /// </para>
    /// <para>
    /// <b>CANCELLATION PRECEDENCE:</b> This demonstrates that cancellation takes precedence over 
    /// business logic results - even if the operation would have returned a successful <c>Result&lt;T&gt;</c>, 
    /// cancellation results in a cancelled result state.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapResultOperation_WithCancellation_ShouldReturnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var operationTask = CancellationAwareResult.WrapResultOperation<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.LongDelayMs, ct);
                return Result<string>.Success(CancellationTestConstants.TestValue);
            },
            cts.Token);

        // Cancel after short delay
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        _ = Task.Delay(CancellationTestConstants.ShortDelayMs).ContinueWith(_ => cts.Cancel());

        var result = await operationTask;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeTrue();
    }

/// <summary>
/// End Tests CancellationAwareResult Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Timeout Handling Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that operations completing within timeout limits return success results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapWithTimeout&lt;T&gt;</c> successfully executes operations 
    /// that complete before the specified timeout duration, returning <c>Result&lt;T&gt;.Success</c> 
    /// with the operation's return value.
    /// </para>
    /// <para>
    /// <b>TIMEOUT INTEGRATION:</b> This demonstrates how timeout functionality integrates with 
    /// cancellation handling without interfering with normal operation execution when timing constraints are met.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapWithTimeout_CompletesInTime_ShouldReturnSuccess()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(CancellationTestConstants.MediumDelayMs);
        var expectedValue = CancellationTestConstants.TestValue;

        // Act
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        var result = await CancellationAwareResult.WrapWithTimeout<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.ShortDelayMs, ct);
                return expectedValue;
            },
            timeout);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
        result.IsCancelled().ShouldBeFalse();
    }

    /// <summary>
    /// Tests that operations exceeding timeout limits return timeout error results (not cancellation results).
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapWithTimeout&lt;T&gt;</c> returns <c>Result&lt;T&gt;.WithFailure</c> 
    /// containing <c>ResultErrors.OperationTimedOut</c> when operations exceed the specified timeout duration.
    /// Importantly, timeout failures are distinct from cancellation failures.
    /// </para>
    /// <para>
    /// <b>TIMEOUT VS CANCELLATION:</b> This demonstrates the important distinction between timeout 
    /// failures (due to duration limits) and cancellation failures (due to external cancellation requests), 
    /// enabling different handling strategies for each scenario.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapWithTimeout_ExceedsTimeout_ShouldReturnTimeoutError()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(CancellationTestConstants.ShortDelayMs);

        // Act
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        var result = await CancellationAwareResult.WrapWithTimeout<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.LongDelayMs, ct);
                return CancellationTestConstants.TestValue;
            },
            timeout);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationTimedOut);
        result.IsCancelled().ShouldBeFalse(); // It's a timeout, not cancellation
    }

    /// <summary>
    /// Tests that external cancellation takes precedence over timeout in combined scenarios.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapWithTimeout&lt;T&gt;</c> properly distinguishes between 
    /// timeout-triggered cancellation and external cancellation requests. When external cancellation 
    /// occurs before timeout, the result is classified as cancellation rather than timeout.
    /// </para>
    /// <para>
    /// <b>CANCELLATION PRECEDENCE:</b> This demonstrates how the wrapper prioritizes external 
    /// cancellation signals over internal timeout mechanisms, ensuring proper error classification 
    /// for downstream handling logic.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapWithTimeout_WithExternalCancellation_ShouldReturnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(CancellationTestConstants.LongDelayMs);

        // Act
        var operationTask = CancellationAwareResult.WrapWithTimeout<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.LongDelayMs, ct);
                return CancellationTestConstants.TestValue;
            },
            timeout,
            cts.Token);

        // Cancel externally before timeout
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of

        _ = Task.Delay(CancellationTestConstants.ShortDelayMs).ContinueWith(_ => cts.Cancel());

        var result = await operationTask;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeTrue();
    }

/// <summary>
/// End Tests Timeout Handling Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Functional Programming Pattern Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests the complete functional pattern: operation → cancellation check → result chaining.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> Demonstrates functional composition of cancellation-aware operations 
    /// using <c>Task.ContinueWith</c> to chain multiple async operations while preserving cancellation 
    /// state and enabling early termination when cancellation is detected.
    /// </para>
    /// <para>
    /// <b>FUNCTIONAL COMPOSITION:</b> This test shows how <c>Result&lt;T&gt;</c> instances returned 
    /// by cancellation wrappers can be safely composed using functional patterns, with cancellation 
    /// checks preventing unnecessary operation execution in the chain.
    /// </para>
    /// <para>
    /// <b>EARLY TERMINATION:</b> When cancellation is detected in any step of the chain, 
    /// subsequent operations are skipped and the cancellation result is propagated, demonstrating 
    /// efficient short-circuiting behavior.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task FunctionalCancellationPattern_ShouldChainOperationsCorrectly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var steps = new List<string>();

        // Act - Chain operations functionally
        var result = await CancellationAwareResult.WrapCancellationAware<string>(
                async ct =>
                {
                    await Task.Delay(CancellationTestConstants.ShortDelayMs, ct);
                    return "Step 1 Complete";
                },
                cts.Token)
            .ContinueWith(async task =>
            {
                var firstResult = await task;
                if (firstResult.IsCancelled())
                    return firstResult;

                steps.Add(firstResult.Value!);

                return await CancellationAwareResult.WrapCancellationAware<string>(
                    async ct =>
                    {
                        await Task.Delay(CancellationTestConstants.ShortDelayMs, ct);
                        return "Step 2 Complete";
                    },
                    cts.Token);
            })
            .Unwrap();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Step 2 Complete");
        steps.ShouldContain("Step 1 Complete");
    }

    /// <summary>
    /// Tests semantic assertions as recommended for functional cancellation handling.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> Demonstrates that cancellation detection using <c>IsCancelled()</c> 
    /// extension method provides clear, semantic assertions that are more expressive than exception-based 
    /// testing patterns.
    /// </para>
    /// <para>
    /// <b>SEMANTIC CLARITY:</b> This test showcases how functional cancellation handling enables 
    /// more readable and maintainable test assertions compared to traditional exception-catching patterns. 
    /// The intent is clearly expressed through result state checks rather than exception expectations.
    /// </para>
    /// <para>
    /// <b>FUNCTIONAL TESTING PATTERN:</b> Using <c>result.IsCancelled()</c>, <c>result.IsFailure</c>, 
    /// and <c>result.IsSuccess</c> provides explicit state verification that aligns with functional 
    /// programming principles.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task SemanticAssertions_ShouldExpressIntentClearly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        cts.Cancel();

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<string>(
            ct => Task.FromResult(CancellationTestConstants.TestValue),
            cts.Token);

        // Assert - Semantic assertions over exception checks
        result.IsCancelled().ShouldBeTrue();
        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();

        // This is much clearer than:
        // Should.Throw<OperationCanceledException>(() => ...)
    }

/// <summary>
/// End Tests Functional Programming Pattern Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Edge Cases and Error Scenarios
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Helper function that creates an operation that throws an exception for testing functional exception handling.
    /// Returns a <see cref="Func{CancellationToken, Task}"/> that throws <see cref="InvalidOperationException"/>
    /// to verify that the wrapper converts exceptions into <c>Result&lt;T&gt;</c> failures rather than propagating them.
    /// </summary>
    /// <returns>A function that throws an exception when executed, used to test exception wrapping behavior.</returns>
    private static Func<CancellationToken, Task<string>> CreateOperationThatThrows()
    {
        return ct => throw new InvalidOperationException("Test exception from helper");
    }

    /// <summary>
    /// Helper function that returns null operation for testing null operation handling.
    /// Returns <c>null</c> to verify that wrapper methods handle null operations gracefully
    /// by returning <c>Result.WithFailure</c> instead of throwing <c>ArgumentNullException</c>.
    /// </summary>
    /// <returns><c>null</c> operation reference for testing null handling behavior.</returns>
    private static Func<CancellationToken, Task<string>>? CreateNullOperation()
    {
        return null;
    }

    /// <summary>
    /// Tests that null operation handling returns descriptive failure result instead of throwing exceptions.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapCancellationAware&lt;T&gt;</c> with null operation parameter 
    /// returns <c>Result&lt;T&gt;.WithFailure</c> containing descriptive error message about the null operation.
    /// </para>
    /// <para>
    /// <b>FUNCTIONAL PATTERN:</b> This test verifies the functional programming approach where 
    /// invalid inputs result in failure results rather than exceptions, enabling safe composition.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_WithNullOperation_ShouldReturnFailureResult()
    {
        // Arrange
        var nullOperation = CreateNullOperation();

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<string>(nullOperation!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Contains("Operation was null"));
        result.Errors.ShouldContain(error => error.Contains("operation"));
        result.Errors.ShouldContain(error => error.Contains("Typeof"));
    }

    /// <summary>
    /// Tests that operations throwing exceptions are converted to failure results with exception details.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapCancellationAware&lt;T&gt;</c> catches all non-cancellation exceptions 
    /// and converts them to <c>Result&lt;T&gt;.WithFailure</c> with "Operation failed: {exception.Message}" format.
    /// </para>
    /// <para>
    /// <b>FUNCTIONAL PATTERN:</b> This demonstrates how the wrapper maintains functional purity by 
    /// preventing exception propagation and converting them to explicit failure states.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_WithExceptionThrowingOperation_ShouldReturnFailureResult()
    {
        // Arrange
        var throwingOperation = CreateOperationThatThrows();

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<string>(throwingOperation);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Contains("Operation failed"));
        result.Errors.ShouldContain(error => error.Contains("Test exception from helper"));
    }

    /// <summary>
    /// Tests that <c>WrapResultOperation</c> handles null operations by returning failure results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapResultOperation&lt;T&gt;</c> with null operation parameter 
    /// returns <c>Result&lt;T&gt;.WithFailure</c> instead of throwing <c>ArgumentNullException</c>.
    /// </para>
    /// <para>
    /// <b>CONSISTENCY:</b> This verifies that all wrapper methods in <c>CancellationAwareResult</c> 
    /// follow the same null-handling pattern for consistent functional behavior.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapResultOperation_WithNullOperation_ShouldReturnFailureResult()
    {
        // Arrange
        Func<CancellationToken, Task<Result<string>>>? nullOperation = null;

        // Act
        var result = await CancellationAwareResult.WrapResultOperation<string>(nullOperation!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Contains("Operation was null"));
        result.Errors.ShouldContain(error => error.Contains("operation"));
        result.Errors.ShouldContain(error => error.Contains("Typeof"));
    }

    /// <summary>
    /// Tests that <c>WrapWithTimeout</c> handles null operations by returning failure results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapWithTimeout&lt;T&gt;</c> with null operation parameter 
    /// returns <c>Result&lt;T&gt;.WithFailure</c> before any timeout processing occurs.
    /// </para>
    /// <para>
    /// <b>FAIL-FAST PATTERN:</b> This verifies that input validation occurs before expensive 
    /// timeout and cancellation token setup, providing immediate feedback for invalid inputs.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapWithTimeout_WithNullOperation_ShouldReturnFailureResult()
    {
        // Arrange
        Func<CancellationToken, Task<string>>? nullOperation = null;
        var timeout = TimeSpan.FromMilliseconds(CancellationTestConstants.ShortDelayMs);

        // Act
        var result = await CancellationAwareResult.WrapWithTimeout<string>(nullOperation!, timeout);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Contains("Operation was null"));
        result.Errors.ShouldContain(error => error.Contains("operation"));
        result.Errors.ShouldContain(error => error.Contains("Typeof"));
    }

    /// <summary>
    /// Tests that non-generic <c>WrapCancellationAware</c> handles null operations by returning failure results.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> Non-generic <c>WrapCancellationAware</c> with null operation parameter 
    /// returns <c>Result.WithFailure</c> instead of throwing <c>ArgumentNullException</c>.
    /// </para>
    /// <para>
    /// <b>OVERLOAD CONSISTENCY:</b> This verifies that both generic and non-generic overloads 
    /// of <c>WrapCancellationAware</c> follow identical null-handling patterns.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_NonGeneric_WithNullOperation_ShouldReturnFailureResult()
    {
        // Arrange
        Func<CancellationToken, Task>? nullOperation = null;

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware(nullOperation!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsCancelled().ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Contains("Operation was null"));
        result.Errors.ShouldContain(error => error.Contains("operation"));
        result.Errors.ShouldContain(error => error.Contains("Typeof"));
    }

    /// <summary>
    /// Tests that operations with multiple linked cancellation sources are properly handled.
    /// <para>
    /// <b>BEHAVIOR VERIFIED:</b> <c>WrapCancellationAware&lt;T&gt;</c> properly handles complex cancellation 
    /// scenarios involving multiple linked <c>CancellationTokenSource</c> instances. When any source 
    /// in the linked chain is cancelled, the operation is cancelled and returns a cancellation result.
    /// </para>
    /// <para>
    /// <b>LINKED CANCELLATION:</b> This demonstrates how the wrapper integrates with .NET's linked 
    /// cancellation token pattern, supporting hierarchical cancellation scenarios common in complex 
    /// async workflows where multiple cancellation sources may need to coordinate.
    /// </para>
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task WrapCancellationAware_WithMultipleCancellationSources_ShouldHandleCorrectly()
    {
        // Arrange
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);

        // Act
        var operationTask = CancellationAwareResult.WrapCancellationAware<string>(
            async ct =>
            {
                await Task.Delay(CancellationTestConstants.LongDelayMs, ct);
                return CancellationTestConstants.TestValue;
            },
            combinedCts.Token);

        // Cancel one of the sources
        /// This suppression is safe because:
        /// 1. cts.Cancel() executes in microseconds (not long-running)
        /// 2. It's deterministic and necessary for testing cancellation behavior
        /// 3. Test isolation requires immediate cancellation, not async delays
        /// 4. This is the recommended pattern for testing cancellation in xUnit
        /// This pragma is applied narrowly to suppress the false-positive without affecting global behavior.
        /// Test methods are expected to use immediate cancellation for precise control and verification of
        _ = Task.Delay(CancellationTestConstants.ShortDelayMs).ContinueWith(_ => cts1.Cancel());

        var result = await operationTask;

        // Assert
        result.IsCancelled().ShouldBeTrue();
    }

/// <summary>
/// End Edge Cases and Error Scenarios
/// </summary>
/// <returns></returns>
}