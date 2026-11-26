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
using OrthoFlowAI.Api.Security;
using OrthoFlowAI.Api.Storage;
using OrthoFlowAI.Application;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Infrastructure;
using OrthoFlowAI.Infrastructure.Data;
using OrthoFlowAI.Infrastructure.Settings;
using Serilog;

namespace OrthoFlowAI.Api;

/// <summary>
/// Everything the API needs, as reusable steps. The API can run on its own (Program.cs)
/// or inside the Blazor Web app, so a single site can be published to hosting that only
/// offers one application.
/// </summary>
public static class ApiHost
{
    /// <summary>Paths served by the API (as opposed to Blazor pages).</summary>
    public static bool IsApiPath(PathString path) =>
        path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs")
        || path.StartsWithSegments("/health") || path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/hangfire");

    public static WebApplicationBuilder AddOrthoFlowApi(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Host.UseSerilog((context, logger) =>
            logger.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

        // Refuse to start with missing or publicly-known secrets.
        StartupSecurityValidator.ValidateOrThrow(configuration, builder.Environment);

        builder.Services.AddInfrastructure(configuration);
        builder.Services.AddSingleton<PrivateFileStore>();
        builder.Services.AddSingleton<SignedFileUrlService>();
        builder.Services.AddApplication();

        builder.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        builder.Services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
        builder.Services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));

        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
                        context.Response.Headers["Token-Expired"] = "true";
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                        context.Token = accessToken;
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
                policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
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
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
                    ? ra.TotalSeconds.ToString() : "some time";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Rate limit exceeded. Please try again later.",
                    errors = new[] { $"Retry after {retryAfter} seconds" }
                }, cancellationToken);
            };
        });

        builder.Services.AddHealthChecks();

        // Controllers live in this assembly; add it explicitly so they are found when
        // the API is hosted inside the Web app.
        builder.Services.AddControllers(o => o.Filters.Add<PatientAccessFilter>())
            .AddApplicationPart(typeof(ApiHost).Assembly)
            .AddJsonOptions(o =>
            {
                // EF entities returned directly have cycles (Patient -> Doctor -> Patients ...).
                o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                // Trim entities (no nested lists, clinic record or secrets).
                o.JsonSerializerOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
                {
                    Modifiers = { Serialization.EntityJsonModifiers.Apply }
                };
            });
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

        return builder;
    }

    /// <summary>
    /// Applies migrations and seeding. Must run before the pipeline: Hangfire needs the
    /// database to exist.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        Log.Information("Applying database migrations and seeding data");
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Identity.ApplicationIdentityUser>>();
        var roleManager = services.GetRequiredService<Infrastructure.Identity.ApplicationRoleManager>();
        await roleManager.SeedRolesAsync();
        var seedOptions = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
        await SeedData.SeedAsync(dbContext, userManager, roleManager, seedOptions);
        if (seedOptions.DemoData)
        {
            if (!app.Environment.IsDevelopment())
                Log.Warning("DEMO DATA is enabled outside Development: demo accounts with well-known passwords exist. Disable Seed:DemoData for real clinics.");
            // Richer presentation data (more patients, instalments, a portal login, a 2nd clinic).
            await DemoDataExtras.SeedAsync(dbContext, userManager, roleManager);
        }
    }

    /// <summary>
    /// API middleware. When hosted inside the Web app, JSON error handling applies only
    /// to API paths so Blazor pages keep their own error page.
    /// </summary>
    public static WebApplication UseOrthoFlowApi(this WebApplication app, bool hostedInWeb = false)
    {
        if (hostedInWeb)
            app.UseWhen(ctx => IsApiPath(ctx.Request.Path), api => api.UseMiddleware<ExceptionHandlingMiddleware>());
        else
            app.UseMiddleware<ExceptionHandlingMiddleware>();

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

        if (!hostedInWeb) app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseRateLimiter();
        app.UseAuthentication();
        // Tenant + audit need the authenticated user, so they run after UseAuthentication.
        app.UseMiddleware<TenantMiddleware>();
        app.UseMiddleware<AuditMiddleware>();
        app.UseAuthorization();

        // Swagger exposes the full API surface; only on when explicitly enabled.
        if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrthoFlowAI API V1");
                options.RoutePrefix = "swagger";
                options.DisplayRequestDuration();
                options.EnableFilter();
            });
        }

        // Uploaded patient files are never served statically (only via authorized /
        // signed-URL endpoints).
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/uploads"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next();
        });

        return app;
    }

    public static WebApplication MapOrthoFlowApi(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");

        var hangfireSettings = new HangfireSettings();
        app.Configuration.GetSection(HangfireSettings.SectionName).Bind(hangfireSettings);
        if (hangfireSettings.EnableDashboard)
        {
            app.UseHangfireDashboard(hangfireSettings.DashboardPath, new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "OrthoFlowAI Background Jobs",
                StatsPollingInterval = 30000
            });
        }
        // Resolving the manager initialises Hangfire storage before jobs are scheduled.
        app.Services.GetRequiredService<IRecurringJobManager>();
        SetupRecurringJobs();

        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.ToString(),
                        description = e.Value.Description
                    }),
                    totalDuration = report.TotalDuration.ToString()
                });
            }
        });
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), totalDuration = report.TotalDuration.ToString() });
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

        return app;
    }

    private static void SetupRecurringJobs()
    {
        RecurringJob.AddOrUpdate<IReminderService>("appointment-reminders",
            s => s.SendAppointmentRemindersAsync(), "*/15 8-18 * * 1-5", TimeZoneInfo.Local);
        RecurringJob.AddOrUpdate<IReminderService>("missed-appointments",
            s => s.SendMissedAppointmentNotificationsAsync(), "0 * * * *", TimeZoneInfo.Local);
        // Daily at 10 AM clinic time: instalment "due soon" / "overdue" WhatsApp reminders.
        RecurringJob.AddOrUpdate<IReminderService>("payment-reminders",
            s => s.SendPaymentDueRemindersAsync(), "0 10 * * *", ClinicTimeZone());
        RecurringJob.AddOrUpdate<IReminderService>("treatment-reminders",
            s => s.SendTreatmentRemindersAsync(), "0 8 * * 1", TimeZoneInfo.Local);
        RecurringJob.AddOrUpdate<IReminderService>("monthly-reports",
            s => s.SendMonthlyReportNotificationsAsync(), "0 8 1 * *", TimeZoneInfo.Local);
        RecurringJob.AddOrUpdate<IReminderService>("birthday-greetings",
            s => s.SendBirthdayGreetingsAsync(), "0 8 * * *", TimeZoneInfo.Local);
        // The old "all-reminders" job (every 30 min) would have messaged patients 48x a day.
        RecurringJob.RemoveIfExists("all-reminders");
    }

    private static TimeZoneInfo ClinicTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Local; }
    }

    private sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("SuperAdmin");
        }
    }
}
