using ExchangeRate.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IO;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ExchangeRate.Api.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<ExceptionHandlingMiddleware> _loggerMock;
    private readonly IWebHostEnvironment _envMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        _envMock = Substitute.For<IWebHostEnvironment>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn502WithGenericMessage_WhenHttpRequestExceptionThrownInProduction()
    {
        // Arrange
        _envMock.EnvironmentName.Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new HttpRequestException("Sensitive external host: https://internal.dns/api"),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("External API error.", problem.Title);
        Assert.Equal("Failed to retrieve data from the external provider.", problem.Detail);
        Assert.DoesNotContain("internal.dns", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn502WithDetailedMessage_WhenHttpRequestExceptionThrownInDevelopment()
    {
        // Arrange
        _envMock.EnvironmentName.Returns("Development");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new HttpRequestException("Connection refused to Frankfurter"),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadGateway, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("External API error.", problem.Title);
        Assert.Equal("Connection refused to Frankfurter", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenArgumentExceptionThrown()
    {
        // Arrange
        _envMock.EnvironmentName.Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("Date cannot be in the future."),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("Invalid request.", problem.Title);
        Assert.Equal("Date cannot be in the future.", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500WithGenericMessage_WhenUnhandledExceptionThrownInProduction()
    {
        // Arrange
        _envMock.EnvironmentName.Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Fatal database connection string leak"),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("An internal server error occurred.", problem.Title);
        Assert.Equal("An unexpected error occurred. Please try again later.", problem.Detail);
        Assert.DoesNotContain("leak", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500WithMessage_WhenUnhandledExceptionThrownInDevelopment()
    {
        // Arrange
        _envMock.EnvironmentName.Returns("Development");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Database table not found"),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("An internal server error occurred.", problem.Title);
        Assert.Equal("Database table not found", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn408WithProblemDetails_WhenOperationCanceledExceptionThrown()
    {
        // Arrange
        _envMock.EnvironmentName.Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.RequestTimeout, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonOptions);

        Assert.NotNull(problem);
        Assert.Equal("The request timed out.", problem.Title);
        Assert.Equal("The operation was canceled because it exceeded the timeout limit.", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnCompletedTaskWithoutWritingResponse_WhenClientCanceledRequest()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(cts.Token),
            _loggerMock,
            _envMock);

        var context = new DefaultHttpContext();
        context.RequestAborted = cts.Token;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode); // Status code unchanged
        Assert.Equal(0, context.Response.Body.Length); // Nothing written
    }
}