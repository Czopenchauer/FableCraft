using FableCraft.Infrastructure;

namespace FableCraft.Server.Middleware;

public class AdventureContextMiddleware
{
    private readonly RequestDelegate _next;

    public AdventureContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("adventureId", out var adventureIdValue)
            && Guid.TryParse(adventureIdValue?.ToString(), out var adventureId))
        {
            ProcessExecutionContext.AdventureId.Value = adventureId;
        }

        if (context.Request.RouteValues.TryGetValue("sceneId", out var sceneIdValue)
            && Guid.TryParse(sceneIdValue?.ToString(), out var sceneId))
        {
            ProcessExecutionContext.SceneId.Value = sceneId;
        }

        await _next(context);
    }
}