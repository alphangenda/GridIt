using System.Text.Json;
using Application;
using Domain.Common;
using Domain.Extensions;
using FastEndpoints;
using FastEndpoints.Swagger;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Persistence;
using Serilog;
using Web.Extensions;

// #region agent log
var _debugLogPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", ".cursor", "debug.log"));
void _agentLog(string location, string message, object? data, string hypothesisId)
{
    try
    {
        var dir = Path.GetDirectoryName(_debugLogPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var line = JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location, message, data, runId = "run1", hypothesisId }) + "\n";
        File.AppendAllText(Path.GetFullPath(_debugLogPath), line);
    }
    catch { /* no-op */ }
}
_agentLog("Program.cs:start", "startup", new { processId = Environment.ProcessId }, "A");
// #endregion

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddSignalR();
builder.Configuration.AddJsonFile("appsettings.local.json", true);
builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(x =>
    {
        x.ExcludeNonFastEndpoints = true;
        x.ShortSchemaNames = true;
    });

// Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .Filter.ByExcluding(x =>
    {
        if (x.Exception?.GetType().Name == null)
            return false;
        var handledErrors = builder.Configuration.GetSection("HandledErrors").Get<List<string>>();
        return handledErrors!.Contains(x.Exception.GetType().Name);
    })
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "corsDomains",
        policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("CorsDomains")
                    .GetChildren()
                    .Select(c => c.Value)
                    .ToArray()!)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


var app = builder.Build();
// #region agent log
var _urls = builder.Configuration["ASPNETCORE_URLS"] ?? "(from launchSettings profile)";
_agentLog("Program.cs:after Build", "configured urls", new { urls = _urls, processId = Environment.ProcessId }, "B");
// #endregion
await app.Services.InitializeAndSeedDatabase();

var supportedCultures = new[] { "en-CA", "fr-CA" };
app.UseRequestLocalization(options =>
{
    // the order of QueryStringRequestCultureProvider and CookieRequestCultureProvider is switched,
    // so the RequestLocalizationMiddleware looks for the cultures from the cookies first, then query string.
    var questStringCultureProvider = options.RequestCultureProviders[0];
    options.RequestCultureProviders.RemoveAt(0);
    options.RequestCultureProviders.Insert(1, questStringCultureProvider);
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

app.UseExceptionHandler(c => c.Run(async context =>
{
    var exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>();
    if (exceptionHandler?.Error == null)
        return;

    var responseBody = new SucceededOrNotResponse(false, exceptionHandler.Error.ErrorObject());
    switch (exceptionHandler.Error)
    {
        case ValidationFailureException exception:
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            responseBody = new SucceededOrNotResponse(false, exception.ErrorObjects());
            break;
    }
    await context.Response.WriteAsJsonAsync(responseBody);
}));

app.UseStaticFiles();
app.UseRouting();
app.UseCors(corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(config => { config.Endpoints.RoutePrefix = "api"; });
app.UseSwaggerGen();

// SPA fallback - serve Vue app for any non-API route
app.MapFallbackToFile("vue/index.html");

// #region agent log
try
{
    app.Run();
}
catch (Exception ex)
{
    _agentLog("Program.cs:Run", "bind_failed", new { message = ex.Message, processId = Environment.ProcessId }, "D");
    throw;
}
// #endregion
