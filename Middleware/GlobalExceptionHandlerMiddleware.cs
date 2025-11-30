using System.Net;
using System.Text.Json;

namespace ECommerceApp.RyanW84.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var (statusCode, message) = MapExceptionToResponse(exception);

        var response = new
        {
            error = new
            {
                message,
                type = exception.GetType().Name,
                timestamp = DateTime.UtcNow,
            },
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static (int StatusCode, string Message) MapExceptionToResponse(Exception exception) =>
        exception switch
        {
            ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                "Required parameter is missing."
            ),
            ArgumentOutOfRangeException => (
                (int)HttpStatusCode.BadRequest,
                "Parameter value is out of acceptable range."
            ),
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Invalid argument provided."),
            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "The requested resource was not found."
            ),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Access is denied."),
            InvalidOperationException => (
                (int)HttpStatusCode.BadRequest,
                "The operation is not valid in the current state."
            ),
            NotSupportedException => (
                (int)HttpStatusCode.BadRequest,
                "The requested operation is not supported."
            ),
            TimeoutException => ((int)HttpStatusCode.RequestTimeout, "The operation timed out."),
            OperationCanceledException => (
                (int)HttpStatusCode.RequestTimeout,
                "The operation was cancelled."
            ),
            FormatException => ((int)HttpStatusCode.BadRequest, "Invalid data format."),
            OverflowException => (
                (int)HttpStatusCode.BadRequest,
                "Numeric value is too large or too small."
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            ),
        };
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
