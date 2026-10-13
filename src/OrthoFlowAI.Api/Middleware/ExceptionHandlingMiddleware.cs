using System.Net;
using System.Text.Json;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ArgumentException argEx =>
                (StatusCodes.Status400BadRequest, argEx.Message, new List<string> { argEx.Message }),

            KeyNotFoundException keyEx =>
                (StatusCodes.Status404NotFound, "Resource not found", new List<string> { keyEx.Message }),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", new List<string> { "You are not authorized to perform this action" }),

            InvalidOperationException opEx =>
                (StatusCodes.Status409Conflict, opEx.Message, new List<string> { opEx.Message }),

            TimeoutException =>
                (StatusCodes.Status408RequestTimeout, "Request timeout", new List<string> { "The request timed out. Please try again." }),

            NotImplementedException =>
                (StatusCodes.Status501NotImplemented, "Not implemented", new List<string> { "This feature is not yet implemented" }),

            HttpRequestException httpEx =>
                (StatusCodes.Status502BadGateway, "External service error", new List<string> { "An error occurred while communicating with an external service" }),

            TaskCanceledException =>
                (StatusCodes.Status408RequestTimeout, "Request timeout", new List<string> { "The request was cancelled due to a timeout." }),

            FormatException =>
                (StatusCodes.Status400BadRequest, "Invalid format", new List<string> { "The provided data is in an invalid format" }),

            OverflowException =>
                (StatusCodes.Status400BadRequest, "Value out of range", new List<string> { "A numeric value is out of the acceptable range" }),

            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred", new List<string> { "An internal server error occurred. Please try again later." })
        };

        context.Response.StatusCode = statusCode;

        var response = ApiResponse.FailResponse(message, errors);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
