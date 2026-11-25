using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Data;
using OrthoFlowAI.Infrastructure.Identity;
using OrthoFlowAI.Infrastructure.Redis;
using OrthoFlowAI.Infrastructure.Repositories;
using OrthoFlowAI.Infrastructure.Services;
using OrthoFlowAI.Infrastructure.Settings;
using SendGrid;
using StackExchange.Redis;
using Twilio;
using OrthoFlowAI.Infrastructure.WhatsApp;

namespace OrthoFlowAI.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

        services.AddIdentity<ApplicationIdentityUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
        services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.Configure<TwilioSettings>(configuration.GetSection(TwilioSettings.SectionName));
        services.Configure<PushNotificationSettings>(configuration.GetSection(PushNotificationSettings.SectionName));
        services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));

        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

        // JWT bearer authentication is configured once, in the API's Program.cs
        // (registering it here too threw "Scheme already exists: Bearer" at startup).
        services.AddAuthorization();

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ReconnectRetryPolicy = new ExponentialRetry(5000);
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IRedisCacheService, RedisCacheService>();

        services.AddHttpContextAccessor();

        // Hangfire Configuration
        var hangfireConnectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddHangfire(config =>
            config.UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                SchemaName = "hangfire"
            })
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseFilter(new AutomaticRetryAttribute { Attempts = 3 }));

        services.AddHangfireServer(options =>
        {
            options.ServerName = "OrthoFlowAI";
            options.WorkerCount = Environment.ProcessorCount;
            options.Queues = new[] { "default", "reminders", "notifications", "reports", "maintenance" };
            options.ServerTimeout = TimeSpan.FromMinutes(30);
            options.ShutdownTimeout = TimeSpan.FromMinutes(5);
        });

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ApplicationUserManager>();
        services.AddScoped<ApplicationRoleManager>();

        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        // Notification Channels
        // SendGridClient throws on an empty key, which crashed startup whenever email
        // wasn't configured (e.g. every dev machine). Use a placeholder; the email
        // channel reports itself unavailable while SendGrid:ApiKey is empty.
        services.AddSingleton<ISendGridClient>(sp =>
        {
            var key = configuration[SendGridSettings.SectionName + ":ApiKey"];
            return new SendGridClient(string.IsNullOrWhiteSpace(key) ? "not-configured" : key);
        });
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, TwilioNotificationChannel>();
        services.AddWhatsApp(configuration);
        services.AddScoped<INotificationChannel, PushNotificationChannel>();
        services.AddScoped<INotificationChannelFactory, NotificationChannelFactory>();

        // Initialize Twilio
        var twilioSettings = new TwilioSettings();
        configuration.GetSection(TwilioSettings.SectionName).Bind(twilioSettings);
        if (!string.IsNullOrEmpty(twilioSettings.AccountSid) && !string.IsNullOrEmpty(twilioSettings.AuthToken))
        {
            TwilioClient.Init(twilioSettings.AccountSid, twilioSettings.AuthToken);
        }

        // Register Reminder Service
        services.AddScoped<IReminderService, ReminderService>();

        services.AddHttpClient<IAIService, AIService>();

        return services;
    }
}
