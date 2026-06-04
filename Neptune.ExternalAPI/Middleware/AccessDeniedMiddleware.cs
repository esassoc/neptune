using System.Text.Json;

namespace Neptune.ExternalAPI.Middleware;

public class AccessDeniedMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Only write the JSON body if no other middleware/endpoint has already started the
        // response. WriteAsync on a started response throws (or corrupts the stream); this
        // guard keeps the middleware safe to layer on top of pipelines that may already
        // return their own 403 body in the future.
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden && !context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json";
            var error = new { message = "Your API key does not have access to this API." };
            var json = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(json);
        }
    }
}
