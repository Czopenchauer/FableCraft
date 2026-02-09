using FableCraft.Application;
using FableCraft.Infrastructure;
using FableCraft.Server;
using FableCraft.Server.Middleware;
using FableCraft.ServiceDefaults;

using Microsoft.Extensions.FileProviders;

using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var externalConfigPath = Path.Combine("/app/config", "appsettings.json");
if (File.Exists(externalConfigPath))
{
    builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
}

var externalEnvConfigPath = Path.Combine("/app/config", $"appsettings.{builder.Environment.EnvironmentName}.json");
if (File.Exists(externalEnvConfigPath))
{
    builder.Configuration.AddJsonFile(externalEnvConfigPath, optional: true, reloadOnChange: true);
}

builder.AddServiceDefaults();

#pragma warning disable EXTEXP0001
builder.Services
    .AddInfrastructureServices(builder.Configuration)
#pragma warning restore EXTEXP0001
    .AddApplicationServices(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<FileUploadOperationFilter>();
});

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "Handled {RequestPath}";

    // Emit debug-level events instead of the defaults
    options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;

    // Attach additional properties to the request completion event
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value!);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
    };
});

app.UseGlobalExceptionHandling();

app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.UseStaticFiles();

// Serve visualization files if path is configured
var visualizationPath = app.Configuration["VisualizationPath"];
if (!string.IsNullOrEmpty(visualizationPath))
{
    var logger = app.Services.GetRequiredService<Serilog.ILogger>();

    // Resolve relative paths based on content root
    if (!Path.IsPathRooted(visualizationPath))
    {
        visualizationPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, visualizationPath));
    }

    if (Directory.Exists(visualizationPath))
    {
        logger.Information("Serving visualization files from: {Path}", visualizationPath);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(visualizationPath),
            RequestPath = "/visualization"
        });
    }
    else
    {
        logger.Warning("Configured visualization path does not exist: {Path}", visualizationPath);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<AdventureContextMiddleware>();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();