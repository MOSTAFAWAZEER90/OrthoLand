using System.Diagnostics;

namespace OrthoFlowAI.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = context.TraceIdentifier;
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
            : "anonymous";
        var clinicId = context.Items["ClinicId"]?.ToString() ?? "none";
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();
        var queryString = context.Request.QueryString.Value;
        var fullPath = string.IsNullOrEmpty(queryString) ? path : $"{path}{queryString}";

        var requestSize = context.Request.ContentLength ?? 0;

        _logger.LogInformation(
            "[AUDIT] {TraceId} | {Method} {Path} | User: {UserId} | Tenant: {ClinicId} | IP: {ClientIp} | UA: {UserAgent} | RequestSize: {RequestSize} bytes",
            traceId, method, fullPath, userId, clinicId, clientIp, userAgent, requestSize);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var responseBodySize = context.Response.ContentLength;

            var logLevel = statusCode switch
            {
                >= 400 and < 500 => LogLevel.Warning,
                >= 500 => LogLevel.Error,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel,
                "[AUDIT] {TraceId} | {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | User: {UserId} | Tenant: {ClinicId} | ResponseSize: {ResponseSize} bytes",
                traceId, method, fullPath, statusCode, stopwatch.ElapsedMilliseconds, userId, clinicId, responseBodySize);
        }
    }
}
