using System.Text.Json;

namespace Neptune.ExternalAPI.Middleware;

public class AccessDeniedMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            context.Response.ContentType = "application/json";
            var error = new { message = "Your API key does not have access to this API." };
            var json = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(json);
        }
    }
}
