using System.Text.RegularExpressions;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Neptune.ExternalAPI.Logging;

public class LogHelper(RequestDelegate next)
{
    // NPT-1078: Redact secret-shaped query params before they hit the log sink. The auth
    // token is accepted via ?token= as a PowerBI escape hatch, and request logging that
    // captures the raw QueryString would otherwise persist tokens to logs (and anything
    // downstream — App Insights, log archives, etc.) for every authenticated request.
    // Compile once and use a case-insensitive match so e.g. ?Token= and ?TOKEN= are also
    // caught. The list mirrors common secret param names; expand if new ones are introduced.
    private static readonly Regex SecretQueryParamPattern = new Regex(
        @"(?<=\b(?:token|api[-_]?key|password|secret)=)[^&]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string RedactSecrets(string queryString)
    {
        return SecretQueryParamPattern.Replace(queryString, "***");
    }


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
            LogContext.PushProperty("QueryString", RedactSecrets(request.QueryString.Value));
        }

        LogContext.PushProperty("ContentType", httpContext.Response.ContentType);

        var endpoint = httpContext.GetEndpoint();
        if (endpoint != null)
        {
            LogContext.PushProperty("EndpointName", endpoint.DisplayName);
        }
    }
}
