using OrthoFlowAI.Api;
using Serilog;

// Standalone API host. The same setup is also used by the Web app (ApiHost), which
// hosts the API in-process so the whole product can be published as one site.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OrthoFlowAI API host");

    var builder = WebApplication.CreateBuilder(args);
    builder.AddOrthoFlowApi();

    var app = builder.Build();
    await app.InitializeDatabaseAsync();
    app.UseOrthoFlowApi();
    app.UseStaticFiles();
    app.MapOrthoFlowApi();

    Log.Information("OrthoFlowAI API started");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
