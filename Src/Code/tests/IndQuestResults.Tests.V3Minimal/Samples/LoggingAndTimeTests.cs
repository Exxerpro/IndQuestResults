using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.TimeProvider.Testing;
using Xunit.Abstractions;

namespace IndQuestResults.Tests.V3Minimal.Samples;

public class LoggingAndTimeTests
{
    private readonly ILogger<LoggingAndTimeTests> _logger;

    public LoggingAndTimeTests(ITestOutputHelper output)
    {
        var factory = LoggerFactory.Create(b => b.AddXunit(output));
        _logger = factory.CreateLogger<LoggingAndTimeTests>();
    }

    [Fact]
    public void Logs_information()
    {
        _logger.LogInformation("hello from v3");
        true.ShouldBeTrue();
    }

    [Fact]
    public void Uses_FakeTimeProvider()
    {
        var ftp = new FakeTimeProvider(DateTimeOffset.Parse("2025-01-01T00:00:00Z"));
        ftp.GetUtcNow().ShouldBe(DateTimeOffset.Parse("2025-01-01T00:00:00Z"));
        ftp.Advance(TimeSpan.FromHours(1));
        ftp.GetUtcNow().ShouldBe(DateTimeOffset.Parse("2025-01-01T01:00:00Z"));
    }
}
