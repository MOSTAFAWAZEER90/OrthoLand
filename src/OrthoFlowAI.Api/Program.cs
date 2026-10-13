using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrthoFlowAI.Api.Hubs;
using OrthoFlowAI.Api.Middleware;
using OrthoFlowAI.Application;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Infrastructure;
using OrthoFlowAI.Infrastructure.Data;
using OrthoFlowAI.Infrastructure.Settings;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting OrthoFlowAI API host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext());

    var configuration = builder.Configuration;

    builder.Services.AddInfrastructure(configuration);
    builder.Services.AddApplication();

    builder.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
    builder.Services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));

    var jwtSettings = new JwtSettings();
    configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
            policy.WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        });

        options.AddSlidingWindowLimiter("sliding", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.SegmentsPerWindow = 6;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
        });

        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
                ? ra.TotalSeconds.ToString() : "some time";
            var response = new
            {
                success = false,
                message = "Rate limit exceeded. Please try again later.",
                errors = new[] { $"Retry after {retryAfter} seconds" }
            };
            await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        };
    });

    builder.Services.AddHealthChecks();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "OrthoFlowAI API",
            Version = "v1",
            Description = "Orthodontic Practice Management API with AI-powered features"
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT token"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
    });

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<AuditMiddleware>();
    app.UseMiddleware<TenantMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000ms}";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            diagnosticContext.Set("TenantId", httpContext.Items["ClinicId"]?.ToString() ?? "none");
        };
    });

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrthoFlowAI API V1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
        options.EnableFilter();
    });

    app.UseStaticFiles();
    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");

    // Hangfire Dashboard
    var hangfireSettings = new HangfireSettings();
    configuration.GetSection(HangfireSettings.SectionName).Bind(hangfireSettings);
    
    if (hangfireSettings.EnableDashboard)
    {
        app.UseHangfireDashboard(hangfireSettings.DashboardPath, new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
            DashboardTitle = "OrthoFlowAI Background Jobs",
            StatsPollingInterval = 30000
        });

        // Schedule recurring jobs
        SetupRecurringJobs(app.Services);
    }

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.ToString(),
                description = e.Value.Description
            });
            var result = new
            {
                status = report.Status.ToString(),
                checks = entries,
                totalDuration = report.TotalDuration.ToString()
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString()
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = async (context, _) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
    });

    Log.Information("Applying database migrations and seeding data");
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<OrthoFlowAI.Infrastructure.Identity.ApplicationIdentityUser>>();
        var roleManager = services.GetRequiredService<OrthoFlowAI.Infrastructure.Identity.ApplicationRoleManager>();
        await roleManager.SeedRolesAsync();
        await SeedData.SeedAsync(dbContext, userManager, roleManager);
    }

    Log.Information("OrthoFlowAI API started successfully on {Urls}", app.Urls);
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

static void SetupRecurringJobs(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

    // Run every 15 minutes during business hours
    RecurringJob.AddOrUpdate<IReminderService>(
        "appointment-reminders",
        s => s.SendAppointmentRemindersAsync(),
        "*/15 8-18 * * 1-5", // Every 15 min, 8am-6pm, Mon-Fri
        TimeZoneInfo.Local);

    // Run every hour for missed appointments
    RecurringJob.AddOrUpdate<IReminderService>(
        "missed-appointments",
        s => s.SendMissedAppointmentNotificationsAsync(),
        "0 * * * *", // Every hour
        TimeZoneInfo.Local);

    // Run daily at 9 AM for payment reminders
    RecurringJob.AddOrUpdate<IReminderService>(
        "payment-reminders",
        s => s.SendPaymentDueRemindersAsync(),
        "0 9 * * *", // Daily at 9 AM
        TimeZoneInfo.Local);

    // Run weekly on Monday at 8 AM for treatment reminders
    RecurringJob.AddOrUpdate<IReminderService>(
        "treatment-reminders",
        s => s.SendTreatmentRemindersAsync(),
        "0 8 * * 1", // Monday 8 AM
        TimeZoneInfo.Local);

    // Run monthly on 1st at 8 AM for monthly reports
    RecurringJob.AddOrUpdate<IReminderService>(
        "monthly-reports",
        s => s.SendMonthlyReportNotificationsAsync(),
        "0 8 1 * *", // 1st of month at 8 AM
        TimeZoneInfo.Local);

    // Run daily at 8 AM for birthday greetings
    RecurringJob.AddOrUpdate<IReminderService>(
        "birthday-greetings",
        s => s.SendBirthdayGreetingsAsync(),
        "0 8 * * *", // Daily at 8 AM
        TimeZoneInfo.Local);

    // Process all reminders every 30 minutes
    RecurringJob.AddOrUpdate<IReminderService>(
        "all-reminders",
        s => s.ProcessAllRemindersAsync(),
        "*/30 * * * *", // Every 30 minutes
        TimeZoneInfo.Local);
}

class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("SuperAdmin");
    }
}
