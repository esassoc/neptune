using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Neptune.ExternalAPI.Logging;

public class LogHelper(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        EnrichLogContext(httpContext);
        await next(httpContext);
    }

    public static LogEventLevel CustomGetLevel(HttpContext ctx, double _, Exception ex)
    {
        if (IsIgnoredEndpoint(ctx))
            return LogEventLevel.Debug;

        return ex != null
            ? LogEventLevel.Error
            : ctx.Response.StatusCode > 499
                ? LogEventLevel.Error
                : ctx.Response.StatusCode > 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
    }

    public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        EnrichLogContext(httpContext);
    }

    private static bool IsIgnoredEndpoint(HttpContext ctx)
    {
        var endpoint = ctx.GetEndpoint();
        return endpoint?.Metadata?.GetMetadata<LogIgnoreAttribute>() != null;
    }

    private static void EnrichLogContext(HttpContext httpContext)
    {
        var request = httpContext.Request;

        LogContext.PushProperty("Host", request.Host);
        LogContext.PushProperty("Protocol", request.Protocol);
        LogContext.PushProperty("Scheme", request.Scheme);
        LogContext.PushProperty("ContentLength", request.ContentLength);

        if (request.QueryString.HasValue)
        {
            LogContext.PushProperty("QueryString", request.QueryString.Value);
        }

        LogContext.PushProperty("ContentType", httpContext.Response.ContentType);

        var endpoint = httpContext.GetEndpoint();
        if (endpoint != null)
        {
            LogContext.PushProperty("EndpointName", endpoint.DisplayName);
        }
    }
}
