using MudBlazor.Services;
using OrthoFlowAI.Api;
using OrthoFlowAI.Web.Components;
using OrthoFlowAI.Web.Services;
using Serilog;

// One site: the Blazor UI and the REST API run in the same app, so the product can be
// published to hosting that offers a single application. The API setup is shared with
// the standalone API project (see ApiHost).
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // API: database, Identity, JWT, Hangfire, controllers, SignalR ...
    builder.AddOrthoFlowApi();

    // UI
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices();

    // The UI calls the API over HTTP with the user's JWT. With both in one site this is the
    // site's own address; ApiSettings:BaseUrl must be set to the public URL when deployed.
    builder.Services.AddHttpClient("OrthoFlowAI.Api", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5154/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    builder.Services.AddScoped<IApiService, ApiService>();
    builder.Services.AddScoped<PatientActions>();
    builder.Services.AddScoped<OrthoFlowAI.Web.Services.IAuthService, AuthService>();
    builder.Services.AddScoped<AuthStateProvider>();
    builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(
        sp => sp.GetRequiredService<AuthStateProvider>());
    builder.Services.AddCascadingAuthenticationState();

    var app = builder.Build();
    await app.InitializeDatabaseAsync();

    if (!app.Environment.IsDevelopment())
    {
        app.UseWhen(ctx => !ApiHost.IsApiPath(ctx.Request.Path),
            ui => ui.UseExceptionHandler("/Error", createScopeForErrors: true));
        app.UseHsts();
    }

    // Blazor's "not found" page only for UI paths; API 404s stay plain.
    app.UseWhen(ctx => !ApiHost.IsApiPath(ctx.Request.Path),
        ui => ui.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseOrthoFlowApi(hostedInWeb: true);
    app.UseAntiforgery();

    app.MapOrthoFlowApi();

    // The login (JWT) lives in the browser's sessionStorage, which the server cannot see on
    // the first HTTP request, so endpoint-level [Authorize] always redirected to /login.
    // Authorization is enforced by AuthorizeRouteView once the page is interactive, and the
    // API enforces it on all data.
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
