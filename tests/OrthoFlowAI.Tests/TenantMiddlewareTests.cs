using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrthoFlowAI.Api.Middleware;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Tests;

public class TenantMiddlewareTests
{
    private static (HttpContext ctx, IServiceScope scope) CreateRequest(ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("mw-" + Guid.NewGuid()));
        var scope = services.BuildServiceProvider().CreateScope();

        var ctx = new DefaultHttpContext { User = user, RequestServices = scope.ServiceProvider };
        return (ctx, scope);
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public async Task Sets_tenant_on_the_request_scoped_DbContext()
    {
        var (ctx, scope) = CreateRequest(Authenticated(new Claim("clinic_id", "clinic-42")));
        using var _ = scope;

        var middleware = new TenantMiddleware(_ => Task.CompletedTask, NullLogger<TenantMiddleware>.Instance);
        await middleware.InvokeAsync(ctx);

        // The same instance controllers receive must carry the tenant.
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("clinic-42", db.CurrentClinicId);
        Assert.Equal("clinic-42", ctx.Items["ClinicId"]);
    }

    [Fact]
    public async Task Anonymous_request_gets_no_tenant()
    {
        var (ctx, scope) = CreateRequest(new ClaimsPrincipal(new ClaimsIdentity()));
        using var _ = scope;

        var middleware = new TenantMiddleware(_ => Task.CompletedTask, NullLogger<TenantMiddleware>.Instance);
        await middleware.InvokeAsync(ctx);

        Assert.False(ctx.Items.ContainsKey("ClinicId"));
        Assert.Null(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CurrentClinicId);
    }
}
