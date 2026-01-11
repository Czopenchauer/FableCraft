using System.Net;
using System.Text.Json;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Middleware;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.Error(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var statusCode = exception switch
                         {
                             ArgumentNullException => HttpStatusCode.BadRequest,
                             ArgumentException => HttpStatusCode.BadRequest,
                             UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                             KeyNotFoundException => HttpStatusCode.NotFound,
                             InvalidOperationException => HttpStatusCode.BadRequest,
                             NotImplementedException => HttpStatusCode.NotImplemented,
                             _ => HttpStatusCode.InternalServerError
                         };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            StatusCode = (int)statusCode,
            Message = GetUserFriendlyMessage(statusCode),
            Details = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true
                ? exception.Message
                : null
        };

        var jsonResponse = JsonSerializer.Serialize(response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetUserFriendlyMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
               {
                   HttpStatusCode.BadRequest => "The request was invalid.",
                   HttpStatusCode.Unauthorized => "You are not authorized to access this resource.",
                   HttpStatusCode.NotFound => "The requested resource was not found.",
                   HttpStatusCode.NotImplemented => "This feature is not implemented.",
                   HttpStatusCode.InternalServerError => "An internal server error occurred.",
                   _ => "An error occurred while processing your request."
               };
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) => app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
}