using ExchangeRate.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Api.Tests;

public class CorrelationIdMiddlewareTests
{
    private readonly ILogger<CorrelationIdMiddleware> _loggerMock;

    public CorrelationIdMiddlewareTests()
    {
        _loggerMock = Substitute.For<ILogger<CorrelationIdMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseIncomingCorrelationId_WhenHeaderIsProvided()
    {
        // Arrange
        const string customCorrelationId = "custom-test-correlation-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = customCorrelationId;

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(customCorrelationId, context.TraceIdentifier);
        Assert.Equal(customCorrelationId, context.Items["CorrelationId"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateNewCorrelationId_WhenHeaderIsMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.NotNull(context.TraceIdentifier);
        Assert.NotEmpty(context.TraceIdentifier);
        Assert.True(Guid.TryParse(context.TraceIdentifier, out _));
        Assert.Equal(context.TraceIdentifier, context.Items["CorrelationId"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateNewCorrelationId_WhenHeaderIsWhitespace()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = "   ";

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.NotNull(context.TraceIdentifier);
        Assert.True(Guid.TryParse(context.TraceIdentifier, out _));
    }
}
